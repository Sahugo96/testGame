namespace Game.Server.World
{
    /// <summary>Health and death state.</summary>
    public struct VitalsData
    {
        public float Health;
        public float MaxHealth;
        public float HealthRegen;
        public float Defense;
        public bool IsDead;
        public float RespawnRemaining;
    }
}
