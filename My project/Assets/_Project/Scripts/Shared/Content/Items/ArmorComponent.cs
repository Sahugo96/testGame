namespace Game.Shared.Content.Items
{
    /// <summary>Armor type (cloth, leather, plate) checked against the class rules. Requires Equipment and EquipSlot.</summary>
    public sealed class ArmorComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Armor;

        public string ArmorType;
    }
}
