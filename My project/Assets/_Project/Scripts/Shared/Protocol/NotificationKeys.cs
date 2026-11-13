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
        public const string GroupBecameRaid = "notify.group_became_raid";
        public const string TradeRequested = "notify.trade_requested";
        public const string TradeDone = "notify.trade_done";
        public const string TradeCancelled = "notify.trade_cancelled";
        public const string TradeFailed = "notify.trade_failed";
        public const string TradeNoRoom = "notify.trade_no_room";
        /// <summary>Chat system lines with {1} = character name.</summary>
        public const string FriendRequestReceived = "notify.friend_request";
        public const string FriendAccepted = "notify.friend_accepted";
        /// <summary>Module 20: {0} = number, {1} = item name.</summary>
        public const string MailNew = "notify.mail_new";
        public const string MailSent = "notify.mail_sent";
        public const string MailClaimed = "notify.mail_claimed";
        public const string ShopOpened = "notify.shop_opened";
        public const string ShopClosed = "notify.shop_closed";
        public const string ShopSold = "notify.shop_sold";
        public const string PurchaseDone = "notify.purchase_done";
        public const string PurchaseByMail = "notify.purchase_by_mail";
        public const string OrderCreated = "notify.order_created";
        public const string OrderFulfilled = "notify.order_fulfilled";
        public const string OrderCancelled = "notify.order_cancelled";
        public const string LootVoteStarted = "notify.loot_vote_started";
        public const string LootVotePassed = "notify.loot_vote_passed";
        public const string LootVoteFailed = "notify.loot_vote_failed";
        public const string LootVoteCancelled = "notify.loot_vote_cancelled";
        /// <summary>Leader mode: the leader had no room, the item stays on the ground for the leader.</summary>
        public const string LootLeaderBagFull = "notify.loot_leader_bag_full";
        /// <summary>Solo: the section was full, the drop stays on the ground for the killer.</summary>
        public const string LootOnGround = "notify.loot_on_ground";
        /// <summary>{0} = gold received.</summary>
        public const string GoldGained = "notify.gold_gained";
        /// <summary>Module 18: {0} = loadout number; {0} = items of the loadout no longer in the bag; {0} = armour pieces left for after combat.</summary>
        public const string LoadoutSaved = "notify.loadout_saved";
        public const string LoadoutPartial = "notify.loadout_partial";
        public const string LoadoutArmourInCombat = "notify.loadout_armour_in_combat";
    }
}
