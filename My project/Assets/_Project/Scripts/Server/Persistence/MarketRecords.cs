using System;
using System.Collections.Generic;

namespace Game.Server.Persistence
{
    /// <summary>The saved market (module 20): shops with lots, letters, upgrade orders, next ids. Times are Unix seconds.</summary>
    [Serializable]
    public class MarketRecord
    {
        public int SchemaVersion;
        public int NextShopId;
        public long NextLotId;
        public long NextMailId;
        public int NextOrderId;
        public ShopRecord[] Shops;
        public MailRecord[] Mail;
        public UpgradeOrderRecord[] Orders;
    }

    [Serializable]
    public class ShopRecord
    {
        public int Id;
        public string OwnerCharacterId;
        public string OwnerName;
        public string Name;
        public string TierId;
        public string SpotId;
        public long OpenedAt;
        public long ExpiresAt;
        public LotRecord[] Lots;
    }

    [Serializable]
    public class LotRecord
    {
        public long Id;
        public long PricePerUnit;
        public int PriceVersion;
        public long ListedAt;
        public ItemRecord Item;
    }

    [Serializable]
    public class MailRecord
    {
        public long Id;
        public string Type;
        public string Recipient;
        public string Sender;
        public string Subject;
        public string Body;
        public string SubjectKey;
        public string ItemDefinitionId;
        public int Number;
        public string Detail;
        public ItemRecord[] Items;
        public long Gold;
        public long SentAt;
        public long ExpiresAt;
        public bool Read;
        public bool Claimed;
    }

    [Serializable]
    public class UpgradeOrderRecord
    {
        public int Id;
        public string OwnerCharacterId;
        public string OwnerName;
        public string SmithName;
        public ItemRecord Item;
        public long Payment;
        public long CreatedAt;
        public long ExpiresAt;
    }

    /// <summary>
    /// The journal of one commit (FileStorage.Commit): written atomically before the files it lists; if it is still there
    /// on start, the commit is replayed. HasMarket because JsonUtility cannot store a null object.
    /// </summary>
    [Serializable]
    public class CommitRecord
    {
        public CharacterRecord[] Characters;
        public bool HasMarket;
        public MarketRecord Market;
    }
}
