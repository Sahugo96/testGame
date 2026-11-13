using System.Collections.Generic;
using Game.Shared.Protocol;

namespace Game.Server.Players
{
    /// <summary>Messages prepared for one player: latest snapshots plus queued events. The transport drains it.</summary>
    public sealed class PlayerOutbox
    {
        public WorldSnapshot World;
        public PlayerStateSnapshot State;
        /// <summary>
        /// How often the transport takes the snapshots (1 = every tick: the local player and tests). The network host sends
        /// the world every NetworkWorldEveryTicks and the state every NetworkStateEveryTicks, so the server builds them for
        /// its players only on those ticks instead of building and throwing away the rest.
        /// </summary>
        public int WorldEveryTicks = 1;
        public int StateEveryTicks = 1;
        public readonly List<CombatEventMessage> CombatEvents = new List<CombatEventMessage>();
        public readonly List<CommandRejectedMessage> Rejections = new List<CommandRejectedMessage>();
        public readonly List<NotificationMessage> Notifications = new List<NotificationMessage>();
        public readonly List<ChatMessage> Chat = new List<ChatMessage>();
        /// <summary>Answers to window requests (module 20): ShopViewMessage, MarketPageMessage, MailboxMessage, UpgradeOrdersMessage.</summary>
        public readonly List<object> Replies = new List<object>();
    }
}
