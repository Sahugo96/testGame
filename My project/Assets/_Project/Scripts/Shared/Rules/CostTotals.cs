namespace Game.Shared.Rules
{
    /// <summary>Skill cost summed per pool: Main = pool A only, Reserve = pool B only, Smart = A then B, Health.</summary>
    public struct CostTotals
    {
        public float Main;
        public float Reserve;
        public float Smart;
        public float Health;
    }
}
