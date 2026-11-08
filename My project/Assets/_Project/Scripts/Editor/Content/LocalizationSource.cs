using System.IO;
using System.Text;
using Game.Client.Localization;
using UnityEngine;

namespace Game.EditorTools.Content
{
    /// <summary>
    /// The Russian texts file (Resources/Localization/ru.json) as the editor writes it: wizards add the name and description
    /// of a new record here. With Unity Localization installed (question 81) LocalizationSync copies it into the string table.
    /// </summary>
    public static class LocalizationSource
    {
        public const string Path = "Assets/_Project/Resources/Localization/ru.json";

        public static LocalizationTableDto Load() =>
            File.Exists(Path) ? JsonUtility.FromJson<LocalizationTableDto>(File.ReadAllText(Path, Encoding.UTF8)) : new LocalizationTableDto { Entries = new LocalizationEntryDto[0] };

        public static bool Has(string key)
        {
            foreach (LocalizationEntryDto entry in Load().Entries ?? new LocalizationEntryDto[0])
            {
                if (entry.Key == key)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Adds or replaces texts (key, value pairs) and writes the file once.</summary>
        public static void Set(params (string key, string value)[] texts)
        {
            LocalizationTableDto table = Load();
            var entries = new System.Collections.Generic.List<LocalizationEntryDto>(table.Entries ?? new LocalizationEntryDto[0]);
            foreach ((string key, string value) in texts)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }
                LocalizationEntryDto entry = entries.Find(e => e.Key == key);
                if (entry == null)
                {
                    entries.Add(new LocalizationEntryDto { Key = key, Value = value ?? string.Empty });
                }
                else
                {
                    entry.Value = value ?? string.Empty;
                }
            }
            table.Entries = entries.ToArray();
            File.WriteAllText(Path, JsonUtility.ToJson(table, true).Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
        }
    }
}
