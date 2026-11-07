using System.Collections.Generic;
using Game.Shared.Content.Dto;

namespace Game.Shared.Content
{
    /// <summary>Character class (03, 7.3). Skills and equipment rules depend on the class.</summary>
    public sealed class ClassDefinition
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public ClassRole Role;
        public StatId PrimaryAttribute;
        public readonly Dictionary<StatId, float> BaseStats = new Dictionary<StatId, float>();
        public readonly Dictionary<StatId, float> StartingAttributes = new Dictionary<StatId, float>();
        public readonly List<AttributeEffect> AttributeEffects = new List<AttributeEffect>();
        public readonly List<ClassSkill> Skills = new List<ClassSkill>();
        public int RingSlots;
        public readonly HashSet<string> AllowedArmorTypes = new HashSet<string>();
        public readonly HashSet<string> AllowedWeaponTypes = new HashSet<string>();
        public readonly List<StartingItemDto> StartingItems = new List<StartingItemDto>();
    }
}
