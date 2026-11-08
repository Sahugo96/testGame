namespace Game.Server.Systems
{
    /// <summary>Result of one upgrade attempt (UpgradeSystem.Attempt).</summary>
    public enum UpgradeOutcome
    {
        /// <summary>Not attempted (no rule, maximum, no gold or materials): nothing was spent.</summary>
        Refused,
        Success,
        Failed,
        /// <summary>Failed and the item broke: the caller removes it.</summary>
        Broken,
    }
}
