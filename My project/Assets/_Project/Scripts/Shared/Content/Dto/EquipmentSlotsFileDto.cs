using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/equipment_slots.json (merged across packs).</summary>
    [Serializable]
    public class EquipmentSlotsFileDto
    {
        public EquipmentSlotDto[] Slots;
    }
}
