using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>One upgrade step (from level N to N+1): success chance, level lost on failure, chance to destroy the item on failure (04, 7.14.1).</summary>
    [Serializable]
    public class UpgradeStepDto
    {
        public float SuccessChance;
        public int LevelLossOnFail;
        public float BreakChance;
    }
}
