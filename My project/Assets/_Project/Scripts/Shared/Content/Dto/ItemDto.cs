using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Serialized ItemDefinition (04, 7.9). Behaviour comes from components.</summary>
    [Serializable]
    public class ItemDto
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public string Icon;
        public string Category;
        public string Rarity;
        public int ItemLevel;
        public int RequiredLevel;
        public string[] ClassRestriction;
        public string Binding;
        public ItemComponentDto[] Components;
    }
}
