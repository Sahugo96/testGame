using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>
    /// Upgrade order with escrow (04, 7.14.1): the owner hands the item and the payment to the server at a whetstone; a smith
    /// (anyone, or the one named) makes one upgrade attempt with his own materials; the item goes back to the owner by mail
    /// and the payment to the smith. Nobody but the owner can ever hold the item.
    /// </summary>
    public struct CreateUpgradeOrderCommand : IGameCommand
    {
        public string ItemInstanceId;
        public long Payment;
        /// <summary>Empty = any smith.</summary>
        public string SmithName;
        public EntityId StationId;
    }

    public struct CancelUpgradeOrderCommand : IGameCommand
    {
        public int OrderId;
    }

    public struct FulfillUpgradeOrderCommand : IGameCommand
    {
        public int OrderId;
        public EntityId StationId;
    }

    /// <summary>Ask for the orders I may see: mine and those open to me as a smith (answered with UpgradeOrdersMessage).</summary>
    public struct ViewUpgradeOrdersCommand : IGameCommand
    {
    }

    public struct UpgradeOrderSnapshot
    {
        public int Id;
        public string OwnerName;
        public string SmithName;
        public ItemSlotSnapshot Item;
        public long Payment;
        public long SecondsLeft;
        public bool IsMine;
    }

    public sealed class UpgradeOrdersMessage
    {
        public UpgradeOrderSnapshot[] Orders = new UpgradeOrderSnapshot[0];
    }
}
