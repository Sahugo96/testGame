using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Item a mob drops on death with a chance (simple solo drop until loot tables and LootEntry, module 10).</summary>
    [Serializable]
    public class MobDropDto
    {
        public string ItemId;
        public float Chance;
        public int Count;
    }
}
