using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Test location layout. File: Resources/Content/world.json.</summary>
    [Serializable]
    public class WorldDto
    {
        public float PlayerSpawnX;
        public float PlayerSpawnZ;
        public SpawnDto[] Spawns;
        public StationDto[] Stations;
        /// <summary>How a player's merchant looks (spawned on a shop spot while the shop is open).</summary>
        public StationDto MerchantVisual;
    }
}
