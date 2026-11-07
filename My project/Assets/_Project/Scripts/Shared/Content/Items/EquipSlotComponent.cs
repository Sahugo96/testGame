namespace Game.Shared.Content.Items
{
    /// <summary>Which slot type the item occupies: Head, Body, Hands, Legs, Feet, Ring, Necklace, MainHand, OffHand (04, 7.9.1). Requires Equipment.</summary>
    public sealed class EquipSlotComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.EquipSlot;

        public string SlotType;

        /// <summary>1 or 2; a two-handed weapon also blocks the off hand.</summary>
        public int Hands = 1;
    }
}
