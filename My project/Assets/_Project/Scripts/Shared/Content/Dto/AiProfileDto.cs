using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/ai_profiles.json.</summary>
    [Serializable]
    public class AiProfilesFileDto
    {
        public AiProfileDto[] Profiles;
    }

    /// <summary>Mob AI profile (01, 7.10: a mob = definition + AI profile + abilities + loot table + spawner).</summary>
    [Serializable]
    public class AiProfileDto
    {
        public string Id;
        /// <summary>AiBehavior: Passive, Aggressive or Defensive.</summary>
        public string Behavior;
        public float MoveSpeed;
        public float AggroRadius;
        public float LeashRadius;
        /// <summary>Skill ids in priority order; the mob uses the Default variant (no costs, its cooldown and range).</summary>
        public string[] Abilities;
    }
}
