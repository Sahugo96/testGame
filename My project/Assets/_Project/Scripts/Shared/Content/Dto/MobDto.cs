using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Mob definition (minimal; full mobs are module 10). Used for training dummies.</summary>
    [Serializable]
    public class MobDto
    {
        public string Id;
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
    }
}
