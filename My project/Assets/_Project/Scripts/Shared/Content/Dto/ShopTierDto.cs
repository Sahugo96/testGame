using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>
    /// Level of a personal shop (05, 7.14.3; question 34): commission per sale (the lower the level, the higher the
    /// commission and the fewer lots), lot limit, how long the paid merchant stays and what the rent costs (gold sink).
    /// </summary>
    [Serializable]
    public class ShopTierDto
    {
        public string Id;
        public string NameKey;
        public float Commission;
        public int MaxLots;
        public float DurationHours;
        public long RentCost;
    }
}
