namespace Game.Client.Input
{
    /// <summary>Outcome of a rebinding: cancelled, done, or done with a clash with <see cref="Other"/> to resolve.</summary>
    public sealed class RebindResult
    {
        public RebindSlot Slot;
        public RebindSlot Other;
        public string PreviousPath;
        public bool Cancelled;

        public bool IsConflict => !Cancelled && Other != null;
    }
}
