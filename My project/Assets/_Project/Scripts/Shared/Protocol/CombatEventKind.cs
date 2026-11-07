namespace Game.Shared.Protocol
{
    /// <summary>Kind of a combat log event.</summary>
    public enum CombatEventKind
    {
        Damage,
        Heal,
        ResourceRestore,
        Death,
        /// <summary>SkillId = status id, Amount = stacks.</summary>
        StatusApplied,
        StatusRemoved,
    }
}
