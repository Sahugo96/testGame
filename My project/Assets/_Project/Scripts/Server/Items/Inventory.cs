namespace Game.Server.Items
{
    /// <summary>Bag of a character: fixed number of slots (config BagSize), each holds one instance or nothing.</summary>
    public sealed class Inventory
    {
        private readonly ItemInstance[] _slots;

        public Inventory(int size) => _slots = new ItemInstance[size];

        public int Size => _slots.Length;

        public ItemInstance Get(int index) => index >= 0 && index < _slots.Length ? _slots[index] : null;

        public void Set(int index, ItemInstance item) => _slots[index] = item;

        public int FindFreeSlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        public int FreeSlotCount()
        {
            int free = 0;
            foreach (ItemInstance slot in _slots)
            {
                if (slot == null)
                {
                    free++;
                }
            }
            return free;
        }

        /// <summary>Puts the item into the first free slot; returns the slot or -1 if the bag is full.</summary>
        public int Add(ItemInstance item)
        {
            int index = FindFreeSlot();
            if (index >= 0)
            {
                _slots[index] = item;
            }
            return index;
        }
    }
}
