using System.Collections.Generic;

namespace Game.Server.Systems
{
    /// <summary>Recently seen request ids (idempotency, recommendation 21): a repeated buy or send is ignored. Bounded FIFO.</summary>
    public sealed class RequestMemory
    {
        private readonly HashSet<string> _seen = new HashSet<string>();
        private readonly Queue<string> _order = new Queue<string>();
        private readonly int _capacity;

        public RequestMemory(int capacity) => _capacity = System.Math.Max(1, capacity);

        /// <summary>True the first time a key is seen; false for a repeat. Empty keys are never remembered.</summary>
        public bool TryAdd(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return true;
            }
            if (!_seen.Add(key))
            {
                return false;
            }
            _order.Enqueue(key);
            while (_order.Count > _capacity)
            {
                _seen.Remove(_order.Dequeue());
            }
            return true;
        }
    }
}
