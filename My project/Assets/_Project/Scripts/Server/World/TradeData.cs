using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>
    /// One trade (module 14): the two sides, request or open, each side's offer and confirmation, and where both stood
    /// (decision of the developer: the trade is cancelled at once when anyone moves).
    /// </summary>
    public struct TradeData
    {
        public int Id;
        public EntityId A;
        public EntityId B;
        public bool Open;
        public float RequestExpiresAt;
        public TradeOffer OfferA;
        public TradeOffer OfferB;
        public Vec3 PositionA;
        public Vec3 PositionB;
    }

    public struct TradeOffer
    {
        public List<string> InstanceIds;
        /// <summary>Stack size of each offered instance when it was offered: a change (used, split, merged) resets confirmations.</summary>
        public List<int> Counts;
        public long Gold;
        public bool Confirmed;
    }
}
