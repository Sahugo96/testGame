#if GAME_UNITY_LOCALIZATION
using System;
using System.Collections.Generic;
using Game.Shared.Core;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Game.Client.Localization
{
    /// <summary>
    /// Texts from Unity Localization (module 4, question 81): the string table "Game" of the selected locale. A key the table
    /// lacks (or an empty translation) falls back to the JSON table; {0}-style placeholders work as before (entries are not
    /// Smart Strings). Compiled only with the com.unity.localization package (GAME_UNITY_LOCALIZATION).
    /// </summary>
    public sealed class UnityLocalizationService : ILocalizationService, ILanguageService
    {
        public const string TableName = "Game";

        private readonly JsonTableLocalizationService _fallback;
        private StringTable _table;

        private UnityLocalizationService(JsonTableLocalizationService fallback) => _fallback = fallback;

        /// <summary>The service, or null when the project has no Localization settings or the table cannot load.</summary>
        public static UnityLocalizationService TryCreate(JsonTableLocalizationService fallback, string preferredLanguage)
        {
            try
            {
                if (!LocalizationSettings.HasSettings)
                {
                    return null;
                }
                LocalizationSettings.InitializationOperation.WaitForCompletion();
                var service = new UnityLocalizationService(fallback);
                if (!string.IsNullOrEmpty(preferredLanguage))
                {
                    service.Select(preferredLanguage);
                }
                service.LoadTable();
                return service._table != null ? service : null;
            }
            catch (Exception exception)
            {
                Log.Warning(LogTags.Client, $"Unity Localization is not usable ({exception.Message}): the JSON table is used");
                return null;
            }
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            StringTableEntry entry = _table?.GetEntry(key);
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : _fallback.Get(key);
        }

        public string Format(string key, params object[] args) => string.Format(Get(key), args);

        public IReadOnlyList<string> Codes
        {
            get
            {
                var codes = new List<string>();
                foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
                {
                    codes.Add(locale.Identifier.Code);
                }
                return codes;
            }
        }

        public string Current => LocalizationSettings.SelectedLocale != null ? LocalizationSettings.SelectedLocale.Identifier.Code : _fallback.Current;

        public string NameOf(string code)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            return locale != null ? locale.LocaleName : code;
        }

        public bool Select(string code)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale == null)
            {
                return false;
            }
            if (LocalizationSettings.SelectedLocale != locale)
            {
                LocalizationSettings.SelectedLocale = locale;
                LoadTable();
            }
            return true;
        }

        private void LoadTable() => _table = LocalizationSettings.StringDatabase.GetTable(TableName);
    }
}
#endif
