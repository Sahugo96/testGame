using System;

namespace Game.Shared.Core
{
    /// <summary>Default <see cref="IRandom"/> backed by System.Random.</summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom(int seed) => _random = new Random(seed);

        public float NextFloat() => (float)_random.NextDouble();
    }
}
