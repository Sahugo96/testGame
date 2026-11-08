using System.Linq;
using Game.Network;
using Game.Server.Items;
using Game.Server.Players;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 14: chat channels, friends, trade, raid of 50 with subgroups and leader votes.</summary>
    public sealed class SocialTests
    {
        private ServerHarness _h;

        [SetUp]
        public void SetUp() => _h = new ServerHarness();

        private EntityId Player(string name, Vec3 at)
        {
            EntityId id = _h.Server.AddPlayer("mage", name);
            _h.World.Transforms.Get(id).Position = at;
            return id;
        }

        private bool Heard(EntityId id, string text) => _h.Player(id).Outbox.Chat.Any(m => m.Text == text);

        [Test]
        public void Chat_Channels_ReachTheRightPlayers()
        {
            float far = _h.Content.Config.InterestRadius + 10f;
            EntityId a = Player("Аня", Vec3.Zero);
            EntityId b = Player("Боб", new Vec3(5f, 0f, 0f));
            EntityId c = Player("Вика", new Vec3(far, 0f, far));

            _h.Send(a, new SendChatCommand { Channel = ChatChannel.Say, Text = "привет рядом" });
            Assert.That(Heard(b, "привет рядом"), Is.True);
            Assert.That(Heard(c, "привет рядом"), Is.False, "Say: only nearby");

            _h.Run(_h.Content.Config.ChatMinIntervalSeconds);
            _h.Send(a, new SendChatCommand { Channel = ChatChannel.World, Text = "всем" });
            Assert.That(Heard(c, "всем"), Is.True);

            _h.Run(_h.Content.Config.ChatMinIntervalSeconds);
            _h.Send(a, new SendChatCommand { Channel = ChatChannel.Whisper, Text = "секрет", Target = "вИкА" });
            Assert.That(Heard(c, "секрет") && Heard(a, "секрет") && !Heard(b, "секрет"), Is.True, "whisper by name, a copy to the sender");

            _h.Send(a, new SendChatCommand { Channel = ChatChannel.World, Text = "спам" });
            Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.ChatTooFast));

            _h.Run(_h.Content.Config.ChatMinIntervalSeconds);
            _h.Send(a, new SendChatCommand { Channel = ChatChannel.Whisper, Text = "?", Target = "Никто" });
            Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.ChatNoSuchPlayer));

            _h.Run(_h.Content.Config.ChatMinIntervalSeconds);
            _h.Send(a, new SendChatCommand { Channel = ChatChannel.World, Text = "  длинно\u0007" + new string('x', 500) });
            ChatMessage longest = _h.Player(b).Outbox.Chat.Last();
            Assert.That(longest.Text.Length, Is.EqualTo(_h.Content.Config.ChatMaxLength));
            Assert.That(longest.Text.IndexOf('\u0007'), Is.EqualTo(-1));
        }

        [Test]
        public void GroupChat_AndVotesAsSystemLines()
        {
            EntityId a = Player("Аня", Vec3.Zero);
            EntityId b = Player("Боб", Vec3.Zero);
            EntityId c = Player("Вика", Vec3.Zero);
            _h.Send(a, new GroupInviteCommand { TargetId = b });
            _h.Send(b, new GroupInviteResponseCommand { Accept = true });
            _h.Send(a, new SendChatCommand { Channel = ChatChannel.Group, Text = "группа" });
            Assert.That(Heard(b, "группа") && !Heard(c, "группа"), Is.True);

            _h.Send(a, new LootModeProposeCommand { Mode = LootMode.Leader });
            Assert.That(_h.Player(b).Outbox.Chat.Any(m => m.Channel == ChatChannel.System && m.TextKey == NotificationKeys.LootVoteStarted), Is.True);
        }

        [Test]
        public void Friends_AreMutual_RequestAcceptDeclineRemove()
        {
            EntityId a = Player("Аня", Vec3.Zero);
            EntityId b = Player("Боб", Vec3.Zero);
            _h.Send(a, new FriendAddCommand { Name = "Призрак" });
            Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.ChatNoSuchPlayer), "unknown names are refused");
            _h.Send(a, new FriendAddCommand { Name = "боб" });
            _h.Send(a, new FriendAddCommand { Name = "Боб" });
            Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.FriendRequestPending));
            FriendSnapshot request = _h.Player(a).Outbox.State.Friends.Single();
            Assert.That((request.Name, request.State), Is.EqualTo(("Боб", FriendState.Outgoing)), "not a friend until accepted");
            FriendSnapshot incoming = _h.Player(b).Outbox.State.Friends.Single();
            Assert.That((incoming.Name, incoming.State), Is.EqualTo(("Аня", FriendState.Incoming)));
            Assert.That(_h.Player(b).Outbox.Chat.Any(m => m.Channel == ChatChannel.System && m.TextKey == NotificationKeys.FriendRequestReceived && m.SenderName == "Аня"), Is.True);

            _h.Send(b, new FriendRespondCommand { Name = "аня", Accept = true });
            Assert.That(_h.Player(a).Outbox.State.Friends.Single().State, Is.EqualTo(FriendState.Friend));
            Assert.That(_h.Player(b).Outbox.State.Friends.Single().State, Is.EqualTo(FriendState.Friend));
            Assert.That(_h.Player(a).Outbox.State.Friends.Single().Online, Is.True);
            _h.Send(b, new FriendAddCommand { Name = "Аня" });
            Assert.That(_h.Rejections(b), Does.Contain(RejectReasons.FriendAlready));

            _h.Send(a, new FriendRemoveCommand { Name = "БОБ" });
            Assert.That(_h.Player(a).Outbox.State.Friends, Is.Empty);
            Assert.That(_h.Player(b).Outbox.State.Friends, Is.Empty, "removed on both sides");

            _h.Send(a, new FriendAddCommand { Name = "Боб" });
            _h.Send(b, new FriendRespondCommand { Name = "Аня", Accept = false });
            Assert.That(_h.Player(a).Outbox.State.Friends, Is.Empty, "a declined request disappears for both");
            Assert.That(_h.Player(b).Outbox.State.Friends, Is.Empty);

            _h.Send(a, new FriendAddCommand { Name = "Боб" });
            _h.Send(b, new FriendAddCommand { Name = "Аня" });
            Assert.That(_h.Player(a).Outbox.State.Friends.Single().State, Is.EqualTo(FriendState.Friend), "asking back accepts");
            Assert.That(_h.Player(b).Outbox.State.Friends.Single().State, Is.EqualTo(FriendState.Friend));
        }

        [Test]
        public void ChatTabs_DefaultsFromConfig_CustomTabsChecked()
        {
            EntityId a = Player("Аня", Vec3.Zero);
            _h.Run(_h.Server.TickInterval);
            ChatTabSnapshot[] defaults = _h.Player(a).Outbox.State.ChatTabs;
            Assert.That(defaults.Select(t => t.NameKey), Is.EqualTo(new[] { "chat.tab.general", "chat.tab.private" }));
            Assert.That(defaults[1].Channels, Is.EqualTo(new[] { ChatChannel.Whisper }));

            var custom = new[]
            {
                defaults[0],
                new ChatTabSnapshot { Name = "  Торг\u0007  ", Channels = new[] { ChatChannel.World, ChatChannel.Say } },
            };
            _h.Send(a, new SetChatTabsCommand { Tabs = custom });
            ChatTabSnapshot[] tabs = _h.Player(a).Outbox.State.ChatTabs;
            Assert.That(tabs[1].Name, Is.EqualTo("Торг"), "trimmed, no control characters");
            Assert.That(tabs[1].Channels, Is.EqualTo(new[] { ChatChannel.World, ChatChannel.Say }));
            var decoded = (PlayerStateSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(_h.Player(a).Outbox.State));
            Assert.That(decoded.ChatTabs[1].Name, Is.EqualTo("Торг"));

            int max = _h.Content.Config.ChatTabsMax;
            ChatTabSnapshot[][] bad =
            {
                new ChatTabSnapshot[0],
                Enumerable.Range(0, max + 1).Select(i => new ChatTabSnapshot { Name = "t" + i, Channels = new[] { ChatChannel.World } }).ToArray(),
                new[] { new ChatTabSnapshot { Name = "пусто", Channels = new ChatChannel[0] } },
                new[] { new ChatTabSnapshot { NameKey = "ui.close", Channels = new[] { ChatChannel.World } } },
                new[] { new ChatTabSnapshot { Name = "два", Channels = new[] { ChatChannel.World, ChatChannel.World } } },
                new[] { new ChatTabSnapshot { Name = "x", Channels = new[] { (ChatChannel)99 } } },
            };
            foreach (ChatTabSnapshot[] tabsToSend in bad)
            {
                _h.ClearOutbox(a);
                _h.Send(a, new SetChatTabsCommand { Tabs = tabsToSend });
                Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.ChatTabsInvalid));
            }
            Assert.That(_h.Player(a).Outbox.State.ChatTabs[1].Name, Is.EqualTo("Торг"), "a refused change keeps the old tabs");

            _h.Send(a, new SetChatTabsCommand { Tabs = defaults });
            Assert.That(_h.Player(a).ChatTabs, Is.Null, "the defaults are stored as defaults");
        }

        private (EntityId a, EntityId b) OpenTrade()
        {
            EntityId a = _h.AddPlayer("warrior");
            EntityId b = _h.AddPlayer("mage");
            _h.Send(a, new TradeRequestCommand { TargetId = b });
            Assert.That(_h.Player(b).Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.IncomingRequest));
            _h.Send(b, new TradeRespondCommand { Accept = true });
            Assert.That(_h.Player(a).Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.Open));
            return (a, b);
        }

        [Test]
        public void Trade_SwapsItemsAndGold_KeepingInstances()
        {
            var (a, b) = OpenTrade();
            PlayerCharacter pa = _h.Player(a);
            PlayerCharacter pb = _h.Player(b);
            ItemInstance axe = pa.Bag.Get(_h.BagIndexOf(a, "great_axe"));
            long goldA = _h.Server.Wallet.Balance(pa, "gold");
            long goldB = _h.Server.Wallet.Balance(pb, "gold");

            _h.Send(a, new TradeOfferCommand { InstanceIds = new[] { axe.SourceId }, Gold = 0 });
            _h.Send(b, new TradeOfferCommand { InstanceIds = new string[0], Gold = 40 });
            _h.Send(a, new TradeConfirmCommand());
            _h.Send(b, new TradeOfferCommand { InstanceIds = new string[0], Gold = 30 });
            Assert.That(pa.Outbox.State.Trade.MyConfirmed, Is.False, "a changed offer resets confirmations");

            _h.Send(a, new TradeConfirmCommand());
            _h.Send(b, new TradeConfirmCommand());
            Assert.That(pb.Bag.Get(_h.BagIndexOf(b, "great_axe")).SourceId, Is.EqualTo(axe.SourceId), "the same instance moved");
            Assert.That(_h.BagIndexOf(a, "great_axe"), Is.EqualTo(-1));
            Assert.That(_h.Server.Wallet.Balance(pa, "gold"), Is.EqualTo(goldA + 30));
            Assert.That(_h.Server.Wallet.Balance(pb, "gold"), Is.EqualTo(goldB - 30));
            Assert.That(_h.Audit.Entries.Any(e => e.Action == AuditActions.TradeGive && e.InstanceId == axe.SourceId), Is.True);
            Assert.That(pa.Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.None));
        }

        [Test]
        public void Trade_RefusesBoundItems_AndCancelsWhenApart()
        {
            var (a, b) = OpenTrade();
            ItemInstance axe = _h.Player(a).Bag.Get(_h.BagIndexOf(a, "great_axe"));
            axe.BoundTo = _h.Player(a).CharacterId;
            _h.Send(a, new TradeOfferCommand { InstanceIds = new[] { axe.SourceId } });
            Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.TradeItemNotTradable));

            _h.World.Transforms.Get(b).Position = new Vec3(_h.Content.Config.TradeRange + 5f, 0f, 0f);
            _h.Server.Tick();
            Assert.That(_h.Player(a).Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.None));
            Assert.That(_h.Player(b).Outbox.Notifications.Any(n => n.TextKey == NotificationKeys.TradeCancelled), Is.True);
        }

        [Test]
        public void Trade_CancelsWhenAnyoneMoves_AndChangedOffersResetConfirmations()
        {
            var (a, b) = OpenTrade();
            int shards = _h.BagIndexOf(a, "philosopher_stone_shard");
            string shardId = _h.Player(a).Bag.Get(shards).SourceId;
            _h.Send(a, new TradeOfferCommand { InstanceIds = new[] { shardId } });
            _h.Send(b, new TradeConfirmCommand());
            Assert.That(_h.Player(a).Outbox.State.Trade.TheirConfirmed, Is.True);

            // The offered stack shrinks (split off one): the other side must confirm again.
            BagSection resources = _h.Content.BagSections.First(section => section.Id == "resources");
            int free = _h.Player(a).Bag.FindFreeSlot(resources.Start, resources.Size);
            _h.Send(a, new MoveBagItemCommand { FromIndex = shards, ToIndex = free, Count = 1 });
            Assert.That(_h.Player(a).Outbox.State.Trade.TheirConfirmed, Is.False);
            Assert.That(_h.Player(b).Outbox.State.Trade.TheirItems.Single().Count, Is.EqualTo(19));

            // Decision of the developer: a step by either side cancels the trade at once.
            _h.World.Transforms.Get(b).Position = _h.World.Transforms.Get(b).Position + new Vec3(0.5f, 0f, 0f);
            _h.Run(_h.Server.TickInterval);
            Assert.That(_h.Player(a).Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.None));
            Assert.That(_h.Player(a).Outbox.Notifications.Any(n => n.TextKey == NotificationKeys.TradeCancelled), Is.True);

            // While a request waits, the one who asked must stand still too.
            _h.Send(a, new TradeRequestCommand { TargetId = b });
            Assert.That(_h.Player(b).Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.IncomingRequest));
            _h.World.Transforms.Get(a).Position = _h.World.Transforms.Get(a).Position + new Vec3(0f, 0f, 0.5f);
            _h.Run(_h.Server.TickInterval);
            Assert.That(_h.Player(b).Outbox.State.Trade.Phase, Is.EqualTo(TradePhase.None));
        }

        [Test]
        public void Raid_Of50_SubgroupsLeaderAssistants_LeadersVote()
        {
            EntityId leader = Player("Лидер", Vec3.Zero);
            EntityId[] members = Enumerable.Range(1, 49).Select(i => Player("Игрок" + i, Vec3.Zero)).ToArray();
            _h.Send(leader, new GroupInviteCommand { TargetId = members[0] });
            _h.Send(members[0], new GroupInviteResponseCommand { Accept = true });
            _h.Send(leader, new GroupConvertToRaidCommand());
            _h.Send(leader, new GroupSetAssistantCommand { MemberId = members[0], Enabled = true });
            for (int i = 1; i < members.Length; i++)
            {
                EntityId inviter = i % 2 == 0 ? leader : members[0];
                _h.Server.Submit(inviter, new GroupInviteCommand { TargetId = members[i] });
                _h.Server.Tick();
                _h.Server.Submit(members[i], new GroupInviteResponseCommand { Accept = true });
                _h.Server.Tick();
            }
            GroupSnapshot raid = _h.Player(leader).Outbox.State.Group;
            Assert.That(raid.IsRaid, Is.True);
            Assert.That(raid.Members.Length, Is.EqualTo(50), "the leader and the assistant filled the raid");
            Assert.That(raid.Subgroups.GroupBy(s => s).All(g => g.Count() == _h.Content.Config.RaidSubgroupSize), Is.True, "10 subgroups of 5");

            EntityId extra = Player("Лишний", Vec3.Zero);
            _h.Send(leader, new GroupInviteCommand { TargetId = extra });
            Assert.That(_h.Rejections(leader), Does.Contain(RejectReasons.GroupFull));

            // Moving needs room: every subgroup is full, so a move fails; after someone leaves it works.
            _h.Send(leader, new GroupMoveCommand { MemberId = members[10], Subgroup = 0 });
            Assert.That(_h.Rejections(leader), Does.Contain(RejectReasons.GroupSubgroupInvalid));
            _h.Send(members[1], new GroupLeaveCommand());
            _h.Send(leader, new GroupMoveCommand { MemberId = members[10], Subgroup = 0 });
            raid = _h.Player(leader).Outbox.State.Group;
            Assert.That(raid.Subgroups[System.Array.IndexOf(raid.Members, members[10])], Is.EqualTo(0));

            // Loot mode: only the raid leader and subgroup leaders vote (question 20).
            EntityId ordinary = members[48];
            _h.Send(ordinary, new LootModeProposeCommand { Mode = LootMode.Leader });
            Assert.That(_h.Rejections(ordinary), Does.Contain(RejectReasons.LootVoteNotVoter));
            _h.Send(leader, new LootModeProposeCommand { Mode = LootMode.Leader });
            int proposal = _h.Player(leader).Outbox.State.Group.VoteProposalId;
            var server = _h.Server;
            var group = new System.Collections.Generic.List<EntityId>(raid.Members);
            var subgroupLeaders = Enumerable.Range(0, 10)
                .Select(sub => group.FirstOrDefault(m => raid.Subgroups[group.IndexOf(m)] == sub))
                .Where(m => m.IsValid && m != leader).Distinct().ToList();
            foreach (EntityId voter in subgroupLeaders)
            {
                server.Submit(voter, new LootModeVoteCommand { ProposalId = proposal, Accept = true });
            }
            server.Tick();
            Assert.That(_h.Player(leader).Outbox.State.Group.LootMode, Is.EqualTo(LootMode.Leader), "raid leader + subgroup leaders agreed");

            // The client learns who votes from the snapshot (vote prompt and counter).
            GroupSnapshot decoded = ((PlayerStateSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(_h.Player(leader).Outbox.State))).Group;
            Assert.That(decoded.Voters, Does.Contain(leader));
            Assert.That(decoded.Voters, Is.SupersetOf(subgroupLeaders));
            Assert.That(decoded.Voters, Does.Not.Contain(ordinary));
        }

        [Test]
        public void Raid_ShowsOnlyOwnSubgroupAtAnyDistance()
        {
            EntityId leader = Player("Лидер", Vec3.Zero);
            EntityId[] members = Enumerable.Range(1, 9).Select(i => Player("Игрок" + i, new Vec3(35f, 0f, 35f))).ToArray();
            _h.Send(leader, new GroupInviteCommand { TargetId = members[0] });
            _h.Send(members[0], new GroupInviteResponseCommand { Accept = true });
            _h.Send(leader, new GroupConvertToRaidCommand());
            foreach (EntityId m in members.Skip(1))
            {
                _h.Send(leader, new GroupInviteCommand { TargetId = m });
                _h.Send(m, new GroupInviteResponseCommand { Accept = true });
            }
            _h.Server.Tick();
            var seen = _h.Player(leader).Outbox.World.Entities.Where(e => e.Kind == EntityKind.Player).Select(e => e.Id).ToList();
            Assert.That(seen, Does.Contain(members[0]), "own subgroup, far away");
            Assert.That(seen, Does.Not.Contain(members[8]), "another subgroup far away is not always visible");
        }

        [Test]
        public void Trade_LimitsItemCount_AndCodecRejectsForgedLength()
        {
            var (a, _) = OpenTrade();
            string[] many = Enumerable.Range(0, _h.Content.Config.TradeMaxItems + 1).Select(i => "id" + i).ToArray();
            _h.Send(a, new TradeOfferCommand { InstanceIds = many });
            Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.TradeInvalid));

            // A forged element count (int.MaxValue) must not make the server allocate a huge array.
            byte[] bytes = ProtocolCodec.Encode(new TradeOfferCommand { InstanceIds = new string[0], Gold = 0 });
            System.BitConverter.GetBytes(int.MaxValue).CopyTo(bytes, 1);
            Assert.Throws<System.IO.InvalidDataException>(() => ProtocolCodec.Decode(bytes));
        }

        [Test]
        public void Codec_RoundTripsSocialState()
        {
            var (a, b) = OpenTrade();
            _h.Send(a, new FriendAddCommand { Name = _h.World.Identities.Get(b).DisplayName ?? "x" });
            var state = (PlayerStateSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(_h.Player(a).Outbox.State));
            Assert.That(state.Trade.Phase, Is.EqualTo(TradePhase.Open));
            Assert.That(state.Trade.PartnerId, Is.EqualTo(b));
            var chat = (ChatMessage)ProtocolCodec.Decode(ProtocolCodec.Encode(new ChatMessage { Channel = ChatChannel.Whisper, SenderName = "А", TargetName = "Б", Text = "т" }));
            Assert.That(chat.TargetName, Is.EqualTo("Б"));
            var offer = (TradeOfferCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new TradeOfferCommand { InstanceIds = new[] { "x", "y" }, Gold = 5 }));
            Assert.That(offer.InstanceIds, Is.EqualTo(new[] { "x", "y" }));
            Assert.That(offer.Gold, Is.EqualTo(5));
        }
    }
}
