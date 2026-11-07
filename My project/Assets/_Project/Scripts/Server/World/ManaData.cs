namespace Game.Server.World
{
    /// <summary>
    /// Mana pools (03, 7.1): Main = pool A, Reserve = pool B.
    /// MainLocked: pool A is empty while B is not full, so A does not regenerate passively until B is full again.
    /// </summary>
    public struct ManaData
    {
        public float Main;
        public float MaxMain;
        public float Reserve;
        public float MaxReserve;
        public float MainRegen;
        public bool MainLocked;
    }
}
