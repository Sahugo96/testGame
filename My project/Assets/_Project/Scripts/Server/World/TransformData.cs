using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>Position, facing and movement intent of an entity.</summary>
    public struct TransformData
    {
        public Vec3 Position;
        public float Yaw;
        public Vec3 MoveDirection;
    }
}
