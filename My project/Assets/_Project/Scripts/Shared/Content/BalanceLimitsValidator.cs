using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Shared.Content.Dto;

namespace Game.Shared.Content
{
    /// <summary>
    /// Balance ceilings (module 4, question 84; data — base/balance_limits.json). Per item: for each capped stat and
    /// operation, the sum of its Stats components plus the best roll of its affix pool must not exceed Max × the rarity's
    /// factor. Per status effect: each modifier's size must not exceed Max × StatusFactor. Catches typos such as +500 damage.
    /// </summary>
    public static class BalanceLimitsValidator
    {
        public static void Validate(ContentFiles files, List<string> errors)
        {
            BalanceLimitsDto limits = files.BalanceLimits;
            if (limits == null)
            {
                errors.Add("content: base/balance_limits.json is missing");
                return;
            }
            var caps = new Dictionary<string, float>();
            foreach (StatCapDto cap in limits.StatCaps ?? new StatCapDto[0])
            {
                if (cap.Max <= 0f)
                {
                    errors.Add($"balance_limits: cap of {cap.Stat} {cap.Op} must be > 0");
                }
                caps[Key(cap.Stat, cap.Op)] = cap.Max;
            }
            var factors = new Dictionary<string, float>();
            foreach (RarityFactorDto factor in limits.RarityFactors ?? new RarityFactorDto[0])
            {
                factors[factor.Rarity ?? string.Empty] = factor.Factor;
            }
            var pools = new Dictionary<string, AffixPoolDto>();
            foreach (AffixPoolDto pool in files.AffixPools?.Pools ?? new AffixPoolDto[0])
            {
                pools[pool.Id ?? string.Empty] = pool;
            }
            foreach (ItemDto item in files.Items?.Items ?? new ItemDto[0])
            {
                if (!factors.TryGetValue(item.Rarity ?? string.Empty, out float factor))
                {
                    errors.Add($"balance_limits: no factor for rarity '{item.Rarity}' (item '{item.Id}')");
                    continue;
                }
                var sums = new Dictionary<string, float>();
                foreach (ItemComponentDto component in item.Components ?? new ItemComponentDto[0])
                {
                    foreach (StatModifierDto stat in component.Stats ?? new StatModifierDto[0])
                    {
                        Add(sums, Key(stat.Stat, stat.Op), stat.Value);
                    }
                    if (!string.IsNullOrEmpty(component.Pool) && pools.TryGetValue(component.Pool, out AffixPoolDto pool))
                    {
                        foreach (AffixStatDto affix in pool.Stats ?? new AffixStatDto[0])
                        {
                            Add(sums, Key(affix.Stat, affix.Op), affix.Max);
                        }
                    }
                }
                foreach (KeyValuePair<string, float> sum in sums)
                {
                    if (caps.TryGetValue(sum.Key, out float cap) && sum.Value > cap * factor + Epsilon)
                    {
                        errors.Add($"balance_limits: item '{item.Id}' ({item.Rarity}) has {sum.Key} {Format(sum.Value)} > {Format(cap * factor)}");
                    }
                }
            }
            foreach (StatusEffectDto status in files.StatusEffects?.Statuses ?? new StatusEffectDto[0])
            {
                foreach (StatModifierDto modifier in status.Modifiers ?? new StatModifierDto[0])
                {
                    if (caps.TryGetValue(Key(modifier.Stat, modifier.Op), out float cap) && Math.Abs(modifier.Value) > cap * limits.StatusFactor + Epsilon)
                    {
                        errors.Add($"balance_limits: status '{status.Id}' has {Key(modifier.Stat, modifier.Op)} {Format(modifier.Value)} > {Format(cap * limits.StatusFactor)}");
                    }
                }
            }
        }

        private const float Epsilon = 0.0001f;

        private static string Key(string stat, string op) => (stat ?? string.Empty) + " " + (op ?? string.Empty);

        private static void Add(Dictionary<string, float> sums, string key, float value)
        {
            sums.TryGetValue(key, out float current);
            sums[key] = current + value;
        }

        private static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
