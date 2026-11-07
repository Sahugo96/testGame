using System.Collections.Generic;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>Target selection (module 6 basics): click selects any living entity; Tab cycles hostile ones by distance.</summary>
    public sealed class TargetingSystem
    {
        private readonly ServerContext _ctx;
        private readonly CombatSystem _combat;
        private readonly List<(float distance, EntityId id)> _candidates = new List<(float, EntityId)>();

        public TargetingSystem(ServerContext ctx, CombatSystem combat)
        {
            _ctx = ctx;
            _combat = combat;
        }

        public void Select(PlayerCharacter player, EntityId targetId)
        {
            if (targetId.IsValid && !_ctx.World.IsAlive(targetId))
            {
                CombatEvents.Reject(player, RejectReasons.InvalidTarget);
                return;
            }
            player.TargetId = targetId;
        }

        public void Cycle(PlayerCharacter player)
        {
            ServerWorld world = _ctx.World;
            Vec3 origin = world.Transforms.Get(player.EntityId).Position;
            float range = _ctx.Content.Config.TargetCycleRange;
            _candidates.Clear();
            foreach (EntityId id in world.Entities)
            {
                if (id == player.EntityId || !world.IsAlive(id) || !_combat.AreHostile(player.EntityId, id))
                {
                    continue;
                }
                float distance = Vec3.DistanceXZ(origin, world.Transforms.Get(id).Position);
                if (distance <= range)
                {
                    _candidates.Add((distance, id));
                }
            }
            if (_candidates.Count == 0)
            {
                CombatEvents.Reject(player, RejectReasons.NoTarget);
                return;
            }
            _candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            int current = _candidates.FindIndex(c => c.id == player.TargetId);
            player.TargetId = _candidates[(current + 1) % _candidates.Count].id;
        }
    }
}
