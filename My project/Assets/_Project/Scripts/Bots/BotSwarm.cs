using System;
using System.Collections.Generic;
using Game.Network;

namespace Game.Bots
{
    /// <summary>
    /// A pack of bots for load tests (module 3): connects N bots to a server (classes alternate, names Bot001…),
    /// updates them and reports how many are alive and what they received.
    /// </summary>
    public sealed class BotSwarm : IDisposable
    {
        private const string NamePrefix = "Bot";

        private readonly List<BotClient> _bots = new List<BotClient>();
        private readonly List<NetworkClientSession> _sessions = new List<NetworkClientSession>();

        public static BotSwarm Connect(string host, int port, int count, string[] classIds, float worldHalfSize)
        {
            var swarm = new BotSwarm();
            for (int i = 0; i < count; i++)
            {
                string classId = classIds[i % classIds.Length];
                NetworkClientSession session = NetworkClientSession.Connect(host, port, NamePrefix + (i + 1).ToString("000"), classId);
                swarm._sessions.Add(session);
                swarm._bots.Add(new BotClient(session, i + 1, worldHalfSize));
            }
            return swarm;
        }

        public IReadOnlyList<BotClient> Bots => _bots;

        public int Connected
        {
            get
            {
                int count = 0;
                foreach (BotClient bot in _bots)
                {
                    count += bot.IsClosed ? 0 : 1;
                }
                return count;
            }
        }

        public long BytesReceived
        {
            get
            {
                long total = 0;
                foreach (NetworkClientSession session in _sessions)
                {
                    total += session.BytesReceived;
                }
                return total;
            }
        }

        public void Update(float dt)
        {
            foreach (BotClient bot in _bots)
            {
                bot.Update(dt);
            }
        }

        public void Dispose()
        {
            foreach (BotClient bot in _bots)
            {
                bot.Dispose();
            }
            _bots.Clear();
        }
    }
}
