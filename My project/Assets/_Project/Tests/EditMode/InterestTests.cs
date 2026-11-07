using System.Linq;
using Game.Server.Systems;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 3: interest management by grid — players get nearby entities only (plus target, group, duel), events too.</summary>
    public sealed class InterestTests
    {
        private static bool Sees(ServerHarness h, EntityId viewer, EntityId other) =>
            h.Player(viewer).Outbox.World.Entities.Any(e => e.Id == other);

        [Test]
        public void FarEntities_AreNotSent_NearOnesAre()
        {
            var h = new ServerHarness();
            EntityId a = h.AddPlayer("mage");
            EntityId b = h.AddPlayer("mage");
            float radius = h.Content.Config.InterestRadius;
            h.World.Transforms.Get(a).Position = new Vec3(-radius, 0f, -radius);
            h.World.Transforms.Get(b).Position = new Vec3(radius, 0f, radius);
            h.Server.Tick();
            Assert.That(Sees(h, a, b), Is.False);

            h.World.Transforms.Get(b).Position = new Vec3(-radius + 5f, 0f, -radius);
            h.Server.Tick();
            Assert.That(Sees(h, a, b), Is.True);
        }

        [Test]
        public void TargetAndGroupMembers_AreAlwaysSent()
        {
            var h = new ServerHarness();
            EntityId a = h.AddPlayer("mage");
            EntityId b = h.AddPlayer("mage");
            float radius = h.Content.Config.InterestRadius;
            h.World.Transforms.Get(a).Position = new Vec3(-radius, 0f, -radius);
            h.World.Transforms.Get(b).Position = new Vec3(radius, 0f, radius);
            h.Send(a, new GroupInviteCommand { TargetId = b });
            h.Send(b, new GroupInviteResponseCommand { Accept = true });
            h.Server.Tick();
            Assert.That(Sees(h, a, b), Is.True, "group frames need far members");
        }

        [Test]
        public void CombatEvents_GoOnlyToThoseWhoSee()
        {
            var h = new ServerHarness();
            EntityId fighter = h.AddPlayer("mage");
            EntityId far = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(fighter, "training_dummy");
            h.PlaceNear(fighter, dummy, 10f);
            h.World.Transforms.Get(far).Position = new Vec3(h.World.Transforms.Get(dummy).Position.X + h.Content.Config.InterestRadius + 15f, 0f, 0f);
            h.Send(fighter, new UseSkillCommand { SkillId = "reserve_nova", TargetId = dummy });
            Assert.That(h.Player(fighter).Outbox.CombatEvents.Any(e => e.TargetId == dummy), Is.True);
            Assert.That(h.Player(far).Outbox.CombatEvents.Any(e => e.TargetId == dummy), Is.False);
        }

        [Test]
        public void SpreadPlayers_ReceiveFarFewerEntities_ThanExist()
        {
            var h = new ServerHarness();
            float half = h.Content.Config.WorldHalfSize;
            const int side = 7;
            for (int x = 0; x < side; x++)
            {
                for (int z = 0; z < side; z++)
                {
                    EntityId id = h.AddPlayer("mage");
                    h.World.Transforms.Get(id).Position = new Vec3(-half + (x + 0.5f) * 2f * half / side, 0f, -half + (z + 0.5f) * 2f * half / side);
                }
            }
            h.Server.Tick();
            ServerMetricsCheck(h);
        }

        [Test]
        public void WorldEncoding_SendsStaticFieldsOnce_PerPeer()
        {
            var h = new ServerHarness();
            EntityId a = h.AddPlayer("mage");
            h.Server.Tick();
            WorldSnapshot world = h.Player(a).Outbox.World;
            var known = new System.Collections.Generic.HashSet<EntityId>();
            byte[] first = Game.Network.ProtocolCodec.EncodeWorld(world, known);
            byte[] second = Game.Network.ProtocolCodec.EncodeWorld(world, known);
            Assert.That(second.Length, Is.LessThan(first.Length));
            var compact = (WorldSnapshot)Game.Network.ProtocolCodec.Decode(second);
            Assert.That(compact.Entities.All(e => e.DefinitionId == null), Is.True, "static fields were already sent");
            Assert.That(known.Count, Is.EqualTo(world.Entities.Length));
        }

        private static void ServerMetricsCheck(ServerHarness h)
        {
            var metrics = h.Server.Metrics;
            Assert.That(metrics.Players, Is.EqualTo(49));
            Assert.That(metrics.EntitiesPerPlayer, Is.LessThan(metrics.Entities * 0.6), "each player sees only its area");
            Assert.That(metrics.AverageTickMs, Is.GreaterThan(0d));
        }
    }
}
