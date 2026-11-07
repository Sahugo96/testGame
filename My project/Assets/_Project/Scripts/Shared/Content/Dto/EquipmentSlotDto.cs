using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Equipment slot of a character (04, 7.9.1). MinRingSlots > 0 means the slot exists only for classes with that many ring slots.</summary>
    [Serializable]
    public class EquipmentSlotDto
    {
        public string Id;
        public string SlotType;
        public string Group;
        public string NameKey;
        public int MinRingSlots;
        public bool IsMainHand;
        public bool IsOffHand;
        /// <summary>Socket limit of items in this slot (04, 7.9.1); the same for the whole Group (validator).</summary>
        public int SocketCap;
    }
}
