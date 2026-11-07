using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Upgrade table (UpgradeRules, 04 7.14.1): material per attempt, stat growth per level, steps up to MaxLevel.</summary>
    [Serializable]
    public class UpgradeRuleDto
    {
        public string Id;
        public int MaxLevel;
        public string MaterialItemId;
        public int MaterialCount;
        public float StatBonusPerLevel;
        public UpgradeStepDto[] Steps;
    }
}
