using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Game.Bots;
using Game.Server.Net;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 3: headless bots connect over real sockets, walk around and fight like players.</summary>
    public sealed class BotTests
    {
        [Test]
        public void Bots_Connect_Walk_AndUseSkills()
        {
            var h = new ServerHarness();
            using (var host = new ServerNetworkHost(h.Server, 0))
            using (BotSwarm swarm = BotSwarm.Connect("127.0.0.1", host.Port, 10, new[] { "mage", "warrior" }, h.Content.Config.WorldHalfSize))
            {
                var clock = Stopwatch.StartNew();
                float simulated = 0f;
                while (clock.ElapsedMilliseconds < 8000 && simulated < 6f)
                {
                    host.Poll();
                    h.Server.Tick();
                    swarm.Update(h.Server.TickInterval);
                    simulated += h.Server.TickInterval;
                    Thread.Sleep(1);
                }
                Assert.That(host.ConnectedPlayers, Is.EqualTo(10));
                Assert.That(swarm.Connected, Is.EqualTo(10));
                Assert.That(swarm.Bots.Count(b => b.State != null), Is.EqualTo(10), "everyone got their state");
                float moved = h.Server.Context.Players.Values.Max(p => Math.Abs(h.World.Transforms.Get(p.EntityId).Position.X)
                    + Math.Abs(h.World.Transforms.Get(p.EntityId).Position.Z));
                Assert.That(moved, Is.GreaterThan(1f), "bots walk");
                Assert.That(swarm.Bots.Sum(b => b.SkillsUsed), Is.GreaterThan(0), "bots fight");
                Assert.That(host.BytesSent, Is.GreaterThan(0));
            }
        }
    }
}
