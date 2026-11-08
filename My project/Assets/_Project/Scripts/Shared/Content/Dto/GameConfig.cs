using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Global tuning values (no magic numbers in code). File: Resources/Content/config.json.</summary>
    [Serializable]
    public class GameConfig
    {
        public int TickRate;
        public string BootstrapScene;
        public string DefaultClassId;
        public string PlayerFaction;
        public float WorldHalfSize;
        public float CombatExitSeconds;
        public float ReserveRegenOutOfCombatFactor;
        public float ManaConsumableInCombatFactor;
        public float GlobalCooldownSeconds;
        public float MasteryMinEfficiency;
        public float MasteryMaxEfficiency;
        public float InstructorMasteryCap;
        public float DefenseCap;
        public float CritChanceCap;
        public int MinDamage;
        public int AttributePointsPerLevel;
        public float TargetCycleRange;
        public float InteractRange;
        public int DefaultPort;
        public int NetworkWorldEveryTicks;
        public int NetworkStateEveryTicks;
        public int MaxPlayerNameLength;
        /// <summary>Duels (temporary defaults, question 70): request range and lifetime, countdown, duration limit, arena radius.</summary>
        public float DuelRequestRange;
        public float DuelRequestSeconds;
        public float DuelCountdownSeconds;
        public float DuelMaxSeconds;
        public float DuelRadius;
        /// <summary>A duel is not fought to death: the loser keeps this much health.</summary>
        public float DuelLoserHealth;
        /// <summary>CombatContext hook (01 §13): damage multiplier when a player hits a player.</summary>
        public float PvpDamageMultiplier;
        /// <summary>Groups (06, 7.8): size, invite lifetime, loot vote timeout and repeat delay, loot radius, loot log length.</summary>
        public int GroupMaxSize;
        public float GroupInviteSeconds;
        public float LootVoteSeconds;
        public float LootVoteRepeatSeconds;
        public float LootRadius;
        public int GroupLogSize;
        /// <summary>Threat (module 8): per point of damage to a mob, and the share of healing given to the healer.</summary>
        public float ThreatPerDamage;
        public float HealThreatFactor;
        /// <summary>Mob AI (module 10): pause between any two abilities, threat for noticing a player, "at home" distance.</summary>
        public float MobGlobalCooldownSeconds;
        public float MobAggroThreat;
        public float MobReturnArriveDistance;
        /// <summary>Group experience (question 72): share of an equal part by level gap to the top member (index = gap); a gap ≥ length gets nothing.</summary>
        public float[] GroupXpGapShares;
        /// <summary>Interest management (module 3): who sees what — radius around the player and grid cell size.</summary>
        public float InterestRadius;
        public float InterestCellSize;
        /// <summary>Metrics: ticks in the sliding window; how often a dedicated server writes them to the log.</summary>
        public int MetricsWindowTicks;
        public float MetricsLogSeconds;
        /// <summary>Saves and accounts (module 13): autosave period, password hashing cost and minimum length, session token life, backups kept.</summary>
        public float AutosaveSeconds;
        public int PasswordIterations;
        public int MinPasswordLength;
        public float SessionTokenHours;
        public int BackupsToKeep;
        /// <summary>Ground loot (06, 7.8): how long a drop lies (5 min) and how far from the death spot it lands.</summary>
        public float LootSeconds;
        public float LootScatterRadius;
        /// <summary>Items (module 11): affixes per rarity (index = Rarity), socket types, the main currency and starting gold.</summary>
        public int[] AffixCountByRarity;
        public string[] SocketTypes;
        public string UniversalSocketType;
        public string GoldCurrencyId;
        public long StartingGold;
        /// <summary>How many recent idempotency keys the wallet remembers (repeat protection).</summary>
        public int WalletIdempotencyMemory;
        /// <summary>Chat (module 14): longest message and the shortest pause between two messages of a player.</summary>
        public int ChatMaxLength;
        public float ChatMinIntervalSeconds;
        public int FriendsMax;
        /// <summary>Chat tabs (decision of the developer): the account's tabs start as these; a player may have up to ChatTabsMax.</summary>
        public ChatTabDto[] ChatDefaultTabs;
        public int ChatTabsMax;
        public int ChatTabNameMax;
        /// <summary>Raid (06 7.8, question 6): size and subgroup size.</summary>
        public int RaidMaxSize;
        public int RaidSubgroupSize;
        /// <summary>Trade (module 14): how far apart the two may stand, how long a request waits.</summary>
        public float TradeRange;
        public float TradeRequestSeconds;
        /// <summary>Most different items one side may put into a trade (module 14).</summary>
        public int TradeMaxItems;
        /// <summary>How far (m) a trader may drift before the trade is cancelled as "moved" (float noise guard).</summary>
        public float TradeMoveTolerance;
        /// <summary>Module 20 (05, 7.14.3): personal shop levels, market search, mail and upgrade orders with escrow.</summary>
        public ShopTierDto[] ShopTiers;
        public int ShopNameMax;
        public long MarketPriceMax;
        public int MarketPageSize;
        public float MarketSearchMinIntervalSeconds;
        /// <summary>A single sale or mail worth this much gold is logged for an administrator (anti-fraud).</summary>
        public long MarketSuspiciousGold;
        /// <summary>How many recent request ids are remembered to ignore repeated buy / send commands (idempotency).</summary>
        public int MarketRequestMemory;
        public float MailExpireDays;
        public int MailboxMax;
        public int MailMaxAttachments;
        public long MailFeePerAttachment;
        public int MailSubjectMax;
        public int MailBodyMax;
        public float UpgradeOrderHours;
        public int UpgradeOrdersMax;
        /// <summary>Module 17 (02, 7.6): account settings defaults and limits; graphics presets and choices (device-local).</summary>
        public AccountSettingsDefaultsDto AccountSettings;
        public GraphicsPresetDto[] GraphicsPresets;
        /// <summary>FPS limit choices; 0 = no limit.</summary>
        public int[] FpsLimits;
        /// <summary>A new resolution or window mode is reverted unless confirmed within this time.</summary>
        public float ResolutionConfirmSeconds;
        public float MainHandSwapCooldownSeconds;
        public float MainHandSwapRiskWindowSeconds;
        public float MainHandSwapPenaltySeconds;
        public float OffHandSwapCooldownSeconds;
        public float PlayerRespawnSeconds;
        public int StartingLevel;
        public int SkillBarRows;
        public int SkillBarSlotsPerRow;
        public int ConsumableBarSlots;
        public FactionRelationDto[] HostileFactions;
    }
}
