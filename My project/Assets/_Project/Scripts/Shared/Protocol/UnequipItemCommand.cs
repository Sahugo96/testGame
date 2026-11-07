namespace Game.Shared.Protocol
{
    /// <summary>Move the item from an equipment slot to the bag.</summary>
    public struct UnequipItemCommand : IGameCommand
    {
        public string SlotId;
    }
}
