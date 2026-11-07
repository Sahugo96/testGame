using System;

namespace Game.Shared.Rules
{
    /// <summary>Skill mastery (questions 5, 54): mastery 0..1 maps linearly to efficiency Min..Max (data: 30%..150%).</summary>
    public static class MasteryMath
    {
        public static float Efficiency(float mastery, float minEfficiency, float maxEfficiency)
        {
            float clamped = Math.Max(0f, Math.Min(1f, mastery));
            return minEfficiency + (maxEfficiency - minEfficiency) * clamped;
        }

        /// <summary>Upkeep cost of channelled skills falls as efficiency grows (question 54).</summary>
        public static float UpkeepCostMultiplier(float efficiency) => efficiency > 0f ? 1f / efficiency : 1f;
    }
}
