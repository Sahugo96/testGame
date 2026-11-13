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

        /// <summary>Online players by character name (any case): friends, whispers, mail and orders look players up by name.</summary>
        private readonly Dictionary<string, PlayerCharacter> _byName = new Dictionary<string, PlayerCharacter>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>The character's name ("" for an entity without one). The one place that reads it.</summary>
        public string NameOf(EntityId id) => World.Identities.Has(id) ? World.Identities.Get(id).DisplayName ?? string.Empty : string.Empty;

        public string NameOf(PlayerCharacter player) => NameOf(player.EntityId);

        /// <summary>The online player with this character name, or null. O(1): the name index is kept by AddPlayerName / RemovePlayerName.</summary>
        public PlayerCharacter FindPlayerByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            return _byName.TryGetValue(name.Trim(), out PlayerCharacter player) ? player : null;
        }

        /// <summary>Called when a player entity gets its identity (guests may share a name: the first keeps the index entry).</summary>
        public void AddPlayerName(PlayerCharacter player)
        {
            string name = NameOf(player);
            if (name.Length > 0 && !_byName.ContainsKey(name))
            {
                _byName[name] = player;
            }
        }

        /// <summary>Called before the player's entity is destroyed; another online player with the same name takes the entry.</summary>
        public void RemovePlayerName(PlayerCharacter player)
        {
            string name = NameOf(player);
            if (name.Length == 0 || !_byName.TryGetValue(name, out PlayerCharacter indexed) || indexed != player)
            {
                return;
            }
            _byName.Remove(name);
            foreach (PlayerCharacter other in Players.Values)
            {
                if (other != player && string.Equals(NameOf(other), name, System.StringComparison.OrdinalIgnoreCase))
                {
                    _byName[name] = other;
                    break;
                }
            }
        }

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
