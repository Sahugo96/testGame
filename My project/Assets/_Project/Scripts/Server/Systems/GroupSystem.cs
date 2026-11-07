using System;
using System.Collections.Generic;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Groups (06, 7.8; module 14): a group always has a leader (the creator); the leader invites, kicks and promotes;
    /// when the leader leaves, leadership passes to the next member by join order. The loot mode starts as "PickedUp"
    /// and changes only by a unanimous vote of all members (30 s, a missing vote counts as "no", a failed vote delays
    /// the next proposal, any change of membership cancels the vote).
    /// Stage simplification (question 71): a disconnect is leaving — characters are not saved yet, so there are no
    /// offline members; a group of one is disbanded.
    /// </summary>
    public sealed class GroupSystem
    {
        private readonly ServerContext _ctx;
        private readonly Dictionary<int, GroupData> _groups = new Dictionary<int, GroupData>();
        private readonly Dictionary<EntityId, int> _byMember = new Dictionary<EntityId, int>();
        private readonly Dictionary<EntityId, GroupInviteData> _invites = new Dictionary<EntityId, GroupInviteData>();
        private readonly List<int> _ids = new List<int>();
        private readonly List<EntityId> _expired = new List<EntityId>();
        private int _nextGroupId = 1;
        private int _nextProposalId = 1;

        public GroupSystem(ServerContext ctx) => _ctx = ctx;

        private GameConfig Config => _ctx.Content.Config;

        public bool TryGetGroup(EntityId member, out GroupData group)
        {
            if (_byMember.TryGetValue(member, out int id))
            {
                group = _groups[id];
                return true;
            }
            group = default;
            return false;
        }

        public bool TryGetGroupById(int groupId, out GroupData group) => _groups.TryGetValue(groupId, out group);

        public bool AreGroupmates(EntityId a, EntityId b) =>
            _byMember.TryGetValue(a, out int ga) && _byMember.TryGetValue(b, out int gb) && ga == gb;

        public void Invite(PlayerCharacter player, EntityId targetId)
        {
            EntityId self = player.EntityId;
            if (targetId == self || _ctx.GetPlayer(targetId) == null)
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotPlayer);
                return;
            }
            if (TryGetGroup(self, out GroupData group))
            {
                if (!CanManage(group, self))
                {
                    CombatEvents.Reject(player, RejectReasons.GroupNotLeader);
                    return;
                }
                if (group.Members.Count >= MaxSize(group))
                {
                    CombatEvents.Reject(player, RejectReasons.GroupFull);
                    return;
                }
            }
            if (_byMember.ContainsKey(targetId))
            {
                CombatEvents.Reject(player, RejectReasons.GroupTargetInGroup);
                return;
            }
            if (_invites.ContainsKey(targetId))
            {
                CombatEvents.Reject(player, RejectReasons.GroupTargetHasInvite);
                return;
            }
            _invites[targetId] = new GroupInviteData { FromId = self, ExpiresAt = _ctx.Time + Config.GroupInviteSeconds };
            Notify(targetId, NotificationKeys.GroupInvited, true);
        }

        public void RespondInvite(PlayerCharacter player, bool accept)
        {
            EntityId self = player.EntityId;
            if (!_invites.TryGetValue(self, out GroupInviteData invite))
            {
                CombatEvents.Reject(player, RejectReasons.GroupNoInvite);
                return;
            }
            _invites.Remove(self);
            if (!accept)
            {
                Notify(invite.FromId, NotificationKeys.GroupInviteDeclined, false);
                return;
            }
            bool inviterHasGroup = TryGetGroup(invite.FromId, out GroupData group);
            if (_ctx.GetPlayer(invite.FromId) == null || (inviterHasGroup && !CanManage(group, invite.FromId)))
            {
                // The inviter left or is no longer a leader: the invite is void.
                CombatEvents.Reject(player, RejectReasons.GroupNoInvite);
                return;
            }
            if (!inviterHasGroup)
            {
                group = CreateGroup(invite.FromId);
            }
            if (group.Members.Count >= MaxSize(group))
            {
                CombatEvents.Reject(player, RejectReasons.GroupFull);
                return;
            }
            NotifyGroup(group, NotificationKeys.GroupMemberJoined, true);
            AddMember(ref group, self);
            Notify(self, NotificationKeys.GroupJoined, true);
        }

        public void Leave(PlayerCharacter player)
        {
            if (!_byMember.ContainsKey(player.EntityId))
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotInGroup);
                return;
            }
            RemoveMember(player.EntityId, NotificationKeys.GroupLeft);
        }

        public void Kick(PlayerCharacter player, EntityId memberId)
        {
            if (!RequireLeader(player, out GroupData group))
            {
                return;
            }
            if (memberId == player.EntityId || !group.Members.Contains(memberId))
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotMember);
                return;
            }
            RemoveMember(memberId, NotificationKeys.GroupKicked);
        }

        public void Promote(PlayerCharacter player, EntityId memberId)
        {
            if (!RequireLeader(player, out GroupData group))
            {
                return;
            }
            if (memberId == player.EntityId || !group.Members.Contains(memberId))
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotMember);
                return;
            }
            group.LeaderId = memberId;
            _groups[group.Id] = group;
            NotifyGroup(group, NotificationKeys.GroupNewLeader, true);
        }

        public void Propose(PlayerCharacter player, LootMode mode)
        {
            if (!TryGetGroup(player.EntityId, out GroupData group))
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotInGroup);
                return;
            }
            if (group.Vote.Active)
            {
                CombatEvents.Reject(player, RejectReasons.LootVoteActive);
                return;
            }
            if (mode == group.LootMode)
            {
                CombatEvents.Reject(player, RejectReasons.LootVoteSameMode);
                return;
            }
            if (_ctx.Time < group.ProposeAllowedAt)
            {
                CombatEvents.Reject(player, RejectReasons.LootVoteCooldown);
                return;
            }
            if (!Voters(group).Contains(player.EntityId))
            {
                CombatEvents.Reject(player, RejectReasons.LootVoteNotVoter);
                return;
            }
            if (!HasQuorum(group))
            {
                CombatEvents.Reject(player, RejectReasons.LootVoteNoQuorum);
                return;
            }
            group.Vote = new LootModeVote
            {
                Active = true,
                ProposalId = _nextProposalId++,
                ProposerId = player.EntityId,
                NewMode = mode,
                VotesFor = new List<EntityId> { player.EntityId },
                ExpiresAt = _ctx.Time + Config.LootVoteSeconds,
            };
            _groups[group.Id] = group;
            Log.Info(LogTags.Server, $"Group {group.Id}: {player.EntityId} proposes loot mode {mode} (vote {group.Vote.ProposalId})");
            NotifyGroup(group, NotificationKeys.LootVoteStarted, true);
            ResolveIfUnanimous(group);
        }

        public void Vote(PlayerCharacter player, int proposalId, bool accept)
        {
            EntityId self = player.EntityId;
            if (!TryGetGroup(self, out GroupData group) || !group.Vote.Active || group.Vote.ProposalId != proposalId)
            {
                CombatEvents.Reject(player, RejectReasons.LootVoteNotFound);
                return;
            }
            if (!Voters(group).Contains(self))
            {
                CombatEvents.Reject(player, RejectReasons.LootVoteNotVoter);
                return;
            }
            Log.Info(LogTags.Server, $"Group {group.Id}: {self} votes {(accept ? "for" : "against")} (vote {proposalId})");
            if (!accept)
            {
                // Unanimity is impossible after one "no": the vote fails at once.
                FailVote(group, NotificationKeys.LootVoteFailed);
                return;
            }
            if (!group.Vote.VotesFor.Contains(self))
            {
                group.Vote.VotesFor.Add(self);
            }
            ResolveIfUnanimous(group);
        }

        public void Update()
        {
            _expired.Clear();
            foreach (KeyValuePair<EntityId, GroupInviteData> pair in _invites)
            {
                if (_ctx.Time >= pair.Value.ExpiresAt)
                {
                    _expired.Add(pair.Key);
                }
            }
            foreach (EntityId invitee in _expired)
            {
                GroupInviteData invite = _invites[invitee];
                _invites.Remove(invitee);
                Notify(invitee, NotificationKeys.GroupInviteExpired, false);
                Notify(invite.FromId, NotificationKeys.GroupInviteExpired, false);
            }

            _ids.Clear();
            _ids.AddRange(_groups.Keys);
            foreach (int id in _ids)
            {
                if (_groups.TryGetValue(id, out GroupData group) && group.Vote.Active && _ctx.Time >= group.Vote.ExpiresAt)
                {
                    // A missing vote counts as "no" (question 21).
                    FailVote(group, NotificationKeys.LootVoteFailed);
                }
            }
        }

        /// <summary>Disconnect = leaving the group in this stage (question 71); pending invites from and to the player are dropped.</summary>
        public void OnPlayerRemoved(EntityId id)
        {
            _invites.Remove(id);
            _expired.Clear();
            foreach (KeyValuePair<EntityId, GroupInviteData> pair in _invites)
            {
                if (pair.Value.FromId == id)
                {
                    _expired.Add(pair.Key);
                }
            }
            foreach (EntityId invitee in _expired)
            {
                _invites.Remove(invitee);
            }
            if (_byMember.ContainsKey(id))
            {
                RemoveMember(id, null);
            }
        }

        /// <summary>Loot log line visible to all members (06, 7.8).</summary>
        public void AddLog(int groupId, GroupLogEntry entry)
        {
            if (!_groups.TryGetValue(groupId, out GroupData group))
            {
                return;
            }
            group.Log.Add(entry);
            if (group.Log.Count > Config.GroupLogSize)
            {
                group.Log.RemoveAt(0);
            }
        }

        /// <summary>"По очереди": the receiver goes to the back of the queue.</summary>
        public void MoveToQueueEnd(int groupId, EntityId member)
        {
            if (_groups.TryGetValue(groupId, out GroupData group) && group.LootQueue.Remove(member))
            {
                group.LootQueue.Add(member);
            }
        }

        public GroupSnapshot Snapshot(EntityId id)
        {
            var snapshot = new GroupSnapshot();
            if (_invites.TryGetValue(id, out GroupInviteData invite))
            {
                snapshot.InviteFromId = invite.FromId;
                snapshot.InviteRemaining = Math.Max(0f, invite.ExpiresAt - _ctx.Time);
            }
            if (!TryGetGroup(id, out GroupData group))
            {
                return snapshot;
            }
            snapshot.InGroup = true;
            snapshot.LeaderId = group.LeaderId;
            snapshot.LootMode = group.LootMode;
            snapshot.Members = group.Members.ToArray();
            snapshot.IsRaid = group.IsRaid;
            snapshot.Subgroups = group.Members.ConvertAll(m => group.Subgroups.TryGetValue(m, out int sub) ? sub : 0).ToArray();
            snapshot.Assistants = new List<EntityId>(group.Assistants).ToArray();
            snapshot.Voters = Voters(group).ToArray();
            snapshot.ProposeCooldownRemaining = Math.Max(0f, group.ProposeAllowedAt - _ctx.Time);
            snapshot.Log = group.Log.ToArray();
            if (group.Vote.Active)
            {
                snapshot.VoteActive = true;
                snapshot.VoteProposalId = group.Vote.ProposalId;
                snapshot.VoteProposerId = group.Vote.ProposerId;
                snapshot.VoteMode = group.Vote.NewMode;
                snapshot.VoteRemaining = Math.Max(0f, group.Vote.ExpiresAt - _ctx.Time);
                snapshot.VotesFor = group.Vote.VotesFor.ToArray();
            }
            return snapshot;
        }

        /// <summary>Leader: the group becomes a raid (up to RaidMaxSize, subgroups of RaidSubgroupSize by join order).</summary>
        public void ConvertToRaid(PlayerCharacter player)
        {
            if (!RequireLeader(player, out GroupData group))
            {
                return;
            }
            if (group.IsRaid)
            {
                CombatEvents.Reject(player, RejectReasons.GroupAlreadyRaid);
                return;
            }
            group.IsRaid = true;
            for (int i = 0; i < group.Members.Count; i++)
            {
                group.Subgroups[group.Members[i]] = i / Config.RaidSubgroupSize;
            }
            _groups[group.Id] = group;
            CancelVote(group);
            NotifyGroup(group, NotificationKeys.GroupBecameRaid, true);
        }

        /// <summary>Leader or assistant: move a raid member to another subgroup that has room.</summary>
        public void MoveToSubgroup(PlayerCharacter player, EntityId member, int subgroup)
        {
            if (!TryGetGroup(player.EntityId, out GroupData group) || !CanManage(group, player.EntityId))
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotLeader);
                return;
            }
            if (!group.IsRaid || !group.Members.Contains(member) || subgroup < 0 || subgroup >= SubgroupCount
                || CountIn(group, subgroup) >= Config.RaidSubgroupSize)
            {
                CombatEvents.Reject(player, RejectReasons.GroupSubgroupInvalid);
                return;
            }
            group.Subgroups[member] = subgroup;
            _groups[group.Id] = group;
            CancelVote(group);
        }

        /// <summary>Leader: make a raid member an assistant (may invite and move members) or take it back.</summary>
        public void SetAssistant(PlayerCharacter player, EntityId member, bool enabled)
        {
            if (!RequireLeader(player, out GroupData group))
            {
                return;
            }
            if (!group.IsRaid || !group.Members.Contains(member) || member == group.LeaderId)
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotMember);
                return;
            }
            if (enabled)
            {
                group.Assistants.Add(member);
            }
            else
            {
                group.Assistants.Remove(member);
            }
        }

        /// <summary>
        /// Who votes on the loot mode: everyone in a group; in a raid the raid leader and the subgroup leaders
        /// (question 20; a subgroup leader is its first member by join order).
        /// </summary>
        public List<EntityId> Voters(GroupData group)
        {
            if (!group.IsRaid)
            {
                return new List<EntityId>(group.Members);
            }
            var voters = new List<EntityId> { group.LeaderId };
            for (int subgroup = 0; subgroup < SubgroupCount; subgroup++)
            {
                foreach (EntityId member in group.Members)
                {
                    if (group.Subgroups[member] == subgroup)
                    {
                        if (!voters.Contains(member))
                        {
                            voters.Add(member);
                        }
                        break;
                    }
                }
            }
            return voters;
        }

        /// <summary>Members always visible and shown in the frames: the whole group, or the own subgroup in a raid.</summary>
        public List<EntityId> CloseMates(EntityId member)
        {
            var result = new List<EntityId>();
            if (!TryGetGroup(member, out GroupData group))
            {
                return result;
            }
            int own = group.Subgroups.TryGetValue(member, out int value) ? value : 0;
            foreach (EntityId other in group.Members)
            {
                if (!group.IsRaid || group.Subgroups[other] == own)
                {
                    result.Add(other);
                }
            }
            return result;
        }

        private int SubgroupCount => Config.RaidMaxSize / Config.RaidSubgroupSize;

        private int MaxSize(GroupData group) => group.IsRaid ? Config.RaidMaxSize : Config.GroupMaxSize;

        private static bool CanManage(GroupData group, EntityId id) => group.LeaderId == id || (group.IsRaid && group.Assistants.Contains(id));

        private int CountIn(GroupData group, int subgroup)
        {
            int count = 0;
            foreach (int value in group.Subgroups.Values)
            {
                count += value == subgroup ? 1 : 0;
            }
            return count;
        }

        private int FreeSubgroup(GroupData group)
        {
            if (!group.IsRaid)
            {
                return 0;
            }
            for (int subgroup = 0; subgroup < SubgroupCount; subgroup++)
            {
                if (CountIn(group, subgroup) < Config.RaidSubgroupSize)
                {
                    return subgroup;
                }
            }
            return 0;
        }

        private GroupData CreateGroup(EntityId leader)
        {
            var group = new GroupData
            {
                Id = _nextGroupId++,
                LeaderId = leader,
                Members = new List<EntityId>(),
                LootMode = LootMode.PickedUp,
                LootQueue = new List<EntityId>(),
                Subgroups = new Dictionary<EntityId, int>(),
                Assistants = new HashSet<EntityId>(),
                Log = new List<GroupLogEntry>(),
            };
            _groups.Add(group.Id, group);
            AddMember(ref group, leader);
            Log.Info(LogTags.Server, $"Group {group.Id} created by {leader}");
            return group;
        }

        private void AddMember(ref GroupData group, EntityId member)
        {
            group.Members.Add(member);
            group.LootQueue.Add(member);
            group.Subgroups[member] = FreeSubgroup(group);
            _byMember[member] = group.Id;
            _groups[group.Id] = group;
            CancelVote(group);
        }

        /// <summary>Removes a member; passes leadership by join order; a group of one is disbanded.</summary>
        private void RemoveMember(EntityId member, string memberKey)
        {
            GroupData group = _groups[_byMember[member]];
            group.Members.Remove(member);
            group.LootQueue.Remove(member);
            group.Subgroups.Remove(member);
            group.Assistants.Remove(member);
            _byMember.Remove(member);
            if (memberKey != null)
            {
                Notify(member, memberKey, false);
            }
            if (group.Members.Count <= 1)
            {
                foreach (EntityId rest in group.Members)
                {
                    _byMember.Remove(rest);
                    Notify(rest, NotificationKeys.GroupDisbanded, false);
                }
                _groups.Remove(group.Id);
                Log.Info(LogTags.Server, $"Group {group.Id} disbanded");
                return;
            }
            bool leaderChanged = group.LeaderId == member;
            if (leaderChanged)
            {
                group.LeaderId = group.Members[0];
            }
            _groups[group.Id] = group;
            NotifyGroup(group, NotificationKeys.GroupMemberLeft, false);
            if (leaderChanged)
            {
                NotifyGroup(group, NotificationKeys.GroupNewLeader, true);
            }
            CancelVote(group);
        }

        /// <summary>Any change of membership cancels the vote (06, 7.8).</summary>
        private void CancelVote(GroupData group)
        {
            if (!group.Vote.Active)
            {
                return;
            }
            group.Vote.Active = false;
            _groups[group.Id] = group;
            NotifyGroup(group, NotificationKeys.LootVoteCancelled, false);
        }

        private void ResolveIfUnanimous(GroupData group)
        {
            foreach (EntityId member in Voters(group))
            {
                // Disconnected members would not block the vote (question 21); in this stage everyone is online.
                if (_ctx.GetPlayer(member) != null && !group.Vote.VotesFor.Contains(member))
                {
                    return;
                }
            }
            group.LootMode = group.Vote.NewMode;
            group.Vote.Active = false;
            _groups[group.Id] = group;
            Log.Info(LogTags.Server, $"Group {group.Id}: loot mode is now {group.LootMode}");
            NotifyGroup(group, NotificationKeys.LootVotePassed, true);
        }

        private void FailVote(GroupData group, string key)
        {
            group.Vote.Active = false;
            group.ProposeAllowedAt = _ctx.Time + Config.LootVoteRepeatSeconds;
            _groups[group.Id] = group;
            Log.Info(LogTags.Server, $"Group {group.Id}: loot mode vote failed");
            NotifyGroup(group, key, false);
        }

        /// <summary>Question 51: more than half of the members online (in a group of two — both).</summary>
        private bool HasQuorum(GroupData group)
        {
            List<EntityId> voters = Voters(group);
            int online = 0;
            foreach (EntityId member in voters)
            {
                if (_ctx.GetPlayer(member) != null)
                {
                    online++;
                }
            }
            return online * 2 > voters.Count;
        }

        private bool RequireLeader(PlayerCharacter player, out GroupData group)
        {
            if (!TryGetGroup(player.EntityId, out group))
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotInGroup);
                return false;
            }
            if (group.LeaderId != player.EntityId)
            {
                CombatEvents.Reject(player, RejectReasons.GroupNotLeader);
                return false;
            }
            return true;
        }

        /// <summary>A group event also goes to the group chat as a system line (06 7.8: votes are shown in the group chat).</summary>
        public event Action<int, string> SystemMessage;

        private void NotifyGroup(GroupData group, string key, bool positive)
        {
            SystemMessage?.Invoke(group.Id, key);
            foreach (EntityId member in group.Members)
            {
                Notify(member, key, positive);
            }
        }

        private void Notify(EntityId id, string key, bool positive)
        {
            PlayerCharacter player = _ctx.GetPlayer(id);
            player?.Outbox.Notifications.Add(new NotificationMessage { TextKey = key, IsPositive = positive });
        }
    }
}
