using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>
    /// Everything of module 20 the server holds for players: shops with lots, letters, upgrade orders. Saved as a whole
    /// (IMarketStore) right after a tick that changed it, together with the characters involved.
    /// </summary>
    public sealed class MarketState
    {
        public readonly Dictionary<int, ShopData> Shops = new Dictionary<int, ShopData>();
        public readonly Dictionary<long, MailData> Mail = new Dictionary<long, MailData>();
        public readonly Dictionary<int, UpgradeOrderData> Orders = new Dictionary<int, UpgradeOrderData>();
        public int NextShopId = 1;
        public long NextLotId = 1;
        public long NextMailId = 1;
        public int NextOrderId = 1;

        public int MarketVersion;
        public int OrdersVersion;
        /// <summary>Per mailbox (lower-case character name).</summary>
        public readonly Dictionary<string, int> MailVersions = new Dictionary<string, int>();

        /// <summary>Set by any change; the server saves once per tick.</summary>
        public bool Dirty;

        /// <summary>Online players whose bag or wallet changed with the market (saved before the market: a crash loses, never duplicates).</summary>
        public readonly HashSet<EntityId> Touched = new HashSet<EntityId>();

        public void Changed(EntityId player)
        {
            Dirty = true;
            if (player.IsValid)
            {
                Touched.Add(player);
            }
        }

        public void MailChanged(string recipient)
        {
            string key = (recipient ?? string.Empty).ToLowerInvariant();
            MailVersions.TryGetValue(key, out int version);
            MailVersions[key] = version + 1;
            Dirty = true;
        }
    }
}
