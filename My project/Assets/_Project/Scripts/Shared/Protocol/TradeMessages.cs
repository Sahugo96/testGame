using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Trade between two players (module 14).</summary>
    public enum TradePhase : byte
    {
        None = 0,
        IncomingRequest = 1,
        OutgoingRequest = 2,
        Open = 3,
    }

    public struct TradeRequestCommand : IGameCommand
    {
        public EntityId TargetId;
    }

    public struct TradeRespondCommand : IGameCommand
    {
        public bool Accept;
    }

    /// <summary>The whole own offer: bag items (instance ids) and gold. Any change resets both confirmations.</summary>
    public struct TradeOfferCommand : IGameCommand
    {
        public string[] InstanceIds;
        public long Gold;
    }

    public struct TradeConfirmCommand : IGameCommand
    {
    }

    public struct TradeCancelCommand : IGameCommand
    {
    }

    /// <summary>The trade as one side sees it.</summary>
    public sealed class TradeSnapshot
    {
        public TradePhase Phase;
        public EntityId PartnerId;
        public float Remaining;
        public ItemSlotSnapshot[] MyItems = new ItemSlotSnapshot[0];
        public long MyGold;
        public bool MyConfirmed;
        public ItemSlotSnapshot[] TheirItems = new ItemSlotSnapshot[0];
        public long TheirGold;
        public bool TheirConfirmed;
    }
}
