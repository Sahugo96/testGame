using System;
using System.Collections.Generic;
using Game.Server.Players;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Friends (module 14). Friendship is mutual (decision of the developer): a request goes to the other account and waits
    /// until it is accepted; removing a friend removes it on both sides. Lists belong to accounts (online or saved, through
    /// <see cref="Store"/>); entries are character names and are matched by the account that owns the name, so two accounts
    /// are friends at most once whatever character either plays. The other side is told by a system line in its chat.
    /// </summary>
    public sealed class FriendSystem
    {
        private readonly ServerContext _ctx;
        private readonly ChatSystem _chat;

        public FriendSystem(ServerContext ctx, ChatSystem chat)
        {
            _ctx = ctx;
            _chat = chat;
            Store = new OnlineFriendStore(ctx, chat);
        }

        /// <summary>Persistence replaces it with the saves-backed store (offline accounts can be asked too).</summary>
        public IFriendStore Store;

        /// <summary>An online player's lists changed (persistence saves the account).</summary>
        public event Action<PlayerCharacter> Changed;

        /// <summary>Send a request; if that character already asked me, this accepts it.</summary>
        public void Request(PlayerCharacter player, string name)
        {
            name = (name ?? string.Empty).Trim();
            string target = name.Length == 0 ? null : Store.AccountOf(name);
            if (target == null)
            {
                CombatEvents.Reject(player, RejectReasons.ChatNoSuchPlayer);
                return;
            }
            string self = Store.AccountOf(player.EntityId);
            if (target == self)
            {
                CombatEvents.Reject(player, RejectReasons.FriendInvalid);
                return;
            }
            FriendLists mine = player.Friends;
            if (IndexOf(mine.Friends, target) >= 0)
            {
                CombatEvents.Reject(player, RejectReasons.FriendAlready);
                return;
            }
            if (IndexOf(mine.Outgoing, target) >= 0)
            {
                CombatEvents.Reject(player, RejectReasons.FriendRequestPending);
                return;
            }
            int incoming = IndexOf(mine.Incoming, target);
            if (incoming >= 0)
            {
                Accept(player, self, mine.Incoming[incoming], target);
                return;
            }
            int max = _ctx.Content.Config.FriendsMax;
            if (mine.Friends.Count + mine.Outgoing.Count >= max)
            {
                CombatEvents.Reject(player, RejectReasons.FriendListFull);
                return;
            }
            FriendLists theirs = Store.Load(target, out EntityId online);
            if (theirs.Incoming.Count >= max)
            {
                CombatEvents.Reject(player, RejectReasons.FriendTargetFull);
                return;
            }
            mine.Outgoing.Add(ShownName(name));
            theirs.Incoming.Add(NameOf(player));
            Changed?.Invoke(player);
            Commit(target, theirs, online);
            Tell(online, NotificationKeys.FriendRequestReceived, NameOf(player));
        }

        public void Respond(PlayerCharacter player, string name, bool accept)
        {
            FriendLists mine = player.Friends;
            string other = Store.AccountOf((name ?? string.Empty).Trim());
            int index = IndexOf(mine.Incoming, other, name);
            if (index < 0)
            {
                CombatEvents.Reject(player, RejectReasons.FriendRequestNotFound);
                return;
            }
            string self = Store.AccountOf(player.EntityId);
            if (accept && other != null)
            {
                Accept(player, self, mine.Incoming[index], other);
                return;
            }
            mine.Incoming.RemoveAt(index);
            Changed?.Invoke(player);
            Unlink(other, self, lists => lists.Outgoing);
        }

        /// <summary>Remove a friend on both sides, withdraw my request, or decline theirs.</summary>
        public void Remove(PlayerCharacter player, string name)
        {
            FriendLists mine = player.Friends;
            string other = Store.AccountOf((name ?? string.Empty).Trim());
            string self = Store.AccountOf(player.EntityId);
            if (RemoveFrom(mine.Friends, other, name))
            {
                Changed?.Invoke(player);
                Unlink(other, self, lists => lists.Friends);
                return;
            }
            if (RemoveFrom(mine.Outgoing, other, name))
            {
                Changed?.Invoke(player);
                Unlink(other, self, lists => lists.Incoming);
                return;
            }
            if (RemoveFrom(mine.Incoming, other, name))
            {
                Changed?.Invoke(player);
                Unlink(other, self, lists => lists.Outgoing);
                return;
            }
            CombatEvents.Reject(player, RejectReasons.FriendInvalid);
        }

        public FriendSnapshot[] Snapshot(PlayerCharacter player)
        {
            FriendLists lists = player.Friends;
            var result = new List<FriendSnapshot>(lists.Friends.Count + lists.Incoming.Count + lists.Outgoing.Count);
            AddEntries(result, lists.Incoming, FriendState.Incoming);
            AddEntries(result, lists.Friends, FriendState.Friend);
            AddEntries(result, lists.Outgoing, FriendState.Outgoing);
            return result.ToArray();
        }

        private void Accept(PlayerCharacter player, string self, string entry, string other)
        {
            FriendLists mine = player.Friends;
            int max = _ctx.Content.Config.FriendsMax;
            if (mine.Friends.Count >= max)
            {
                CombatEvents.Reject(player, RejectReasons.FriendListFull);
                return;
            }
            FriendLists theirs = Store.Load(other, out EntityId online);
            if (theirs.Friends.Count >= max)
            {
                CombatEvents.Reject(player, RejectReasons.FriendTargetFull);
                return;
            }
            RemoveFrom(mine.Incoming, other, entry);
            RemoveFrom(mine.Outgoing, other, entry);
            RemoveAccount(theirs.Outgoing, self);
            RemoveAccount(theirs.Incoming, self);
            mine.Friends.Add(entry);
            theirs.Friends.Add(NameOf(player));
            Changed?.Invoke(player);
            Commit(other, theirs, online);
            Tell(online, NotificationKeys.FriendAccepted, NameOf(player));
        }

        /// <summary>Removes every entry of my account from one of the other account's lists.</summary>
        private void Unlink(string other, string self, Func<FriendLists, List<string>> list)
        {
            if (other == null)
            {
                return;
            }
            FriendLists theirs = Store.Load(other, out EntityId online);
            if (RemoveAccount(list(theirs), self))
            {
                Commit(other, theirs, online);
            }
        }

        private void Commit(string account, FriendLists lists, EntityId online)
        {
            PlayerCharacter player = online.IsValid ? _ctx.GetPlayer(online) : null;
            if (player != null)
            {
                Changed?.Invoke(player);
            }
            else
            {
                Store.Save(account, lists);
            }
        }

        private void Tell(EntityId online, string key, string name)
        {
            PlayerCharacter player = online.IsValid ? _ctx.GetPlayer(online) : null;
            if (player != null)
            {
                _chat.SystemToPlayer(player, key, name);
            }
        }

        private void AddEntries(List<FriendSnapshot> result, List<string> names, FriendState state)
        {
            foreach (string name in names)
            {
                result.Add(new FriendSnapshot { Name = name, Online = _chat.FindByName(name) != null, State = state });
            }
        }

        /// <summary>First entry owned by the account (or, if the character no longer exists, with exactly that name).</summary>
        private int IndexOf(List<string> list, string account, string name = null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                string owner = Store.AccountOf(list[i]);
                if (account != null ? owner == account : string.Equals(list[i], (name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private bool RemoveFrom(List<string> list, string account, string name)
        {
            int index = IndexOf(list, account, name);
            if (index < 0)
            {
                return false;
            }
            list.RemoveAt(index);
            return true;
        }

        private bool RemoveAccount(List<string> list, string account)
        {
            if (account == null)
            {
                return false;
            }
            return list.RemoveAll(entry => Store.AccountOf(entry) == account) > 0;
        }

        /// <summary>The name as the character spells it when online, else as typed.</summary>
        private string ShownName(string name)
        {
            PlayerCharacter online = _chat.FindByName(name);
            return online != null ? NameOf(online) : name;
        }

        private string NameOf(PlayerCharacter player) => _ctx.World.Identities.Get(player.EntityId).DisplayName ?? string.Empty;
    }
}
