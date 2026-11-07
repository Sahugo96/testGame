using System.Linq;
using Game.Network;
using Game.Server.World;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Groups and loot modes (06, 7.8; module 14 criteria: leader, unanimous vote with timeout and membership changes, three modes).</summary>
    public sealed class GroupTests
    {
        private ServerHarness _h;

        [SetUp]
        public void SetUp() => _h = new ServerHarness();

        private GroupSnapshot Group(EntityId id) => _h.Player(id).Outbox.State.Group;

        private bool Notified(EntityId id, string key) => _h.Player(id).Outbox.Notifications.Any(n => n.TextKey == key);

        private void Join(EntityId leader, EntityId member)
        {
            _h.Send(leader, new GroupInviteCommand { TargetId = member });
            _h.Send(member, new GroupInviteResponseCommand { Accept = true });
        }

        private EntityId[] MakeGroup(params string[] classes)
        {
            EntityId[] ids = classes.Select(c => _h.AddPlayer(c)).ToArray();
            for (int i = 1; i < ids.Length; i++)
            {
                Join(ids[0], ids[i]);
            }
            return ids;
        }

        [Test]
        public void InviteAndAccept_CreatesGroup_WithInviterAsLeader_AndPickedUpMode()
        {
            EntityId a = _h.AddPlayer("mage");
            EntityId b = _h.AddPlayer("warrior");
            _h.Send(a, new GroupInviteCommand { TargetId = b });
            Assert.That(Group(b).InviteFromId, Is.EqualTo(a));
            Assert.That(Notified(b, NotificationKeys.GroupInvited), Is.True);

            _h.Send(b, new GroupInviteResponseCommand { Accept = true });

            GroupSnapshot group = Group(a);
            Assert.That(group.InGroup, Is.True);
            Assert.That(group.LeaderId, Is.EqualTo(a));
            Assert.That(group.Members, Is.EqualTo(new[] { a, b }));
            Assert.That(group.LootMode, Is.EqualTo(LootMode.PickedUp));
            Assert.That(Group(b).InviteFromId.IsValid, Is.False);
        }

        [Test]
        public void Invite_IsValidated()
        {
            EntityId[] g = MakeGroup("mage", "warrior");
            EntityId outsider = _h.AddPlayer("mage");
            _h.Send(g[0], new GroupInviteCommand { TargetId = g[0] });
            _h.Send(g[0], new GroupInviteCommand { TargetId = _h.FindMob("cube") });
            Assert.That(_h.Rejections(g[0]).Count(r => r == RejectReasons.GroupNotPlayer), Is.EqualTo(2));

            _h.Send(g[1], new GroupInviteCommand { TargetId = outsider });
            Assert.That(_h.Rejections(g[1]), Does.Contain(RejectReasons.GroupNotLeader));

            _h.Send(outsider, new GroupInviteCommand { TargetId = g[1] });
            Assert.That(_h.Rejections(outsider), Does.Contain(RejectReasons.GroupTargetInGroup));

            _h.Send(g[0], new GroupInviteCommand { TargetId = outsider });
            EntityId other = _h.AddPlayer("mage");
            _h.Send(other, new GroupInviteCommand { TargetId = outsider });
            Assert.That(_h.Rejections(other), Does.Contain(RejectReasons.GroupTargetHasInvite));
        }

        [Test]
        public void GroupSize_IsLimitedByData()
        {
            int max = _h.Content.Config.GroupMaxSize;
            EntityId[] g = MakeGroup(Enumerable.Repeat("mage", max).ToArray());
            Assert.That(Group(g[0]).Members.Length, Is.EqualTo(max));
            EntityId extra = _h.AddPlayer("mage");
            _h.Send(g[0], new GroupInviteCommand { TargetId = extra });
            Assert.That(_h.Rejections(g[0]), Does.Contain(RejectReasons.GroupFull));
        }

        [Test]
        public void Decline_And_Expire()
        {
            EntityId a = _h.AddPlayer("mage");
            EntityId b = _h.AddPlayer("mage");
            _h.Send(a, new GroupInviteCommand { TargetId = b });
            _h.Send(b, new GroupInviteResponseCommand { Accept = false });
            Assert.That(Notified(a, NotificationKeys.GroupInviteDeclined), Is.True);
            Assert.That(Group(a).InGroup, Is.False);

            _h.Send(a, new GroupInviteCommand { TargetId = b });
            _h.Run(_h.Content.Config.GroupInviteSeconds + 0.1f);
            Assert.That(Notified(b, NotificationKeys.GroupInviteExpired), Is.True);
            _h.ClearOutbox(b);
            _h.Send(b, new GroupInviteResponseCommand { Accept = true });
            Assert.That(_h.Rejections(b), Does.Contain(RejectReasons.GroupNoInvite));
        }

        [Test]
        public void LeaderLeaves_LeadershipPassesByJoinOrder_LastMemberDisbands()
        {
            EntityId[] g = MakeGroup("mage", "warrior", "mage");
            _h.Send(g[0], new GroupLeaveCommand());
            Assert.That(Group(g[1]).LeaderId, Is.EqualTo(g[1]), "the next by join order");
            Assert.That(Group(g[0]).InGroup, Is.False);
            Assert.That(Notified(g[2], NotificationKeys.GroupNewLeader), Is.True);

            _h.Server.RemovePlayer(g[1]);
            _h.Server.Tick();
            Assert.That(Group(g[2]).InGroup, Is.False, "a group of one is disbanded");
            Assert.That(Notified(g[2], NotificationKeys.GroupDisbanded), Is.True);
        }

        [Test]
        public void KickAndPromote_OnlyByLeader()
        {
            EntityId[] g = MakeGroup("mage", "warrior", "mage");
            _h.Send(g[1], new GroupKickCommand { MemberId = g[2] });
            _h.Send(g[1], new GroupPromoteCommand { MemberId = g[1] });
            Assert.That(_h.Rejections(g[1]).Count(r => r == RejectReasons.GroupNotLeader), Is.EqualTo(2));

            _h.Send(g[0], new GroupPromoteCommand { MemberId = g[1] });
            Assert.That(Group(g[0]).LeaderId, Is.EqualTo(g[1]));
            _h.Send(g[1], new GroupKickCommand { MemberId = g[2] });
            Assert.That(Group(g[1]).Members, Is.EqualTo(new[] { g[0], g[1] }));
            Assert.That(Notified(g[2], NotificationKeys.GroupKicked), Is.True);
        }

        [Test]
        public void LootVote_Unanimous_ChangesMode()
        {
            EntityId[] g = MakeGroup("mage", "warrior", "mage");
            _h.Send(g[1], new LootModeProposeCommand { Mode = LootMode.RoundRobin });
            GroupSnapshot vote = Group(g[0]);
            Assert.That(vote.VoteActive, Is.True);
            Assert.That(vote.VotesFor, Is.EqualTo(new[] { g[1] }), "the proposer votes for");

            _h.Send(g[0], new LootModeVoteCommand { ProposalId = vote.VoteProposalId, Accept = true });
            Assert.That(Group(g[0]).LootMode, Is.EqualTo(LootMode.PickedUp), "not everyone voted yet");
            _h.Send(g[2], new LootModeVoteCommand { ProposalId = vote.VoteProposalId, Accept = true });

            Assert.That(Group(g[0]).LootMode, Is.EqualTo(LootMode.RoundRobin));
            Assert.That(Group(g[0]).VoteActive, Is.False);
            Assert.That(Notified(g[2], NotificationKeys.LootVotePassed), Is.True);
        }

        [Test]
        public void LootVote_OneAgainst_Fails_AndDelaysTheNextProposal()
        {
            EntityId[] g = MakeGroup("mage", "warrior");
            _h.Send(g[0], new LootModeProposeCommand { Mode = LootMode.Leader });
            _h.Send(g[1], new LootModeVoteCommand { ProposalId = Group(g[1]).VoteProposalId, Accept = false });
            Assert.That(Group(g[0]).LootMode, Is.EqualTo(LootMode.PickedUp));
            Assert.That(Notified(g[0], NotificationKeys.LootVoteFailed), Is.True);

            _h.Send(g[1], new LootModeProposeCommand { Mode = LootMode.Leader });
            Assert.That(_h.Rejections(g[1]), Does.Contain(RejectReasons.LootVoteCooldown));
            _h.Run(_h.Content.Config.LootVoteRepeatSeconds);
            _h.Send(g[1], new LootModeProposeCommand { Mode = LootMode.Leader });
            Assert.That(Group(g[1]).VoteActive, Is.True);
        }

        [Test]
        public void LootVote_Timeout_CountsAsAgainst()
        {
            EntityId[] g = MakeGroup("mage", "warrior");
            _h.Send(g[0], new LootModeProposeCommand { Mode = LootMode.Leader });
            _h.Run(_h.Content.Config.LootVoteSeconds + 0.1f);
            Assert.That(Group(g[0]).VoteActive, Is.False);
            Assert.That(Group(g[0]).LootMode, Is.EqualTo(LootMode.PickedUp));
            Assert.That(Notified(g[1], NotificationKeys.LootVoteFailed), Is.True);
        }

        [Test]
        public void LootVote_MembershipChange_CancelsIt()
        {
            EntityId[] g = MakeGroup("mage", "warrior", "mage");
            _h.Send(g[0], new LootModeProposeCommand { Mode = LootMode.Leader });
            int proposal = Group(g[0]).VoteProposalId;
            _h.Send(g[2], new GroupLeaveCommand());
            Assert.That(Group(g[0]).VoteActive, Is.False);
            Assert.That(Notified(g[1], NotificationKeys.LootVoteCancelled), Is.True);

            _h.ClearOutbox(g[1]);
            _h.Send(g[1], new LootModeVoteCommand { ProposalId = proposal, Accept = true });
            Assert.That(_h.Rejections(g[1]), Does.Contain(RejectReasons.LootVoteNotFound));
        }

        [Test]
        public void LootVote_SameModeOrSecondVote_IsRejected()
        {
            EntityId[] g = MakeGroup("mage", "warrior");
            _h.Send(g[0], new LootModeProposeCommand { Mode = LootMode.PickedUp });
            Assert.That(_h.Rejections(g[0]), Does.Contain(RejectReasons.LootVoteSameMode));
            _h.Send(g[0], new LootModeProposeCommand { Mode = LootMode.Leader });
            _h.Send(g[1], new LootModeProposeCommand { Mode = LootMode.RoundRobin });
            Assert.That(_h.Rejections(g[1]), Does.Contain(RejectReasons.LootVoteActive));
        }

        // ---- Loot distribution ----

        private EntityId PrepareCube(EntityId near)
        {
            _h.Content.Mobs["cube"].Drops[0].Chance = 1f;
            EntityId cube = _h.NearestMob(near, "cube");
            ref VitalsData vitals = ref _h.Vitals(cube);
            vitals.MaxHealth = 10000f;
            vitals.Health = 10000f;
            vitals.HealthRegen = 0f;
            return cube;
        }

        /// <summary>The player hits the mob from 5 m with an instant skill (cooldowns and mana reset for the test).</summary>
        private void Hit(EntityId player, EntityId mob)
        {
            _h.PlaceNear(player, mob, 5f);
            _h.Player(player).SkillCooldowns.Clear();
            _h.World.Casts.Get(player).GlobalCooldownRemaining = 0f;
            ref ManaData mana = ref _h.Mana(player);
            mana.Main = mana.MaxMain;
            mana.Reserve = mana.MaxReserve;
            mana.MainLocked = false;
            _h.Send(player, new UseSkillCommand { SkillId = "reserve_nova", TargetId = mob });
        }

        private void Kill(EntityId killer, EntityId mob)
        {
            _h.Vitals(mob).Health = 1f;
            Hit(killer, mob);
            Assert.That(_h.Vitals(mob).IsDead, Is.True);
        }

        private EntityId GroundLoot()
        {
            foreach (EntityId id in _h.World.Entities)
            {
                if (_h.World.Loot.Has(id))
                {
                    return id;
                }
            }
            return EntityId.None;
        }

        private void PickUp(EntityId player, EntityId loot)
        {
            _h.World.Transforms.Get(player).Position = _h.World.Transforms.Get(loot).Position;
            _h.Send(player, new PickUpLootCommand { LootId = loot });
        }

        private int Shards(EntityId id) => _h.Player(id).Bag.Get(_h.BagIndexOf(id, "philosopher_stone_shard"))?.Count ?? 0;

        private void SetMode(EntityId[] g, LootMode mode)
        {
            _h.Send(g[0], new LootModeProposeCommand { Mode = mode });
            int proposal = Group(g[0]).VoteProposalId;
            foreach (EntityId member in g.Skip(1))
            {
                _h.Send(member, new LootModeVoteCommand { ProposalId = proposal, Accept = true });
            }
            Assert.That(Group(g[0]).LootMode, Is.EqualTo(mode));
        }

        [Test]
        public void PickedUp_KillerGetsTheDrop_AndTheLogShowsIt()
        {
            EntityId[] g = MakeGroup("mage", "mage");
            EntityId cube = PrepareCube(g[0]);
            int before = Shards(g[1]);
            Hit(g[0], cube);
            Kill(g[1], cube);
            EntityId loot = GroundLoot();
            Assert.That(loot.IsValid, Is.True, "PickedUp: the drop lies on the ground");
            Assert.That(Shards(g[1]), Is.EqualTo(before));
            PickUp(g[1], loot);
            Assert.That(Shards(g[1]), Is.EqualTo(before + 1));
            Assert.That(_h.World.Loot.Has(loot), Is.False, "picked up once");
            GroupLogEntry entry = Group(g[0]).Log.Last();
            Assert.That(entry.ItemDefinitionId, Is.EqualTo("philosopher_stone_shard"));
            Assert.That(entry.Mode, Is.EqualTo(LootMode.PickedUp));
        }

        [Test]
        public void Leader_GetsTheDrop_OnlyIfHeTookPart()
        {
            EntityId[] g = MakeGroup("mage", "mage");
            SetMode(g, LootMode.Leader);
            EntityId cube = PrepareCube(g[0]);
            int leaderBefore = Shards(g[0]);
            Hit(g[0], cube);
            Kill(g[1], cube);
            Assert.That(Shards(g[0]), Is.EqualTo(leaderBefore + 1), "the leader fought: the drop is his");

            _h.Run(_h.Content.Mobs["cube"].RespawnSeconds + 0.1f);
            cube = PrepareCube(g[1]);
            int memberBefore = Shards(g[1]);
            Kill(g[1], cube);
            PickUp(g[1], GroundLoot());
            Assert.That(Shards(g[1]), Is.EqualTo(memberBefore + 1), "the leader did not fight: PickedUp, the killer picks it up");
        }

        [Test]
        public void RoundRobin_GoesAround_IneligibleKeepsHisPlace()
        {
            EntityId[] g = MakeGroup("mage", "mage", "mage");
            SetMode(g, LootMode.RoundRobin);
            int[] before = g.Select(Shards).ToArray();

            EntityId cube = PrepareCube(g[0]);
            Hit(g[1], cube);
            Hit(g[2], cube);
            Kill(g[1], cube);
            // g[0] did not fight: skipped, keeps the front; g[1] is next in the queue.
            Assert.That(Shards(g[1]), Is.EqualTo(before[1] + 1));
            Assert.That(Shards(g[0]), Is.EqualTo(before[0]));

            _h.Run(_h.Content.Mobs["cube"].RespawnSeconds + 0.1f);
            cube = PrepareCube(g[0]);
            foreach (EntityId member in g)
            {
                Hit(member, cube);
            }
            Kill(g[2], cube);
            Assert.That(Shards(g[0]), Is.EqualTo(before[0] + 1), "g[0] kept his place at the front");
        }

        [Test]
        public void HealingAFighter_CountsAsTakingPart()
        {
            EntityId[] g = MakeGroup("mage", "mage");
            SetMode(g, LootMode.Leader);
            EntityId cube = PrepareCube(g[0]);
            Hit(g[1], cube);
            _h.Vitals(g[1]).Health -= 50f;
            _h.World.Transforms.Get(g[0]).Position = _h.World.Transforms.Get(g[1]).Position;
            _h.Send(g[0], new UseSkillCommand { SkillId = "arcane_mend", TargetId = g[1] });
            _h.Run(1.1f);
            int leaderBefore = Shards(g[0]);
            Kill(g[1], cube);
            Assert.That(Shards(g[0]), Is.EqualTo(leaderBefore + 1), "the leader healed a fighter: he took part");
        }

        [Test]
        public void Eligibility_RequiresRadius()
        {
            EntityId[] g = MakeGroup("mage", "mage");
            SetMode(g, LootMode.Leader);
            EntityId cube = PrepareCube(g[0]);
            Hit(g[0], cube);
            Vec3 cubePosition = _h.World.Transforms.Get(cube).Position;
            _h.World.Transforms.Get(g[0]).Position = new Vec3(cubePosition.X + _h.Content.Config.LootRadius + 1f, 0f, cubePosition.Z);
            int leaderBefore = Shards(g[0]);
            Kill(g[1], cube);
            Assert.That(Shards(g[0]), Is.EqualTo(leaderBefore), "too far from the death spot");
        }

        [Test]
        public void ExperienceSplit_DeveloperExample()
        {
            // Question 72: two level 20, one 18, one 15 and six ≤ 10 → 33%, 33%, 18%, 16%; the ≤ 10 get nothing.
            int[] levels = { 20, 20, 18, 15, 10, 9, 8, 7, 6, 5 };
            int[] shares = Game.Shared.Rules.GroupExperienceMath.Split(100, levels, _h.Content.Config.GroupXpGapShares);
            Assert.That(shares, Is.EqualTo(new[] { 33, 33, 18, 16, 0, 0, 0, 0, 0, 0 }));
            Assert.That(Game.Shared.Rules.GroupExperienceMath.Split(10, new[] { 5, 5 }, _h.Content.Config.GroupXpGapShares),
                Is.EqualTo(new[] { 5, 5 }), "equal levels: equal parts");
        }

        [Test]
        public void GroupKill_SharesExperience_WithMembersNearby()
        {
            EntityId[] g = MakeGroup("mage", "mage");
            EntityId cube = PrepareCube(g[0]);
            Hit(g[0], cube);
            _h.World.Transforms.Get(g[1]).Position = _h.World.Transforms.Get(g[0]).Position;
            int xp0 = _h.Player(g[0]).Experience;
            int xp1 = _h.Player(g[1]).Experience;
            Kill(g[0], cube);
            int reward = _h.Content.Mobs["cube"].ExperienceReward;
            Assert.That(_h.Player(g[0]).Experience - xp0 + _h.Player(g[1]).Experience - xp1, Is.EqualTo(reward));
            Assert.That(_h.Player(g[1]).Experience - xp1, Is.EqualTo(reward / 2), "same level: half each");
        }

        [Test]
        public void Codec_RoundTripsGroupState()
        {
            EntityId[] g = MakeGroup("mage", "warrior");
            _h.Send(g[1], new LootModeProposeCommand { Mode = LootMode.RoundRobin });
            var state = (PlayerStateSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(_h.Player(g[0]).Outbox.State));
            Assert.That(state.Group.InGroup, Is.True);
            Assert.That(state.Group.Members, Is.EqualTo(new[] { g[0], g[1] }));
            Assert.That(state.Group.VoteMode, Is.EqualTo(LootMode.RoundRobin));
            Assert.That(state.Group.VotesFor, Is.EqualTo(new[] { g[1] }));

            var vote = (LootModeVoteCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new LootModeVoteCommand { ProposalId = 7, Accept = true }));
            Assert.That(vote.ProposalId, Is.EqualTo(7));
            var kick = (GroupKickCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new GroupKickCommand { MemberId = g[1] }));
            Assert.That(kick.MemberId, Is.EqualTo(g[1]));
        }
    }
}
