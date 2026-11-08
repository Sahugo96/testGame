using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/ai_profiles.json (merged across packs).</summary>
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
        /// <summary>Herd: kin within AssistRadius join the fight (developer decision, question 75); false — a loner.</summary>
        public bool Herd;
        public float AssistRadius;
        /// <summary>Skill ids in priority order; the mob uses the Default variant (no costs, its cooldown and range).</summary>
        public string[] Abilities;
    }
}
