namespace Game.Shared.Content.Items
{
    /// <summary>Using the item applies an effect and consumes one unit; cooldown is shared per group (02, 7.7 Б).</summary>
    public sealed class ConsumableComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Consumable;

        public string EffectKey;
        public ResourceKind Resource;
        public float Amount;
        public string CooldownGroup;
        public float Cooldown;
    }
}
