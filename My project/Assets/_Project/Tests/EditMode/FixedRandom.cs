using Game.Shared.Core;

namespace Game.Tests
{
    /// <summary>Deterministic random: always returns Value.</summary>
    public sealed class FixedRandom : IRandom
    {
        public float Value;

        public FixedRandom(float value) => Value = value;

        public float NextFloat() => Value;
    }
}
