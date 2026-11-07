namespace Game.Shared.Content.Items
{
    /// <summary>
    /// A property attached to an item definition. Components may depend on others
    /// (e.g. EquipSlot "Head" requires Equipment); dependencies are declared in <see cref="ItemComponentRegistry"/>.
    /// </summary>
    public abstract class ItemComponent
    {
        /// <summary>Registry key, e.g. "Equipment".</summary>
        public abstract string Type { get; }
    }
}
