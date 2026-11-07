using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>Mob-specific state: where it respawns and after how long.</summary>
    public struct MobData
    {
        public Vec3 SpawnPosition;
        public float RespawnSeconds;
    }
}
