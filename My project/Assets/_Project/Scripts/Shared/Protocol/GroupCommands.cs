using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Invite a player into the group (leader, or anyone without a group — then a new group is created).</summary>
    public struct GroupInviteCommand : IGameCommand
    {
        public EntityId TargetId;
    }

    /// <summary>Answer to an incoming group invite.</summary>
    public struct GroupInviteResponseCommand : IGameCommand
    {
        public bool Accept;
    }

    /// <summary>Leave the group (a leader passes leadership to the next member by join order).</summary>
    public struct GroupLeaveCommand : IGameCommand
    {
    }

    /// <summary>Leader only: remove a member.</summary>
    public struct GroupKickCommand : IGameCommand
    {
        public EntityId MemberId;
    }

    /// <summary>Leader only: hand leadership to another member.</summary>
    public struct GroupPromoteCommand : IGameCommand
    {
        public EntityId MemberId;
    }

    /// <summary>Leader: turn the group into a raid (06 7.8).</summary>
    public struct GroupConvertToRaidCommand : IGameCommand
    {
    }

    /// <summary>Raid leader or assistant: move a member to a subgroup.</summary>
    public struct GroupMoveCommand : IGameCommand
    {
        public EntityId MemberId;
        public int Subgroup;
    }

    /// <summary>Raid leader: make a member an assistant or take it back.</summary>
    public struct GroupSetAssistantCommand : IGameCommand
    {
        public EntityId MemberId;
        public bool Enabled;
    }

    /// <summary>Any member: propose another loot mode (06, 7.8: unanimous vote).</summary>
    public struct LootModeProposeCommand : IGameCommand
    {
        public LootMode Mode;
    }

    /// <summary>CmdVoteLootMode { proposalId, accept } (06, 7.8). The server counts the votes.</summary>
    public struct LootModeVoteCommand : IGameCommand
    {
        public int ProposalId;
        public bool Accept;
    }
}
