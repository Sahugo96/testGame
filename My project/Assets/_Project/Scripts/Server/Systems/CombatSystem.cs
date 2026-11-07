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
        private readonly DuelSystem _duels;

        /// <summary>Called when an entity dies: (victim, killer). Casts are cancelled, rewards are given.</summary>
        public event Action<EntityId, EntityId> Died;

        /// <summary>(source, target) after damage or healing was applied: loot participation (06, 7.8).</summary>
        public event Action<EntityId, EntityId> Damaged;
        public event Action<EntityId, EntityId> Healed;

        public CombatSystem(ServerContext ctx, ResourceSystem resources, DuelSystem duels)
        {
            _ctx = ctx;
            _resources = resources;
            _duels = duels;
        }

        /// <summary>
        /// Hostility rules (module 6 hook): factions, plus active duel opponents. Accepted duelists are closed to outsiders:
        /// nobody else can attack them and they cannot attack anyone else's targets among players.
        /// </summary>
        public bool AreHostile(EntityId a, EntityId b)
        {
            ServerWorld world = _ctx.World;
            if (!world.Identities.Has(a) || !world.Identities.Has(b))
            {
                return false;
            }
            if (_duels.AreOpponents(a, b))
            {
                return true;
            }
            return _ctx.Content.AreHostile(world.Identities.Get(a).Faction, world.Identities.Get(b).Faction);
        }

        /// <summary>Helpful effects (heals): not on enemies, and nobody outside a duel can help a duelist (or vice versa).</summary>
        public bool CanAssist(EntityId a, EntityId b)
        {
            if (a == b)
            {
                return true;
            }
            return !AreHostile(a, b) && !_duels.IsLockedIn(a) && !_duels.IsLockedIn(b);
        }

        public CombatContext ContextFor(EntityId source, EntityId target) =>
            new CombatContext(source, target, source != target && _ctx.Players.ContainsKey(source) && _ctx.Players.ContainsKey(target));

        public int DealDamage(EntityId source, EntityId target, string skillId, float baseAmount, float efficiency)
        {
            ServerWorld world = _ctx.World;
            if (!world.IsAlive(target))
            {
                return 0;
            }
            GameConfig cfg = _ctx.Content.Config;
            CombatContext context = ContextFor(source, target);
            if (context.IsPvp)
            {
                baseAmount *= cfg.PvpDamageMultiplier;
            }
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
            Damaged?.Invoke(source, target);
            if (vitals.Health <= 0f)
            {
                if (_duels.AreOpponents(source, target))
                {
                    // A duel is not to the death (question 70): the loser keeps a little health.
                    vitals.Health = cfg.DuelLoserHealth;
                    _duels.DefeatByOpponent(source, target);
                }
                else
                {
                    Kill(target, source);
                }
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
            Healed?.Invoke(source, target);
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
            _duels.OnDied(target);
            Died?.Invoke(target, killer);
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
