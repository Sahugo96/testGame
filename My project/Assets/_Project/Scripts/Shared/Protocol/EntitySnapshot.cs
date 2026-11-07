using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Replicated state of one entity for rendering.</summary>
    public struct EntitySnapshot
    {
        public EntityId Id;
        public EntityKind Kind;
        public string DefinitionId;
        public string NameKey;
        public string Faction;
        public Vec3 Position;
        public float Yaw;
        public float Health;
        public float MaxHealth;
        public bool IsDead;
        public bool IsCasting;
    }
}
