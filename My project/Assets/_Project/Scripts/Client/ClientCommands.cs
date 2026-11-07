using Game.Client.Net;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Client
{
    /// <summary>The only way views and input send intents to the server (views never touch the connection directly).</summary>
    public sealed class ClientCommands
    {
        private readonly IServerConnection _connection;
        private MoveCommand _lastMove;
        private int _clientSeq;

        public ClientCommands(IServerConnection connection) => _connection = connection;

        public void Move(float dirX, float dirZ)
        {
            if (dirX == _lastMove.DirX && dirZ == _lastMove.DirZ)
            {
                return;
            }
            _lastMove = new MoveCommand { DirX = dirX, DirZ = dirZ };
            _connection.Send(_lastMove);
        }

        public void UseSkill(string skillId, EntityId target, SkillModifier modifier) =>
            _connection.Send(new UseSkillCommand { SkillId = skillId, TargetId = target, Modifier = modifier, ClientSeq = ++_clientSeq });

        public void SelectTarget(EntityId target) => _connection.Send(new SelectTargetCommand { TargetId = target });

        public void CycleTarget() => _connection.Send(new CycleTargetCommand());

        public void Equip(int bagIndex) => _connection.Send(new EquipItemCommand { BagIndex = bagIndex });

        public void Unequip(string slotId) => _connection.Send(new UnequipItemCommand { SlotId = slotId });

        public void UseConsumable(int bagIndex) => _connection.Send(new UseConsumableCommand { BagIndex = bagIndex });

        public void UpgradeItem(string itemInstanceId, EntityId stationId) =>
            _connection.Send(new UpgradeItemCommand { ItemInstanceId = itemInstanceId, StationId = stationId });

        public void Craft(string recipeId, EntityId stationId) => _connection.Send(new CraftItemCommand { RecipeId = recipeId, StationId = stationId });

        public void CancelCraft() => _connection.Send(new CancelCraftCommand());

        public void InsertIngredient(string itemInstanceId, int socketIndex, int ingredientBagIndex, EntityId stationId) =>
            _connection.Send(new InsertIngredientCommand { ItemInstanceId = itemInstanceId, SocketIndex = socketIndex, IngredientBagIndex = ingredientBagIndex, StationId = stationId });

        public void SetAutoLoot(bool enabled) => _connection.Send(new SetAutoLootCommand { Enabled = enabled });

        public void PickUpLoot(EntityId lootId) => _connection.Send(new PickUpLootCommand { LootId = lootId });

        public void SpendAttributePoint(StatId attribute) => _connection.Send(new SpendAttributePointCommand { Attribute = attribute });

        public void RequestDuel(EntityId target) => _connection.Send(new DuelRequestCommand { TargetId = target });

        public void RespondDuel(bool accept) => _connection.Send(new DuelResponseCommand { Accept = accept });

        public void SurrenderDuel() => _connection.Send(new DuelSurrenderCommand());

        public void InviteToGroup(EntityId target) => _connection.Send(new GroupInviteCommand { TargetId = target });

        public void RespondGroupInvite(bool accept) => _connection.Send(new GroupInviteResponseCommand { Accept = accept });

        public void LeaveGroup() => _connection.Send(new GroupLeaveCommand());

        public void KickFromGroup(EntityId member) => _connection.Send(new GroupKickCommand { MemberId = member });

        public void PromoteToLeader(EntityId member) => _connection.Send(new GroupPromoteCommand { MemberId = member });

        public void ProposeLootMode(LootMode mode) => _connection.Send(new LootModeProposeCommand { Mode = mode });

        public void VoteLootMode(int proposalId, bool accept) => _connection.Send(new LootModeVoteCommand { ProposalId = proposalId, Accept = accept });
    }
}
