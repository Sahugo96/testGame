using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>
    /// A personal shop (05, 7.14.3): a player's merchant on a shop spot for the paid time, with lots on its counter.
    /// Times are Unix seconds (real time: the shop works while the owner is offline and survives restarts).
    /// </summary>
    public struct ShopData
    {
        public int Id;
        public string OwnerCharacterId;
        public string OwnerName;
        public string Name;
        public string TierId;
        /// <summary>Content id of the shop spot (world.json Stations).</summary>
        public string SpotId;
        public EntityId MerchantId;
        public long OpenedAt;
        public long ExpiresAt;
        public List<LotData> Lots;
    }
}
