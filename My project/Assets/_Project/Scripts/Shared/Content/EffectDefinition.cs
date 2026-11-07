namespace Game.Shared.Content
{
    /// <summary>Typed effect block handled by a registered handler (01, 7.10).</summary>
    public sealed class EffectDefinition
    {
        public string Key;
        public float Amount;
        public ResourceKind Resource;
        /// <summary>ApplyStatus: which status (03, 7.12).</summary>
        public string StatusId;
        /// <summary>Dispel: which dispel type (empty = any); Amount = how many statuses.</summary>
        public string DispelType;
    }
}
