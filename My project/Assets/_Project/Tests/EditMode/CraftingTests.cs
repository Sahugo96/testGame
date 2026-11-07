using System.Linq;
using Game.Network;
using Game.Server.Items;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Crafting at the anvil (05, 7.4; developer decision v15) and upgrading only at the whetstone.</summary>
    public sealed class CraftingTests
    {
        private ServerHarness _h;
        private EntityId _player;
        private EntityId _anvil;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _player = _h.AddPlayer("mage");
            _anvil = _h.FindStation(StationTypes.Anvil);
            _h.PlaceNear(_player, _anvil, 2f);
        }

        private int Count(string itemId)
        {
            int total = 0;
            for (int i = 0; i < _h.Player(_player).Bag.Size; i++)
            {
                ItemInstance item = _h.Player(_player).Bag.Get(i);
                if (item != null && item.DefinitionId == itemId)
                {
                    total += item.Count;
                }
            }
            return total;
        }

        private bool Notified(string key) => _h.Player(_player).Outbox.Notifications.Any(n => n.TextKey == key);

        private void Craft(string recipeId, EntityId station) =>
            _h.Send(_player, new CraftItemCommand { RecipeId = recipeId, StationId = station });

        [Test]
        public void Success_SpendsInputs_GivesOutput_AfterCraftTime()
        {
            _h.Random.Value = 0f;
            int ore = Count("iron_ore");
            int helms = Count("iron_helm");

            Craft("craft_iron_helm", _anvil);
            Assert.That(_h.Player(_player).Outbox.State.CraftRecipeId, Is.EqualTo("craft_iron_helm"));
            Assert.That(Count("iron_ore"), Is.EqualTo(ore), "nothing is spent before the end");
            _h.Run(_h.Content.Recipes["craft_iron_helm"].CraftTime + 0.1f);

            Assert.That(Count("iron_ore"), Is.EqualTo(ore - 3));
            Assert.That(Count("iron_helm"), Is.EqualTo(helms + 1));
            Assert.That(Notified(NotificationKeys.CraftSuccess), Is.True);
            Assert.That(_h.Player(_player).Outbox.State.CraftRecipeId, Is.Null);
        }

        [Test]
        public void Failure_DestroysAllInputs()
        {
            _h.Random.Value = 0.99f;
            int ore = Count("iron_ore");
            int shards = Count("philosopher_stone_shard");
            int swords = Count("iron_sword");

            Craft("craft_iron_sword", _anvil);
            _h.Run(_h.Content.Recipes["craft_iron_sword"].CraftTime + 0.1f);

            Assert.That(Count("iron_ore"), Is.EqualTo(ore - 4));
            Assert.That(Count("philosopher_stone_shard"), Is.EqualTo(shards - 1));
            Assert.That(Count("iron_sword"), Is.EqualTo(swords));
            Assert.That(Notified(NotificationKeys.CraftFailed), Is.True);
        }

        [Test]
        public void WalkingAway_CancelsTheCraft_NothingSpent()
        {
            int ore = Count("iron_ore");
            Craft("craft_iron_helm", _anvil);
            _h.PlaceNear(_player, _anvil, _h.Content.Config.InteractRange + 3f);
            _h.Run(_h.Content.Recipes["craft_iron_helm"].CraftTime + 0.1f);

            Assert.That(Notified(NotificationKeys.CraftCancelled), Is.True);
            Assert.That(Count("iron_ore"), Is.EqualTo(ore));
        }

        [Test]
        public void Start_IsValidated()
        {
            Craft("no_such_recipe", _anvil);
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.RecipeUnknown));

            EntityId whetstone = _h.FindStation(StationTypes.Whetstone);
            _h.PlaceNear(_player, whetstone, 2f);
            Craft("craft_iron_helm", whetstone);
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.NotNearStation), "recipes of the anvil are not crafted at the whetstone");

            _h.PlaceNear(_player, _anvil, 2f);
            _h.ClearOutbox(_player);
            Craft("craft_iron_helm", _anvil);
            Craft("craft_iron_helm", _anvil);
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.AlreadyCrafting));

            _h.Send(_player, new CancelCraftCommand());
            _h.Player(_player).Bag.Set(_h.BagIndexOf(_player, "iron_ore"), null);
            Craft("craft_iron_helm", _anvil);
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.NoMaterials));
        }

        [Test]
        public void InCombat_IsRejected()
        {
            _h.World.CombatStates.Get(_player).InCombat = true;
            Craft("craft_iron_helm", _anvil);
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.CraftInCombat));
        }

        [Test]
        public void Upgrade_AtTheAnvil_IsRejected_OnlyTheWhetstoneUpgrades()
        {
            EntityId warrior = _h.AddPlayer("warrior");
            _h.PlaceNear(warrior, _anvil, 2f);
            string sword = _h.Player(warrior).Equipment["MainHand"].SourceId;
            _h.Send(warrior, new UpgradeItemCommand { ItemInstanceId = sword, StationId = _anvil });
            Assert.That(_h.Rejections(warrior), Does.Contain(RejectReasons.NotNearStation));
        }

        [Test]
        public void Codec_RoundTripsCraft()
        {
            Craft("craft_iron_helm", _anvil);
            _h.Run(0.5f);
            var state = (PlayerStateSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(_h.Player(_player).Outbox.State));
            Assert.That(state.CraftRecipeId, Is.EqualTo("craft_iron_helm"));
            Assert.That(state.CraftProgress, Is.GreaterThan(0f).And.LessThan(1f));
            var command = (CraftItemCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new CraftItemCommand { RecipeId = "x", StationId = _anvil }));
            Assert.That(command.RecipeId, Is.EqualTo("x"));
            Assert.That(command.StationId, Is.EqualTo(_anvil));
        }
    }
}
