#if GAME_UNITY_LOCALIZATION
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Client.Localization;
using Game.Shared.Content.Packs;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Game.EditorTools.Content
{
    /// <summary>
    /// Unity Localization in the project (module 4, question 81 — decision of the developer). "Game → Localization → Set up"
    /// creates (once) the Localization Settings, the Russian locale and the string table collection "Game" under
    /// Assets/_Project/Localization. Then ru.json and the Russian string table are kept the same both ways, like content cards:
    /// whichever changed since the last sync wins (ContentSyncRule); if both did, ru.json wins and the table's version is saved
    /// to Library/ContentConflicts. ru.json stays because tests, tools and the game's fallback read it.
    /// </summary>
    [InitializeOnLoad]
    public static class LocalizationSync
    {
        private const string Root = "Assets/_Project/Localization";
        private const string SettingsPath = Root + "/LocalizationSettings.asset";
        private const string LocalesFolder = Root + "/Locales";
        private const string TablesFolder = Root + "/Tables";
        private const string StatePath = Root + "/sync_state.txt";
        private const string ConflictFolder = "Library/ContentConflicts";
        private const string MenuRoot = "Game/Localization/";
        private const string LogPrefix = "[Localization] ";

        private static bool _running;

        static LocalizationSync()
        {
            EditorApplication.delayCall += () => Sync(false);
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    Sync(false);
                }
            };
        }

        [MenuItem(MenuRoot + "Set up (settings, Russian locale, table Game)", priority = 0)]
        private static void SetUpMenu()
        {
            SetUp();
            Sync(true);
        }

        [MenuItem(MenuRoot + "Sync ru.json and the table", priority = 1)]
        private static void SyncMenu() => Sync(true);

        /// <summary>Creates what is missing; returns the Russian string table (null if the package cannot do it).</summary>
        public static StringTable SetUp()
        {
            ContentSync.EnsureFolder(LocalesFolder);
            ContentSync.EnsureFolder(TablesFolder);
            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                }
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }
            Locale russian = LocalizationEditorSettings.GetLocale(LocalizationFactory.JsonLanguage);
            if (russian == null)
            {
                russian = Locale.CreateLocale(LocalizationFactory.JsonLanguage);
                AssetDatabase.CreateAsset(russian, $"{LocalesFolder}/Russian ({LocalizationFactory.JsonLanguage}).asset");
                LocalizationEditorSettings.AddLocale(russian);
            }
            LocalizationSettings.ProjectLocale = russian;
            EditorUtility.SetDirty(settings);
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(UnityLocalizationService.TableName)
                ?? LocalizationEditorSettings.CreateStringTableCollection(UnityLocalizationService.TableName, TablesFolder, new List<Locale> { russian });
            AssetDatabase.SaveAssets();
#if GAME_ADDRESSABLES
            AddressablesNaming.UseShortNames();
#endif
            return collection.GetTable(LocalizationFactory.JsonLanguage) as StringTable;
        }

        /// <summary>ru.json ↔ the Russian table; quiet unless something changed or it was asked for.</summary>
        public static void Sync(bool report)
        {
            if (_running || EditorApplication.isPlaying)
            {
                return;
            }
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(UnityLocalizationService.TableName);
            if (collection == null)
            {
                if (report)
                {
                    Debug.LogWarning(LogPrefix + "Not set up yet: Game → Localization → Set up.");
                }
                return;
            }
            _running = true;
            try
            {
                var table = collection.GetTable(LocalizationFactory.JsonLanguage) as StringTable;
                if (table == null)
                {
                    Debug.LogError(LogPrefix + $"The table '{UnityLocalizationService.TableName}' has no '{LocalizationFactory.JsonLanguage}' locale.");
                    return;
                }
                LocalizationTableDto json = LocalizationSource.Load();
                string jsonText = Canonical(FromJson(json));
                string tableText = Canonical(FromTable(table));
                string last = File.Exists(StatePath) ? File.ReadAllText(StatePath).Trim() : null;
                string jsonHash = Hash(jsonText);
                string tableHash = Hash(tableText);
                switch (ContentSyncRule.Decide(jsonHash, table.SharedData.Entries.Count > 0 ? tableHash : null, last))
                {
                    case ContentSyncAction.InSync:
                        break;
                    case ContentSyncAction.Export:
                        ToJson(json, FromTable(table));
                        AssetDatabase.ImportAsset(LocalizationSource.Path);
                        Debug.Log(LogPrefix + "Texts edited in the string table were written to ru.json.");
                        break;
                    case ContentSyncAction.Conflict:
                        Directory.CreateDirectory(ConflictFolder);
                        string backup = Path.Combine(ConflictFolder, $"strings_{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                        File.WriteAllText(backup, tableText, new UTF8Encoding(false));
                        Debug.LogWarning(LogPrefix + $"Both ru.json and the string table changed: ru.json was taken, the table's texts are in {backup}.");
                        ToTable(table, FromJson(json));
                        break;
                    case ContentSyncAction.Import:
                        ToTable(table, FromJson(json));
                        if (report || last != null)
                        {
                            Debug.Log(LogPrefix + "ru.json was copied into the string table.");
                        }
                        break;
                }
                File.WriteAllText(StatePath, jsonHash ?? string.Empty);
                if (report)
                {
                    Debug.Log(LogPrefix + $"ru.json and the table '{UnityLocalizationService.TableName}' are in sync ({table.SharedData.Entries.Count} keys).");
                }
            }
            finally
            {
                _running = false;
            }
        }

        private static List<KeyValuePair<string, string>> FromJson(LocalizationTableDto json)
        {
            var result = new List<KeyValuePair<string, string>>();
            foreach (LocalizationEntryDto entry in json.Entries ?? new LocalizationEntryDto[0])
            {
                result.Add(new KeyValuePair<string, string>(entry.Key, entry.Value ?? string.Empty));
            }
            return result;
        }

        private static List<KeyValuePair<string, string>> FromTable(StringTable table)
        {
            var result = new List<KeyValuePair<string, string>>();
            foreach (SharedTableData.SharedTableEntry shared in table.SharedData.Entries)
            {
                StringTableEntry entry = table.GetEntry(shared.Key);
                result.Add(new KeyValuePair<string, string>(shared.Key, entry?.Value ?? string.Empty));
            }
            return result;
        }

        /// <summary>The table becomes exactly ru.json: values set, new keys added, keys missing from ru.json removed.</summary>
        private static void ToTable(StringTable table, List<KeyValuePair<string, string>> texts)
        {
            var keys = new HashSet<string>();
            foreach (KeyValuePair<string, string> text in texts)
            {
                keys.Add(text.Key);
                StringTableEntry entry = table.GetEntry(text.Key) ?? table.AddEntry(text.Key, text.Value);
                entry.Value = text.Value;
            }
            foreach (SharedTableData.SharedTableEntry shared in new List<SharedTableData.SharedTableEntry>(table.SharedData.Entries))
            {
                if (!keys.Contains(shared.Key))
                {
                    table.SharedData.RemoveKey(shared.Key);
                }
            }
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            AssetDatabase.SaveAssets();
        }

        /// <summary>ru.json takes the table's texts, keeping its own order; new keys go to the end.</summary>
        private static void ToJson(LocalizationTableDto json, List<KeyValuePair<string, string>> texts)
        {
            var values = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> text in texts)
            {
                values[text.Key] = text.Value;
            }
            var pairs = new List<(string key, string value)>();
            var written = new HashSet<string>();
            foreach (LocalizationEntryDto entry in json.Entries ?? new LocalizationEntryDto[0])
            {
                if (values.TryGetValue(entry.Key, out string value))
                {
                    pairs.Add((entry.Key, value));
                    written.Add(entry.Key);
                }
            }
            foreach (KeyValuePair<string, string> text in texts)
            {
                if (!written.Contains(text.Key))
                {
                    pairs.Add((text.Key, text.Value));
                }
            }
            json.Entries = pairs.ConvertAll(p => new LocalizationEntryDto { Key = p.key, Value = p.value }).ToArray();
            File.WriteAllText(LocalizationSource.Path, JsonUtility.ToJson(json, true).Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
        }

        /// <summary>Order-free text of the pairs (sorted by key) for comparing the two sides.</summary>
        private static string Canonical(List<KeyValuePair<string, string>> texts)
        {
            var sorted = new List<KeyValuePair<string, string>>(texts);
            sorted.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> text in sorted)
            {
                sb.Append(text.Key).Append('\t').Append(text.Value.Replace("\r\n", "\n")).Append('\n');
            }
            return sb.ToString();
        }

        private static string Hash(string text)
        {
            var hash = new ContentHash();
            hash.Add(string.Empty, text);
            return hash.Finish();
        }

        /// <summary>ru.json changed on disk (git pull, a wizard): sync after the import.</summary>
        private sealed class RussianJsonWatcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (!_running && Array.IndexOf(imported, LocalizationSource.Path) >= 0)
                {
                    EditorApplication.delayCall += () => Sync(false);
                }
            }
        }
    }
}
#endif
