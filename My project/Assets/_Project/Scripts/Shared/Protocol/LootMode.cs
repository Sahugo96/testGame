namespace Game.Shared.Protocol
{
    /// <summary>Group loot modes (06, 7.8). One mode for the whole group; changed only by a unanimous vote.</summary>
    public enum LootMode : byte
    {
        /// <summary>"Поднявшему": the first eligible player to pick it up (until ground loot exists — the killer).</summary>
        PickedUp = 0,
        /// <summary>"Лидеру": all loot goes to the group leader.</summary>
        Leader = 1,
        /// <summary>"По очереди": items go around the loot queue.</summary>
        RoundRobin = 2,
    }
}
