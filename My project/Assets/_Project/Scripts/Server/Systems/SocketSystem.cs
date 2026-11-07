using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Items;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Socket ingredients (04, 7.9; module 11): at a whetstone an ingredient of a matching type (or into a universal socket)
    /// goes into a socket of an owned item. One unit is spent and becomes part of the item; a previous ingredient in that
    /// socket is destroyed (question 16: extraction comes with professions). Worn items get their stats recalculated.
    /// </summary>
    public sealed class SocketSystem
    {
        private readonly ServerContext _ctx;
        private readonly InventorySystem _inventory;
        private readonly StatSystem _stats;

        public SocketSystem(ServerContext ctx, InventorySystem inventory, StatSystem stats)
        {
            _ctx = ctx;
            _inventory = inventory;
            _stats = stats;
        }

        public void Insert(PlayerCharacter player, InsertIngredientCommand command)
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
            if (command.SocketIndex < 0 || command.SocketIndex >= item.Sockets.Count)
            {
                CombatEvents.Reject(player, RejectReasons.SocketInvalid);
                return;
            }
            ItemInstance ingredientItem = player.Bag.Get(command.IngredientBagIndex);
            SocketIngredientComponent ingredient = ingredientItem == null ? null : _ctx.Content.Items[ingredientItem.DefinitionId].Get<SocketIngredientComponent>();
            if (ingredient == null)
            {
                CombatEvents.Reject(player, RejectReasons.NotIngredient);
                return;
            }
            ItemSocket socket = item.Sockets[command.SocketIndex];
            if (socket.Type != ingredient.SocketType && socket.Type != _ctx.Content.Config.UniversalSocketType)
            {
                CombatEvents.Reject(player, RejectReasons.SocketWrongType);
                return;
            }
            if (socket.IngredientId != null)
            {
                Audit(AuditActions.SocketDestroy, player, item, socket.IngredientId);
            }
            socket.IngredientId = ingredientItem.DefinitionId;
            ingredientItem.Count--;
            if (ingredientItem.Count <= 0)
            {
                player.Bag.Set(command.IngredientBagIndex, null);
            }
            Audit(AuditActions.SocketInsert, player, item, socket.IngredientId);
            if (equippedSlot != null)
            {
                _stats.Recalculate(player);
            }
        }

        private void Audit(string action, PlayerCharacter player, ItemInstance item, string ingredientId) =>
            _ctx.Audit.Write(new AuditEntry
            {
                Time = _ctx.Time,
                Action = action,
                ActorId = player.CharacterId,
                InstanceId = item.SourceId,
                DefinitionId = ingredientId,
                Count = 1,
            });

        private bool IsNearWhetstone(PlayerCharacter player, EntityId stationId)
        {
            ServerWorld world = _ctx.World;
            return world.Stations.Has(stationId) && world.Stations.Get(stationId).Type == StationTypes.Whetstone
                && Vec3.DistanceXZ(world.Transforms.Get(player.EntityId).Position, world.Transforms.Get(stationId).Position) <= _ctx.Content.Config.InteractRange;
        }
    }
}
