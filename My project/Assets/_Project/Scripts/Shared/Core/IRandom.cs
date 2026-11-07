namespace Game.Shared.Core
{
    /// <summary>Random source abstraction so server rolls are deterministic in tests.</summary>
    public interface IRandom
    {
        /// <summary>Uniform value in [0, 1).</summary>
        float NextFloat();
    }
}
