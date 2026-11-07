using System.Collections.Generic;

namespace Game.Shared.Content
{
    /// <summary>Backpack section (tab) with its slot range in the bag: [Start, Start + Size).</summary>
    public sealed class BagSection
    {
        public string Id;
        public string NameKey;
        public int Start;
        public int Size;
        public readonly HashSet<ItemCategory> Categories = new HashSet<ItemCategory>();

        public bool Contains(int bagIndex) => bagIndex >= Start && bagIndex < Start + Size;
    }
}
