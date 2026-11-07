using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>One component of an item; Type selects the registered component factory, other fields are its parameters.</summary>
    [Serializable]
    public class ItemComponentDto
    {
        public string Type;
        public string SlotType;
        public string ArmorType;
        public string WeaponType;
        public int Hands;
        public StatModifierDto[] Stats;
        public string Effect;
        public string Resource;
        public float Amount;
        public string CooldownGroup;
        public float Cooldown;
        public int MaxStack;
    }
}
