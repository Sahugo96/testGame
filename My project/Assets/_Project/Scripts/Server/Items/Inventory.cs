namespace Game.Server.Items
{
    /// <summary>
    /// Bag of a character: one flat array split into sections (tabs) by content (bag_sections.json).
    /// Placement rules (which section, stacking) live in InventorySystem; this class only stores.
    /// </summary>
    public sealed class Inventory
    {
        private readonly ItemInstance[] _slots;

        public Inventory(int size) => _slots = new ItemInstance[size];

        public int Size => _slots.Length;

        public ItemInstance Get(int index) => index >= 0 && index < _slots.Length ? _slots[index] : null;

        public void Set(int index, ItemInstance item) => _slots[index] = item;

        /// <summary>Slot of the instance with this id (ItemInstance.SourceId), or -1. The one place that searches the bag by id.</summary>
        public int IndexOfInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return -1;
            }
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].SourceId == instanceId)
                {
                    return i;
                }
            }
            return -1;
        }

        public int FindFreeSlot(int start, int count)
        {
            for (int i = start; i < start + count && i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        public int FreeSlotCount(int start, int count)
        {
            int free = 0;
            for (int i = start; i < start + count && i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    free++;
                }
            }
            return free;
        }

        public int IndexOf(ItemInstance item)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == item)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
