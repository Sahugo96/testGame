using System;
using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>
    /// Uniform grid over the zone (module 3, principle 5: interest management from day one): entities are put into square
    /// cells once per tick, a query looks only at the cells that overlap the radius.
    /// </summary>
    public sealed class SpatialGrid
    {
        private readonly float _cellSize;
        private readonly Dictionary<long, List<EntityId>> _cells = new Dictionary<long, List<EntityId>>();
        private readonly Stack<List<EntityId>> _pool = new Stack<List<EntityId>>();

        public SpatialGrid(float cellSize) => _cellSize = cellSize;

        public void Clear()
        {
            foreach (List<EntityId> cell in _cells.Values)
            {
                cell.Clear();
                _pool.Push(cell);
            }
            _cells.Clear();
        }

        public void Add(EntityId id, Vec3 position)
        {
            long key = Key(Cell(position.X), Cell(position.Z));
            if (!_cells.TryGetValue(key, out List<EntityId> cell))
            {
                cell = _pool.Count > 0 ? _pool.Pop() : new List<EntityId>();
                _cells.Add(key, cell);
            }
            cell.Add(id);
        }

        /// <summary>Adds to `result` every entity in the cells touching the circle (callers filter by exact distance).</summary>
        public void Query(Vec3 center, float radius, List<EntityId> result)
        {
            int minX = Cell(center.X - radius);
            int maxX = Cell(center.X + radius);
            int minZ = Cell(center.Z - radius);
            int maxZ = Cell(center.Z + radius);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (_cells.TryGetValue(Key(x, z), out List<EntityId> cell))
                    {
                        result.AddRange(cell);
                    }
                }
            }
        }

        private int Cell(float coordinate) => (int)Math.Floor(coordinate / _cellSize);

        private static long Key(int x, int z) => ((long)x << 32) | (uint)z;
    }
}
