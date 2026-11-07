using System;
using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>Dense storage of one struct component per entity (data-oriented core, principle 12). Get returns a ref for in-place edits.</summary>
    public sealed class ComponentStore<T> where T : struct
    {
        private const int InitialCapacity = 16;

        private readonly Dictionary<int, int> _indexById = new Dictionary<int, int>();
        private T[] _items = new T[InitialCapacity];
        private EntityId[] _owners = new EntityId[InitialCapacity];
        private int _count;

        public int Count => _count;

        public bool Has(EntityId id) => _indexById.ContainsKey(id.Value);

        public ref T Get(EntityId id) => ref _items[_indexById[id.Value]];

        public ref T At(int index) => ref _items[index];

        public EntityId OwnerAt(int index) => _owners[index];

        public void Add(EntityId id, T value)
        {
            if (Has(id))
            {
                throw new InvalidOperationException($"{typeof(T).Name} already exists for {id}");
            }
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, _count * 2);
                Array.Resize(ref _owners, _count * 2);
            }
            _items[_count] = value;
            _owners[_count] = id;
            _indexById[id.Value] = _count;
            _count++;
        }

        public void Remove(EntityId id)
        {
            if (!_indexById.TryGetValue(id.Value, out int index))
            {
                return;
            }
            int last = _count - 1;
            _items[index] = _items[last];
            _owners[index] = _owners[last];
            _indexById[_owners[index].Value] = index;
            _items[last] = default;
            _owners[last] = EntityId.None;
            _indexById.Remove(id.Value);
            _count--;
        }
    }
}
