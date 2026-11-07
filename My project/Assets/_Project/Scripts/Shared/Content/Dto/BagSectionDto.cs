using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Backpack section (tab): its own capacity; items go to the section that lists their category.</summary>
    [Serializable]
    public class BagSectionDto
    {
        public string Id;
        public string NameKey;
        public int Size;
        public string[] Categories;
    }
}
