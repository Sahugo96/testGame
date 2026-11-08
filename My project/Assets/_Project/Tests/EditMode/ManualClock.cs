using System;
using Game.Server;

namespace Game.Tests
{
    /// <summary>Real time under the test's control (shops, mail and orders live for hours and days).</summary>
    public sealed class ManualClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public void Advance(TimeSpan span) => UtcNow += span;
    }
}
