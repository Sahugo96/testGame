using System;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;
using Game.Shared.Rules;

namespace Game.Server.Systems
{
    /// <summary>Damage, healing and death (03, 7.12). Crit rolls happen only here, on the server.</summary>
    public sealed class CombatSystem
    {
        private readonly ServerContext _ctx;
        private readonly ResourceSystem _resources;

        /// <summary>Called when an entity dies, so casts can be cancelled.</summary>
        public event Action<EntityId> Died;

        public CombatSystem(ServerContext ctx, ResourceSystem resources)
        {
            _ctx = ctx;
            _resources = resources;
        }

        public bool AreHostile(EntityId a, EntityId b)
        {
            ServerWorld world = _ctx.World;
            if (!world.Identities.Has(a) || !world.Identities.Has(b))
            {
                return false;
            }
            return _ctx.Content.AreHostile(world.Identities.Get(a).Faction, world.Identities.Get(b).Faction);
        }

        public int DealDamage(EntityId source, EntityId target, string skillId, float baseAmount, float efficiency)
        {
            ServerWorld world = _ctx.World;
            if (!world.IsAlive(target))
            {
                return 0;
            }
            GameConfig cfg = _ctx.Content.Config;
            ReadOffense(source, out float damageBonus, out float critChance, out float critSize);
            CritRoll crit = CombatMath.RollCrit(critChance, critSize, cfg.CritChanceCap, _ctx.Random);
            ref VitalsData vitals = ref world.Vitals.Get(target);
            int amount = CombatMath.CalculateDamage(baseAmount, efficiency, damageBonus, crit, vitals.Defense, cfg.DefenseCap, cfg.MinDamage);
            vitals.Health -= amount;
            _resources.MarkCombat(target);
            if (source != target)
            {
                _resources.MarkCombat(source);
            }
            CombatEvents.Broadcast(_ctx, new CombatEventMessage
            {
                SourceId = source,
                TargetId = target,
                SkillId = skillId,
                Kind = CombatEventKind.Damage,
                Amount = amount,
                CritTier = crit.Tier,
            });
            if (vitals.Health <= 0f)
            {
                Kill(target, source);
            }
            return amount;
        }

        public int Heal(EntityId source, EntityId target, string skillId, float baseAmount, float efficiency)
        {
            ServerWorld world = _ctx.World;
            if (!world.IsAlive(target))
            {
                return 0;
            }
            ReadOffense(source, out _, out float critChance, out float critSize);
            CritRoll crit = CombatMath.RollCrit(critChance, critSize, _ctx.Content.Config.CritChanceCap, _ctx.Random);
            int amount = CombatMath.CalculateHeal(baseAmount, efficiency, crit);
            ref VitalsData vitals = ref world.Vitals.Get(target);
            int healed = (int)Math.Min(amount, Math.Ceiling(vitals.MaxHealth - vitals.Health));
            vitals.Health = Math.Min(vitals.MaxHealth, vitals.Health + amount);
            CombatEvents.Broadcast(_ctx, new CombatEventMessage
            {
                SourceId = source,
                TargetId = target,
                SkillId = skillId,
                Kind = CombatEventKind.Heal,
                Amount = healed,
                CritTier = crit.Tier,
            });
            return healed;
        }

        /// <summary>Marks the entity dead and schedules respawn.</summary>
        public void Kill(EntityId target, EntityId killer)
        {
            ServerWorld world = _ctx.World;
            ref VitalsData vitals = ref world.Vitals.Get(target);
            if (vitals.IsDead)
            {
                return;
            }
            vitals.Health = 0f;
            vitals.IsDead = true;
            vitals.RespawnRemaining = world.Mobs.Has(target)
                ? world.Mobs.Get(target).RespawnSeconds
                : _ctx.Content.Config.PlayerRespawnSeconds;
            CombatEvents.Broadcast(_ctx, new CombatEventMessage
            {
                SourceId = killer,
                TargetId = target,
                Kind = CombatEventKind.Death,
            });
            Died?.Invoke(target);
        }

        private void ReadOffense(EntityId source, out float damageBonus, out float critChance, out float critSize)
        {
            PlayerCharacter player = _ctx.GetPlayer(source);
            if (player == null)
            {
                damageBonus = 0f;
                critChance = 0f;
                critSize = 1f;
                return;
            }
            damageBonus = player.Stats.Get(StatId.DamageBonus);
            critChance = player.Stats.Get(StatId.CritChance);
            critSize = player.Stats.Get(StatId.CritSize);
        }
    }
}
