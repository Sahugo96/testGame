using Game.Server.Items;

namespace Game.Server.World
{
    /// <summary>An upgrade order (04, 7.14.1): the item and the payment are held by the server (escrow) until a smith fulfils it.</summary>
    public struct UpgradeOrderData
    {
        public int Id;
        public string OwnerCharacterId;
        public string OwnerName;
        /// <summary>Null = any smith.</summary>
        public string SmithName;
        public ItemInstance Item;
        public long Payment;
        public long CreatedAt;
        public long ExpiresAt;
    }
}
