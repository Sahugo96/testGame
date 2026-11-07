namespace Game.Shared.Content
{
    /// <summary>Built-in effect handler keys (01, 7.10). Content refers to handlers by these strings; new keys are added by registering a handler.</summary>
    public static class EffectKeys
    {
        public const string Damage = "Damage";
        public const string Heal = "Heal";
        public const string RestoreResource = "RestoreResource";
        /// <summary>Puts a status effect on the target (03, 7.12).</summary>
        public const string ApplyStatus = "ApplyStatus";
        /// <summary>Removes dispellable debuffs (or buffs of an enemy, later) of a dispel type.</summary>
        public const string Dispel = "Dispel";
    }
}
