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
            Assert.That(world.Entities.Length, Is.EqualTo(h.Player(mage).Outbox.World.Entities.Length));
            Assert.That(world.Entities.Any(e => e.Id == mage), Is.True, "a player always sees themselves");
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
        public void UnchangedData_IsNotResent_ButTheClientStillSeesItAll()
        {
            var h = new ServerHarness();
            using (var host = new ServerNetworkHost(h.Server, 0))
            using (NetworkClientSession alice = NetworkClientSession.Connect("127.0.0.1", host.Port, "Alice", "mage"))
            {
                WorldSnapshot world = null;
                int states = 0;
                Pump(h, host, () =>
                {
                    while (alice.TryReceive(out object message))
                    {
                        world = message as WorldSnapshot ?? world;
                        states += message is PlayerStateSnapshot ? 1 : 0;
                    }
                    return world != null && world.Tick >= 60;
                });
                // Many snapshots later a mob that never moved still has its name, place and health on the client.
                EntitySnapshot mob = world.Entities.First(e => e.Kind == EntityKind.Mob);
                Assert.That(mob.DefinitionId, Is.Not.Null);
                Assert.That(mob.MaxHealth, Is.GreaterThan(0f));
                Assert.That(mob.Position, Is.EqualTo(h.World.Transforms.Get(mob.Id).Position));
                Assert.That(world.Entities.Any(e => e.DynamicOmitted), Is.False, "the session completes every entity");
                // An idle player's private state does not change: it is sent once, not every third tick.
                Assert.That(states, Is.LessThan(5));
                long before = host.BytesSent;
                Pump(h, host, () => { while (alice.TryReceive(out _)) { } return h.Server.CurrentTick >= 120; });
                Assert.That(host.BytesSent - before, Is.LessThan(30 * 400), "an idle world costs a few bytes per entity, not a full snapshot");
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

        [Test]
        public void Duel_OverTheNetwork_RequestAcceptCountdownAndHit()
        {
            var h = new ServerHarness();
            using (var host = new ServerNetworkHost(h.Server, 0))
            using (NetworkClientSession alice = NetworkClientSession.Connect("127.0.0.1", host.Port, "Alice", "mage"))
            using (NetworkClientSession bob = NetworkClientSession.Connect("127.0.0.1", host.Port, "Bob", "warrior"))
            {
                var aliceView = new ClientView();
                var bobView = new ClientView();
                Pump(h, host, () => aliceView.Drain(alice) & bobView.Drain(bob) && alice.IsWelcomed && bob.IsWelcomed);

                alice.Send(new DuelRequestCommand { TargetId = bob.PlayerId });
                Pump(h, host, () => aliceView.Drain(alice) & bobView.Drain(bob) && bobView.Phase == DuelPhase.IncomingRequest);
                Assert.That(bobView.State.Duel.OpponentId, Is.EqualTo(alice.PlayerId));
                Assert.That(bobView.Notifications, Does.Contain(NotificationKeys.DuelRequested));

                bob.Send(new DuelResponseCommand { Accept = true });
                Pump(h, host, () => aliceView.Drain(alice) & bobView.Drain(bob) && aliceView.Phase == DuelPhase.Active && bobView.Phase == DuelPhase.Active);
                Assert.That(aliceView.Notifications, Does.Contain(NotificationKeys.DuelStarted));

                alice.Send(new UseSkillCommand { SkillId = "reserve_nova", TargetId = bob.PlayerId });
                Pump(h, host, () => aliceView.Drain(alice) & bobView.Drain(bob)
                    && bobView.Events.Any(e => e.Kind == CombatEventKind.Damage && e.TargetId == bob.PlayerId && e.SourceId == alice.PlayerId));
            }
        }

        /// <summary>What a test client has received: latest private state, notifications and combat events.</summary>
        private sealed class ClientView
        {
            public PlayerStateSnapshot State;
            public readonly System.Collections.Generic.List<string> Notifications = new System.Collections.Generic.List<string>();
            public readonly System.Collections.Generic.List<CombatEventMessage> Events = new System.Collections.Generic.List<CombatEventMessage>();

            public DuelPhase Phase => State != null ? State.Duel.Phase : DuelPhase.None;

            /// <summary>Always true: lets several clients drain in one condition with the non-short-circuit &amp;.</summary>
            public bool Drain(NetworkClientSession session)
            {
                while (session.TryReceive(out object message))
                {
                    switch (message)
                    {
                        case PlayerStateSnapshot state:
                            State = state;
                            break;
                        case NotificationMessage notification:
                            Notifications.Add(notification.TextKey);
                            break;
                        case CombatEventMessage combat:
                            Events.Add(combat);
                            break;
                    }
                }
                return true;
            }
        }

        public static void Pump(ServerHarness h, ServerNetworkHost host, Func<bool> done)
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
