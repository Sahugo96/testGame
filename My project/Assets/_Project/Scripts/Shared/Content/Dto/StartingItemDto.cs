using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Item given on character creation.</summary>
    [Serializable]
    public class StartingItemDto
    {
        public string ItemId;
        public int Count;
        public bool Equip;
    }
}
