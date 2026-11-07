namespace Game.Shared.Protocol
{
    /// <summary>Kind of a replicated entity.</summary>
    public enum EntityKind
    {
        Player,
        Mob,
        Npc,
        /// <summary>A drop on the ground (LootEntry, 06 7.8): click to pick up.</summary>
        Loot,
    }
}
