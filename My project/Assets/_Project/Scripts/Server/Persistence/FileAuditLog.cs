using System;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Server.Items;

namespace Game.Server.Persistence
{
    /// <summary>Item audit log on disk (module 13): one tab-separated line per operation, a file per UTC day.</summary>
    public sealed class FileAuditLog : IAuditLog
    {
        private const string DayFormat = "yyyyMMdd";
        private readonly string _folder;
        private readonly object _lock = new object();

        public FileAuditLog(string folder)
        {
            _folder = folder;
            Directory.CreateDirectory(folder);
        }

        public void Write(AuditEntry entry)
        {
            DateTime now = DateTime.UtcNow;
            string line = string.Join("\t", now.ToString("o", CultureInfo.InvariantCulture), entry.Action, entry.ActorId, entry.InstanceId,
                entry.DefinitionId, entry.Count.ToString(CultureInfo.InvariantCulture));
            lock (_lock)
            {
                File.AppendAllText(Path.Combine(_folder, now.ToString(DayFormat, CultureInfo.InvariantCulture) + ".log"), line + "\n", Encoding.UTF8);
            }
        }
    }
}
