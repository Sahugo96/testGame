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
            ItemDefinition definition = _ctx.Content.Items[item.DefinitionId];
            UpgradeableComponent upgradeable = definition.Get<UpgradeableComponent>();
            if (upgradeable == null || definition.Rarity == Rarity.Divine
                || !_ctx.Content.UpgradeRules.TryGetValue(upgradeable.RulesId, out UpgradeRuleDto rule))
            {
                CombatEvents.Reject(player, RejectReasons.CannotUpgrade);
                return;
            }
            if (item.UpgradeLevel >= rule.MaxLevel)
            {
                CombatEvents.Reject(player, RejectReasons.MaxUpgradeLevel);
                return;
            }
            UpgradeStepDto step = rule.Steps[item.UpgradeLevel];
            string gold = _ctx.Content.Config.GoldCurrencyId;
            if (_wallet.Balance(player, gold) < step.GoldCost)
            {
                CombatEvents.Reject(player, RejectReasons.NotEnoughGold);
                return;
            }
            if (_inventory.CountItems(player, rule.MaterialItemId) < rule.MaterialCount)
            {
                CombatEvents.Reject(player, RejectReasons.NoMaterials);
                return;
            }
            // Gold and materials are both checked first, then both spent: the attempt is paid in full or not at all.
            if (step.GoldCost > 0)
            {
                _wallet.Transact(new[] { new WalletEntry { Player = player, CurrencyId = gold, Amount = step.GoldCost } }, null,
                    $"upgrade:{item.SourceId}:{item.UpgradeLevel}:{_ctx.Time}", "upgrade");
            }
            _inventory.ConsumeItems(player, rule.MaterialItemId, rule.MaterialCount);
            bool success = _ctx.Random.NextFloat() < step.SuccessChance;
            if (success)
            {
                item.UpgradeLevel++;
                Audit(AuditActions.UpgradeSuccess, player, item);
                Notify(player, NotificationKeys.UpgradeSuccess, item.UpgradeLevel, definition, true);
            }
            else
            {
                item.UpgradeLevel = System.Math.Max(0, item.UpgradeLevel - step.LevelLossOnFail);
                Audit(AuditActions.UpgradeFail, player, item);
                if (step.BreakChance > 0f && _ctx.Random.NextFloat() < step.BreakChance)
                {
                    _inventory.DestroyOwned(player, item, equippedSlot);
                    Notify(player, NotificationKeys.UpgradeBroken, 0, definition, false);
                    return;
                }
                Notify(player, NotificationKeys.UpgradeFailed, item.UpgradeLevel, definition, false);
            }
            if (equippedSlot != null)
            {
                _stats.Recalculate(player);
            }
        }

        private bool IsNearWhetstone(PlayerCharacter player, EntityId stationId)
        {
            ServerWorld world = _ctx.World;
            if (!world.Stations.Has(stationId) || world.Stations.Get(stationId).Type != StationTypes.Whetstone)
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
