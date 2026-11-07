using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>One trade (module 14): the two sides, request or open, each side's offer and confirmation.</summary>
    public struct TradeData
    {
        public int Id;
        public EntityId A;
        public EntityId B;
        public bool Open;
        public float RequestExpiresAt;
        public TradeOffer OfferA;
        public TradeOffer OfferB;
    }

    public struct TradeOffer
    {
        public List<string> InstanceIds;
        public long Gold;
        public bool Confirmed;
    }
}
