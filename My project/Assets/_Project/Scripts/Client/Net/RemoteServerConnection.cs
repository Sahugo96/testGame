using System;
using System.Collections.Generic;
using Game.Network;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Client.Net
{
    /// <summary>
    /// Connection to a server over the network (temporary own TCP transport, question 69). Messages arrive on network
    /// threads and are raised as events only in <see cref="Update"/>, on the main thread.
    /// </summary>
    public sealed class RemoteServerConnection : IServerConnection, IDisposable
    {
        private const float FallbackInterval = 0.1f;

        /// <summary>Static fields of entities in view: the server sends them once (module 3 traffic), compact entries are filled from here.</summary>
        private readonly Dictionary<EntityId, EntitySnapshot> _known = new Dictionary<EntityId, EntitySnapshot>();

        private readonly NetworkClientSession _session;

        public RemoteServerConnection(NetworkClientSession session) => _session = session;

        public EntityId LocalPlayerId => _session.PlayerId;

        public float TickInterval => _session.TickRate > 0 ? (float)_session.WorldEveryTicks / _session.TickRate : FallbackInterval;

        public bool IsReady => _session.IsWelcomed;

        /// <summary>Session token from the server (module 13): reconnects without the password.</summary>
        public string Token => _session.Token;

        public bool IsClosed => _session.IsClosed;

        public string CloseReason => _session.Error ?? _session.CloseReason;

        public event Action<WorldSnapshot> WorldReceived;
        public event Action<PlayerStateSnapshot> PlayerStateReceived;
        public event Action<CombatEventMessage> CombatEventReceived;
        public event Action<CommandRejectedMessage> RejectedReceived;
        public event Action<NotificationMessage> NotificationReceived;
        public event Action<ChatMessage> ChatReceived;
        public event Action<object> ReplyReceived;

        public void Send(IGameCommand command) => _session.Send(command);

        public void Update(float deltaTime)
        {
            while (_session.TryReceive(out object message))
            {
                switch (message)
                {
                    case WorldSnapshot world:
                        FillStaticFields(world);
                        WorldReceived?.Invoke(world);
                        break;
                    case PlayerStateSnapshot state:
                        PlayerStateReceived?.Invoke(state);
                        break;
                    case CombatEventMessage combat:
                        CombatEventReceived?.Invoke(combat);
                        break;
                    case CommandRejectedMessage rejected:
                        RejectedReceived?.Invoke(rejected);
                        break;
                    case ChatMessage chat:
                        ChatReceived?.Invoke(chat);
                        break;
                    case ShopViewMessage _:
                    case MarketPageMessage _:
                    case MailboxMessage _:
                    case UpgradeOrdersMessage _:
                    case AccountSettingsMessage _:
                        ReplyReceived?.Invoke(message);
                        break;
                    case NotificationMessage notification:
                        NotificationReceived?.Invoke(notification);
                        break;
                }
            }
        }

        public void Dispose() => _session.Dispose();

        private void FillStaticFields(WorldSnapshot world)
        {
            for (int i = 0; i < world.Entities.Length; i++)
            {
                ref EntitySnapshot entity = ref world.Entities[i];
                if (entity.DefinitionId != null)
                {
                    _known[entity.Id] = entity;
                }
                else if (_known.TryGetValue(entity.Id, out EntitySnapshot cached))
                {
                    entity.DefinitionId = cached.DefinitionId;
                    entity.NameKey = cached.NameKey;
                    entity.DisplayName = cached.DisplayName;
                    entity.Faction = cached.Faction;
                }
            }
        }
    }
}
