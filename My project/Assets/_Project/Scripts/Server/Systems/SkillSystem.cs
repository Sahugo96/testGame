using System;
using System.Collections.Generic;
using Game.Server.Effects;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using Game.Shared.Rules;

namespace Game.Server.Systems
{
    /// <summary>
    /// Skill use (03, 7.2): the server resolves the variant from the modifier, checks mastery unlocks, target, range,
    /// cooldowns and costs; casts are checked at start and at completion and paid at completion (rule 4).
    /// Health costs may kill the caster; then effects on others still apply, effects on the caster do not (question 3).
    /// </summary>
    public sealed class SkillSystem
    {
        private readonly ServerContext _ctx;
        private readonly ResourceSystem _resources;
        private readonly CombatSystem _combat;
        private readonly EffectHandlerRegistry _effects;
        private readonly List<string> _cooldownKeys = new List<string>();

        public SkillSystem(ServerContext ctx, ResourceSystem resources, CombatSystem combat, EffectHandlerRegistry effects)
        {
            _ctx = ctx;
            _resources = resources;
            _combat = combat;
            _effects = effects;
            _combat.Died += (victim, killer) => CancelCast(victim);
        }

        /// <summary>Variant actually used for a modifier: missing or not yet unlocked variants fall back to Default.</summary>
        public SkillVariantKind ResolveVariant(PlayerCharacter player, SkillDefinition skill, SkillModifier modifier)
        {
            SkillMasteryRule rule = _ctx.Content.GetMasteryRule(skill.Id);
            float mastery = player.GetMastery(skill.Id);
            if (modifier == SkillModifier.Shift && skill.Empowered != null && (rule == null || mastery >= rule.EmpoweredUnlock))
            {
                return SkillVariantKind.Empowered;
            }
            if (modifier == SkillModifier.Ctrl && skill.Weakened != null && (rule == null || mastery >= rule.WeakenedUnlock))
            {
                return SkillVariantKind.Weakened;
            }
            return SkillVariantKind.Default;
        }

        public float Efficiency(PlayerCharacter player, string skillId) =>
            MasteryMath.Efficiency(player.GetMastery(skillId), _ctx.Content.Config.MasteryMinEfficiency, _ctx.Content.Config.MasteryMaxEfficiency);

        public void TryUse(PlayerCharacter player, UseSkillCommand command)
        {
            EntityId caster = player.EntityId;
            ServerWorld world = _ctx.World;
            if (!world.IsAlive(caster))
            {
                CombatEvents.Reject(player, RejectReasons.Dead);
                return;
            }
            if (command.SkillId == null || !_ctx.Content.Skills.TryGetValue(command.SkillId, out SkillDefinition skill) || !player.KnowsSkill(skill.Id))
            {
                CombatEvents.Reject(player, RejectReasons.SkillUnknown);
                return;
            }
            ref CastData cast = ref world.Casts.Get(caster);
            if (cast.IsCasting)
            {
                CombatEvents.Reject(player, RejectReasons.AlreadyCasting);
                return;
            }
            if ((skill.UsesGlobalCooldown && cast.GlobalCooldownRemaining > 0f) || IsOnCooldown(player, skill.Id))
            {
                CombatEvents.Reject(player, RejectReasons.OnCooldown);
                return;
            }
            SkillVariantKind kind = ResolveVariant(player, skill, command.Modifier);
            SkillVariant variant = skill.GetVariant(kind);
            EntityId target = command.TargetId.IsValid ? command.TargetId : player.TargetId;
            if (!ValidateTarget(player, skill, ref target))
            {
                return;
            }
            if (!CanAfford(caster, variant))
            {
                CombatEvents.Reject(player, RejectReasons.NotEnoughMana);
                return;
            }
            if (skill.TargetType == TargetType.Enemy)
            {
                player.TargetId = target;
                _resources.MarkCombat(caster);
            }
            if (skill.UsesGlobalCooldown)
            {
                cast.GlobalCooldownRemaining = _ctx.Content.Config.GlobalCooldownSeconds;
                cast.GlobalCooldownDuration = cast.GlobalCooldownRemaining;
            }
            if (variant.CastTime <= 0f)
            {
                Complete(player, skill, kind, target);
                return;
            }
            cast.IsCasting = true;
            cast.SkillId = skill.Id;
            cast.Variant = kind;
            cast.TargetId = target;
            cast.Elapsed = 0f;
            cast.Duration = variant.CastTime;
        }

        public void Update(float dt)
        {
            ServerWorld world = _ctx.World;
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                TickCooldowns(player, dt);
                ref CastData cast = ref world.Casts.Get(player.EntityId);
                cast.GlobalCooldownRemaining = Math.Max(0f, cast.GlobalCooldownRemaining - dt);
                if (!cast.IsCasting)
                {
                    continue;
                }
                if (!world.IsAlive(cast.TargetId))
                {
                    CancelCast(player.EntityId);
                    continue;
                }
                cast.Elapsed += dt;
                if (cast.Elapsed >= cast.Duration)
                {
                    SkillDefinition skill = _ctx.Content.Skills[cast.SkillId];
                    SkillVariantKind kind = cast.Variant;
                    EntityId target = cast.TargetId;
                    cast.IsCasting = false;
                    Complete(player, skill, kind, target);
                }
            }
        }

        public void OnMoved(EntityId caster)
        {
            if (!_ctx.World.Casts.Has(caster))
            {
                return;
            }
            CastData cast = _ctx.World.Casts.Get(caster);
            if (cast.IsCasting && _ctx.Content.Skills[cast.SkillId].GetVariant(cast.Variant).IsInterruptible)
            {
                CancelCast(caster);
            }
        }

        public void CancelCast(EntityId caster)
        {
            if (!_ctx.World.Casts.Has(caster))
            {
                return;
            }
            ref CastData cast = ref _ctx.World.Casts.Get(caster);
            cast.IsCasting = false;
            cast.SkillId = null;
        }

        private void Complete(PlayerCharacter player, SkillDefinition skill, SkillVariantKind kind, EntityId target)
        {
            EntityId caster = player.EntityId;
            ServerWorld world = _ctx.World;
            SkillVariant variant = skill.GetVariant(kind);
            if (!ValidateTarget(player, skill, ref target))
            {
                return;
            }
            ref ManaData mana = ref world.Mana.Get(caster);
            ref VitalsData vitals = ref world.Vitals.Get(caster);
            CostTotals totals = CostCalculator.Sum(variant.Costs, mana, vitals);
            if (!CostCalculator.CanPay(totals, mana))
            {
                CombatEvents.Reject(player, RejectReasons.NotEnoughMana);
                return;
            }
            bool casterKilled = CostCalculator.Pay(totals, ref mana, ref vitals);
            if (casterKilled)
            {
                _combat.Kill(caster, caster);
            }

            float efficiency = Efficiency(player, skill.Id);
            foreach (EffectDefinition effect in variant.Effects)
            {
                if (casterKilled && target == caster)
                {
                    continue;
                }
                _effects.Apply(new EffectContext
                {
                    Server = _ctx,
                    SourceId = caster,
                    TargetId = target,
                    SkillId = skill.Id,
                    Effect = effect,
                    Efficiency = efficiency,
                });
            }

            player.SkillCooldowns[skill.Id] = new SkillCooldown { Remaining = variant.Cooldown, Duration = variant.Cooldown };
            GainMastery(player, skill.Id);
        }

        /// <summary>Mastery from uses counts only in combat (recommendation 26) and never exceeds 100%.</summary>
        private void GainMastery(PlayerCharacter player, string skillId)
        {
            SkillMasteryRule rule = _ctx.Content.GetMasteryRule(skillId);
            if (rule == null || rule.MasteryPerUse <= 0f || !_ctx.World.IsAlive(player.EntityId) || !_resources.IsInCombat(player.EntityId))
            {
                return;
            }
            player.Mastery[skillId] = Math.Min(1f, player.GetMastery(skillId) + rule.MasteryPerUse);
        }

        private bool ValidateTarget(PlayerCharacter player, SkillDefinition skill, ref EntityId target)
        {
            EntityId caster = player.EntityId;
            ServerWorld world = _ctx.World;
            switch (skill.TargetType)
            {
                case TargetType.Self:
                    target = caster;
                    return true;
                case TargetType.Ally:
                    if (!target.IsValid || !world.IsAlive(target) || _combat.AreHostile(caster, target))
                    {
                        target = caster;
                        return true;
                    }
                    break;
                default:
                    if (!target.IsValid)
                    {
                        CombatEvents.Reject(player, RejectReasons.NoTarget);
                        return false;
                    }
                    if (!world.IsAlive(target) || !_combat.AreHostile(caster, target))
                    {
                        CombatEvents.Reject(player, RejectReasons.InvalidTarget);
                        return false;
                    }
                    break;
            }
            float distance = Vec3.DistanceXZ(world.Transforms.Get(caster).Position, world.Transforms.Get(target).Position);
            if (distance > skill.Range)
            {
                CombatEvents.Reject(player, RejectReasons.OutOfRange);
                return false;
            }
            return true;
        }

        private bool CanAfford(EntityId caster, SkillVariant variant)
        {
            ManaData mana = _ctx.World.Mana.Get(caster);
            VitalsData vitals = _ctx.World.Vitals.Get(caster);
            return CostCalculator.CanPay(CostCalculator.Sum(variant.Costs, mana, vitals), mana);
        }

        private static bool IsOnCooldown(PlayerCharacter player, string skillId) =>
            player.SkillCooldowns.TryGetValue(skillId, out SkillCooldown cooldown) && cooldown.Remaining > 0f;

        private void TickCooldowns(PlayerCharacter player, float dt)
        {
            _cooldownKeys.Clear();
            _cooldownKeys.AddRange(player.SkillCooldowns.Keys);
            foreach (string key in _cooldownKeys)
            {
                SkillCooldown cooldown = player.SkillCooldowns[key];
                cooldown.Remaining = Math.Max(0f, cooldown.Remaining - dt);
                player.SkillCooldowns[key] = cooldown;
            }
        }
    }
}
