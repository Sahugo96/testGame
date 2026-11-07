using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Skill granted by a class at a level.</summary>
    [Serializable]
    public class ClassSkillDto
    {
        public string SkillId;
        public int LevelUnlock;
        public float StartingMastery;
    }
}
