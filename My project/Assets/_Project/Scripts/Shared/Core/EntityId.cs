using System;

namespace Game.Shared.Core
{
    /// <summary>Runtime id of a world entity issued by the server. Entities reference each other by id only (principle 12).</summary>
    [Serializable]
    public struct EntityId : IEquatable<EntityId>
    {
        public int Value;

        public EntityId(int value) => Value = value;

        public static EntityId None => new EntityId(0);

        public bool IsValid => Value != 0;

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(EntityId a, EntityId b) => a.Value == b.Value;
        public static bool operator !=(EntityId a, EntityId b) => a.Value != b.Value;
        public override string ToString() => $"#{Value}";
    }
}
