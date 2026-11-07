using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Crafting by recipes at a station (05, 7.4; developer decision v15: the anvil crafts). Checked at the start and at
    /// the end: the station type and distance, no combat, all inputs, room for the outputs. The craft takes CraftTime and
    /// stops (nothing spent) if the player walks away, enters combat or dies. At the end the inputs are spent and the chance
    /// is rolled on the server: success gives the outputs, failure destroys all inputs (question 19).
    /// Until professions (module 12) anyone may craft; profession level and XP are not used yet (question 73).
    /// </summary>
    public sealed class CraftingSystem
    {
        private readonly ServerContext _ctx;
        private readonly InventorySystem _inventory;
        private readonly ResourceSystem _resources;

        public CraftingSystem(ServerContext ctx, InventorySystem inventory, ResourceSystem resources)
        {
            _ctx = ctx;
            _inventory = inventory;
            _resources = resources;
        }

        public void Start(PlayerCharacter player, CraftItemCommand command)
        {
            if (player.Craft.Active)
            {
                CombatEvents.Reject(player, RejectReasons.AlreadyCrafting);
                return;
            }
            if (command.RecipeId == null || !_ctx.Content.Recipes.TryGetValue(command.RecipeId, out RecipeDto recipe))
            {
                CombatEvents.Reject(player, RejectReasons.RecipeUnknown);
                return;
            }
            if (!CanContinue(player, recipe, command.StationId, true) || !HasInputs(player, recipe, true) || !OutputsFit(player, recipe, true))
            {
                return;
            }
            player.Craft = new CraftData
            {
                Active = true,
                RecipeId = recipe.Id,
                StationId = command.StationId,
                Duration = recipe.CraftTime,
            };
            if (recipe.CraftTime <= 0f)
            {
                Complete(player, recipe);
            }
        }

        public void Cancel(PlayerCharacter player, bool notify)
        {
            if (!player.Craft.Active)
            {
                return;
            }
            player.Craft = default;
            if (notify)
            {
                player.Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.CraftCancelled });
            }
        }

        public void Update(float dt)
        {
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                if (!player.Craft.Active)
                {
                    continue;
                }
                RecipeDto recipe = _ctx.Content.Recipes[player.Craft.RecipeId];
                if (!CanContinue(player, recipe, player.Craft.StationId, false))
                {
                    Cancel(player, true);
                    continue;
                }
                player.Craft.Elapsed += dt;
                if (player.Craft.Elapsed >= player.Craft.Duration)
                {
                    Complete(player, recipe);
                }
            }
        }

        public float Progress(PlayerCharacter player) =>
            !player.Craft.Active || player.Craft.Duration <= 0f ? 0f : System.Math.Min(1f, player.Craft.Elapsed / player.Craft.Duration);

        /// <summary>Spends the inputs and rolls the chance; one transaction on the server (05, 7.4: failure destroys inputs).</summary>
        private void Complete(PlayerCharacter player, RecipeDto recipe)
        {
            player.Craft = default;
            if (!HasInputs(player, recipe, true) || !OutputsFit(player, recipe, true))
            {
                return;
            }
            foreach (ItemCountDto input in recipe.Inputs)
            {
                _inventory.ConsumeItems(player, input.ItemId, input.Count);
            }
            ItemCountDto main = recipe.Outputs[0];
            string mainName = _ctx.Content.Items[main.ItemId].NameKey;
            if (_ctx.Random.NextFloat() >= recipe.SuccessChance)
            {
                Log.Info(LogTags.Items, $"Craft '{recipe.Id}' failed for {player.CharacterId}: inputs destroyed");
                player.Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.CraftFailed, ItemNameKey = mainName });
                return;
            }
            foreach (ItemCountDto output in recipe.Outputs)
            {
                _inventory.GiveItem(player, output.ItemId, output.Count);
            }
            Log.Info(LogTags.Items, $"Craft '{recipe.Id}' succeeded for {player.CharacterId}");
            player.Outbox.Notifications.Add(new NotificationMessage
            {
                TextKey = NotificationKeys.CraftSuccess,
                Number = main.Count,
                ItemNameKey = mainName,
                IsPositive = true,
            });
        }

        private bool CanContinue(PlayerCharacter player, RecipeDto recipe, EntityId stationId, bool reject)
        {
            ServerWorld world = _ctx.World;
            string reason = null;
            if (!world.IsAlive(player.EntityId))
            {
                reason = RejectReasons.Dead;
            }
            else if (_resources.IsInCombat(player.EntityId))
            {
                reason = RejectReasons.CraftInCombat;
            }
            else if (!world.Stations.Has(stationId) || world.Stations.Get(stationId).Type != recipe.Station
                || Vec3.DistanceXZ(world.Transforms.Get(player.EntityId).Position, world.Transforms.Get(stationId).Position) > _ctx.Content.Config.InteractRange)
            {
                reason = RejectReasons.NotNearStation;
            }
            if (reason != null && reject)
            {
                CombatEvents.Reject(player, reason);
            }
            return reason == null;
        }

        private bool HasInputs(PlayerCharacter player, RecipeDto recipe, bool reject)
        {
            foreach (ItemCountDto input in recipe.Inputs)
            {
                if (_inventory.CountItems(player, input.ItemId) < input.Count)
                {
                    if (reject)
                    {
                        CombatEvents.Reject(player, RejectReasons.NoMaterials);
                    }
                    return false;
                }
            }
            return true;
        }

        private bool OutputsFit(PlayerCharacter player, RecipeDto recipe, bool reject)
        {
            foreach (ItemCountDto output in recipe.Outputs)
            {
                if (!_inventory.CanFit(player, output.ItemId, output.Count))
                {
                    if (reject)
                    {
                        CombatEvents.Reject(player, RejectReasons.BagFull);
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
