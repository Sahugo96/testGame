using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Game.Server.Persistence
{
    /// <summary>
    /// File storage (temporary DEFAULT, question 76; PostgreSQL later behind the same interfaces): one JSON file per
    /// account and character, a name index and the unique item registry. Writes are atomic (temporary file, then replace),
    /// shared files are guarded by locks. Backup copies the whole folder and keeps the last N copies.
    /// </summary>
    public sealed class FileStorage : IAccountStore, ICharacterStore, IUniqueItemRegistry
    {
        private const string AccountsFolder = "accounts";
        private const string CharactersFolder = "characters";
        private const string BackupsFolder = "backups";
        private const string NamesFile = "names.json";
        private const string UniqueFile = "unique_items.json";
        private const string AuditFolder = "audit";
        private const string TempSuffix = ".tmp";
        private const string BackupTimeFormat = "yyyyMMdd-HHmmss";

        private readonly string _root;
        private readonly IJsonCodec _json;
        private readonly object _namesLock = new object();
        private readonly object _uniqueLock = new object();
        private Dictionary<string, string> _names;
        private Dictionary<string, UniqueEntryRecord> _unique;

        public FileStorage(string root, IJsonCodec json)
        {
            _root = root;
            _json = json;
            Directory.CreateDirectory(Path.Combine(_root, AccountsFolder));
            Directory.CreateDirectory(Path.Combine(_root, CharactersFolder));
        }

        public string Root => _root;

        public string AuditPath => Path.Combine(_root, AuditFolder);

        public AccountRecord LoadAccount(string login) => Read<AccountRecord>(AccountPath(login));

        public void SaveAccount(AccountRecord account) => Write(AccountPath(account.Login), account);

        public CharacterRecord LoadCharacter(string login, string classId)
        {
            CharacterRecord record = Read<CharacterRecord>(CharacterPath(login, classId));
            return record == null ? null : CharacterMigrations.Upgrade(record);
        }

        public void SaveCharacter(CharacterRecord character)
        {
            character.SchemaVersion = CharacterMigrations.Current;
            Write(CharacterPath(character.AccountLogin, character.ClassId), character);
        }

        public bool TryClaimName(string name, string login)
        {
            string key = name.Trim().ToLowerInvariant();
            lock (_namesLock)
            {
                LoadNames();
                if (_names.TryGetValue(key, out string owner))
                {
                    return owner == login;
                }
                _names[key] = login;
                var entries = new List<NameEntryRecord>();
                foreach (KeyValuePair<string, string> pair in _names)
                {
                    entries.Add(new NameEntryRecord { Name = pair.Key, Login = pair.Value });
                }
                Write(Path.Combine(_root, NamesFile), new NameIndexRecord { Entries = entries.ToArray() });
                return true;
            }
        }

        public bool NameExists(string name) => LoginOfName(name) != null;

        public string LoginOfName(string name)
        {
            lock (_namesLock)
            {
                LoadNames();
                return _names.TryGetValue((name ?? string.Empty).Trim().ToLowerInvariant(), out string login) ? login : null;
            }
        }

        public bool TryReserve(string definitionId, string instanceId, string ownerId)
        {
            lock (_uniqueLock)
            {
                LoadUnique();
                if (_unique.TryGetValue(definitionId, out UniqueEntryRecord entry))
                {
                    return entry.InstanceId == instanceId;
                }
                _unique[definitionId] = new UniqueEntryRecord
                {
                    DefinitionId = definitionId,
                    InstanceId = instanceId,
                    OwnerId = ownerId,
                    Since = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                };
                SaveUnique();
                return true;
            }
        }

        public void Release(string definitionId, string instanceId)
        {
            lock (_uniqueLock)
            {
                LoadUnique();
                if (_unique.TryGetValue(definitionId, out UniqueEntryRecord entry) && entry.InstanceId == instanceId)
                {
                    _unique.Remove(definitionId);
                    SaveUnique();
                }
            }
        }

        public string OwnerOf(string definitionId)
        {
            lock (_uniqueLock)
            {
                LoadUnique();
                return _unique.TryGetValue(definitionId, out UniqueEntryRecord entry) ? entry.OwnerId : null;
            }
        }

        /// <summary>Copies the saves (not older backups) into backups/&lt;time&gt; and deletes all but the newest `keep` copies.</summary>
        public string Backup(int keep)
        {
            string backups = Path.Combine(_root, BackupsFolder);
            string target = Path.Combine(backups, DateTime.UtcNow.ToString(BackupTimeFormat, CultureInfo.InvariantCulture));
            int suffix = 1;
            while (Directory.Exists(target))
            {
                target = Path.Combine(backups, DateTime.UtcNow.ToString(BackupTimeFormat, CultureInfo.InvariantCulture) + "-" + suffix++);
            }
            CopyFolder(_root, target, backups);
            var existing = new List<string>(Directory.GetDirectories(backups));
            existing.Sort(StringComparer.Ordinal);
            for (int i = 0; i < existing.Count - keep; i++)
            {
                Directory.Delete(existing[i], true);
            }
            return target;
        }

        /// <summary>File names from any login or class id: hex of the UTF-8 bytes (safe on every file system, case-insensitive).</summary>
        public static string SafeName(string text)
        {
            var sb = new StringBuilder();
            foreach (byte b in Encoding.UTF8.GetBytes(text.Trim().ToLowerInvariant()))
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private string AccountPath(string login) => Path.Combine(_root, AccountsFolder, SafeName(login) + ".json");

        private string CharacterPath(string login, string classId) =>
            Path.Combine(_root, CharactersFolder, SafeName(login), SafeName(classId) + ".json");

        private void LoadNames()
        {
            if (_names != null)
            {
                return;
            }
            _names = new Dictionary<string, string>();
            NameIndexRecord record = Read<NameIndexRecord>(Path.Combine(_root, NamesFile));
            foreach (NameEntryRecord entry in record?.Entries ?? new NameEntryRecord[0])
            {
                _names[entry.Name] = entry.Login;
            }
        }

        private void LoadUnique()
        {
            if (_unique != null)
            {
                return;
            }
            _unique = new Dictionary<string, UniqueEntryRecord>();
            UniqueRegistryRecord record = Read<UniqueRegistryRecord>(Path.Combine(_root, UniqueFile));
            foreach (UniqueEntryRecord entry in record?.Entries ?? new UniqueEntryRecord[0])
            {
                _unique[entry.DefinitionId] = entry;
            }
        }

        private void SaveUnique() =>
            Write(Path.Combine(_root, UniqueFile), new UniqueRegistryRecord { Entries = new List<UniqueEntryRecord>(_unique.Values).ToArray() });

        private T Read<T>(string path) where T : class
        {
            if (!File.Exists(path))
            {
                return null;
            }
            return _json.FromJson<T>(File.ReadAllText(path, Encoding.UTF8));
        }

        private void Write(string path, object value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + TempSuffix;
            File.WriteAllText(temp, _json.ToJson(value), new UTF8Encoding(false));
            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }

        private static void CopyFolder(string source, string target, string skip)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            }
            foreach (string folder in Directory.GetDirectories(source))
            {
                if (string.Equals(Path.GetFullPath(folder), Path.GetFullPath(skip), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                CopyFolder(folder, Path.Combine(target, Path.GetFileName(folder)), skip);
            }
        }
    }
}
