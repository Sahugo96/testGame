using System;
using Game.Server.Players;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Friends list (module 14): names of other characters with their online state. The list belongs to the account
    /// (saved with it). A name can be added if that character is online or exists in the saves (NameExists).
    /// </summary>
    public sealed class FriendSystem
    {
        private readonly ServerContext _ctx;
        private readonly ChatSystem _chat;

        public FriendSystem(ServerContext ctx, ChatSystem chat)
        {
            _ctx = ctx;
            _chat = chat;
        }

        /// <summary>Saved characters' names (set by persistence); null — only online players can be added.</summary>
        public Func<string, bool> NameExists;

        /// <summary>The friends list changed (persistence saves the account).</summary>
        public event Action<PlayerCharacter> Changed;

        public void Add(PlayerCharacter player, string name)
        {
            name = (name ?? string.Empty).Trim();
            PlayerCharacter online = _chat.FindByName(name);
            if (online == player || name.Length == 0)
            {
                CombatEvents.Reject(player, RejectReasons.FriendInvalid);
                return;
            }
            if (online == null && (NameExists == null || !NameExists(name)))
            {
                CombatEvents.Reject(player, RejectReasons.ChatNoSuchPlayer);
                return;
            }
            if (IndexOf(player, name) >= 0)
            {
                CombatEvents.Reject(player, RejectReasons.FriendAlready);
                return;
            }
            if (player.Friends.Count >= _ctx.Content.Config.FriendsMax)
            {
                CombatEvents.Reject(player, RejectReasons.FriendListFull);
                return;
            }
            string shown = online != null ? _ctx.World.Identities.Get(online.EntityId).DisplayName : name;
            player.Friends.Add(shown);
            Changed?.Invoke(player);
        }

        public void Remove(PlayerCharacter player, string name)
        {
            int index = IndexOf(player, name);
            if (index < 0)
            {
                CombatEvents.Reject(player, RejectReasons.FriendInvalid);
                return;
            }
            player.Friends.RemoveAt(index);
            Changed?.Invoke(player);
        }

        public FriendSnapshot[] Snapshot(PlayerCharacter player)
        {
            var result = new FriendSnapshot[player.Friends.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new FriendSnapshot { Name = player.Friends[i], Online = _chat.FindByName(player.Friends[i]) != null };
            }
            return result;
        }

        private static int IndexOf(PlayerCharacter player, string name) =>
            player.Friends.FindIndex(f => string.Equals(f, (name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
