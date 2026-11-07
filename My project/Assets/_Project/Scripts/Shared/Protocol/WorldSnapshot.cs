namespace Game.Shared.Protocol
{
    /// <summary>Entities visible to one player at a server tick.</summary>
    public sealed class WorldSnapshot
    {
        public int Tick;
        public float ServerTime;
        public EntitySnapshot[] Entities;
    }
}
