using System;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Client.Net
{
    /// <summary>
    /// Client view of the server. Today it is an in-process host (Game.Host); in module 1 a Mirror implementation
    /// replaces it without touching client code.
    /// </summary>
    public interface IServerConnection
    {
        EntityId LocalPlayerId { get; }

        /// <summary>Seconds between world snapshots (used for interpolation).</summary>
        float TickInterval { get; }

        /// <summary>The connection is gone (server stopped, network lost or refused).</summary>
        bool IsClosed { get; }

        /// <summary>Why the connection closed: a localization key from the server or a transport message.</summary>
        string CloseReason { get; }

        /// <summary>Delivers received messages as events (and, for a local host, runs the server ticks). Main thread.</summary>
        void Update(float deltaTime);

        void Send(IGameCommand command);

        event Action<WorldSnapshot> WorldReceived;
        event Action<PlayerStateSnapshot> PlayerStateReceived;
        event Action<CombatEventMessage> CombatEventReceived;
        event Action<CommandRejectedMessage> RejectedReceived;
        event Action<NotificationMessage> NotificationReceived;

        /// <summary>Chat lines (module 14).</summary>
        event Action<ChatMessage> ChatReceived;

        /// <summary>Answers to window requests (module 20): ShopViewMessage, MarketPageMessage, MailboxMessage, UpgradeOrdersMessage.</summary>
        event Action<object> ReplyReceived;
    }
}
