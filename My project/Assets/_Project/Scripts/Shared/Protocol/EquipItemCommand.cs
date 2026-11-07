namespace Game.Shared.Protocol
{
    /// <summary>Equip the item from a bag slot (swaps with the item in the target slot).</summary>
    public struct EquipItemCommand : IGameCommand
    {
        public int BagIndex;
    }
}
