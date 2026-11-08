using System.Collections.Generic;
using System.Linq;
using Game.Server;
using Game.Shared.Content;
using Game.Shared.Content.Csv;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Ids;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 4, questions 83 and 84: stable ids with Deprecated, balance limits, CSV balance tables.</summary>
    public sealed class ContentDataTests
    {
        private static List<string> BuildErrors(ContentFiles files)
        {
            var errors = new List<string>();
            ServerContentFactory.Build(files, errors);
            return errors;
        }

        [Test]
        public void Ledger_ListsEveryCurrentId()
        {
            List<string> unlisted = ContentIdLedger.Unlisted(TestContentLoader.LoadFiles());
            Assert.That(unlisted, Is.Empty, "new ids must be added to content_ids.json (Game → Content → Fix ids): " + string.Join(", ", unlisted));
        }

        [Test]
        public void DeletingAReleasedItem_IsAnError_DeprecatingIsNot()
        {
            ContentFiles files = TestContentLoader.LoadFiles();
            files.Items.Items = files.Items.Items.Where(i => i.Id != "wolf_core").ToArray();
            Assert.That(BuildErrors(files).Any(e => e.Contains("items 'wolf_core' was released")), Is.True);

            files = TestContentLoader.LoadFiles();
            files.Items.Items.First(i => i.Id == "wolf_core").Deprecated = true;
            files.Mobs.Mobs.First(m => m.Id == "training_dummy").Deprecated = true;
            var errors = new List<string>();
            ContentDatabase db = ServerContentFactory.Build(files, errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(db.Items.ContainsKey("wolf_core"), Is.True, "old instances still load");
            Assert.That(db.Mobs["wolf"].Drops.Any(d => d.ItemId == "wolf_core"), Is.False, "no longer dropped");
            Assert.That(db.World.Spawns.Any(s => s.MobId == "training_dummy"), Is.False, "no longer spawned");
            Assert.That(db.Recipes.Values.Any(r => r.Inputs.Concat(r.Outputs).Any(p => p.ItemId == "wolf_core")), Is.False, "no longer crafted");
        }

        [Test]
        public void BalanceLimits_CatchATypo_ScaledByRarity()
        {
            ContentFiles files = TestContentLoader.LoadFiles();
            ItemDto ring = files.Items.Items.First(i => i.Components != null && i.Components.Any(c => c.Stats != null && c.Stats.Any(s => s.Stat == "MaxHealth")));
            ring.Components.First(c => c.Stats != null && c.Stats.Any(s => s.Stat == "MaxHealth")).Stats.First(s => s.Stat == "MaxHealth").Value = 5000;
            Assert.That(BuildErrors(files).Any(e => e.Contains($"item '{ring.Id}'") && e.Contains("MaxHealth Add 50")), Is.True);

            files = TestContentLoader.LoadFiles();
            files.StatusEffects.Statuses.First(s => s.Id == "fury").Modifiers[0].Value = 3f;
            Assert.That(BuildErrors(files).Any(e => e.Contains("status 'fury'")), Is.True);
        }

        [Test]
        public void Csv_RoundTrip_ChangesOnlyWhatWasEdited()
        {
            ContentFiles files = TestContentLoader.LoadFiles();
            List<object> mobs = files.Mobs.Mobs.Cast<object>().ToList();
            string csv = CsvBalanceMapper.Export(typeof(MobDto), mobs);
            Assert.That(csv.Split('\n')[0].Split(';')[0], Is.EqualTo("Id"));
            Assert.That(csv, Does.Contain("0,05"), "decimal comma for Excel with Russian settings");

            var errors = new List<string>();
            Assert.That(CsvBalanceMapper.Import(typeof(MobDto), csv, mobs, errors), Is.Empty, "an unchanged table changes nothing");

            List<string[]> rows = CsvTable.Parse(csv);
            int health = System.Array.IndexOf(rows[0], "MaxHealth");
            int defense = System.Array.IndexOf(rows[0], "Defense");
            string[] wolf = rows.First(r => r[0] == "wolf");
            wolf[health] = "120";
            wolf[defense] = "0.07";
            List<object> changed = CsvBalanceMapper.Import(typeof(MobDto), CsvTable.Write(rows), mobs, errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(changed.Cast<MobDto>().Select(m => m.Id), Is.EqualTo(new[] { "wolf" }));
            Assert.That((files.Mobs.Mobs.First(m => m.Id == "wolf").MaxHealth, files.Mobs.Mobs.First(m => m.Id == "wolf").Defense), Is.EqualTo((120f, 0.07f)));
        }

        [Test]
        public void Csv_WithAnErrorAppliesNothing()
        {
            ContentFiles files = TestContentLoader.LoadFiles();
            List<object> mobs = files.Mobs.Mobs.Cast<object>().ToList();
            List<string[]> rows = CsvTable.Parse(CsvBalanceMapper.Export(typeof(MobDto), mobs));
            int health = System.Array.IndexOf(rows[0], "MaxHealth");
            rows.First(r => r[0] == "wolf")[health] = "999";
            rows.First(r => r[0] == "training_dummy")[health] = "много";
            rows.Add(new[] { "dragon" });
            float before = files.Mobs.Mobs.First(m => m.Id == "wolf").MaxHealth;
            var errors = new List<string>();
            CsvBalanceMapper.Import(typeof(MobDto), CsvTable.Write(rows), mobs, errors);
            Assert.That(errors.Any(e => e.Contains("много")) && errors.Any(e => e.Contains("'dragon'")), Is.True, string.Join("\n", errors));
            Assert.That(files.Mobs.Mobs.First(m => m.Id == "wolf").MaxHealth, Is.EqualTo(before));
        }

        [Test]
        public void CsvTable_QuotesSeparatorsAndQuotes()
        {
            var rows = new List<string[]> { new[] { "a;b", "say \"hi\"", "line\nbreak" } };
            Assert.That(CsvTable.Parse(CsvTable.Write(rows)).Single(), Is.EqualTo(rows[0]));
        }
    }
}
