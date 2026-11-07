using System;
using System.Collections.Generic;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using Game.Shared.Rules;

namespace Game.Server.Systems
{
    /// <summary>
    /// Status effects (03, 7.12; module 8): applied by skills, potions and other sources, stacked by the definition's rule,
    /// ticking DoT/HoT through CombatSystem (so defense, crits and shields apply), shields absorbing damage after defense,
    /// control (stun, root, silence), immunity tags, exclusive groups (buff potions replace each other), dispel.
    /// The server keeps the status with time left and source; clients only show it. All statuses are removed on death
    /// (question 74). Stat modifiers are read by StatSystem (SourceId = the status instance).
    /// </summary>
    public sealed class StatusSystem
    {
        private const string DamageImmunityTag = "Damage";

        private readonly ServerContext _ctx;
        private readonly Dictionary<EntityId, List<StatusData>> _statuses = new Dictionary<EntityId, List<StatusData>>();
        private readonly List<EntityId> _entities = new List<EntityId>();
        private readonly List<(EntityId target, StatusData status)> _ticks = new List<(EntityId, StatusData)>();
        private CombatSystem _combat;
        private int _nextKey = 1;

        public StatusSystem(ServerContext ctx) => _ctx = ctx;

        /// <summary>Stats of the entity depend on its statuses: recalculate (players: StatSystem, mobs: base + modifiers).</summary>
        public event Action<EntityId> Changed;

        /// <summary>A stun or silence landed: the target's cast is interrupted.</summary>
        public event Action<EntityId> Controlled;

        /// <summary>DoT/HoT ticks go through combat; set once after both systems exist.</summary>
        public void BindCombat(CombatSystem combat) => _combat = combat;

        public bool Apply(EntityId source, EntityId target, string statusId, float power)
        {
            if (statusId == null || !_ctx.Content.StatusEffects.TryGetValue(statusId, out StatusEffectDefinition definition)
                || !_ctx.World.IsAlive(target) || IsImmune(target, definition))
            {
                return false;
            }
            List<StatusData> list = ListOf(target);
            if (!string.IsNullOrEmpty(definition.ExclusiveGroup))
            {
                list.RemoveAll(s => s.StatusId != statusId && Definition(s).ExclusiveGroup == definition.ExclusiveGroup);
            }
            int index = list.FindIndex(s => s.StatusId == statusId);
            if (index >= 0)
            {
                StatusData existing = list[index];
                switch (definition.Stacking)
                {
                    case StatusStacking.AddStacks:
                        existing.Stacks = Math.Min(definition.MaxStacks, existing.Stacks + 1);
                        existing.Power = Math.Max(existing.Power, power);
                        break;
                    case StatusStacking.ReplaceStronger:
                        if (power < existing.Power)
                        {
                            return false;
                        }
                        existing.Power = power;
                        existing.SourceId = source;
                        break;
                    default:
                        existing.Power = power;
                        existing.SourceId = source;
                        break;
                }
                existing.Remaining = definition.Duration;
                existing.ShieldLeft = definition.ShieldAmount * existing.Power;
                list[index] = existing;
            }
            else
            {
                list.Add(new StatusData
                {
                    StatusId = statusId,
                    SourceId = source,
                    Stacks = 1,
                    Remaining = definition.Duration,
                    TickTimer = definition.TickInterval,
                    Power = power,
                    ShieldLeft = definition.ShieldAmount * power,
                    SourceKey = $"status:{_nextKey++}",
                });
            }
            Broadcast(source, target, statusId, CombatEventKind.StatusApplied, Find(target, statusId).Stacks);
            if ((definition.Control & (ControlType.Stun | ControlType.Silence)) != 0)
            {
                Controlled?.Invoke(target);
            }
            Changed?.Invoke(target);
            return true;
        }

        public bool Remove(EntityId target, string statusId)
        {
            if (!_statuses.TryGetValue(target, out List<StatusData> list))
            {
                return false;
            }
            int index = list.FindIndex(s => s.StatusId == statusId);
            if (index < 0)
            {
                return false;
            }
            list.RemoveAt(index);
            Broadcast(EntityId.None, target, statusId, CombatEventKind.StatusRemoved, 0);
            Changed?.Invoke(target);
            return true;
        }

        /// <summary>Removes up to count dispellable statuses of the kind and dispel type (empty = any type). Returns how many.</summary>
        public int Dispel(EntityId target, StatusKind kind, string dispelType, int count)
        {
            if (!_statuses.TryGetValue(target, out List<StatusData> list))
            {
                return 0;
            }
            var removed = new List<string>();
            foreach (StatusData status in list)
            {
                StatusEffectDefinition definition = Definition(status);
                if (removed.Count < count && definition.Dispellable && definition.Kind == kind
                    && (string.IsNullOrEmpty(dispelType) || definition.DispelType == dispelType))
                {
                    removed.Add(status.StatusId);
                }
            }
            foreach (string id in removed)
            {
                Remove(target, id);
            }
            return removed.Count;
        }

        /// <summary>Death clears all statuses (question 74).</summary>
        public void OnDied(EntityId target) => Clear(target);

        public void OnEntityRemoved(EntityId target) => _statuses.Remove(target);

        public bool Has(EntityId target, string statusId) => FindIndex(target, statusId) >= 0;

        public StatusData Find(EntityId target, string statusId)
        {
            int index = FindIndex(target, statusId);
            return index >= 0 ? _statuses[target][index] : default;
        }

        public ControlType Controls(EntityId target)
        {
            ControlType result = ControlType.None;
            if (_statuses.TryGetValue(target, out List<StatusData> list))
            {
                foreach (StatusData status in list)
                {
                    result |= Definition(status).Control;
                }
            }
            return result;
        }

        public bool IsImmuneToDamage(EntityId target) => HasImmunityTag(target, DamageImmunityTag);

        /// <summary>Shields absorb damage after defense and rounding (03, 7.12); emptied shields are removed. Returns what got through.</summary>
        public int AbsorbDamage(EntityId target, int amount)
        {
            if (amount <= 0 || !_statuses.TryGetValue(target, out List<StatusData> list))
            {
                return amount;
            }
            List<string> emptied = null;
            for (int i = 0; i < list.Count && amount > 0; i++)
            {
                StatusData status = list[i];
                if (Definition(status).Category != StatusCategory.Shield || status.ShieldLeft <= 0f)
                {
                    continue;
                }
                int absorbed = (int)Math.Min(amount, Math.Floor(status.ShieldLeft));
                status.ShieldLeft -= absorbed;
                amount -= absorbed;
                list[i] = status;
                if (status.ShieldLeft < 1f)
                {
                    (emptied ??= new List<string>()).Add(status.StatusId);
                }
            }
            if (emptied != null)
            {
                foreach (string id in emptied)
                {
                    Remove(target, id);
                }
            }
            return amount;
        }

        /// <summary>Stat modifiers of all statuses on the entity, multiplied by stacks (Add and PercentAdd), SourceId = status instance.</summary>
        public IEnumerable<StatModifier> ModifiersOf(EntityId target)
        {
            if (!_statuses.TryGetValue(target, out List<StatusData> list))
            {
                yield break;
            }
            foreach (StatusData status in list)
            {
                foreach (StatModifier template in Definition(status).Modifiers)
                {
                    float value = template.Op == ModifierOp.Multiply ? (float)Math.Pow(template.Value, status.Stacks) : template.Value * status.Stacks;
                    yield return new StatModifier(template.Stat, template.Op, value, status.SourceKey);
                }
            }
        }

        public StatusSnapshot[] Snapshot(EntityId target)
        {
            if (!_statuses.TryGetValue(target, out List<StatusData> list) || list.Count == 0)
            {
                return Array.Empty<StatusSnapshot>();
            }
            var result = new StatusSnapshot[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                result[i] = new StatusSnapshot
                {
                    StatusId = list[i].StatusId,
                    Stacks = list[i].Stacks,
                    Remaining = Definition(list[i]).Duration > 0f ? Math.Max(0f, list[i].Remaining) : 0f,
                };
            }
            return result;
        }

        /// <summary>Time runs out and DoT/HoT tick. Ticks are collected first: a tick may kill and clear statuses.</summary>
        public void Update(float dt)
        {
            _entities.Clear();
            _entities.AddRange(_statuses.Keys);
            _ticks.Clear();
            foreach (EntityId target in _entities)
            {
                List<StatusData> list = _statuses[target];
                bool changed = false;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    StatusData status = list[i];
                    StatusEffectDefinition definition = Definition(status);
                    if (definition.TickInterval > 0f)
                    {
                        status.TickTimer -= dt;
                        if (status.TickTimer <= 0f)
                        {
                            status.TickTimer += definition.TickInterval;
                            _ticks.Add((target, status));
                        }
                    }
                    if (definition.Duration > 0f)
                    {
                        status.Remaining -= dt;
                        if (status.Remaining <= 0f)
                        {
                            list.RemoveAt(i);
                            Broadcast(EntityId.None, target, status.StatusId, CombatEventKind.StatusRemoved, 0);
                            changed = true;
                            continue;
                        }
                    }
                    list[i] = status;
                }
                if (changed)
                {
                    Changed?.Invoke(target);
                }
            }
            foreach ((EntityId target, StatusData status) in _ticks)
            {
                Tick(target, status);
            }
        }

        private void Tick(EntityId target, StatusData status)
        {
            if (_combat == null || !_ctx.World.IsAlive(target))
            {
                return;
            }
            StatusEffectDefinition definition = Definition(status);
            float amount = definition.TickAmount * status.Stacks;
            if (definition.Category == StatusCategory.DoT)
            {
                _combat.DealDamage(status.SourceId, target, status.StatusId, amount, status.Power);
            }
            else if (definition.Category == StatusCategory.HoT)
            {
                _combat.Heal(status.SourceId, target, status.StatusId, amount, status.Power);
            }
        }

        private bool IsImmune(EntityId target, StatusEffectDefinition incoming) =>
            HasImmunityTag(target, incoming.Id) || HasImmunityTag(target, incoming.Category.ToString())
            || (!string.IsNullOrEmpty(incoming.DispelType) && HasImmunityTag(target, incoming.DispelType));

        private bool HasImmunityTag(EntityId target, string tag)
        {
            if (!_statuses.TryGetValue(target, out List<StatusData> list))
            {
                return false;
            }
            foreach (StatusData status in list)
            {
                if (Definition(status).ImmunityTags.Contains(tag))
                {
                    return true;
                }
            }
            return false;
        }

        private void Clear(EntityId target)
        {
            if (_statuses.TryGetValue(target, out List<StatusData> list) && list.Count > 0)
            {
                list.Clear();
                Changed?.Invoke(target);
            }
        }

        private int FindIndex(EntityId target, string statusId) =>
            _statuses.TryGetValue(target, out List<StatusData> list) ? list.FindIndex(s => s.StatusId == statusId) : -1;

        private List<StatusData> ListOf(EntityId target)
        {
            if (!_statuses.TryGetValue(target, out List<StatusData> list))
            {
                list = new List<StatusData>();
                _statuses.Add(target, list);
            }
            return list;
        }

        private StatusEffectDefinition Definition(StatusData status) => _ctx.Content.StatusEffects[status.StatusId];

        private void Broadcast(EntityId source, EntityId target, string statusId, CombatEventKind kind, int stacks) =>
            CombatEvents.Broadcast(_ctx, new CombatEventMessage { SourceId = source, TargetId = target, SkillId = statusId, Kind = kind, Amount = stacks });
    }
}
