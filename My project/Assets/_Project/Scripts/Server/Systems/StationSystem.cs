using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Core;

namespace Game.Server.Systems
{
    /// <summary>Where the player stands relative to NPC stations (market, mailbox, shop spots, merchants): within InteractRange.</summary>
    public sealed class StationSystem
    {
        private readonly ServerContext _ctx;

        public StationSystem(ServerContext ctx) => _ctx = ctx;

        public bool IsNear(PlayerCharacter player, EntityId stationId, string type)
        {
            ServerWorld world = _ctx.World;
            if (!world.Stations.Has(stationId) || world.Stations.Get(stationId).Type != type || !world.Transforms.Has(player.EntityId))
            {
                return false;
            }
            return Vec3.DistanceXZ(world.Transforms.Get(player.EntityId).Position, world.Transforms.Get(stationId).Position) <= _ctx.Content.Config.InteractRange;
        }

        /// <summary>Near any station of the type (there may be several mailboxes or market NPCs).</summary>
        public bool IsNearAny(PlayerCharacter player, string type)
        {
            ComponentStore<StationData> stations = _ctx.World.Stations;
            for (int i = 0; i < stations.Count; i++)
            {
                if (stations.At(i).Type == type && IsNear(player, stations.OwnerAt(i), type))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
