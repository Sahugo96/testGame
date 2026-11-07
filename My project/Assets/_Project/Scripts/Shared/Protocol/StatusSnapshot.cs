namespace Game.Shared.Protocol
{
    /// <summary>A status on an entity, compact (03, 7.12): id, stacks, seconds left (0 = until removed).</summary>
    public struct StatusSnapshot
    {
        public string StatusId;
        public int Stacks;
        public float Remaining;
    }
}
