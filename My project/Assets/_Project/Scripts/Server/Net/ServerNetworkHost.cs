using System;
using System.Collections.Generic;
using Game.Network;
using Game.Server.Players;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Net
{
    /// <summary>
    /// Network front of the authoritative server (temporary own TCP transport, question 69): accepts players, creates
    /// their characters on Hello, forwards their commands to <see cref="GameServer"/> and sends snapshots after each
    /// tick (world every NetworkWorldEveryTicks, private state every NetworkStateEveryTicks, events always).
    /// Game logic stays in GameServer; this class only moves messages (principle 2).
    /// </summary>
    public sealed class ServerNetworkHost : IDisposable
    {
        private sealed class Peer
        {
            public NetConnection Connection;
            public EntityId Player;
            public bool Joined;
            /// <summary>The last world snapshot sent to this peer, by entity: unchanged parts are not sent again (module 3).</summary>
            public readonly Dictionary<EntityId, EntitySnapshot> SentEntities = new Dictionary<EntityId, EntitySnapshot>();
            /// <summary>The last private state sent (encoded): an identical one is not sent again.</summary>
            public byte[] SentState;
        }

        private const string DefaultNamePrefix = "Player";

        private readonly GameServer _server;
        private readonly NetListener _listener;
        private readonly List<Peer> _peers = new List<Peer>();
        private readonly GameConfig _config;
        private int _guestNumber;

        private readonly Persistence.CharacterPersistence _persistence;

        public ServerNetworkHost(GameServer server, int port) : this(server, port, null)
        {
        }

        /// <summary>With persistence players log in to accounts and their characters are saved (module 13); without — guests.</summary>
        public ServerNetworkHost(GameServer server, int port, Persistence.CharacterPersistence persistence)
        {
            _server = server;
            _persistence = persistence;
            if (_persistence != null)
            {
                _persistence.Replaced += OnReplaced;
            }
            _config = server.Context.Content.Config;
            _listener = new NetListener(port);
            _server.Ticked += OnTicked;
            Log.Info(LogTags.Server, $"Listening on port {_listener.Port}");
        }

        public int Port => _listener.Port;

        private long _bytesSentByClosed;
        private long _bytesReceivedByClosed;

        /// <summary>All bytes sent / received since the start, including disconnected players (module 3 metrics).</summary>
        public long BytesSent
        {
            get
            {
                long total = _bytesSentByClosed;
                foreach (Peer peer in _peers)
                {
                    total += peer.Connection.BytesSent;
                }
                return total;
            }
        }

        public long BytesReceived
        {
            get
            {
                long total = _bytesReceivedByClosed;
                foreach (Peer peer in _peers)
                {
                    total += peer.Connection.BytesReceived;
                }
                return total;
            }
        }

        public int ConnectedPlayers
        {
            get
            {
                int count = 0;
                foreach (Peer peer in _peers)
                {
                    if (peer.Joined)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>Accepts connections and applies received messages. Call before every server tick (main thread).</summary>
        public void Poll()
        {
            while (_listener.TryAccept(out NetConnection connection))
            {
                _peers.Add(new Peer { Connection = connection });
                Log.Info(LogTags.Server, $"Connection from {connection.RemoteAddress}");
            }
            for (int i = _peers.Count - 1; i >= 0; i--)
            {
                Peer peer = _peers[i];
                // A flooding client gets MaxCommandsPerTick per tick; the rest waits (NetConnection closes it when too many).
                int taken = 0;
                while (taken++ < _config.MaxCommandsPerTick && peer.Connection.TryReceive(out object message))
                {
                    if (!peer.Joined)
                    {
                        HandleHello(peer, message);
                    }
                    else if (message is IGameCommand command)
                    {
                        _server.Submit(peer.Player, command);
                    }
                }
                if (!peer.Joined && !peer.Connection.IsClosed && (DateTime.UtcNow - peer.Connection.OpenedAt).TotalSeconds > _config.HelloTimeoutSeconds)
                {
                    peer.Connection.Close("no Hello");
                }
                if (peer.Connection.IsClosed)
                {
                    if (peer.Joined)
                    {
                        if (_persistence != null)
                        {
                            _persistence.Leave(peer.Player);
                        }
                        else
                        {
                            _server.RemovePlayer(peer.Player);
                        }
                    }
                    Log.Info(LogTags.Server, $"Disconnected {peer.Connection.RemoteAddress}: {peer.Connection.CloseReason}");
                    _bytesSentByClosed += peer.Connection.BytesSent;
                    _bytesReceivedByClosed += peer.Connection.BytesReceived;
                    _peers.RemoveAt(i);
                }
            }
        }

        public void Dispose()
        {
            _server.Ticked -= OnTicked;
            if (_persistence != null)
            {
                _persistence.Replaced -= OnReplaced;
            }
            _listener.Dispose();
            foreach (Peer peer in _peers)
            {
                if (peer.Joined && _persistence != null)
                {
                    _persistence.Leave(peer.Player);
                }
                peer.Connection.Close("server stopped");
            }
            _peers.Clear();
        }

        /// <summary>The account logged in again: the old connection is closed, its character was already saved and removed.</summary>
        private void OnReplaced(EntityId player)
        {
            foreach (Peer peer in _peers)
            {
                if (peer.Joined && peer.Player == player)
                {
                    peer.Joined = false;
                    peer.Connection.Close(LoginErrors.LoggedElsewhere);
                }
            }
        }

        private void HandleHello(Peer peer, object message)
        {
            if (!(message is HelloMessage hello))
            {
                peer.Connection.Close("expected Hello");
                return;
            }
            if (hello.ProtocolVersion != NetworkConstants.ProtocolVersion)
            {
                Refuse(peer, NetworkConstants.ErrorVersionMismatch);
                return;
            }
            string contentHash = _server.Context.Content.ContentHash;
            if (!string.IsNullOrEmpty(hello.ContentHash) && !string.IsNullOrEmpty(contentHash) && hello.ContentHash != contentHash)
            {
                Refuse(peer, NetworkConstants.ErrorContentMismatch);
                return;
            }
            if (!_server.HasClass(hello.ClassId))
            {
                Refuse(peer, NetworkConstants.ErrorUnknownClass);
                return;
            }
            string token = null;
            if (_persistence != null)
            {
                peer.Player = _persistence.Enter(hello.Login, hello.Password, hello.Token, hello.PlayerName, hello.ClassId, false, out string error, out token);
                if (!peer.Player.IsValid)
                {
                    Refuse(peer, error);
                    return;
                }
            }
            else
            {
                peer.Player = _server.AddPlayer(hello.ClassId, CleanName(hello.PlayerName));
            }
            peer.Joined = true;
            PlayerOutbox joined = _server.GetPlayer(peer.Player).Outbox;
            joined.WorldEveryTicks = Math.Max(1, _config.NetworkWorldEveryTicks);
            joined.StateEveryTicks = Math.Max(1, _config.NetworkStateEveryTicks);
            peer.Connection.Send(new WelcomeMessage
            {
                PlayerId = peer.Player,
                TickRate = _config.TickRate,
                WorldEveryTicks = _config.NetworkWorldEveryTicks,
                Token = token,
            });
        }

        private static void Refuse(Peer peer, string error)
        {
            peer.Connection.Send(new WelcomeMessage { Error = error });
            peer.Connection.CloseAfterFlush(error);
        }

        private string CleanName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0)
            {
                return DefaultNamePrefix + (++_guestNumber);
            }
            return name.Length > _config.MaxPlayerNameLength ? name.Substring(0, _config.MaxPlayerNameLength) : name;
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (b == null || a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private void OnTicked()
        {
            int tick = _server.CurrentTick;
            bool sendWorld = tick % _config.NetworkWorldEveryTicks == 0;
            bool sendState = tick % _config.NetworkStateEveryTicks == 0;
            foreach (Peer peer in _peers)
            {
                if (!peer.Joined || peer.Connection.IsClosed)
                {
                    continue;
                }
                PlayerCharacter player = _server.GetPlayer(peer.Player);
                if (player == null)
                {
                    continue;
                }
                PlayerOutbox outbox = player.Outbox;
                foreach (CombatEventMessage message in outbox.CombatEvents)
                {
                    peer.Connection.Send(message);
                }
                outbox.CombatEvents.Clear();
                foreach (CommandRejectedMessage message in outbox.Rejections)
                {
                    peer.Connection.Send(message);
                }
                outbox.Rejections.Clear();
                foreach (NotificationMessage message in outbox.Notifications)
                {
                    peer.Connection.Send(message);
                }
                outbox.Notifications.Clear();
                foreach (ChatMessage message in outbox.Chat)
                {
                    peer.Connection.Send(message);
                }
                outbox.Chat.Clear();
                foreach (object reply in outbox.Replies)
                {
                    peer.Connection.Send(reply);
                }
                outbox.Replies.Clear();
                if (sendWorld && outbox.World != null)
                {
                    peer.Connection.SendEncoded(ProtocolCodec.EncodeWorld(outbox.World, peer.SentEntities));
                }
                if (sendState && outbox.State != null)
                {
                    // The private state is large (bag, equipment, skills) and mostly idle: resend only when something changed.
                    byte[] state = ProtocolCodec.Encode(outbox.State);
                    if (!SameBytes(state, peer.SentState))
                    {
                        peer.SentState = state;
                        peer.Connection.SendEncoded(state);
                    }
                }
            }
        }
    }
}
