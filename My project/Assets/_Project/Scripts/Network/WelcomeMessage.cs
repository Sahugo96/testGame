using Game.Shared.Core;

namespace Game.Network
{
    /// <summary>Server → client answer to Hello: the player's entity and timing of snapshots.</summary>
    public struct WelcomeMessage
    {
        public EntityId PlayerId;
        public int TickRate;
        public int WorldEveryTicks;
        public string Error;
    }
}
