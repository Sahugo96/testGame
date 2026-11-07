namespace Game.Shared.Content
{
    /// <summary>Skill cost part (03, 7.1). FROZEN structure: Resource, Amount, Mode.</summary>
    public sealed class CostEntry
    {
        public ResourceKind Resource;
        public float Amount;
        public CostMode Mode;
    }
}
