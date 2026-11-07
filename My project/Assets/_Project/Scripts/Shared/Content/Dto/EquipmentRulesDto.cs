using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>What a class may equip (03, 7.3; 04, 7.9.1).</summary>
    [Serializable]
    public class EquipmentRulesDto
    {
        public int RingSlots;
        public string[] AllowedArmorTypes;
        public string[] AllowedWeaponTypes;
    }
}
