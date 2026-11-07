using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.Systems
{
    /// <summary>
    /// Threat (module 8; used by mob AI in module 10): every mob keeps a table "player → threat". Damage to the mob adds
    /// damage × ThreatPerDamage; healing a player who is on a mob's table adds healed × HealThreatFactor to the healer on
    /// that table. The table is cleared when the mob dies and the player is dropped when they leave or die.
    /// </summary>
    public sealed class ThreatSystem
    {
        private readonly ServerContext _ctx;
        private readonly Dictionary<EntityId, Dictionary<EntityId, float>> _tables = new Dictionary<EntityId, Dictionary<EntityId, float>>();

        public ThreatSystem(ServerContext ctx) => _ctx = ctx;

        public void OnDamaged(EntityId source, EntityId target, int amount)
        {
            if (amount <= 0 || !_ctx.World.Mobs.Has(target) || !_ctx.Players.ContainsKey(source))
            {
                return;
            }
            Add(target, source, amount * _ctx.Content.Config.ThreatPerDamage);
        }

        public void OnHealed(EntityId source, EntityId target, int amount)
        {
            if (amount <= 0 || !_ctx.Players.ContainsKey(source))
            {
                return;
            }
            float threat = amount * _ctx.Content.Config.HealThreatFactor;
            foreach (KeyValuePair<EntityId, Dictionary<EntityId, float>> table in _tables)
            {
                if (table.Value.ContainsKey(target))
                {
                    table.Value.TryGetValue(source, out float current);
                    table.Value[source] = current + threat;
                }
            }
        }

        /// <summary>A mob died: its table is cleared; a player died or left: they are dropped from all tables.</summary>
        public void OnDied(EntityId entity)
        {
            _tables.Remove(entity);
            foreach (Dictionary<EntityId, float> table in _tables.Values)
            {
                table.Remove(entity);
            }
        }

        public float ThreatOf(EntityId mob, EntityId player) =>
            _tables.TryGetValue(mob, out Dictionary<EntityId, float> table) && table.TryGetValue(player, out float value) ? value : 0f;

        /// <summary>The player with the most threat on the mob, or None.</summary>
        public EntityId TopThreat(EntityId mob)
        {
            EntityId best = EntityId.None;
            float bestValue = 0f;
            if (_tables.TryGetValue(mob, out Dictionary<EntityId, float> table))
            {
                foreach (KeyValuePair<EntityId, float> pair in table)
                {
                    if (pair.Value > bestValue)
                    {
                        bestValue = pair.Value;
                        best = pair.Key;
                    }
                }
            }
            return best;
        }

        /// <summary>The mob forgets everyone (it leashed home).</summary>
        public void ClearTable(EntityId mob) => _tables.Remove(mob);

        /// <summary>Adds threat directly (e.g. an aggressive mob noticing a player, module 10).</summary>
        public void Add(EntityId mob, EntityId player, float value)
        {
            if (!_tables.TryGetValue(mob, out Dictionary<EntityId, float> table))
            {
                table = new Dictionary<EntityId, float>();
                _tables.Add(mob, table);
            }
            table.TryGetValue(player, out float current);
            table[player] = current + value;
        }
    }
}
