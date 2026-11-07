using System;
using System.IO;
using System.Linq;
using Game.Network;
using Game.Server.Items;
using Game.Server.Persistence;
using Game.Server.Players;
using Game.Server.Systems;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 11: wallet transactions, gold, sockets with ingredients, affixes, persistence and wire format.</summary>
    public sealed class ItemsModule11Tests
    {
        private const string Gold = "gold";

        private static ItemInstance Find(PlayerCharacter player, string definitionId)
        {
            foreach (ItemInstance item in player.Equipment.Values)
            {
                if (item.DefinitionId == definitionId)
                {
                    return item;
                }
            }
            for (int i = 0; i < player.Bag.Size; i++)
            {
                if (player.Bag.Get(i)?.DefinitionId == definitionId)
                {
                    return player.Bag.Get(i);
                }
            }
            return null;
        }

        [Test]
        public void Wallet_Transact_IsAtomic_Capped_AndIdempotent()
        {
            var h = new ServerHarness();
            PlayerCharacter a = h.Player(h.AddPlayer("mage"));
            PlayerCharacter b = h.Player(h.AddPlayer("mage"));
            WalletSystem wallet = h.Server.Wallet;
            long start = h.Content.Config.StartingGold;
            Assert.That(wallet.Balance(a, Gold), Is.EqualTo(start));

            WalletResult result = wallet.Transact(
                new[] { new WalletEntry { Player = a, CurrencyId = Gold, Amount = 30 } },
                new[] { new WalletEntry { Player = b, CurrencyId = Gold, Amount = 30 } }, "pay-1", "test");
            Assert.That(result, Is.EqualTo(WalletResult.Ok));
            Assert.That(wallet.Transact(
                new[] { new WalletEntry { Player = a, CurrencyId = Gold, Amount = 30 } },
                new[] { new WalletEntry { Player = b, CurrencyId = Gold, Amount = 30 } }, "pay-1", "test"), Is.EqualTo(WalletResult.Duplicate));
            Assert.That(wallet.Balance(a, Gold), Is.EqualTo(start - 30), "the same key is applied once");

            result = wallet.Transact(
                new[] { new WalletEntry { Player = a, CurrencyId = Gold, Amount = 1000 } },
                new[] { new WalletEntry { Player = b, CurrencyId = Gold, Amount = 1000 } }, "pay-2", "test");
            Assert.That(result, Is.EqualTo(WalletResult.Insufficient));
            Assert.That(wallet.Balance(b, Gold), Is.EqualTo(start + 30), "nothing moved");

            long cap = h.Content.Currencies[Gold].Cap;
            Assert.That(wallet.Transact(null, new[] { new WalletEntry { Player = b, CurrencyId = Gold, Amount = cap } }, "pay-3", "test"), Is.EqualTo(WalletResult.OverCap));
            Assert.That(h.Audit.Entries.Count(e => e.Action == AuditActions.CurrencyDebit && e.Reason == "test"), Is.EqualTo(1));
        }

        [Test]
        public void KillingMobs_GivesGold()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId cube = h.NearestMob(mage, "cube");
            h.PlaceNear(mage, cube, 5f);
            long before = h.Server.Wallet.Balance(h.Player(mage), Gold);
            h.Send(mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = cube });
            long gained = h.Server.Wallet.Balance(h.Player(mage), Gold) - before;
            Assert.That(gained, Is.InRange(h.Content.Mobs["cube"].GoldMin, h.Content.Mobs["cube"].GoldMax));
            Assert.That(h.Player(mage).Outbox.Notifications.Any(n => n.TextKey == NotificationKeys.GoldGained), Is.True);
        }

        [Test]
        public void Upgrade_CostsGold_AndWithoutGoldNothingIsSpent()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            PlayerCharacter player = h.Player(warrior);
            EntityId whetstone = h.FindStation(StationTypes.Whetstone);
            h.PlaceNear(warrior, whetstone, 2f);
            string sword = player.Equipment["MainHand"].SourceId;
            long gold = h.Server.Wallet.Balance(player, Gold);
            h.Random.Value = 0f;
            h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = sword, StationId = whetstone });
            long cost = h.Content.UpgradeRules.Values.First().Steps[0].GoldCost;
            Assert.That(h.Server.Wallet.Balance(player, Gold), Is.EqualTo(gold - cost));

            player.Wallet[Gold] = 0;
            int shards = h.Player(warrior).Bag.Get(h.BagIndexOf(warrior, "philosopher_stone_shard")).Count;
            h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = sword, StationId = whetstone });
            Assert.That(h.Rejections(warrior), Does.Contain(RejectReasons.NotEnoughGold));
            Assert.That(h.Player(warrior).Bag.Get(h.BagIndexOf(warrior, "philosopher_stone_shard")).Count, Is.EqualTo(shards), "materials stay");
        }

        [Test]
        public void Sockets_FollowTheSlotLimit()
        {
            var h = new ServerHarness();
            PlayerCharacter warrior = h.Player(h.AddPlayer("warrior"));
            Assert.That(Find(warrior, "swordsman_armor").Sockets.Select(s => s.Type), Is.EqualTo(new[] { "Soul" }), "clothing: 1 socket");
            Assert.That(Find(warrior, "swordsman_sword").Sockets.Count, Is.EqualTo(2), "weapons: up to 2");
            Assert.That(Find(warrior, "amber_amulet").Sockets, Is.Empty, "jewelry: SocketCap 0");
        }

        [Test]
        public void IngredientInSocket_ChangesTheItemsStats_ServerValidates()
        {
            var h = new ServerHarness();
            EntityId id = h.AddPlayer("warrior");
            PlayerCharacter player = h.Player(id);
            EntityId whetstone = h.FindStation(StationTypes.Whetstone);
            h.Send(id, new EquipItemCommand { BagIndex = h.BagIndexOf(id, "swordsman_armor") });
            ItemInstance armor = player.Equipment["Body"];
            Assert.That(armor.DefinitionId, Is.EqualTo("swordsman_armor"));
            float health = player.Stats.Get(StatId.MaxHealth);

            h.PlaceNear(id, whetstone, 20f);
            h.Send(id, new InsertIngredientCommand { ItemInstanceId = armor.SourceId, SocketIndex = 0, IngredientBagIndex = h.BagIndexOf(id, "wolf_soul"), StationId = whetstone });
            Assert.That(h.Rejections(id), Does.Contain(RejectReasons.NotNearStation));

            h.PlaceNear(id, whetstone, 2f);
            h.Send(id, new InsertIngredientCommand { ItemInstanceId = armor.SourceId, SocketIndex = 0, IngredientBagIndex = h.BagIndexOf(id, "wolf_core"), StationId = whetstone });
            Assert.That(h.Rejections(id), Does.Contain(RejectReasons.SocketWrongType), "a core does not fit a soul socket");

            h.Send(id, new InsertIngredientCommand { ItemInstanceId = armor.SourceId, SocketIndex = 0, IngredientBagIndex = h.BagIndexOf(id, "wolf_soul"), StationId = whetstone });
            Assert.That(armor.Sockets[0].IngredientId, Is.EqualTo("wolf_soul"));
            Assert.That(player.Stats.Get(StatId.MaxHealth), Is.EqualTo(health + 30f).Within(0.001f), "the ingredient's stats join the item's");
            Assert.That(h.BagIndexOf(id, "wolf_soul"), Is.EqualTo(-1), "the ingredient was spent");

            h.Send(id, new UnequipItemCommand { SlotId = "Body" });
            Assert.That(player.Stats.Get(StatId.MaxHealth), Is.LessThan(health + 30f), "only while worn");
        }

        [Test]
        public void Affixes_CountByRarity_ValuesInRange()
        {
            var h = new ServerHarness();
            PlayerCharacter mage = h.Player(h.AddPlayer("mage"));
            ItemInstance ring = Find(mage, "ring_of_focus");
            Assert.That(ring.Affixes.Count, Is.EqualTo(h.Content.Config.AffixCountByRarity[(int)Rarity.Rare]));
            Assert.That(ring.Affixes.Select(a => a.Stat).Distinct().Count(), Is.EqualTo(ring.Affixes.Count), "distinct stats");
            Assert.That(Find(mage, "cloth_hood").Affixes, Is.Empty, "common: no affixes");
            var pool = h.Content.AffixPools["jewelry"];
            foreach (var affix in ring.Affixes)
            {
                var range = pool.Stats.First(s => s.Stat == affix.Stat.ToString());
                Assert.That(affix.Value, Is.InRange(range.Min - 0.001f, range.Max + 0.001f));
            }
        }

        [Test]
        public void WalletSocketsAffixes_SurviveRestart_AndCrossTheWire()
        {
            string root = Path.Combine(Path.GetTempPath(), "game-saves-" + Guid.NewGuid().ToString("N"));
            try
            {
                var h = new ServerHarness();
                var storage = new FileStorage(root, new TestJsonCodec());
                var persistence = new CharacterPersistence(h.Server, storage, storage);
                EntityId id = persistence.Enter("hank", "secret1", null, "Хэнк", "warrior", false, out _, out _);
                PlayerCharacter player = h.Player(id);
                player.Wallet[Gold] = 777;
                ItemInstance armor = Find(player, "swordsman_armor");
                armor.Sockets[0].IngredientId = "wolf_soul";
                ItemInstance shield = Find(player, "swordsman_shield");
                var affixes = shield.Affixes.ToList();

                h.Server.Tick();
                var state = (PlayerStateSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(player.Outbox.State));
                Assert.That(state.Currencies.Single(c => c.CurrencyId == Gold).Amount, Is.EqualTo(777));
                ItemSlotSnapshot armorSlot = state.Bag.FirstOrDefault(b => b.InstanceId == armor.SourceId);
                Assert.That(armorSlot.Sockets.Single().IngredientId, Is.EqualTo("wolf_soul"));
                persistence.Leave(id);

                var restarted = new ServerHarness();
                var again = new CharacterPersistence(restarted.Server, new FileStorage(root, new TestJsonCodec()), new FileStorage(root, new TestJsonCodec()));
                PlayerCharacter back = restarted.Player(again.Enter("hank", "secret1", null, null, "warrior", false, out _, out _));
                Assert.That(restarted.Server.Wallet.Balance(back, Gold), Is.EqualTo(777));
                Assert.That(Find(back, "swordsman_armor").Sockets[0].IngredientId, Is.EqualTo("wolf_soul"));
                Assert.That(Find(back, "swordsman_shield").Affixes.Select(a => a.Value), Is.EqualTo(affixes.Select(a => a.Value)));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
