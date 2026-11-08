using System;
using System.IO;
using System.Linq;
using Game.Network;
using Game.Server.Items;
using Game.Server.Persistence;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Regressions of the project review: item duplication through a login spelled in another case, a trade giving less than
    /// confirmed, a trade or market operation half-saved before a stop, unbound items turning untradable after a restart.
    /// </summary>
    public sealed class IntegrityTests
    {
        private string _root;

        [SetUp]
        public void SetUp() => _root = Path.Combine(Path.GetTempPath(), "game-integrity-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private FileStorage Storage() => new FileStorage(_root, new TestJsonCodec());

        private static CharacterPersistence Persistence(ServerHarness h, FileStorage storage)
        {
            h.Server.Context.Items.UniqueRegistry = storage;
            return new CharacterPersistence(h.Server, storage, storage, storage);
        }

        private static void OpenTrade(ServerHarness h, EntityId a, EntityId b)
        {
            h.Send(a, new TradeRequestCommand { TargetId = b });
            h.Send(b, new TradeRespondCommand { Accept = true });
            Assert.That(h.Player(a).Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.Open));
        }

        [Test]
        public void Login_InAnotherCase_IsTheSameAccount_AndReplacesTheSession()
        {
            var h = new ServerHarness();
            CharacterPersistence persistence = Persistence(h, Storage());
            EntityId first = persistence.Enter("Alice", "secret1", null, "Алиса", "mage", false, out string error, out _);
            Assert.That(error, Is.Null);

            EntityId second = persistence.Enter("alice", "secret1", null, null, "mage", false, out error, out _);
            Assert.That(error, Is.Null);
            Assert.That(h.Server.GetPlayer(first), Is.Null, "the same character must never be in the world twice");
            Assert.That(persistence.Online, Is.EqualTo(1));
            Assert.That(h.World.Identities.Get(second).DisplayName, Is.EqualTo("Алиса"));
        }

        [Test]
        public void Login_WithABadNewName_DoesNotKickTheLiveSession()
        {
            var h = new ServerHarness();
            CharacterPersistence persistence = Persistence(h, Storage());
            EntityId mage = persistence.Enter("alice", "secret1", null, "Алиса", "mage", false, out _, out _);

            persistence.Enter("alice", "secret1", null, "!", "warrior", false, out string error, out _);
            Assert.That(error, Is.EqualTo(LoginErrors.BadName));
            Assert.That(h.Server.GetPlayer(mage), Is.Not.Null, "a refused login leaves the online character alone");
        }

        [Test]
        public void Trade_StackSplitInTheConfirmTick_DoesNotGiveLessThanConfirmed()
        {
            var h = new ServerHarness();
            EntityId a = h.AddPlayer("warrior");
            EntityId b = h.AddPlayer("mage");
            OpenTrade(h, a, b);
            int shards = h.BagIndexOf(a, "philosopher_stone_shard");
            ItemInstance stack = h.Player(a).Bag.Get(shards);
            int confirmed = stack.Count;
            h.Send(a, new TradeOfferCommand { InstanceIds = new[] { stack.SourceId } });
            h.Send(b, new TradeConfirmCommand());

            // Split and confirm in one tick: Update has not seen the smaller stack yet.
            BagSection resources = h.Content.BagSections.First(section => section.Id == "resources");
            int free = h.Player(a).Bag.FindFreeSlot(resources.Start, resources.Size);
            h.Server.Submit(a, new MoveBagItemCommand { FromIndex = shards, ToIndex = free, Count = confirmed - 1 });
            h.Server.Submit(a, new TradeConfirmCommand());
            h.Server.Tick();

            Assert.That(h.Player(a).Bag.IndexOf(stack), Is.GreaterThanOrEqualTo(0), "nothing was swapped");
            Assert.That(h.Player(a).Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.Open));
            Assert.That(h.Player(b).Outbox.State.Trade.MyConfirmed, Is.False, "the other side confirms the new offer again");
            Assert.That(h.Player(b).Outbox.State.Trade.TheirItems.Single().Count, Is.EqualTo(1));
        }

        [Test]
        public void Trade_SavesBothSidesAtOnce()
        {
            var h = new ServerHarness();
            FileStorage storage = Storage();
            CharacterPersistence persistence = Persistence(h, storage);
            EntityId a = persistence.Enter("ann", "secret1", null, "Аня", "warrior", false, out _, out _);
            EntityId b = persistence.Enter("bob", "secret1", null, "Боб", "mage", false, out _, out _);
            OpenTrade(h, a, b);
            string axe = h.Player(a).Bag.Get(h.BagIndexOf(a, "great_axe")).SourceId;
            h.Send(a, new TradeOfferCommand { InstanceIds = new[] { axe } });
            h.Send(a, new TradeConfirmCommand());
            h.Send(b, new TradeConfirmCommand());

            // No logout, no autosave: the files already show the trade (a forced stop now cannot bring the axe back).
            Assert.That(storage.LoadCharacter("ann", "warrior").Bag.Any(item => item.InstanceId == axe), Is.False);
            Assert.That(storage.LoadCharacter("bob", "mage").Bag.Any(item => item.InstanceId == axe), Is.True);
            Assert.That(File.Exists(Path.Combine(_root, "commit.json")), Is.False, "the journal is removed after the commit");
        }

        [Test]
        public void InterruptedCommit_IsFinishedOnStart()
        {
            var h = new ServerHarness();
            FileStorage storage = Storage();
            CharacterPersistence persistence = Persistence(h, storage);
            EntityId a = persistence.Enter("ann", "secret1", null, "Аня", "warrior", false, out _, out _);
            CharacterRecord record = h.Server.ExportPlayer(a);
            record.AccountLogin = "ann";
            record.Level = 7;

            // A crash right after the journal was written: the character file still has the old version.
            var codec = new TestJsonCodec();
            File.WriteAllText(Path.Combine(_root, "commit.json"), codec.ToJson(new CommitRecord { Characters = new[] { record }, HasMarket = false }));
            FileStorage restarted = Storage();
            Assert.That(restarted.LoadCharacter("ann", "warrior").Level, Is.EqualTo(7));
            Assert.That(File.Exists(Path.Combine(_root, "commit.json")), Is.False);
        }

        [Test]
        public void RestoredItems_StayUnbound_AndTradable()
        {
            var h = new ServerHarness();
            CharacterPersistence persistence = Persistence(h, Storage());
            EntityId a = persistence.Enter("ann", "secret1", null, "Аня", "warrior", false, out _, out _);
            persistence.Leave(a);

            var restarted = new ServerHarness();
            CharacterPersistence again = Persistence(restarted, Storage());
            EntityId back = again.Enter("ann", "secret1", null, null, "warrior", false, out _, out _);
            EntityId other = restarted.AddPlayer("mage");
            Assert.That(Enumerable.Range(0, restarted.Player(back).Bag.Size).Select(i => restarted.Player(back).Bag.Get(i)).Where(i => i != null)
                .Any(i => i.BoundTo == string.Empty), Is.False, "JsonUtility gives \"\" for null; an empty binding is no binding");
            OpenTrade(restarted, back, other);
            string axe = restarted.Player(back).Bag.Get(restarted.BagIndexOf(back, "great_axe")).SourceId;
            restarted.Send(back, new TradeOfferCommand { InstanceIds = new[] { axe } });
            Assert.That(restarted.Rejections(back), Does.Not.Contain(RejectReasons.TradeItemNotTradable));
        }

        [Test]
        public void ForgedNaNMove_IsIgnored()
        {
            var h = new ServerHarness();
            EntityId a = h.AddPlayer("warrior");
            Vec3 before = h.World.Transforms.Get(a).Position;
            h.Send(a, new MoveCommand { DirX = float.NaN, DirZ = float.PositiveInfinity });
            h.Run(1f);
            Vec3 after = h.World.Transforms.Get(a).Position;
            Assert.That(float.IsNaN(after.X) || float.IsNaN(after.Z), Is.False);
            Assert.That(after.X, Is.EqualTo(before.X));
        }
    }
}
