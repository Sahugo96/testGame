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
        public const string DuelRequested = "notify.duel_requested";
        public const string DuelDeclined = "notify.duel_declined";
        public const string DuelExpired = "notify.duel_expired";
        public const string DuelCancelled = "notify.duel_cancelled";
        public const string DuelStarted = "notify.duel_started";
        public const string DuelWon = "notify.duel_won";
        public const string DuelLost = "notify.duel_lost";
        public const string DuelLostLeftArea = "notify.duel_left_area";
        public const string DuelDraw = "notify.duel_draw";
    }
}
