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
