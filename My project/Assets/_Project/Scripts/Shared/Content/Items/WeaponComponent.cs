namespace Game.Shared.Content.Items
{
    /// <summary>Weapon type (sword, staff, ...) checked against the class rules. Requires Equipment and EquipSlot.</summary>
    public sealed class WeaponComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Weapon;

        public string WeaponType;
    }
}
