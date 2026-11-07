namespace Game.Shared.Rules
{
    /// <summary>Result of a multi-tier crit roll (question 25).</summary>
    public struct CritRoll
    {
        /// <summary>0 = no crit, 1 = crit, 2 = double crit, ...</summary>
        public int Tier;

        /// <summary>Damage/heal multiplier: 1 without crit, otherwise CritSize × Tier.</summary>
        public float Multiplier;
    }
}
