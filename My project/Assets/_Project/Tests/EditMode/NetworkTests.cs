using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Game.Network;
using Game.Server.Net;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Own TCP transport (question 69) over real loopback sockets: module 1 criterion "two clients connect and see each other".</summary>
    public sealed class NetworkTests
    {
        private const int TimeoutMs = 5000;

        [Test]
        public void Codec_RoundTripsPlayerStateAndWorld()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            h.Server.Tick();
            PlayerStateSnapshot state = h.Player(mage).Outbox.State;

            var decoded = (PlayerStateSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(state));
            var world = (WorldSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(h.Player(mage).Outbox.World));

            Assert.That(decoded.ClassId, Is.EqualTo("mage"));
            Assert.That(decoded.MaxMainMana, Is.EqualTo(state.MaxMainMana));
            Assert.That(decoded.Bag.Length, Is.EqualTo(state.Bag.Length));
            Assert.That(decoded.Bag.Count(b => b.DefinitionId != null), Is.EqualTo(state.Bag.Count(b => b.DefinitionId != null)));
            Assert.That(decoded.Equipment.First(e => e.SlotId == "MainHand").InstanceId, Is.EqualTo(state.Equipment.First(e => e.SlotId == "MainHand").InstanceId));
            Assert.That(world.Entities.Length, Is.EqualTo(h.World.Entities.Count));
        }

        [Test]
        public void TwoClients_Connect_SeeEachOther_AndMovementIsShared()
        {
            var h = new ServerHarness();
            using (var host = new ServerNetworkHost(h.Server, 0))
            using (NetworkClientSession alice = NetworkClientSession.Connect("127.0.0.1", host.Port, "Alice", "mage"))
            using (NetworkClientSession bob = NetworkClientSession.Connect("127.0.0.1", host.Port, "Bob", "warrior"))
            {
                WorldSnapshot aliceWorld = null;
                WorldSnapshot bobWorld = null;
                Pump(h, host, () =>
                {
                    aliceWorld = Latest(alice) ?? aliceWorld;
                    bobWorld = Latest(bob) ?? bobWorld;
                    return alice.IsWelcomed && bob.IsWelcomed && Players(aliceWorld) == 2 && Players(bobWorld) == 2;
                });
                Assert.That(aliceWorld.Entities.Any(e => e.DisplayName == "Bob"), Is.True);
                Assert.That(bobWorld.Entities.Any(e => e.DisplayName == "Alice"), Is.True);

                float startX = bobWorld.Entities.First(e => e.Id == alice.PlayerId).Position.X;
                alice.Send(new MoveCommand { DirX = 1f, DirZ = 0f });
                Pump(h, host, () =>
                {
                    bobWorld = Latest(bob) ?? bobWorld;
                    return bobWorld.Entities.First(e => e.Id == alice.PlayerId).Position.X > startX + 1f;
                });
            }
        }

        [Test]
        public void Disconnect_RemovesPlayer_AndWrongClassIsRefused()
        {
            var h = new ServerHarness();
            using (var host = new ServerNetworkHost(h.Server, 0))
            {
                NetworkClientSession alice = NetworkClientSession.Connect("127.0.0.1", host.Port, "Alice", "mage");
                Pump(h, host, () => { Latest(alice); return alice.IsWelcomed; });
                Assert.That(host.ConnectedPlayers, Is.EqualTo(1));

                alice.Dispose();
                Pump(h, host, () => host.ConnectedPlayers == 0);
                Assert.That(h.Server.Context.Players.Count, Is.EqualTo(0));

                using (NetworkClientSession stranger = NetworkClientSession.Connect("127.0.0.1", host.Port, "X", "necromancer"))
                {
                    Pump(h, host, () => { Latest(stranger); return stranger.Error != null; });
                    Assert.That(stranger.Error, Is.EqualTo(NetworkConstants.ErrorUnknownClass));
                }
            }
        }

        private static void Pump(ServerHarness h, ServerNetworkHost host, Func<bool> done)
        {
            var clock = Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < TimeoutMs)
            {
                host.Poll();
                h.Server.Tick();
                if (done())
                {
                    return;
                }
                Thread.Sleep(2);
            }
            Assert.Fail("Timed out waiting for the network");
        }

        /// <summary>Drains the session and returns the newest world snapshot received, if any.</summary>
        private static WorldSnapshot Latest(NetworkClientSession session)
        {
            WorldSnapshot latest = null;
            while (session.TryReceive(out object message))
            {
                if (message is WorldSnapshot world)
                {
                    latest = world;
                }
            }
            return latest;
        }

        private static int Players(WorldSnapshot world) => world?.Entities.Count(e => e.Kind == EntityKind.Player) ?? 0;
    }
}
