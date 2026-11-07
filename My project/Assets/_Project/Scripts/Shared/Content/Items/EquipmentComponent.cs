namespace Game.Shared.Content.Items
{
    /// <summary>The item can be equipped. Base property for EquipSlot, Armor, Weapon and Stats.</summary>
    public sealed class EquipmentComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Equipment;
    }
}
