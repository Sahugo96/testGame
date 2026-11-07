namespace Game.Shared.Protocol
{
    /// <summary>Localization keys of server notifications.</summary>
    public static class NotificationKeys
    {
        public const string LevelUp = "notify.level_up";
        public const string ExperienceGained = "notify.experience";
        public const string Loot = "notify.loot";
        public const string UpgradeSuccess = "notify.upgrade_success";
        public const string UpgradeFailed = "notify.upgrade_failed";
        public const string UpgradeBroken = "notify.upgrade_broken";
        /// <summary>The section for the item is full: the item is not picked up (question 68).</summary>
        public const string BagFullItemLost = "notify.bag_full_lost";
    }
}
