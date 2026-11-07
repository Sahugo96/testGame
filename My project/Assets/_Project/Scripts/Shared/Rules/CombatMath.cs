using System;
using Game.Shared.Core;

namespace Game.Shared.Rules
{
    /// <summary>Damage and heal formulas (03, 7.12). Pure functions; the server is the only caller that rolls.</summary>
    public static class CombatMath
    {
        /// <summary>
        /// Multi-tier crit (question 25): the integer part of the chance gives guaranteed tiers,
        /// the fractional part is the chance of one more tier; multiplier = CritSize × tier.
        /// </summary>
        public static CritRoll RollCrit(float critChance, float critSize, float critChanceCap, IRandom random)
        {
            float chance = Math.Max(0f, Math.Min(critChance, critChanceCap));
            int tier = (int)Math.Floor(chance);
            float fraction = chance - tier;
            if (fraction > 0f && random.NextFloat() < fraction)
            {
                tier++;
            }
            return new CritRoll
            {
                Tier = tier,
                Multiplier = tier > 0 ? critSize * tier : 1f,
            };
        }

        /// <summary>
        /// Damage order (03, 7.12): base × efficiency × (1 + bonus) → crit → defense → round, minimum MinDamage.
        /// Shields are not implemented yet (status effects are module 8).
        /// </summary>
        public static int CalculateDamage(float baseAmount, float efficiency, float damageBonus, CritRoll crit,
            float targetDefense, float defenseCap, int minDamage)
        {
            float defense = Math.Max(0f, Math.Min(targetDefense, defenseCap));
            float raw = baseAmount * efficiency * (1f + damageBonus) * crit.Multiplier;
            float afterDefense = raw * (1f - defense);
            int rounded = (int)Math.Round(afterDefense, MidpointRounding.AwayFromZero);
            return Math.Max(minDamage, rounded);
        }

        /// <summary>Heal: same order without defense and shields (question 46).</summary>
        public static int CalculateHeal(float baseAmount, float efficiency, CritRoll crit)
        {
            return (int)Math.Round(baseAmount * efficiency * crit.Multiplier, MidpointRounding.AwayFromZero);
        }
    }
}
