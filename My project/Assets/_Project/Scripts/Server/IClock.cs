using System;

namespace Game.Server
{
    /// <summary>Wall-clock time for things that live for hours and days across restarts (shops, mail, orders). Tests set it.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
