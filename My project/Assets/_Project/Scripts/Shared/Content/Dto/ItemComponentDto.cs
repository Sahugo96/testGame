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
        public string StatusId;
        public string Resource;
        public float Amount;
        public string CooldownGroup;
        public float Cooldown;
        public int MaxStack;
        public string Rules;
        /// <summary>Sockets: allowed socket types, how many, with what chance.</summary>
        public string[] SocketTypes;
        public int SocketCount;
        public float SocketChance;
        /// <summary>SocketIngredient: which socket type it fits (its stats are in Stats).</summary>
        public string SocketType;
        /// <summary>Affixes: affix pool id.</summary>
        public string Pool;
    }
}
