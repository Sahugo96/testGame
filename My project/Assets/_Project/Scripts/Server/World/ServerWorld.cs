using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>All entities of one zone/instance. No global state: every zone owns its world (principle 8).</summary>
    public sealed class ServerWorld
    {
        public readonly ComponentStore<IdentityData> Identities = new ComponentStore<IdentityData>();
        public readonly ComponentStore<TransformData> Transforms = new ComponentStore<TransformData>();
        public readonly ComponentStore<VitalsData> Vitals = new ComponentStore<VitalsData>();
        public readonly ComponentStore<ManaData> Mana = new ComponentStore<ManaData>();
        public readonly ComponentStore<CombatStateData> CombatStates = new ComponentStore<CombatStateData>();
        public readonly ComponentStore<CastData> Casts = new ComponentStore<CastData>();
        public readonly ComponentStore<MobData> Mobs = new ComponentStore<MobData>();

        private readonly List<EntityId> _entities = new List<EntityId>();
        private int _nextId = 1;

        public IReadOnlyList<EntityId> Entities => _entities;

        public EntityId CreateEntity()
        {
            var id = new EntityId(_nextId++);
            _entities.Add(id);
            return id;
        }

        public bool Exists(EntityId id) => Identities.Has(id);

        public bool IsAlive(EntityId id) => Vitals.Has(id) && !Vitals.Get(id).IsDead;
    }
}
