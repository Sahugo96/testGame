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

        /// <summary>Player-chosen name (raw text, not a localization key); empty for mobs and NPCs.</summary>
        public string DisplayName;
        public string Faction;
        public Vec3 Position;
        public float Yaw;
        public float Health;
        public float MaxHealth;
        public bool IsDead;
        public bool IsCasting;
        /// <summary>Statuses on the entity (03, 7.12): seen by everyone who sees the entity.</summary>
        public StatusSnapshot[] Statuses;
        /// <summary>Who the entity is attacking: a player's target, a mob's top threat ("target of target").</summary>
        public EntityId TargetOfId;

        /// <summary>
        /// Wire only: the server left out position, health, statuses and target because they are the same as in the last
        /// snapshot it sent to this client. NetworkClientSession fills them in from that snapshot and clears the flag.
        /// </summary>
        public bool DynamicOmitted;
    }
}
