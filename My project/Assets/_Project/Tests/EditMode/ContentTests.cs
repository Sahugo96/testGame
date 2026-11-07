using System.Collections.Generic;
using Game.Server;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class ContentTests
    {
        [Test]
        public void RealContent_BuildsWithoutErrors()
        {
            var errors = new List<string>();
            ContentDatabase db = ServerContentFactory.Build(TestContentLoader.LoadFiles(), errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(db.Classes.ContainsKey("mage"), Is.True);
            Assert.That(db.Skills["fireball"].Empowered, Is.Not.Null);
        }

        [Test]
        public void ItemComponentWithoutRequiredComponent_IsReported()
        {
            ContentFiles files = TestContentLoader.LoadFiles();
            var broken = new ItemDto
            {
                Id = "broken_helm",
                NameKey = "item.broken_helm.name",
                Category = "Armor",
                Rarity = "Common",
                Components = new[]
                {
                    new ItemComponentDto { Type = "EquipSlot", SlotType = "Head" },
                    new ItemComponentDto { Type = "Stats", Stats = new StatModifierDto[0] },
                },
            };
            var items = new List<ItemDto>(files.Items.Items) { broken };
            files.Items.Items = items.ToArray();

            var errors = new List<string>();
            ServerContentFactory.Build(files, errors);

            Assert.That(errors.Exists(e => e.Contains("broken_helm") && e.Contains("requires 'Equipment'")), Is.True, string.Join("\n", errors));
        }

        [Test]
        public void UnknownEffectKey_IsReported()
        {
            ContentFiles files = TestContentLoader.LoadFiles();
            files.Skills.Skills[0].Variants[0].Effects[0].Key = "Teleport";
            var errors = new List<string>();
            ServerContentFactory.Build(files, errors);
            Assert.That(errors.Exists(e => e.Contains("Teleport")), Is.True);
        }
    }
}
