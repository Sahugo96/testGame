namespace Game.Shared.Rules
{
    /// <summary>How the upgrade level strengthens an item (04, 7.14.1): additive and percent bonuses grow by StatBonusPerLevel per level.</summary>
    public static class UpgradeMath
    {
        public static float StatMultiplier(int upgradeLevel, float statBonusPerLevel) => 1f + upgradeLevel * statBonusPerLevel;
    }
}
