using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Item upgrade at a whetstone (developer decision v15) (04, 7.14.1; developer decision v11): one attempt costs MaterialCount of MaterialItemId,
    /// success chance and failure consequences come from the upgrade table steps; the roll is made only on the server.
    /// Simplifications until professions (module 12): anyone may use the whetstone (question 66); destruction chance is
    /// data and 0 for now (question 65).
    /// </summary>
    public sealed class UpgradeSystem
    {
        private readonly ServerContext _ctx;
        private readonly InventorySystem _inventory;
        private readonly StatSystem _stats;
        private readonly WalletSystem _wallet;

        public UpgradeSystem(ServerContext ctx, InventorySystem inventory, StatSystem stats, WalletSystem wallet)
        {
            _wallet = wallet;
            _ctx = ctx;
            _inventory = inventory;
            _stats = stats;
        }

        public void TryUpgrade(PlayerCharacter player, UpgradeItemCommand command)
        {
            if (!IsNearWhetstone(player, command.StationId))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return;
            }
            string equippedSlot = null;
            ItemInstance item = command.ItemInstanceId != null ? _inventory.FindOwned(player, command.ItemInstanceId, out equippedSlot) : null;
            if (item == null)
            {
                CombatEvents.Reject(player, RejectReasons.ItemNotFound);
                return;
            }
            UpgradeOutcome outcome = Attempt(player, item);
            if (outcome == UpgradeOutcome.Broken)
            {
                _inventory.DestroyOwned(player, item, equippedSlot);
            }
            else if (outcome != UpgradeOutcome.Refused && equippedSlot != null)
            {
                _stats.Recalculate(player);
            }
        }

        /// <summary>Whether the item can be upgraded at all (a rule exists, not divine, not at the maximum).</summary>
        public bool CanUpgrade(ItemInstance item) => RuleFor(item, out UpgradeRuleDto rule) && item.UpgradeLevel < rule.MaxLevel;

        /// <summary>
        /// One attempt by <paramref name="smith"/> on any item (own, or held in escrow by an upgrade order): the smith pays the
        /// gold and materials of the step, the roll is made here. On Broken the caller removes the item. Refused = nothing spent.
        /// </summary>
        public UpgradeOutcome Attempt(PlayerCharacter smith, ItemInstance item)
        {
            if (!RuleFor(item, out UpgradeRuleDto rule))
            {
                CombatEvents.Reject(smith, RejectReasons.CannotUpgrade);
                return UpgradeOutcome.Refused;
            }
            if (item.UpgradeLevel >= rule.MaxLevel)
            {
                CombatEvents.Reject(smith, RejectReasons.MaxUpgradeLevel);
                return UpgradeOutcome.Refused;
            }
            ItemDefinition definition = _ctx.Content.Items[item.DefinitionId];
            UpgradeStepDto step = rule.Steps[item.UpgradeLevel];
            string gold = _ctx.Content.Config.GoldCurrencyId;
            if (_wallet.Balance(smith, gold) < step.GoldCost)
            {
                CombatEvents.Reject(smith, RejectReasons.NotEnoughGold);
                return UpgradeOutcome.Refused;
            }
            if (_inventory.CountItems(smith, rule.MaterialItemId) < rule.MaterialCount)
            {
                CombatEvents.Reject(smith, RejectReasons.NoMaterials);
                return UpgradeOutcome.Refused;
            }
            // Gold and materials are both checked first, then both spent: the attempt is paid in full or not at all.
            if (step.GoldCost > 0)
            {
                _wallet.Transact(new[] { new WalletEntry { Player = smith, CurrencyId = gold, Amount = step.GoldCost } }, null,
                    $"upgrade:{item.SourceId}:{item.UpgradeLevel}:{_ctx.Time}", "upgrade");
            }
            _inventory.ConsumeItems(smith, rule.MaterialItemId, rule.MaterialCount);
            if (_ctx.Random.NextFloat() < step.SuccessChance)
            {
                item.UpgradeLevel++;
                Audit(AuditActions.UpgradeSuccess, smith, item);
                Notify(smith, NotificationKeys.UpgradeSuccess, item.UpgradeLevel, definition, true);
                return UpgradeOutcome.Success;
            }
            item.UpgradeLevel = System.Math.Max(0, item.UpgradeLevel - step.LevelLossOnFail);
            Audit(AuditActions.UpgradeFail, smith, item);
            if (step.BreakChance > 0f && _ctx.Random.NextFloat() < step.BreakChance)
            {
                Notify(smith, NotificationKeys.UpgradeBroken, 0, definition, false);
                return UpgradeOutcome.Broken;
            }
            Notify(smith, NotificationKeys.UpgradeFailed, item.UpgradeLevel, definition, false);
            return UpgradeOutcome.Failed;
        }

        private bool RuleFor(ItemInstance item, out UpgradeRuleDto rule)
        {
            rule = null;
            ItemDefinition definition = _ctx.Content.Items[item.DefinitionId];
            UpgradeableComponent upgradeable = definition.Get<UpgradeableComponent>();
            return upgradeable != null && definition.Rarity != Rarity.Divine && _ctx.Content.UpgradeRules.TryGetValue(upgradeable.RulesId, out rule);
        }

        public bool IsNearWhetstone(PlayerCharacter player, EntityId stationId) => IsNear(player, stationId, StationTypes.Whetstone);

        /// <summary>The station exists, is of this type and the player stands within InteractRange.</summary>
        public bool IsNear(PlayerCharacter player, EntityId stationId, string type)
        {
            ServerWorld world = _ctx.World;
            if (!world.Stations.Has(stationId) || world.Stations.Get(stationId).Type != type)
            {
                return false;
            }
            float distance = Vec3.DistanceXZ(world.Transforms.Get(player.EntityId).Position, world.Transforms.Get(stationId).Position);
            return distance <= _ctx.Content.Config.InteractRange;
        }

        private static void Notify(PlayerCharacter player, string key, int level, ItemDefinition definition, bool positive) =>
            player.Outbox.Notifications.Add(new NotificationMessage { TextKey = key, Number = level, ItemNameKey = definition.NameKey, IsPositive = positive });

        private void Audit(string action, PlayerCharacter player, ItemInstance item) =>
            _ctx.Audit.Write(new AuditEntry
            {
                Time = _ctx.Time,
                Action = action,
                ActorId = player.CharacterId,
                InstanceId = item.SourceId,
                DefinitionId = item.DefinitionId,
                Count = item.UpgradeLevel,
            });
    }
}
