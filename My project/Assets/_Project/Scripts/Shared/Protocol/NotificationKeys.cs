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
        public const string CraftSuccess = "notify.craft_success";
        /// <summary>Failure destroys all inputs (05, 7.4; question 19).</summary>
        public const string CraftFailed = "notify.craft_failed";
        public const string CraftCancelled = "notify.craft_cancelled";
        public const string DuelRequested = "notify.duel_requested";
        public const string DuelDeclined = "notify.duel_declined";
        public const string DuelExpired = "notify.duel_expired";
        public const string DuelCancelled = "notify.duel_cancelled";
        public const string DuelStarted = "notify.duel_started";
        public const string DuelWon = "notify.duel_won";
        public const string DuelLost = "notify.duel_lost";
        public const string DuelLostLeftArea = "notify.duel_left_area";
        public const string DuelDraw = "notify.duel_draw";
        public const string GroupInvited = "notify.group_invited";
        public const string GroupInviteDeclined = "notify.group_invite_declined";
        public const string GroupInviteExpired = "notify.group_invite_expired";
        public const string GroupJoined = "notify.group_joined";
        public const string GroupLeft = "notify.group_left";
        public const string GroupKicked = "notify.group_kicked";
        public const string GroupDisbanded = "notify.group_disbanded";
        public const string GroupMemberJoined = "notify.group_member_joined";
        public const string GroupMemberLeft = "notify.group_member_left";
        public const string GroupNewLeader = "notify.group_new_leader";
        public const string LootVoteStarted = "notify.loot_vote_started";
        public const string LootVotePassed = "notify.loot_vote_passed";
        public const string LootVoteFailed = "notify.loot_vote_failed";
        public const string LootVoteCancelled = "notify.loot_vote_cancelled";
        /// <summary>Leader mode: the leader had no room, the item stays on the ground for the leader.</summary>
        public const string LootLeaderBagFull = "notify.loot_leader_bag_full";
        /// <summary>Solo: the section was full, the drop stays on the ground for the killer.</summary>
        public const string LootOnGround = "notify.loot_on_ground";
    }
}
