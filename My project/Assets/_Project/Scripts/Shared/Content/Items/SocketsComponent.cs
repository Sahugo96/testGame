using System.Collections.Generic;

namespace Game.Shared.Content.Items
{
    /// <summary>
    /// SocketRules (04, 7.9): with Chance a new instance gets Count sockets (never more than the slot's SocketCap); each socket
    /// takes one of the allowed Types in turn. Requires EquipSlot.
    /// </summary>
    public sealed class SocketsComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Sockets;

        public readonly List<string> Types = new List<string>();
        public int Count;
        public float Chance;
    }
}
