namespace Game.Shared.Content
{
    /// <summary>Mastery data of a skill (questions 5, 54): variant unlock thresholds and gain per use. Stored apart from the frozen SkillDefinition.</summary>
    public sealed class SkillMasteryRule
    {
        public string SkillId;
        public float EmpoweredUnlock;
        public float WeakenedUnlock;
        public float MasteryPerUse;
    }
}
