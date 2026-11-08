using System.Collections.Generic;
using Game.Shared.Core;
using UnityEngine;

namespace Game.Client.Localization
{
    /// <summary>
    /// Key table loaded from Resources/Localization/&lt;language&gt;.json (the source of the Unity string table and its
    /// fallback). Missing keys show as #key and are logged once. Offers only its own language.
    /// </summary>
    public sealed class JsonTableLocalizationService : ILocalizationService, ILanguageService
    {
        private const string Folder = "Localization/";
        private const string MissingPrefix = "#";

        private readonly Dictionary<string, string> _table = new Dictionary<string, string>();
        private readonly HashSet<string> _reportedMissing = new HashSet<string>();

        private readonly string _language;

        public JsonTableLocalizationService(string language)
        {
            _language = language;
            var asset = Resources.Load<TextAsset>(Folder + language);
            if (asset == null)
            {
                Log.Error(LogTags.Client, $"Localization table '{language}' not found");
                return;
            }
            LocalizationTableDto table = JsonUtility.FromJson<LocalizationTableDto>(asset.text);
            foreach (LocalizationEntryDto entry in table.Entries ?? new LocalizationEntryDto[0])
            {
                _table[entry.Key] = entry.Value;
            }
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            if (_table.TryGetValue(key, out string value))
            {
                return value;
            }
            if (_reportedMissing.Add(key))
            {
                Log.Warning(LogTags.Client, $"Missing localization key '{key}'");
            }
            return MissingPrefix + key;
        }

        public string Format(string key, params object[] args) => string.Format(Get(key), args);

        /// <summary>The key's text, or null when the table has none (Unity Localization falls back here).</summary>
        public string Find(string key) => key != null && _table.TryGetValue(key, out string value) ? value : null;

        public IReadOnlyList<string> Codes => new[] { _language };

        public string Current => _language;

        public string NameOf(string code) => code;

        public bool Select(string code) => code == _language;
    }
}
