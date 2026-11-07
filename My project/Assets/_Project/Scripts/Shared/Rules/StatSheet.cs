using System;
using System.Collections.Generic;
using Game.Shared.Content;

namespace Game.Shared.Rules
{
    /// <summary>
    /// Computes final stats: base → Add → PercentAdd (summed) → Multiply → caps (03, 7.12).
    /// Pure calculation, shared by server and client previews.
    /// </summary>
    public sealed class StatSheet
    {
        private static readonly StatId[] AllStats = (StatId[])Enum.GetValues(typeof(StatId));

        private readonly Dictionary<StatId, float> _base = new Dictionary<StatId, float>();
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        private readonly Dictionary<StatId, float> _caps = new Dictionary<StatId, float>();
        private readonly Dictionary<StatId, float> _final = new Dictionary<StatId, float>();
        private bool _dirty = true;

        public void Clear()
        {
            _base.Clear();
            _modifiers.Clear();
            _caps.Clear();
            _dirty = true;
        }

        public void SetBase(StatId stat, float value)
        {
            _base[stat] = value;
            _dirty = true;
        }

        public void AddBase(StatId stat, float value)
        {
            _base.TryGetValue(stat, out float current);
            _base[stat] = current + value;
            _dirty = true;
        }

        public void SetCap(StatId stat, float cap)
        {
            _caps[stat] = cap;
            _dirty = true;
        }

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            _dirty = true;
        }

        public int RemoveSource(string sourceId)
        {
            int removed = _modifiers.RemoveAll(m => m.SourceId == sourceId);
            if (removed > 0)
            {
                _dirty = true;
            }
            return removed;
        }

        public float Get(StatId stat)
        {
            if (_dirty)
            {
                Recalculate();
            }
            _final.TryGetValue(stat, out float value);
            return value;
        }

        private void Recalculate()
        {
            _final.Clear();
            foreach (StatId stat in AllStats)
            {
                _base.TryGetValue(stat, out float value);
                float percent = 0f;
                float multiplier = 1f;
                foreach (StatModifier modifier in _modifiers)
                {
                    if (modifier.Stat != stat)
                    {
                        continue;
                    }
                    switch (modifier.Op)
                    {
                        case ModifierOp.Add:
                            value += modifier.Value;
                            break;
                        case ModifierOp.PercentAdd:
                            percent += modifier.Value;
                            break;
                        case ModifierOp.Multiply:
                            multiplier *= modifier.Value;
                            break;
                    }
                }
                value = value * (1f + percent) * multiplier;
                if (_caps.TryGetValue(stat, out float cap) && value > cap)
                {
                    value = cap;
                }
                _final[stat] = value;
            }
            _dirty = false;
        }
    }
}
