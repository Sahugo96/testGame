using System.Collections.Generic;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Interest management (module 3; 01 §4 principle 5): every player gets only the entities within InterestRadius
    /// (found through a SpatialGrid), plus always themselves, their target, group members and duel opponent — so group
    /// frames and the target frame keep working at any distance. Combat events go only to players who see the source or
    /// the target (CombatEvents). Traffic per player then depends on how crowded the area is, not on how many play.
    /// </summary>
    public sealed class InterestSystem
    {
        private readonly ServerContext _ctx;
        private readonly GroupSystem _groups;
        private readonly DuelSystem _duels;
        private readonly SpatialGrid _grid;
        private readonly Dictionary<EntityId, int> _index = new Dictionary<EntityId, int>();
        private readonly List<EntityId> _candidates = new List<EntityId>();
        private readonly HashSet<EntityId> _picked = new HashSet<EntityId>();
        private readonly List<EntitySnapshot> _visible = new List<EntitySnapshot>();
        private EntitySnapshot[] _all = new EntitySnapshot[0];

        public InterestSystem(ServerContext ctx, GroupSystem groups, DuelSystem duels)
        {
            _ctx = ctx;
            _groups = groups;
            _duels = duels;
            _grid = new SpatialGrid(ctx.Content.Config.InterestCellSize);
        }

        /// <summary>Entities sent in the last tick, summed over players (metrics).</summary>
        public int LastTickEntitiesSent { get; private set; }

        /// <summary>Indexes the full world snapshot of this tick: grid by position, array index by id.</summary>
        public void Rebuild(WorldSnapshot world)
        {
            _all = world.Entities;
            _index.Clear();
            _grid.Clear();
            for (int i = 0; i < _all.Length; i++)
            {
                _index[_all[i].Id] = i;
                _grid.Add(_all[i].Id, _all[i].Position);
            }
            LastTickEntitiesSent = 0;
        }

        public WorldSnapshot BuildFor(PlayerCharacter player, WorldSnapshot world)
        {
            _visible.Clear();
            _picked.Clear();
            _candidates.Clear();
            float radius = _ctx.Content.Config.InterestRadius;
            if (_index.TryGetValue(player.EntityId, out int selfIndex))
            {
                Vec3 center = _all[selfIndex].Position;
                _grid.Query(center, radius, _candidates);
                foreach (EntityId id in _candidates)
                {
                    if (Vec3.DistanceXZ(_all[_index[id]].Position, center) <= radius)
                    {
                        Pick(id);
                    }
                }
            }
            Pick(player.EntityId);
            Pick(player.TargetId);
            foreach (EntityId member in _groups.CloseMates(player.EntityId))
            {
                Pick(member);
            }
            if (_duels.TryGet(player.EntityId, out DuelData duel))
            {
                Pick(duel.Opponent(player.EntityId));
            }
            LastTickEntitiesSent += _visible.Count;
            return new WorldSnapshot { Tick = world.Tick, ServerTime = world.ServerTime, Entities = _visible.ToArray() };
        }

        /// <summary>Does the player see this entity (in range or always-visible)? Used for combat events.</summary>
        public static bool InRange(ServerContext ctx, PlayerCharacter player, EntityId other)
        {
            if (other == player.EntityId)
            {
                return true;
            }
            if (!ctx.World.Transforms.Has(other) || !ctx.World.Transforms.Has(player.EntityId))
            {
                return false;
            }
            return Vec3.DistanceXZ(ctx.World.Transforms.Get(other).Position, ctx.World.Transforms.Get(player.EntityId).Position)
                <= ctx.Content.Config.InterestRadius;
        }

        private void Pick(EntityId id)
        {
            if (id.IsValid && _index.TryGetValue(id, out int i) && _picked.Add(id))
            {
                _visible.Add(_all[i]);
            }
        }
    }
}
