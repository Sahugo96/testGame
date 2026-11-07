using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Serialized SkillDefinition (FROZEN structure, 03 7.2). Variants are listed by Kind.</summary>
    [Serializable]
    public class SkillDto
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public string Icon;
        public string TargetType;
        public float Range;
        public bool UsesGlobalCooldown;
        public SkillVariantDto[] Variants;
    }
}
