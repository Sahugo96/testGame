using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Game.Network
{
    /// <summary>Accepts TCP connections on a port in a background thread; the game loop takes them from <see cref="TryAccept"/>.</summary>
    public sealed class NetListener : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentQueue<NetConnection> _accepted = new ConcurrentQueue<NetConnection>();
        private volatile bool _stopped;

        public NetListener(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            new Thread(AcceptLoop) { IsBackground = true, Name = "NetAccept" }.Start();
        }

        /// <summary>Actual port (useful when 0 was requested).</summary>
        public int Port { get; }

        public bool TryAccept(out NetConnection connection) => _accepted.TryDequeue(out connection);

        public void Dispose()
        {
            _stopped = true;
            _listener.Stop();
        }

        private void AcceptLoop()
        {
            while (!_stopped)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    _accepted.Enqueue(new NetConnection(client));
                }
                catch (Exception)
                {
                    if (_stopped)
                    {
                        return;
                    }
                }
            }
        }
    }
}
