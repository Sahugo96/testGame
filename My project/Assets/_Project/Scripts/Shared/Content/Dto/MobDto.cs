using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Mob definition (minimal; full mobs are module 10). Used for training dummies.</summary>
    [Serializable]
    public class MobDto
    {
        public string Id;
        /// <summary>Retired (question 83): kept so old saves load, but no longer dropped, crafted or spawned.</summary>
        public bool Deprecated;
        public string NameKey;
        public string Faction;
        public float MaxHealth;
        public float HealthRegen;
        public float Defense;
        public float RespawnSeconds;
        public string VisualColor;
        public string VisualShape;
        public int ExperienceReward;
        public MobDropDto[] Drops;
        /// <summary>AI profile id (ai_profiles.json); empty = passive.</summary>
        public string AiProfile;
        /// <summary>Gold dropped on death (05, 7.14.2), split like experience.</summary>
        public int GoldMin;
        public int GoldMax;
    }
}
