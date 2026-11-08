using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game.Network;
using Game.Server;
using Game.Server.Items;
using Game.Server.Net;
using Game.Server.Persistence;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 13: accounts, login, saved characters surviving a restart, migrations, backups, unique items, audit.</summary>
    public sealed class PersistenceTests
    {
        private string _root;

        [SetUp]
        public void SetUp() => _root = Path.Combine(Path.GetTempPath(), "game-saves-" + Guid.NewGuid().ToString("N"));

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
            return new CharacterPersistence(h.Server, storage, storage);
        }

        [Test]
        public void FriendRequest_ReachesAnOfflineAccount_AndFriendshipIsSaved()
        {
            var h = new ServerHarness();
            FileStorage storage = Storage();
            CharacterPersistence persistence = Persistence(h, storage);
            EntityId bob = persistence.Enter("bob", "secret1", null, "Боб", "warrior", false, out string error, out _);
            Assert.That(error, Is.Null);
            persistence.Leave(bob);

            EntityId ann = persistence.Enter("ann", "secret1", null, "Аня", "mage", false, out error, out _);
            h.Send(ann, new FriendAddCommand { Name = "боб" });
            Assert.That(h.Player(ann).Outbox.State.Friends.Single().State, Is.EqualTo(FriendState.Outgoing), "Боб is offline: the request waits in his account");

            bob = persistence.Enter("bob", "secret1", null, null, "warrior", false, out error, out _);
            h.Run(h.Server.TickInterval);
            FriendSnapshot request = h.Player(bob).Outbox.State.Friends.Single();
            Assert.That((request.Name, request.State, request.Online), Is.EqualTo(("Аня", FriendState.Incoming, true)));
            h.Send(bob, new FriendRespondCommand { Name = "Аня", Accept = true });
            Assert.That(h.Player(ann).Outbox.State.Friends.Single().State, Is.EqualTo(FriendState.Friend));
            persistence.SaveAll();

            var restarted = new ServerHarness();
            CharacterPersistence again = Persistence(restarted, Storage());
            EntityId back = again.Enter("ann", "secret1", null, null, "mage", false, out error, out _);
            restarted.Run(restarted.Server.TickInterval);
            FriendSnapshot friend = restarted.Player(back).Outbox.State.Friends.Single();
            Assert.That((friend.Name, friend.State, friend.Online), Is.EqualTo(("Боб", FriendState.Friend, false)));

            // A friend's other character is the same account: no second friendship with it.
            EntityId bobMage = again.Enter("bob", "secret1", null, "БобМаг", "mage", false, out error, out _);
            restarted.Send(back, new FriendAddCommand { Name = "БобМаг" });
            Assert.That(restarted.Rejections(back), Does.Contain(RejectReasons.FriendAlready));
            Assert.That(bobMage.IsValid, Is.True);
        }

        [Test]
        public void Character_SurvivesServerRestart()
        {
            var h = new ServerHarness();
            FileStorage storage = Storage();
            CharacterPersistence persistence = Persistence(h, storage);
            EntityId id = persistence.Enter("alice", "secret1", null, "Алиса", "warrior", false, out string error, out string token);
            Assert.That(error, Is.Null);
            Assert.That(token, Is.Not.Null.And.Not.Empty);

            var player = h.Player(id);
            player.Level = 7;
            player.Experience = 123;
            player.AttributePoints = 4;
            player.Attributes[StatId.Strength] = 42f;
            player.Mastery["power_strike"] = 0.77f;
            ItemInstance sword = player.Equipment["MainHand"];
            sword.UpgradeLevel = 5;
            int shardIndex = h.BagIndexOf(id, "philosopher_stone_shard");
            int shards = player.Bag.Get(shardIndex).Count;
            h.World.Transforms.Get(id).Position = new Vec3(12f, 0f, -7f);
            h.Server.Statuses.Apply(id, id, "iron_skin", 1f);
            h.Run(1f);
            float statusLeft = h.Server.Statuses.Find(id, "iron_skin").Remaining;
            persistence.Leave(id);

            // A new server process over the same save folder.
            var restarted = new ServerHarness();
            CharacterPersistence again = Persistence(restarted, Storage());
            EntityId back = again.Enter("alice", "secret1", null, "ignored", "warrior", false, out error, out _);
            Assert.That(error, Is.Null);
            var restored = restarted.Player(back);
            Assert.That(restored.Level, Is.EqualTo(7));
            Assert.That(restored.Experience, Is.EqualTo(123));
            Assert.That(restored.AttributePoints, Is.EqualTo(4));
            Assert.That(restored.Attributes[StatId.Strength], Is.EqualTo(42f));
            Assert.That(restored.Mastery["power_strike"], Is.EqualTo(0.77f).Within(0.0001f));
            Assert.That(restored.Equipment["MainHand"].SourceId, Is.EqualTo(sword.SourceId), "the same item instance (GUID)");
            Assert.That(restored.Equipment["MainHand"].UpgradeLevel, Is.EqualTo(5));
            Assert.That(restored.Bag.Get(shardIndex).Count, Is.EqualTo(shards));
            Assert.That(restarted.World.Transforms.Get(back).Position.X, Is.EqualTo(12f));
            Assert.That(restarted.World.Identities.Get(back).DisplayName, Is.EqualTo("Алиса"));
            Assert.That(restarted.Server.Statuses.Find(back, "iron_skin").Remaining, Is.EqualTo(statusLeft).Within(0.001f), "status time ran only online");
        }

        [Test]
        public void Login_WrongPassword_Token_NameTaken_SecondLoginReplaces()
        {
            var h = new ServerHarness();
            CharacterPersistence persistence = Persistence(h, Storage());
            EntityId alice = persistence.Enter("alice", "secret1", null, "Алиса", "mage", false, out _, out string token);

            EntityId again = persistence.Enter("alice", "secret1", null, "Алиса", "mage", false, out string error, out _);
            Assert.That(error, Is.Null);
            Assert.That(h.Server.GetPlayer(alice), Is.Null, "the second login replaced the first session");
            persistence.Leave(again);

            persistence.Enter("alice", "wrong", null, "x", "mage", false, out error, out _);
            Assert.That(error, Is.EqualTo(LoginErrors.WrongPassword));

            EntityId byToken = persistence.Enter("alice", null, token, null, "mage", false, out error, out _);
            Assert.That(error, Is.Null, "the session token replaces the password");
            persistence.Leave(byToken);

            persistence.Enter("bob", "secret2", null, "алиса", "mage", false, out error, out _);
            Assert.That(error, Is.EqualTo(LoginErrors.NameTaken), "names are unique per world, case-insensitive");

            persistence.Enter("b", "secret2", null, "Боб", "mage", false, out error, out _);
            Assert.That(error, Is.EqualTo(LoginErrors.BadLogin));
        }

        [Test]
        public void AutoLootSetting_IsSavedWithTheAccount()
        {
            var h = new ServerHarness();
            FileStorage storage = Storage();
            CharacterPersistence persistence = Persistence(h, storage);
            EntityId id = persistence.Enter("gina", "secret1", null, "Джина", "mage", false, out _, out _);
            h.Send(id, new SetAutoLootCommand { Enabled = false });
            persistence.Leave(id);

            var restarted = new ServerHarness();
            EntityId back = Persistence(restarted, Storage()).Enter("gina", "secret1", null, "Джина", "warrior", false, out _, out _);
            Assert.That(restarted.Player(back).AutoLoot, Is.False, "an account setting: the same for every character");
        }

        [Test]
        public void Account_StoresOnlyASaltedHash()
        {
            var h = new ServerHarness();
            FileStorage storage = Storage();
            Persistence(h, storage).Enter("carol", "my-password", null, "Кэрол", "mage", false, out _, out _);
            string text = File.ReadAllText(Directory.GetFiles(Path.Combine(_root, "accounts")).Single());
            Assert.That(text, Does.Not.Contain("my-password"));
            AccountRecord account = storage.LoadAccount("carol");
            Assert.That(PasswordHasher.Verify("my-password", account.PasswordHash, account.PasswordSalt, account.PasswordIterations), Is.True);
        }

        [Test]
        public void OldSave_IsMigrated()
        {
            FileStorage storage = Storage();
            var old = new CharacterRecord { SchemaVersion = 0, AccountLogin = "dave", ClassId = "mage", DisplayName = "Дейв", CharacterId = "c1", Level = 3 };
            storage.SaveCharacter(old);
            string path = Directory.GetFiles(Path.Combine(_root, "characters"), "*.json", SearchOption.AllDirectories).Single();
            File.WriteAllText(path, File.ReadAllText(path).Replace("\"SchemaVersion\": 2", "\"SchemaVersion\": 0"));

            CharacterRecord loaded = storage.LoadCharacter("dave", "mage");
            Assert.That(loaded.SchemaVersion, Is.EqualTo(CharacterMigrations.Current));
            Assert.That(loaded.Statuses, Is.Not.Null);
            Assert.That(loaded.Level, Is.EqualTo(3));
        }

        [Test]
        public void Backup_CopiesSaves_AndKeepsTheNewest()
        {
            FileStorage storage = Storage();
            storage.SaveAccount(new AccountRecord { Login = "erin" });
            for (int i = 0; i < 4; i++)
            {
                storage.Backup(2);
            }
            string[] backups = Directory.GetDirectories(Path.Combine(_root, "backups"));
            Assert.That(backups.Length, Is.EqualTo(2));
            Assert.That(Directory.GetFiles(Path.Combine(backups[0], "accounts")).Length, Is.EqualTo(1));
        }

        [Test]
        public void UniqueItem_CannotExistTwice_EvenUnderARace()
        {
            FileStorage storage = Storage();
            int winners = 0;
            Parallel.For(0, 32, i =>
            {
                if (storage.TryReserve("divine_test", Guid.NewGuid().ToString("N"), "owner" + i))
                {
                    Interlocked.Increment(ref winners);
                }
            });
            Assert.That(winners, Is.EqualTo(1));
            Assert.That(Storage().OwnerOf("divine_test"), Is.Not.Null, "the registry is on disk");

            var h = new ServerHarness();
            h.Server.Context.Items.UniqueRegistry = Storage();
            h.Content.Items["amber_amulet"].Rarity = Rarity.Divine;
            ItemInstance first = h.Server.Context.Items.Create("amber_amulet", 1, "a", 0f);
            ItemInstance second = h.Server.Context.Items.Create("amber_amulet", 1, "b", 0f);
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Null, "a second divine instance is refused");
            Assert.That(h.Audit.Entries.Any(e => e.Action == AuditActions.UniqueRefused), Is.True);
        }

        [Test]
        public void FileAudit_WritesLines()
        {
            var audit = new FileAuditLog(Path.Combine(_root, "audit"));
            audit.Write(new AuditEntry { Action = AuditActions.Create, ActorId = "c1", InstanceId = "i1", DefinitionId = "iron_sword", Count = 1 });
            string file = Directory.GetFiles(Path.Combine(_root, "audit")).Single();
            Assert.That(File.ReadAllText(file), Does.Contain("iron_sword"));
        }

        [Test]
        public void Network_LoginSaveReconnect()
        {
            var h = new ServerHarness();
            CharacterPersistence persistence = Persistence(h, Storage());
            using (var host = new ServerNetworkHost(h.Server, 0, persistence))
            {
                NetworkClientSession first = NetworkClientSession.Connect("127.0.0.1", host.Port, "Фрэнк", "mage", "frank", "pass1", null);
                Pump(h, host, () => { Drain(first); return first.IsWelcomed; });
                h.Player(first.PlayerId).Level = 9;
                string token = first.Token;
                first.Dispose();
                Pump(h, host, () => host.ConnectedPlayers == 0);

                using (NetworkClientSession second = NetworkClientSession.Connect("127.0.0.1", host.Port, null, "mage", "frank", null, token))
                {
                    Pump(h, host, () => { Drain(second); return second.IsWelcomed; });
                    Assert.That(h.Player(second.PlayerId).Level, Is.EqualTo(9));
                }
                using (NetworkClientSession wrong = NetworkClientSession.Connect("127.0.0.1", host.Port, null, "mage", "frank", "nope", null))
                {
                    Pump(h, host, () => { Drain(wrong); return wrong.Error != null; });
                    Assert.That(wrong.Error, Is.EqualTo(LoginErrors.WrongPassword));
                }
            }
        }

        private static void Drain(NetworkClientSession session)
        {
            while (session.TryReceive(out _))
            {
            }
        }

        private static void Pump(ServerHarness h, ServerNetworkHost host, Func<bool> done)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < 8000)
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
    }
}
