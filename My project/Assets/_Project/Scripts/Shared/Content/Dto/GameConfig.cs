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
