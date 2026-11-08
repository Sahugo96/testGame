using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Network;
using Game.Server;
using Game.Server.Net;
using Game.Shared.Content;
using Game.Shared.Content.Packs;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 4, question 82: content packs add records without code, cannot redefine others, and the hash guards logins.</summary>
    public sealed class ContentPackTests
    {
        private const string SamplePack = "sample";

        /// <summary>The real content with some files replaced or added (null text = the file is missing).</summary>
        private sealed class OverlaySource : IContentSource
        {
            private readonly IContentSource _inner;
            private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

            public OverlaySource(IContentSource inner) => _inner = inner;

            public OverlaySource Set(string path, string text)
            {
                _files[path] = text;
                return this;
            }

            public string ReadText(string path) => _files.TryGetValue(path, out string text) ? text : _inner.ReadText(path);
        }

        private static string FixturesRoot => TestContentLoader.FixturesRoot;

        private static OverlaySource WithSamplePack()
        {
            var source = new OverlaySource(new FileContentSource(TestContentLoader.Root));
            source.Set(ContentPacks.PackList, "{ \"Packs\": [ \"base\", \"sample\" ] }");
            foreach (string file in Directory.GetFiles(Path.Combine(FixturesRoot, "sample_pack"), "*" + FileContentSource.Extension))
            {
                source.Set(ContentPacks.PathOf(SamplePack, Path.GetFileNameWithoutExtension(file)), File.ReadAllText(file));
            }
            return source;
        }

        private static ContentDatabase Build(IContentSource source, List<string> errors) =>
            ServerContentFactory.Build(ContentPackLoader.Load(source, TestContentLoader.Parse, errors), errors);

        [Test]
        public void SamplePack_AddsAMobItemAndSkill_WithoutCode()
        {
            var errors = new List<string>();
            ContentDatabase db = Build(WithSamplePack(), errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(db.Packs, Is.EqualTo(new[] { ContentPacks.Base, SamplePack }));
            Assert.That(db.Mobs.ContainsKey("sample_pup") && db.Items.ContainsKey("sample_pup_fang") && db.Skills.ContainsKey("mob_sample_nip"), Is.True);
            Assert.That(db.Items.ContainsKey("great_axe"), Is.True, "the base pack is still there");
            Assert.That(db.World.Spawns.Any(s => s.MobId == "sample_pup"), Is.True, "a pack may add spawns to the world");

            var h = new ServerHarness(db);
            Assert.That(h.FindMob("sample_pup").IsValid, Is.True, "the new mob spawns in the world");
        }

        [Test]
        public void Pack_CannotRedefineABaseRecord_NorCarryConfig()
        {
            OverlaySource source = WithSamplePack()
                .Set(ContentPacks.PathOf(SamplePack, "items"), "{ \"Items\": [ { \"Id\": \"great_axe\", \"NameKey\": \"x\", \"Category\": \"Loot\", \"Rarity\": \"Common\", \"Components\": [] } ] }")
                .Set(ContentPacks.PathOf(SamplePack, ContentFileTable.Config), "{ \"TickRate\": 5 }");
            var errors = new List<string>();
            ContentPackLoader.Load(source, TestContentLoader.Parse, errors);
            Assert.That(errors.Any(e => e.Contains("redefines items 'great_axe'")), Is.True, string.Join("\n", errors));
            Assert.That(errors.Any(e => e.Contains("only the 'base' pack may have 'config'")), Is.True, string.Join("\n", errors));
        }

        [Test]
        public void PackList_MustStartWithBase_AndRespectDependencies()
        {
            var source = new OverlaySource(new FileContentSource(TestContentLoader.Root))
                .Set(ContentPacks.PackList, "{ \"Packs\": [ \"sample\", \"base\" ] }")
                .Set(ContentPacks.PathOf(SamplePack, ContentPacks.Manifest), File.ReadAllText(Path.Combine(FixturesRoot, "sample_pack", "pack.json")));
            var errors = new List<string>();
            ContentPackLoader.Load(source, TestContentLoader.Parse, errors);
            Assert.That(errors.Any(e => e.Contains("must start with 'base'")), Is.True);
            Assert.That(errors.Any(e => e.Contains("needs 'base' loaded before it")), Is.True);
        }

        [Test]
        public void Hash_DependsOnTheData_NotOnLineEndings()
        {
            ContentFiles plain = TestContentLoader.LoadFiles();
            string items = new FileContentSource(TestContentLoader.Root).ReadText(ContentPacks.PathOf(ContentPacks.Base, "items"));
            var crlf = new OverlaySource(new FileContentSource(TestContentLoader.Root)).Set(ContentPacks.PathOf(ContentPacks.Base, "items"), items.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            var changed = new OverlaySource(new FileContentSource(TestContentLoader.Root)).Set(ContentPacks.PathOf(ContentPacks.Base, "items"), items.Replace("\"MaxStack\": 50", "\"MaxStack\": 51"));
            Assert.That(plain.Hash, Has.Length.EqualTo(64));
            Assert.That(ContentPackLoader.Load(crlf, TestContentLoader.Parse, new List<string>()).Hash, Is.EqualTo(plain.Hash));
            Assert.That(ContentPackLoader.Load(changed, TestContentLoader.Parse, new List<string>()).Hash, Is.Not.EqualTo(plain.Hash));
        }

        [Test]
        public void ClientWithOtherContent_IsRefusedAtLogin()
        {
            var h = new ServerHarness();
            Assert.That(h.Content.ContentHash, Is.Not.Empty);
            using (var host = new ServerNetworkHost(h.Server, 0))
            {
                using (NetworkClientSession other = NetworkClientSession.Connect("127.0.0.1", host.Port, "Old", "mage", "Old", null, null, "0000"))
                {
                    NetworkTests.Pump(h, host, () => { other.TryReceive(out _); return other.Error != null; });
                    Assert.That(other.Error, Is.EqualTo(NetworkConstants.ErrorContentMismatch));
                }
                using (NetworkClientSession same = NetworkClientSession.Connect("127.0.0.1", host.Port, "Same", "mage", "Same", null, null, h.Content.ContentHash))
                {
                    NetworkTests.Pump(h, host, () => { same.TryReceive(out _); return same.IsWelcomed; });
                    Assert.That(same.Error, Is.Null);
                }
            }
        }
    }
}
