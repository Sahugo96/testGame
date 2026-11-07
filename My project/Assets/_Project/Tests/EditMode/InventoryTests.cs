using System.Linq;
using Game.Server.Items;
using Game.Server.Players;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>04, 7.9 / 7.9.1 and questions 10, 36, 56: equipment via components, class rules, combat swaps, consumables.</summary>
    public sealed class InventoryTests
    {
        [Test]
        public void StartingItems_AreCreatedAuditedAndEquipped()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            PlayerCharacter player = h.Player(mage);

            Assert.That(player.Equipment["MainHand"].DefinitionId, Is.EqualTo("apprentice_staff"));
            Assert.That(player.Equipment["Head"].DefinitionId, Is.EqualTo("cloth_hood"));
            Assert.That(h.BagIndexOf(mage, "health_potion"), Is.GreaterThanOrEqualTo(0));
            Assert.That(h.Audit.Entries.Count(e => e.Action == AuditActions.Create), Is.EqualTo(h.Content.Classes["mage"].StartingItems.Count));
            Assert.That(h.Audit.Entries.Count(e => e.Action == AuditActions.Equip), Is.EqualTo(3));
            Assert.That(player.Equipment["Head"].BoundTo, Is.EqualTo(player.CharacterId), "OnEquip binding");
        }

        [Test]
        public void EquippedItemStats_AreApplied_AndRemovedOnUnequip()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            PlayerCharacter player = h.Player(mage);
            float crit = player.Stats.Get(StatId.CritChance);

            h.Send(mage, new EquipItemCommand { BagIndex = h.BagIndexOf(mage, "ring_of_focus") });
            Assert.That(player.Equipment["Ring1"].DefinitionId, Is.EqualTo("ring_of_focus"));
            Assert.That(player.Stats.Get(StatId.CritChance), Is.EqualTo(crit + 0.05f).Within(0.0001f));

            h.Send(mage, new UnequipItemCommand { SlotId = "Ring1" });
            Assert.That(player.Stats.Get(StatId.CritChance), Is.EqualTo(crit).Within(0.0001f));
        }

        [Test]
        public void ClassRules_BlockArmorTypeAndClassRestriction()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");

            h.Send(mage, new EquipItemCommand { BagIndex = h.BagIndexOf(mage, "iron_helm") });

            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.WrongClass), "mage may wear cloth only");
            Assert.That(h.Player(mage).Equipment["Head"].DefinitionId, Is.EqualTo("cloth_hood"));
        }

        [Test]
        public void RingSlots_DependOnClass()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId warrior = h.AddPlayer("warrior");
            h.Server.Tick();

            Assert.That(h.Player(mage).Outbox.State.Equipment.Count(s => s.SlotId.StartsWith("Ring")), Is.EqualTo(4));
            Assert.That(h.Player(warrior).Outbox.State.Equipment.Count(s => s.SlotId.StartsWith("Ring")), Is.EqualTo(2));
        }

        [Test]
        public void TwoHandedWeapon_FreesOffHand()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            PlayerCharacter player = h.Player(warrior);
            Assert.That(player.Equipment.ContainsKey("OffHand"), Is.True);

            h.Send(warrior, new EquipItemCommand { BagIndex = h.BagIndexOf(warrior, "great_axe") });

            Assert.That(player.Equipment["MainHand"].DefinitionId, Is.EqualTo("great_axe"));
            Assert.That(player.Equipment.ContainsKey("OffHand"), Is.False);
            Assert.That(h.BagIndexOf(warrior, "iron_sword"), Is.GreaterThanOrEqualTo(0));
            Assert.That(h.BagIndexOf(warrior, "wooden_shield"), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void InCombat_ArmorIsLocked_WeaponSwapHasCooldown()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            EnterCombat(h, warrior);

            h.Send(warrior, new EquipItemCommand { BagIndex = h.BagIndexOf(warrior, "leather_gloves") });
            Assert.That(h.Rejections(warrior), Does.Contain(RejectReasons.ArmorInCombat));

            h.Send(warrior, new EquipItemCommand { BagIndex = h.BagIndexOf(warrior, "great_axe") });
            Assert.That(h.Player(warrior).Equipment["MainHand"].DefinitionId, Is.EqualTo("great_axe"));

            h.ClearOutbox(warrior);
            h.Send(warrior, new EquipItemCommand { BagIndex = h.BagIndexOf(warrior, "iron_sword") });
            Assert.That(h.Rejections(warrior), Does.Contain(RejectReasons.WeaponSwapLocked));
        }

        [Test]
        public void MainHandSwapTimeline_LockThenRiskWindowThenPenalty()
        {
            var swap = new WeaponSwapState();
            swap.OnMainHandSwapped(15f, 5f, 60f);
            Assert.That(swap.CanSwapMainHand, Is.False);

            Advance(swap, 15.1f);
            Assert.That(swap.CanSwapMainHand, Is.True);
            Assert.That(swap.MainHandRiskRemaining, Is.GreaterThan(0f), "risk window after the lock");

            swap.OnMainHandSwapped(15f, 5f, 60f);
            Assert.That(swap.MainHandLockRemaining, Is.EqualTo(60f), "swap inside the risk window → penalty");
            Advance(swap, 60.1f);
            Assert.That(swap.CanSwapMainHand, Is.True);
            Assert.That(swap.MainHandRiskRemaining, Is.EqualTo(0f), "no risk window after the penalty");
        }

        [Test]
        public void MainHandSwap_AfterRiskWindow_IsFree()
        {
            var swap = new WeaponSwapState();
            swap.OnMainHandSwapped(15f, 5f, 60f);
            Advance(swap, 20.2f);
            swap.OnMainHandSwapped(15f, 5f, 60f);
            Assert.That(swap.MainHandLockRemaining, Is.EqualTo(15f));
        }

        [Test]
        public void ManaPotion_HalfEffectInCombat_FullOutOfCombat()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            h.Mana(mage).Main = 10f;
            h.Mana(mage).MainRegen = 0f;

            h.Send(mage, new UseConsumableCommand { BagIndex = h.BagIndexOf(mage, "main_mana_potion") });
            Assert.That(h.Mana(mage).Main, Is.EqualTo(60f).Within(0.01f));

            var h2 = new ServerHarness();
            EntityId mage2 = h2.AddPlayer("mage");
            EnterCombat(h2, mage2);
            h2.Mana(mage2).Main = 10f;
            h2.Mana(mage2).MainRegen = 0f;
            h2.Send(mage2, new UseConsumableCommand { BagIndex = h2.BagIndexOf(mage2, "main_mana_potion") });
            Assert.That(h2.Mana(mage2).Main, Is.EqualTo(35f).Within(0.01f));
        }

        [Test]
        public void Potion_OnFullPool_IsRejected_AndCooldownGroupBlocksRepeat()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            int index = h.BagIndexOf(mage, "health_potion");

            h.Send(mage, new UseConsumableCommand { BagIndex = index });
            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.ResourceFull));

            h.Vitals(mage).Health = 10f;
            h.ClearOutbox(mage);
            h.Send(mage, new UseConsumableCommand { BagIndex = index });
            Assert.That(h.Player(mage).Bag.Get(index).Count, Is.EqualTo(4));
            h.Send(mage, new UseConsumableCommand { BagIndex = index });
            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.OnCooldown));
        }

        private static void EnterCombat(ServerHarness h, EntityId player)
        {
            ref var state = ref h.World.CombatStates.Get(player);
            state.InCombat = true;
            state.SecondsSinceCombatAction = 0f;
        }

        private static void Advance(WeaponSwapState swap, float seconds)
        {
            const float step = 0.1f;
            for (float t = 0f; t < seconds; t += step)
            {
                swap.Tick(step);
            }
        }
    }
}
