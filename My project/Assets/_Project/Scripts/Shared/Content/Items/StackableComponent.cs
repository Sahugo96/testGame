namespace Game.Shared.Content.Items
{
    /// <summary>The item stacks up to MaxStack in one bag slot.</summary>
    public sealed class StackableComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Stackable;

        public int MaxStack;
    }
}
