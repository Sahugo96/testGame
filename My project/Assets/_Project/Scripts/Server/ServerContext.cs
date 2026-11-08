using System.Collections.Generic;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;

namespace Game.Server
{
    /// <summary>Shared services of one server world, passed to systems and effect handlers.</summary>
    public sealed class ServerContext
    {
        public ContentDatabase Content;
        public ServerWorld World;
        public IRandom Random;
        public IAuditLog Audit;
        public ItemFactory Items;
        public float Time;
        /// <summary>Real time (UTC) for shops, mail and orders (module 20).</summary>
        public IClock Clock = new SystemClock();

        /// <summary>Unix seconds of <see cref="Clock"/>.</summary>
        public long Now => new System.DateTimeOffset(Clock.UtcNow).ToUnixTimeSeconds();
        public readonly Dictionary<EntityId, PlayerCharacter> Players = new Dictionary<EntityId, PlayerCharacter>();

        public PlayerCharacter GetPlayer(EntityId id) => Players.TryGetValue(id, out PlayerCharacter player) ? player : null;

        /// <summary>Players whose items or gold moved to another player this tick (trade): saved together after the tick.</summary>
        public readonly HashSet<EntityId> Committed = new HashSet<EntityId>();

        public void Commit(EntityId player)
        {
            if (player.IsValid)
            {
                Committed.Add(player);
            }
        }
    }
}
