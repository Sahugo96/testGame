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
    }
}
