namespace Game.Server.World
{
    /// <summary>Combat state (03, 7.1): entered on dealing/receiving damage or using a combat skill, left after CombatExitSeconds of quiet.</summary>
    public struct CombatStateData
    {
        public bool InCombat;
        public float SecondsSinceCombatAction;
    }
}
