using System.Collections.Generic;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.World
{
    /// <summary>
    /// One group (06, 7.8): members by join order (the first is the next leader candidate), leader, loot mode,
    /// round-robin loot queue, the loot-mode vote and the loot log.
    /// </summary>
    public struct GroupData
    {
        public int Id;
        public EntityId LeaderId;
        public List<EntityId> Members;
        public LootMode LootMode;
        /// <summary>"По очереди": the front gets the next item and moves to the back; skipped players keep their place.</summary>
        public List<EntityId> LootQueue;
        public LootModeVote Vote;
        /// <summary>Server time after which a new proposal is allowed (a failed vote delays the next one, question 21).</summary>
        public float ProposeAllowedAt;
        public List<GroupLogEntry> Log;
    }

    /// <summary>LootModeVote { proposalId, proposerId, newMode, votes[], expiresAt } (06, 7.8). The server counts the votes.</summary>
    public struct LootModeVote
    {
        public bool Active;
        public int ProposalId;
        public EntityId ProposerId;
        public LootMode NewMode;
        public List<EntityId> VotesFor;
        public float ExpiresAt;
    }

    /// <summary>A pending group invite: who invited and until when it is valid.</summary>
    public struct GroupInviteData
    {
        public EntityId FromId;
        public float ExpiresAt;
    }
}
