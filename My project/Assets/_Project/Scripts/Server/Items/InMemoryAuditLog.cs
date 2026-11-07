using System.Collections.Generic;

namespace Game.Server.Items
{
    /// <summary>Keeps audit entries in memory (until module 13 adds persistent storage).</summary>
    public sealed class InMemoryAuditLog : IAuditLog
    {
        private readonly List<AuditEntry> _entries = new List<AuditEntry>();

        public IReadOnlyList<AuditEntry> Entries => _entries;

        public void Write(AuditEntry entry) => _entries.Add(entry);
    }
}
