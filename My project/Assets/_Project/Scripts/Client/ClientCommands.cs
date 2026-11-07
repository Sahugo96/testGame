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
    }
}
