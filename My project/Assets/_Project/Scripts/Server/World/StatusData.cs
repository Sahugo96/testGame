using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>
    /// One status on an entity (03, 7.12): definition id, source, stacks, time left, tick timer, strength (the source's
    /// efficiency at application), shield left. SourceKey identifies its stat modifiers (StatModifier.SourceId).
    /// </summary>
    public struct StatusData
    {
        public string StatusId;
        public EntityId SourceId;
        public int Stacks;
        /// <summary>Seconds left; ignored when the definition's Duration is 0 (until removed).</summary>
        public float Remaining;
        public float TickTimer;
        public float Power;
        public float ShieldLeft;
        public string SourceKey;
    }
}
