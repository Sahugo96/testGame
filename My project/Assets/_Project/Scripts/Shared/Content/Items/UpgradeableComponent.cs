namespace Game.Shared.Content.Items
{
    /// <summary>The item can be upgraded at a whetstone (04, 7.14.1) by the upgrade table RulesId. Requires Equipment.</summary>
    public sealed class UpgradeableComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Upgradeable;

        public string RulesId;
    }
}
