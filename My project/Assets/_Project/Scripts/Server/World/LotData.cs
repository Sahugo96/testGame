using Game.Server.Items;

namespace Game.Server.World
{
    /// <summary>A lot on a merchant's counter: the listed instance (held by the server), price per piece and its version.</summary>
    public struct LotData
    {
        public long Id;
        public ItemInstance Item;
        public long PricePerUnit;
        /// <summary>Grows on every price change: a buyer must name the version he saw (no price swap at the moment of buying).</summary>
        public int PriceVersion;
        public long ListedAt;
    }
}
