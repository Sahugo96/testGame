using System;
using System.Collections.Generic;
using Game.Client.Net;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Client.State
{
    /// <summary>
    /// Client-side model (ViewModel source): the last snapshots received from the server. Views read it and subscribe
    /// to its events; they never talk to the network directly (02, 7.13).
    /// </summary>
    public sealed class ClientState
    {
        private readonly Dictionary<EntityId, EntitySnapshot> _entities = new Dictionary<EntityId, EntitySnapshot>();

        public WorldSnapshot World { get; private set; }
        public PlayerStateSnapshot Player { get; private set; }
        private IServerConnection _connection;

        /// <summary>Known after the server's Welcome (remote) or immediately (local host).</summary>
        public EntityId LocalPlayerId => _connection != null ? _connection.LocalPlayerId : EntityId.None;

        public event Action WorldChanged;
        public event Action PlayerChanged;
        public event Action<CombatEventMessage> CombatEvent;

        private const int CombatLogSize = 50;

        /// <summary>Recent combat events about the player or their target, oldest first (the combat log window shows them).</summary>
        public readonly List<CombatEventMessage> CombatLog = new List<CombatEventMessage>();
        public event Action<string> Rejected;
        public event Action<NotificationMessage> Notification;

        /// <summary>Chat lines as they arrive (module 14); the last ChatLogSize are kept in ChatLog for the chat window.</summary>
        public event Action<ChatMessage> Chat;

        private const int ChatLogSize = 100;

        public readonly List<ChatMessage> ChatLog = new List<ChatMessage>();

        /// <summary>Latest answers for the market windows (module 20); MarketData is raised when any of them arrives.</summary>
        public ShopViewMessage LastShop;
        public MarketPageMessage LastMarketPage;
        public MailboxMessage LastMailbox;
        public UpgradeOrdersMessage LastOrders;
        public event Action MarketData;

        /// <summary>The account's settings as the server sent them (module 17): after login and after every change.</summary>
        public event Action<AccountSettingsSnapshot> AccountSettings;

        /// <summary>The character's quick panels and loadouts (module 18): after login and after each change.</summary>
        public PanelsMessage Panels { get; private set; }

        public event Action<PanelsMessage> PanelsChanged;
        /// <summary>An entity in view started an emote.</summary>
        public event Action<EmoteMessage> Emote;

        public void Bind(IServerConnection connection)
        {
            _connection = connection;
            connection.WorldReceived += OnWorld;
            connection.PlayerStateReceived += OnPlayer;
            connection.CombatEventReceived += OnCombatEvent;
            connection.RejectedReceived += OnRejected;
            connection.NotificationReceived += OnNotification;
            connection.ChatReceived += OnChat;
            connection.ReplyReceived += OnReply;
        }

        public void Unbind(IServerConnection connection)
        {
            CombatLog.Clear();
            ChatLog.Clear();
            connection.WorldReceived -= OnWorld;
            connection.PlayerStateReceived -= OnPlayer;
            connection.CombatEventReceived -= OnCombatEvent;
            connection.RejectedReceived -= OnRejected;
            connection.NotificationReceived -= OnNotification;
            connection.ChatReceived -= OnChat;
            connection.ReplyReceived -= OnReply;
            LastShop = null;
            LastMarketPage = null;
            LastMailbox = null;
            LastOrders = null;
            Panels = null;
            _entities.Clear();
            World = null;
            Player = null;
            _connection = null;
        }

        public bool TryGetEntity(EntityId id, out EntitySnapshot entity) => _entities.TryGetValue(id, out entity);

        public IEnumerable<EntitySnapshot> Entities => _entities.Values;

        private void OnWorld(WorldSnapshot snapshot)
        {
            World = snapshot;
            _entities.Clear();
            foreach (EntitySnapshot entity in snapshot.Entities)
            {
                _entities[entity.Id] = entity;
            }
            WorldChanged?.Invoke();
        }

        private void OnCombatEvent(CombatEventMessage message)
        {
            if (Player != null && Concerns(Player, message))
            {
                CombatLog.Add(message);
                if (CombatLog.Count > CombatLogSize)
                {
                    CombatLog.RemoveAt(0);
                }
            }
            CombatEvent?.Invoke(message);
        }

        /// <summary>Combat log events concern the player or their target (module 8).</summary>
        private static bool Concerns(PlayerStateSnapshot player, CombatEventMessage message)
        {
            EntityId self = player.EntityId;
            EntityId target = player.TargetId;
            return message.SourceId == self || message.TargetId == self || (target.IsValid && (message.SourceId == target || message.TargetId == target));
        }

        private void OnRejected(CommandRejectedMessage message) => Rejected?.Invoke(message.ReasonKey);

        private void OnNotification(NotificationMessage message) => Notification?.Invoke(message);

        private void OnReply(object reply)
        {
            switch (reply)
            {
                case ShopViewMessage shop:
                    LastShop = shop;
                    break;
                case MarketPageMessage page:
                    LastMarketPage = page;
                    break;
                case MailboxMessage mailbox:
                    LastMailbox = mailbox;
                    break;
                case UpgradeOrdersMessage orders:
                    LastOrders = orders;
                    break;
                case AccountSettingsMessage settings:
                    AccountSettings?.Invoke(settings.Settings);
                    return;
                case EmoteMessage emote:
                    Emote?.Invoke(emote);
                    return;
                case PanelsMessage panels:
                    Panels = panels;
                    PanelsChanged?.Invoke(panels);
                    return;
            }
            MarketData?.Invoke();
        }

        private void OnChat(ChatMessage message)
        {
            ChatLog.Add(message);
            if (ChatLog.Count > ChatLogSize)
            {
                ChatLog.RemoveAt(0);
            }
            Chat?.Invoke(message);
        }

        private void OnPlayer(PlayerStateSnapshot snapshot)
        {
            Player = snapshot;
            PlayerChanged?.Invoke();
        }
    }
}
