using System.Linq;
using Game.Server.Items;
using Game.Server.Players;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Developer request v11: bag sections, whetstone upgrade (1 shard, 100% → 55%), experience and levels, 1-HP cubes.</summary>
    public sealed class ProgressionAndUpgradeTests
    {
        [Test]
        public void Items_GoToTheSectionOfTheirCategory_AndStack()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            PlayerCharacter player = h.Player(mage);

            foreach (BagSection section in h.Content.BagSections)
            {
                for (int i = section.Start; i < section.Start + section.Size; i++)
                {
                    ItemInstance item = player.Bag.Get(i);
                    if (item != null)
                    {
                        Assert.That(section.Categories.Contains(h.Content.Items[item.DefinitionId].Category), Is.True, item.DefinitionId);
                    }
                }
            }
            int shardIndex = h.BagIndexOf(mage, "philosopher_stone_shard");
            Assert.That(player.Bag.Get(shardIndex).Count, Is.EqualTo(20));
        }

        [Test]
        public void Whetstone_UpgradesWithOneShard_WhenNear()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            EntityId whetstone = h.FindStation(StationTypes.Whetstone);
            h.PlaceNear(warrior, whetstone, 2f);
            ItemInstance sword = h.Player(warrior).Equipment["MainHand"];
            h.Random.Value = 0f;

            h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = sword.SourceId, StationId = whetstone });

            Assert.That(sword.UpgradeLevel, Is.EqualTo(1));
            Assert.That(h.Player(warrior).Bag.Get(h.BagIndexOf(warrior, "philosopher_stone_shard")).Count, Is.EqualTo(19));
            Assert.That(h.Player(warrior).Outbox.Notifications.Any(n => n.TextKey == NotificationKeys.UpgradeSuccess), Is.True);
        }

        [Test]
        public void Whetstone_FarAway_IsRejected()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            EntityId whetstone = h.FindStation(StationTypes.Whetstone);
            h.PlaceNear(warrior, whetstone, 10f);
            ItemInstance sword = h.Player(warrior).Equipment["MainHand"];

            h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = sword.SourceId, StationId = whetstone });

            Assert.That(h.Rejections(warrior), Does.Contain(RejectReasons.NotNearStation));
            Assert.That(sword.UpgradeLevel, Is.EqualTo(0));
        }

        [Test]
        public void Whetstone_ChanceDropsByFivePercentPerLevel_FailureLowersLevel_CapIsTen()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            EntityId whetstone = h.FindStation(StationTypes.Whetstone);
            h.PlaceNear(warrior, whetstone, 2f);
            ItemInstance sword = h.Player(warrior).Equipment["MainHand"];
            var rule = h.Content.UpgradeRules["default"];
            for (int i = 0; i < rule.MaxLevel; i++)
            {
                Assert.That(rule.Steps[i].SuccessChance, Is.EqualTo(1f - 0.05f * i).Within(0.0001f));
            }

            sword.UpgradeLevel = 3;
            h.Random.Value = 0.86f;
            h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = sword.SourceId, StationId = whetstone });
            Assert.That(sword.UpgradeLevel, Is.EqualTo(2), "step +3→+4 succeeds with 85%, roll 0.86 fails and loses a level");

            sword.UpgradeLevel = 10;
            h.ClearOutbox(warrior);
            h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = sword.SourceId, StationId = whetstone });
            Assert.That(h.Rejections(warrior), Does.Contain(RejectReasons.MaxUpgradeLevel));
        }

        [Test]
        public void UpgradeLevel_StrengthensEquippedStats()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            EntityId whetstone = h.FindStation(StationTypes.Whetstone);
            h.PlaceNear(warrior, whetstone, 2f);
            PlayerCharacter player = h.Player(warrior);
            float before = player.Stats.Get(StatId.DamageBonus);
            h.Random.Value = 0f;

            h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = player.Equipment["MainHand"].SourceId, StationId = whetstone });

            Assert.That(player.Stats.Get(StatId.DamageBonus), Is.EqualTo(before + 0.1f * 0.1f).Within(0.0001f), "iron sword +10% damage bonus × 1.1");
        }

        [Test]
        public void Whetstone_WithoutShards_IsRejected()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            EntityId whetstone = h.FindStation(StationTypes.Whetstone);
            h.PlaceNear(warrior, whetstone, 2f);
            PlayerCharacter player = h.Player(warrior);
            player.Bag.Set(h.BagIndexOf(warrior, "philosopher_stone_shard"), null);

            h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = player.Equipment["MainHand"].SourceId, StationId = whetstone });

            Assert.That(h.Rejections(warrior), Does.Contain(RejectReasons.NoMaterials));
        }

        [Test]
        public void Cube_HasOneHp_GivesTenExperience_RespawnsInFiveSeconds()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId cube = h.NearestMob(mage, "cube");
            Assert.That(h.Vitals(cube).MaxHealth, Is.EqualTo(1f));
            h.PlaceNear(mage, cube, 5f);
            h.Random.Value = 0.99f;

            h.Send(mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = cube });

            Assert.That(h.Vitals(cube).IsDead, Is.True);
            Assert.That(h.Player(mage).Experience, Is.EqualTo(10));
            h.Run(4.9f);
            Assert.That(h.Vitals(cube).IsDead, Is.True);
            h.Run(0.2f);
            Assert.That(h.Vitals(cube).IsDead, Is.False);
        }

        [Test]
        public void CubeDrops_PhilosopherStoneShard()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId cube = h.NearestMob(mage, "cube");
            h.PlaceNear(mage, cube, 5f);
            h.Random.Value = 0f;

            h.Send(mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = cube });

            Assert.That(h.Player(mage).Bag.Get(h.BagIndexOf(mage, "philosopher_stone_shard")).Count, Is.EqualTo(21));
        }

        [Test]
        public void Experience_LevelsUp_GivesAttributeAndClassPoints()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            PlayerCharacter player = h.Player(mage);
            int needed = h.Content.ExperienceFor(1);
            float maxHealth = player.Stats.Get(StatId.MaxHealth);

            h.Server.Context.Players[mage].Experience = needed - 5;
            var progression = new Game.Server.Systems.ProgressionSystem(h.Server.Context, new Game.Server.Systems.StatSystem(h.Server.Context));
            progression.AwardExperience(player, 10);

            Assert.That(player.Level, Is.EqualTo(2));
            Assert.That(player.Experience, Is.EqualTo(5));
            Assert.That(player.AttributePoints, Is.EqualTo(h.Content.Config.AttributePointsPerLevel));
            Assert.That(player.ClassPoints, Is.EqualTo(1));

            h.Send(mage, new SpendAttributePointCommand { Attribute = StatId.Stamina });
            Assert.That(player.AttributePoints, Is.EqualTo(h.Content.Config.AttributePointsPerLevel - 1));
            Assert.That(player.Stats.Get(StatId.MaxHealth), Is.GreaterThan(maxHealth));
        }

        [Test]
        public void SpendWithoutPoints_IsRejected()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            h.Send(mage, new SpendAttributePointCommand { Attribute = StatId.Strength });
            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.NoAttributePoints));
        }
    }
}
