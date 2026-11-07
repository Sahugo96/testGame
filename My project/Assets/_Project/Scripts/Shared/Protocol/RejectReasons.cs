namespace Game.Shared.Protocol
{
    /// <summary>Localization keys of command rejection reasons.</summary>
    public static class RejectReasons
    {
        public const string NotEnoughMana = "error.not_enough_mana";
        public const string OnCooldown = "error.on_cooldown";
        public const string NoTarget = "error.no_target";
        public const string InvalidTarget = "error.invalid_target";
        public const string OutOfRange = "error.out_of_range";
        public const string AlreadyCasting = "error.already_casting";
        public const string SkillUnknown = "error.skill_unknown";
        public const string Dead = "error.dead";
        public const string ItemNotFound = "error.item_not_found";
        public const string CannotEquip = "error.cannot_equip";
        public const string WrongClass = "error.wrong_class";
        public const string LevelTooLow = "error.level_too_low";
        public const string ArmorInCombat = "error.armor_in_combat";
        public const string WeaponSwapLocked = "error.weapon_swap_locked";
        public const string BagFull = "error.bag_full";
        public const string NotConsumable = "error.not_consumable";
        public const string ResourceFull = "error.resource_full";
        public const string NotNearStation = "error.not_near_station";
        public const string NoMaterials = "error.no_materials";
        public const string MaxUpgradeLevel = "error.max_upgrade_level";
        public const string CannotUpgrade = "error.cannot_upgrade";
        public const string NoAttributePoints = "error.no_attribute_points";
        public const string DuelBusy = "error.duel_busy";
        public const string DuelTargetBusy = "error.duel_target_busy";
        public const string DuelInCombat = "error.duel_in_combat";
        public const string DuelNotPlayer = "error.duel_not_player";
        public const string DuelNoRequest = "error.duel_no_request";
        public const string GroupNotPlayer = "error.group_not_player";
        public const string GroupTargetInGroup = "error.group_target_in_group";
        public const string GroupTargetHasInvite = "error.group_target_has_invite";
        public const string GroupNotLeader = "error.group_not_leader";
        public const string GroupFull = "error.group_full";
        public const string GroupNoInvite = "error.group_no_invite";
        public const string GroupNotInGroup = "error.group_not_in_group";
        public const string GroupNotMember = "error.group_not_member";
        public const string LootVoteActive = "error.loot_vote_active";
        public const string LootVoteCooldown = "error.loot_vote_cooldown";
        public const string LootVoteSameMode = "error.loot_vote_same_mode";
        public const string LootVoteNoQuorum = "error.loot_vote_no_quorum";
        public const string LootVoteNotFound = "error.loot_vote_not_found";
    }
}
