using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Character class (03, 7.3).</summary>
    [Serializable]
    public class ClassDto
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public string Role;
        public string PrimaryAttribute;
        public StatValueDto[] BaseStats;
        public StatValueDto[] StartingAttributes;
        public AttributeEffectDto[] AttributeEffects;
        public ClassSkillDto[] Skills;
        public EquipmentRulesDto EquipmentRules;
        public StartingItemDto[] StartingItems;
    }
}
