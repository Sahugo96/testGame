namespace Game.Shared.Protocol
{
    /// <summary>
    /// Drag a bag item onto another cell of its section (decision of the developer): Count 0 = the whole stack (move, merge
    /// into the same item, or swap with another item); Count &gt; 0 = split that many off into an empty cell or the same item.
    /// The client sends half the stack with Shift and one with Ctrl; the server checks everything.
    /// </summary>
    public struct MoveBagItemCommand : IGameCommand
    {
        public int FromIndex;
        public int ToIndex;
        public int Count;
    }
}
