using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>
    /// Group state of the owning player (part of <see cref="PlayerStateSnapshot"/>, never null): members by join order,
    /// leader, loot mode, a pending invite, the loot-mode vote and the loot log. Names and health come from the world snapshot.
    /// </summary>
    public sealed class GroupSnapshot
    {
        public bool InGroup;
        public EntityId LeaderId;
        public LootMode LootMode;
        public EntityId[] Members = new EntityId[0];
        /// <summary>Raid (06 7.8): subgroup of each member (parallel to Members) and assistants.</summary>
        public bool IsRaid;
        public int[] Subgroups = new int[0];
        public EntityId[] Assistants = new EntityId[0];

        /// <summary>Who votes on the loot mode: all members, or in a raid the leader and the first member of every subgroup.</summary>
        public EntityId[] Voters = new EntityId[0];

        public EntityId InviteFromId;
        public float InviteRemaining;

        public bool VoteActive;
        public int VoteProposalId;
        public EntityId VoteProposerId;
        public LootMode VoteMode;
        public float VoteRemaining;
        public EntityId[] VotesFor = new EntityId[0];
        public float ProposeCooldownRemaining;

        public GroupLogEntry[] Log = new GroupLogEntry[0];
    }

    /// <summary>Loot log line (06, 7.8: who received what, kept for disputes).</summary>
    public struct GroupLogEntry
    {
        public string RecipientName;
        public string ItemDefinitionId;
        public int Count;
        public LootMode Mode;
    }
}
