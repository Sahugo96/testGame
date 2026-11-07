using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Pick up a drop from the ground (06, 7.8). The server checks who may, distance and bag room; the first one wins.</summary>
    public struct PickUpLootCommand : IGameCommand
    {
        public EntityId LootId;
    }
}
