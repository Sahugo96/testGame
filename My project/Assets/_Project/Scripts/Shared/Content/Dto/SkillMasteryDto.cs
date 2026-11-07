using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Mastery rules of a skill, kept outside the frozen SkillDefinition (question 5).</summary>
    [Serializable]
    public class SkillMasteryDto
    {
        public string SkillId;
        public float EmpoweredUnlock;
        public float WeakenedUnlock;
        public float MasteryPerUse;
    }
}
