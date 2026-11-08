using System;
using System.Net.Sockets;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Network
{
    /// <summary>Client side of a network game: connects, says Hello, remembers Welcome, passes messages through.</summary>
    public sealed class NetworkClientSession : IDisposable
    {
        private readonly NetConnection _connection;

        private NetworkClientSession(NetConnection connection) => _connection = connection;

        public EntityId PlayerId { get; private set; }
        public bool IsWelcomed { get; private set; }
        public int TickRate { get; private set; }
        public int WorldEveryTicks { get; private set; }

        /// <summary>Error key from the server (version mismatch, unknown class) or the transport close reason.</summary>
        public string Error { get; private set; }

        /// <summary>Session token from the server's Welcome (module 13).</summary>
        public string Token { get; private set; }

        public bool IsClosed => _connection.IsClosed;

        public string CloseReason => _connection.CloseReason;

        /// <summary>Bytes received from the server (load test metrics).</summary>
        public long BytesReceived => _connection.BytesReceived;

        /// <summary>Connects (blocks up to ConnectTimeoutMs) and sends Hello. Throws on failure.</summary>
        public static NetworkClientSession Connect(string host, int port, string playerName, string classId) =>
            Connect(host, port, playerName, classId, playerName, null, null);

        /// <summary>Connects with an account (module 13): login + password, or a session token from a previous Welcome.</summary>
        public static NetworkClientSession Connect(string host, int port, string playerName, string classId, string login, string password, string token) =>
            Connect(host, port, playerName, classId, login, password, token, null);

        /// <summary>With the hash of the client's content (module 4): the server refuses a client with other data.</summary>
        public static NetworkClientSession Connect(string host, int port, string playerName, string classId, string login, string password, string token, string contentHash)
        {
            var client = new TcpClient();
            if (!client.ConnectAsync(host, port).Wait(NetworkConstants.ConnectTimeoutMs) || !client.Connected)
            {
                client.Close();
                throw new TimeoutException($"No answer from {host}:{port}");
            }
            var session = new NetworkClientSession(new NetConnection(client));
            session._connection.Send(new HelloMessage
            {
                ProtocolVersion = NetworkConstants.ProtocolVersion, PlayerName = playerName, ClassId = classId, Login = login, Password = password, Token = token,
                ContentHash = contentHash,
            });
            return session;
        }

        public void Send(IGameCommand command) => _connection.Send(command);

        /// <summary>Next message from the server; Welcome is also remembered here.</summary>
        public bool TryReceive(out object message)
        {
            if (!_connection.TryReceive(out message))
            {
                return false;
            }
            if (message is WelcomeMessage welcome)
            {
                if (!string.IsNullOrEmpty(welcome.Error))
                {
                    Error = welcome.Error;
                    _connection.Close(welcome.Error);
                }
                else
                {
                    PlayerId = welcome.PlayerId;
                    Token = welcome.Token;
                    TickRate = welcome.TickRate;
                    WorldEveryTicks = welcome.WorldEveryTicks;
                    IsWelcomed = true;
                }
            }
            return true;
        }

        public void Dispose() => _connection.Close("client left");
    }
}
