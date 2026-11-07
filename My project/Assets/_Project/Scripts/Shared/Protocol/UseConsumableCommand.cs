namespace Game.Shared.Protocol
{
    /// <summary>CmdUseConsumable (02, 7.7 Б): use the item in a bag slot.</summary>
    public struct UseConsumableCommand : IGameCommand
    {
        public int BagIndex;
    }
}
