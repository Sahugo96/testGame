using System;
using System.IO;
using System.Linq;
using Game.Network;
using Game.Server.Persistence;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 18 (02, 7.7 А–В): skill and consumable panels kept per character on the server, equipment loadouts.</summary>
    public sealed class PanelTests
    {
        private ServerHarness _h;
        private EntityId _warrior;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _warrior = _h.AddPlayer("warrior");
        }

        private PanelsMessage LastPanels() => _h.Player(_warrior).Outbox.Replies.OfType<PanelsMessage>().LastOrDefault();

        private string Worn(string slotId) => _h.Player(_warrior).Equipment.TryGetValue(slotId, out var item) ? item.DefinitionId : null;

        [Test]
        public void NewCharacter_GetsClassSkillsAndBagConsumables_SentOnce()
        {
            _h.Server.Tick();
            PanelsMessage panels = LastPanels();
            Assert.That(panels, Is.Not.Null);
            Assert.That(panels.SkillBar.Length, Is.EqualTo(_h.Content.Config.SkillBarRows * _h.Content.Config.SkillBarSlotsPerRow));
            Assert.That(panels.SkillBar[0], Is.EqualTo(_h.Player(_warrior).Class.Skills[0].SkillId));
            Assert.That(panels.ConsumableBar, Does.Contain("health_potion"));
            Assert.That(panels.Loadouts.Length, Is.EqualTo(_h.Content.Config.LoadoutCount));

            _h.Player(_warrior).Outbox.Replies.Clear();
            _h.Server.Tick();
            Assert.That(LastPanels(), Is.Null, "unchanged panels are not sent again");
        }

        [Test]
        public void Slots_AcceptOnlyClassSkillsAndConsumables()
        {
            string skill = _h.Player(_warrior).Class.Skills[1].SkillId;
            _h.Send(_warrior, new SetSkillSlotCommand { Index = 20, SkillId = skill });
            Assert.That(LastPanels().SkillBar[20], Is.EqualTo(skill));

            _h.Send(_warrior, new SetSkillSlotCommand { Index = 21, SkillId = "fireball" });
            _h.Send(_warrior, new SetSkillSlotCommand { Index = 999, SkillId = skill });
            _h.Send(_warrior, new SetConsumableSlotCommand { Index = 0, ItemId = "iron_sword" });
            Assert.That(_h.Rejections(_warrior).Count(r => r == RejectReasons.PanelSlotInvalid), Is.EqualTo(3));

            _h.Send(_warrior, new SetConsumableSlotCommand { Index = 7, ItemId = "main_mana_potion" });
            _h.Send(_warrior, new SetSkillSlotCommand { Index = 20, SkillId = null });
            Assert.That(LastPanels().ConsumableBar[7], Is.EqualTo("main_mana_potion"));
            Assert.That(LastPanels().SkillBar[20], Is.Null, "an empty id clears the slot");
        }

        [Test]
        public void Loadouts_SwitchWeaponsAndArmour_OutOfCombat()
        {
            _h.Send(_warrior, new SaveLoadoutCommand { Index = 0 });
            _h.Send(_warrior, new EquipItemCommand { BagIndex = _h.BagIndexOf(_warrior, "great_axe") });
            _h.Send(_warrior, new EquipItemCommand { BagIndex = _h.BagIndexOf(_warrior, "swordsman_armor") });
            _h.Send(_warrior, new SaveLoadoutCommand { Index = 1 });
            Assert.That((Worn("MainHand"), Worn("OffHand")), Is.EqualTo(("great_axe", (string)null)));

            _h.Send(_warrior, new ApplyLoadoutCommand { Index = 0 });
            Assert.That((Worn("MainHand"), Worn("OffHand"), Worn("Body")), Is.EqualTo(("iron_sword", "wooden_shield", "plate_armor")));
            Assert.That(LastPanels().Loadouts[1].Items.Any(i => i.DefinitionId == "great_axe"), Is.True);

            _h.Send(_warrior, new ApplyLoadoutCommand { Index = 1 });
            Assert.That((Worn("MainHand"), Worn("OffHand"), Worn("Body")), Is.EqualTo(("great_axe", (string)null, "swordsman_armor")));

            _h.Send(_warrior, new ApplyLoadoutCommand { Index = 4 });
            Assert.That(_h.Rejections(_warrior), Does.Contain(RejectReasons.LoadoutEmpty));
        }

        [Test]
        public void Loadout_InCombat_ChangesOnlyWeapons_AndMissingItemsArePartial()
        {
            _h.Send(_warrior, new SaveLoadoutCommand { Index = 0 });
            _h.Send(_warrior, new EquipItemCommand { BagIndex = _h.BagIndexOf(_warrior, "great_axe") });
            _h.Send(_warrior, new EquipItemCommand { BagIndex = _h.BagIndexOf(_warrior, "swordsman_armor") });
            _h.Send(_warrior, new SaveLoadoutCommand { Index = 1 });

            _h.World.CombatStates.Get(_warrior).InCombat = true;
            _h.Send(_warrior, new ApplyLoadoutCommand { Index = 0 });
            Assert.That((Worn("MainHand"), Worn("OffHand")), Is.EqualTo(("iron_sword", "wooden_shield")), "weapons change in combat");
            Assert.That(Worn("Body"), Is.EqualTo("swordsman_armor"), "armour waits for the end of combat (question 10)");
            Assert.That(_h.Player(_warrior).Outbox.Notifications.Any(n => n.TextKey == NotificationKeys.LoadoutArmourInCombat && n.Number == 1), Is.True);

            _h.World.CombatStates.Get(_warrior).InCombat = false;
            var axe = _h.Player(_warrior).Bag.Get(_h.BagIndexOf(_warrior, "great_axe"));
            _h.Player(_warrior).Bag.Set(_h.Player(_warrior).Bag.IndexOf(axe), null);
            _h.Send(_warrior, new ApplyLoadoutCommand { Index = 1 });
            Assert.That(_h.Player(_warrior).Outbox.Notifications.Any(n => n.TextKey == NotificationKeys.LoadoutPartial && n.Number == 1), Is.True);
            Assert.That(Worn("MainHand"), Is.EqualTo("iron_sword"), "the sold axe cannot come back");
        }

        [Test]
        public void Panels_AreSavedWithTheCharacter()
        {
            string root = Path.Combine(Path.GetTempPath(), "game-panels-" + Guid.NewGuid().ToString("N"));
            try
            {
                var h = new ServerHarness();
                var storage = new FileStorage(root, new TestJsonCodec());
                var persistence = new CharacterPersistence(h.Server, storage, storage, storage);
                EntityId id = persistence.Enter("ann", "secret1", null, "Аня", "warrior", false, out _, out _);
                string skill = h.Player(id).Class.Skills[0].SkillId;
                h.Send(id, new SetSkillSlotCommand { Index = 0, SkillId = null });
                h.Send(id, new SetSkillSlotCommand { Index = 13, SkillId = skill });
                h.Send(id, new SetConsumableSlotCommand { Index = 2, ItemId = "main_mana_potion" });
                h.Send(id, new SaveLoadoutCommand { Index = 3 });
                persistence.Leave(id);

                var restarted = new ServerHarness();
                var reopened = new FileStorage(root, new TestJsonCodec());
                var again = new CharacterPersistence(restarted.Server, reopened, reopened, reopened);
                EntityId back = again.Enter("ann", "secret1", null, null, "warrior", false, out _, out _);
                restarted.Server.Tick();
                PanelsMessage panels = restarted.Player(back).Outbox.Replies.OfType<PanelsMessage>().Last();
                Assert.That(panels.SkillBar[0], Is.Null);
                Assert.That(panels.SkillBar[13], Is.EqualTo(skill));
                Assert.That(panels.ConsumableBar[2], Is.EqualTo("main_mana_potion"));
                Assert.That(panels.Loadouts[3].Items.Any(i => i.DefinitionId == "iron_sword"), Is.True);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Test]
        public void PanelsMessage_SurvivesTheWire()
        {
            _h.Send(_warrior, new SaveLoadoutCommand { Index = 0 });
            PanelsMessage sent = LastPanels();
            var decoded = (PanelsMessage)ProtocolCodec.Decode(ProtocolCodec.Encode(sent));
            Assert.That(decoded.SkillBar, Is.EqualTo(sent.SkillBar));
            Assert.That(decoded.ConsumableBar, Is.EqualTo(sent.ConsumableBar));
            Assert.That(decoded.Loadouts[0].Items.Select(i => i.InstanceId), Is.EqualTo(sent.Loadouts[0].Items.Select(i => i.InstanceId)));
            var command = (SetSkillSlotCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new SetSkillSlotCommand { Index = 5, SkillId = null }));
            Assert.That((command.Index, command.SkillId), Is.EqualTo((5, (string)null)));
        }
    }
}
