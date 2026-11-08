using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Market state in the player snapshot (module 20): small numbers only; windows ask for the data when these change.</summary>
    public sealed class MarketStateSnapshot
    {
        /// <summary>The player's merchant entity while the shop is open, else None.</summary>
        public EntityId MyMerchantId;
        public int UnreadMail;
        /// <summary>Changes whenever this player's mailbox changes.</summary>
        public int MailVersion;
        /// <summary>Changes whenever any shop or lot changes (market and shop windows refresh).</summary>
        public int MarketVersion;
        /// <summary>Changes whenever any upgrade order changes.</summary>
        public int OrdersVersion;
    }

    public enum MarketSort : byte
    {
        PriceAscending = 0,
        PriceDescending = 1,
        LevelAscending = 2,
        LevelDescending = 3,
        Newest = 4,
    }

    /// <summary>Open a personal shop on a free shop spot: name, shop level (rent paid at once).</summary>
    public struct OpenShopCommand : IGameCommand
    {
        public EntityId SpotId;
        public string TierId;
        public string Name;
    }

    /// <summary>Close my shop before its time: the merchant leaves, unsold lots come back by mail.</summary>
    public struct CloseShopCommand : IGameCommand
    {
    }

    /// <summary>Put a whole bag stack on my merchant's counter at a price per piece (near the merchant).</summary>
    public struct ListLotCommand : IGameCommand
    {
        public string ItemInstanceId;
        public long PricePerUnit;
    }

    /// <summary>Take a lot back into the bag (near the merchant).</summary>
    public struct UnlistLotCommand : IGameCommand
    {
        public long LotId;
    }

    /// <summary>Change the price of my lot; its PriceVersion grows so a buyer who saw the old price is refused.</summary>
    public struct SetLotPriceCommand : IGameCommand
    {
        public long LotId;
        public long PricePerUnit;
    }

    /// <summary>
    /// Buy Count pieces of a lot (at its merchant or at the market NPC). PriceVersion is the version the buyer saw;
    /// RequestId makes a repeated command harmless (idempotency).
    /// </summary>
    public struct BuyLotCommand : IGameCommand
    {
        public long LotId;
        public int Count;
        public int PriceVersion;
        public string RequestId;
    }

    /// <summary>Ask for the contents of a merchant (answered with ShopViewMessage).</summary>
    public struct ViewShopCommand : IGameCommand
    {
        public EntityId MerchantId;
    }

    /// <summary>
    /// Market search (at the market NPC; answered with MarketPageMessage). The client turns the name typed into matching
    /// item ids (it has the texts); empty lists and zero / negative bounds mean "any".
    /// </summary>
    public struct MarketSearchCommand : IGameCommand
    {
        public string[] DefinitionIds;
        public string Category;
        public string ClassId;
        public int MinLevel;
        public int MaxLevel;
        public int MinSockets;
        public int MinUpgrade;
        /// <summary>-1 = any rarity.</summary>
        public int Rarity;
        public long MaxPrice;
        public MarketSort Sort;
        public int Page;
    }

    /// <summary>A lot as the client sees it.</summary>
    public struct LotSnapshot
    {
        public long LotId;
        public string ShopName;
        public EntityId MerchantId;
        public ItemSlotSnapshot Item;
        public long PricePerUnit;
        public int PriceVersion;
    }

    public sealed class ShopViewMessage
    {
        public EntityId MerchantId;
        public string Name;
        public string OwnerName;
        public string TierId;
        public bool IsMine;
        public long SecondsLeft;
        public LotSnapshot[] Lots = new LotSnapshot[0];
    }

    public sealed class MarketPageMessage
    {
        public int Total;
        public int Page;
        public int PageCount;
        public LotSnapshot[] Lots = new LotSnapshot[0];
    }
}
