using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>
    /// File: Resources/Content/base/balance_limits.json — ceilings that catch typos in content (question 84). An item's sum of
    /// one stat (its Stats components plus the best roll of its affix pool) may not exceed the stat's cap times the
    /// rarity factor; a status effect's modifier may not exceed the cap times StatusFactor.
    /// </summary>
    [Serializable]
    public class BalanceLimitsDto
    {
        public StatCapDto[] StatCaps;
        public RarityFactorDto[] RarityFactors;
        public float StatusFactor;
    }

    [Serializable]
    public class StatCapDto
    {
        public string Stat;
        /// <summary>ModifierOp the cap applies to (Add, PercentAdd, Multiply).</summary>
        public string Op;
        public float Max;
    }

    [Serializable]
    public class RarityFactorDto
    {
        public string Rarity;
        public float Factor;
    }
}
