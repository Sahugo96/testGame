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

        public bool IsClosed => _connection.IsClosed;

        public string CloseReason => _connection.CloseReason;

        /// <summary>Connects (blocks up to ConnectTimeoutMs) and sends Hello. Throws on failure.</summary>
        public static NetworkClientSession Connect(string host, int port, string playerName, string classId)
        {
            var client = new TcpClient();
            if (!client.ConnectAsync(host, port).Wait(NetworkConstants.ConnectTimeoutMs) || !client.Connected)
            {
                client.Close();
                throw new TimeoutException($"No answer from {host}:{port}");
            }
            var session = new NetworkClientSession(new NetConnection(client));
            session._connection.Send(new HelloMessage { ProtocolVersion = NetworkConstants.ProtocolVersion, PlayerName = playerName, ClassId = classId });
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
