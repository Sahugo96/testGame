namespace Game.Shared.Content
{
    /// <summary>StatModifier operation; applied in order base → Add → PercentAdd → Multiply → caps (03, 7.12).</summary>
    public enum ModifierOp
    {
        Add,
        PercentAdd,
        Multiply,
    }
}
