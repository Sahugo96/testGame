using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.World
{
    /// <summary>
    /// LootEntry { definitionId, rolledData, eligibleMembers, mode, expiresAt } (06, 7.8): a drop lying on the ground.
    /// The item instance (GUID) is created only when someone picks it up; the server decides who is first.
    /// </summary>
    public struct LootData
    {
        public string ItemId;
        public int Count;
        public LootMode Mode;
        /// <summary>Who may pick it up.</summary>
        public EntityId[] Eligible;
        /// <summary>Group of the kill (for the loot log), 0 — solo.</summary>
        public int GroupId;
        public float ExpiresAt;
    }
}
