namespace Game.Shared.Content
{
    /// <summary>
    /// Skill (03, 7.2). FROZEN structure: Id, NameKey, DescriptionKey, Icon, TargetType, Range, Default, Empowered?, Weakened?.
    /// Mastery lives outside (SkillMasteryRule + character data), so this structure is unchanged.
    /// </summary>
    public sealed class SkillDefinition
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public string Icon;
        public TargetType TargetType;
        public float Range;
        public bool UsesGlobalCooldown;
        public SkillVariant Default;
        public SkillVariant Empowered;
        public SkillVariant Weakened;

        public SkillVariant GetVariant(SkillVariantKind kind)
        {
            switch (kind)
            {
                case SkillVariantKind.Empowered:
                    return Empowered;
                case SkillVariantKind.Weakened:
                    return Weakened;
                default:
                    return Default;
            }
        }
    }
}
