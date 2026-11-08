using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Server;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Ids;
using Game.Shared.Content.Packs;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Content
{
    /// <summary>
    /// Keeps the content cards (Assets/_Project/Content, edited in the inspector — question 80) and the content JSON
    /// (Assets/_Project/Resources/Content, read by the game, the server, tests and tools) the same. Runs when the editor
    /// loads, shortly after a card changes, when JSON changes on disk (git pull), before Play and before a build.
    /// Direction per file — ContentSyncRule: the side that changed since the last sync wins; if both changed, the JSON wins
    /// and the cards' version is saved to Library/ContentConflicts. New ids go into content_ids.json; the content is
    /// validated after every sync (errors in the Console).
    /// </summary>
    [InitializeOnLoad]
    public static class ContentSync
    {
        public const string JsonRoot = "Assets/_Project/Resources/Content";
        public const string CardsRoot = "Assets/_Project/Content";
        private const string StatePath = CardsRoot + "/sync_state.json";
        private const string ConflictFolder = "Library/ContentConflicts";
        private const string LogPrefix = "[Content] ";
        /// <summary>Card edits are written to JSON this long after the last change (typing in the inspector is not a sync per key).</summary>
        private const double DebounceSeconds = 1.5;

        private static bool _running;
        private static double _dueAt = -1;

        static ContentSync()
        {
            EditorApplication.delayCall += () => Run("editor load");
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    Run("before Play");
                }
            };
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        /// <summary>Schedules a sync shortly (after a card change or a JSON file change on disk).</summary>
        public static void Schedule()
        {
            if (!_running)
            {
                _dueAt = EditorApplication.timeSinceStartup + DebounceSeconds;
            }
        }

        /// <summary>Syncs all packs and validates; returns the content errors (empty = fine).</summary>
        public static List<string> Run(string reason)
        {
            var errors = new List<string>();
            if (_running || EditorApplication.isPlaying)
            {
                return errors;
            }
            _running = true;
            _dueAt = -1;
            try
            {
                SyncState state = SyncState.Load(StatePath);
                bool jsonWritten = false;
                bool cardsChanged = false;
                foreach (string pack in PackIds())
                {
                    foreach (ContentSyncTarget target in ContentSyncTarget.All)
                    {
                        SyncFile(pack, target, state, ref jsonWritten, ref cardsChanged);
                    }
                }
                if (cardsChanged)
                {
                    AssetDatabase.SaveAssets();
                }
                state.Save(StatePath);
                jsonWritten |= UpdateLedger();
                if (jsonWritten)
                {
                    AssetDatabase.Refresh();
                }
                errors = Validate();
                if (errors.Count > 0)
                {
                    foreach (string error in errors)
                    {
                        Debug.LogError(LogPrefix + error);
                    }
                }
                else if (jsonWritten || cardsChanged || reason == Menu.Manual)
                {
                    Debug.Log(LogPrefix + $"Cards and JSON are in sync ({reason}); content is valid.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                errors.Add(exception.Message);
            }
            finally
            {
                _running = false;
            }
            return errors;
        }

        /// <summary>Loads the JSON packs with JsonUtility (as the game does) and builds them like the server.</summary>
        public static List<string> Validate()
        {
            var errors = new List<string>();
            ContentFiles files = LoadFiles(errors);
            ServerContentFactory.Build(files, errors);
            return errors;
        }

        public static ContentFiles LoadFiles(List<string> errors) => ContentPackLoader.Load(new FileContentSource(JsonRoot), Parse, errors);

        public static object Parse(string json, Type type) => JsonUtility.FromJson(json, type);

        /// <summary>Packs listed in packs.json (the list itself is plain JSON, edited by Game → Content → New pack).</summary>
        public static List<string> PackIds()
        {
            var result = new List<string>();
            string text = new FileContentSource(JsonRoot).ReadText(ContentPacks.PackList);
            if (text == null)
            {
                return result;
            }
            foreach (string pack in JsonUtility.FromJson<PackListDto>(text)?.Packs ?? new string[0])
            {
                if (!string.IsNullOrEmpty(pack) && !result.Contains(pack))
                {
                    result.Add(pack);
                }
            }
            return result;
        }

        public static string CardFolder(string pack, ContentSyncTarget target) =>
            target.IsRecords ? $"{CardsRoot}/{pack}/{target.Name}" : $"{CardsRoot}/{pack}";

        /// <summary>The cards of one file in one pack, in file order.</summary>
        public static List<ContentCard> FindCards(string pack, ContentSyncTarget target)
        {
            var cards = new List<ContentCard>();
            string folder = CardFolder(pack, target);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return cards;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:" + target.CardType.Name, new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Single-file cards live directly in the pack folder; records in their own subfolder.
                if (!target.IsRecords && Path.GetFileNameWithoutExtension(path) != target.Name)
                {
                    continue;
                }
                if (AssetDatabase.LoadAssetAtPath(path, target.CardType) is ContentCard card)
                {
                    cards.Add(card);
                }
            }
            cards.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.name, b.name));
            return cards;
        }

        /// <summary>Creates folders under Assets one level at a time (AssetDatabase needs each to exist).</summary>
        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>The JSON a set of cards stands for (JsonUtility, pretty, "\n" line ends).</summary>
        public static string ExportText(ContentSyncTarget target, List<ContentCard> cards)
        {
            if (cards.Count == 0)
            {
                return null;
            }
            object file;
            if (target.IsRecords)
            {
                var records = new object[cards.Count];
                for (int i = 0; i < records.Length; i++)
                {
                    records[i] = cards[i].Record;
                }
                file = target.Spec.WithRecords(records);
            }
            else
            {
                file = cards[0].Record;
            }
            return Normalize(JsonUtility.ToJson(file, true));
        }

        private static string Canonical(ContentSyncTarget target, string text) =>
            text == null ? null : Normalize(JsonUtility.ToJson(JsonUtility.FromJson(text, target.FileType), true));

        private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\n') + "\n";

        private static string Hash(string text)
        {
            if (text == null)
            {
                return null;
            }
            var hash = new ContentHash();
            hash.Add(string.Empty, text);
            return hash.Finish();
        }

        private static void SyncFile(string pack, ContentSyncTarget target, SyncState state, ref bool jsonWritten, ref bool cardsChanged)
        {
            string key = ContentPacks.PathOf(pack, target.Name);
            string jsonPath = $"{JsonRoot}/{key}{FileContentSource.Extension}";
            string jsonText = File.Exists(jsonPath) ? File.ReadAllText(jsonPath, Encoding.UTF8) : null;
            List<ContentCard> cards = FindCards(pack, target);
            string cardsText = ExportText(target, cards);
            string jsonCanonical = Canonical(target, jsonText);
            string jsonHash = Hash(jsonCanonical);
            string cardsHash = Hash(cardsText);
            switch (ContentSyncRule.Decide(jsonHash, cardsHash, state.Get(key)))
            {
                case ContentSyncAction.Nothing:
                    return;
                case ContentSyncAction.InSync:
                    state.Set(key, jsonHash);
                    return;
                case ContentSyncAction.Export:
                    Directory.CreateDirectory(Path.GetDirectoryName(jsonPath));
                    File.WriteAllText(jsonPath, cardsText, new UTF8Encoding(false));
                    state.Set(key, cardsHash);
                    jsonWritten = true;
                    return;
                case ContentSyncAction.Conflict:
                    Directory.CreateDirectory(ConflictFolder);
                    string backup = Path.Combine(ConflictFolder, $"{pack}_{target.Name}_{DateTime.Now:yyyyMMdd-HHmmss}.json");
                    File.WriteAllText(backup, cardsText, new UTF8Encoding(false));
                    Debug.LogWarning(LogPrefix + $"{key}: both the cards and the JSON changed since the last sync. The JSON was taken; "
                        + $"the cards' version is saved in {backup} — compare and redo the lost edits.");
                    break;
            }
            ImportCards(pack, target, JsonUtility.FromJson(jsonText, target.FileType), cards);
            state.Set(key, jsonHash);
            cardsChanged = true;
        }

        /// <summary>JSON → cards: records update their card (found by id) or get a new one; cards missing from the JSON are deleted.</summary>
        private static void ImportCards(string pack, ContentSyncTarget target, object file, List<ContentCard> existing)
        {
            string folder = CardFolder(pack, target);
            EnsureFolder(folder);
            if (!target.IsRecords)
            {
                ContentCard single = existing.Count > 0 ? existing[0] : Create(target, $"{folder}/{target.Name}.asset");
                single.Record = file;
                single.Order = 0;
                EditorUtility.SetDirty(single);
                return;
            }
            var byId = new Dictionary<string, ContentCard>();
            foreach (ContentCard card in existing)
            {
                byId[target.Spec.IdOf(card.Record) ?? string.Empty] = card;
            }
            var seen = new HashSet<string>();
            object[] records = target.Spec.Records(file);
            for (int i = 0; i < records.Length; i++)
            {
                string id = target.Spec.IdOf(records[i]) ?? string.Empty;
                if (!seen.Add(id))
                {
                    Debug.LogError(LogPrefix + $"{pack}/{target.Name}.json has id '{id}' twice: only the first became a card");
                    continue;
                }
                if (!byId.TryGetValue(id, out ContentCard card))
                {
                    card = Create(target, $"{folder}/{AssetName(id)}.asset");
                }
                card.Record = records[i];
                card.Order = i;
                EditorUtility.SetDirty(card);
            }
            foreach (KeyValuePair<string, ContentCard> pair in byId)
            {
                if (!seen.Contains(pair.Key))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(pair.Value));
                }
            }
        }

        public static ContentCard Create(ContentSyncTarget target, string path)
        {
            var card = (ContentCard)ScriptableObject.CreateInstance(target.CardType);
            AssetDatabase.CreateAsset(card, AssetDatabase.GenerateUniqueAssetPath(path));
            return card;
        }

        /// <summary>A file name from an id (ids are lower-case words; anything else becomes '_').</summary>
        public static string AssetName(string id)
        {
            var sb = new StringBuilder();
            foreach (char c in id ?? string.Empty)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            }
            return sb.Length > 0 ? sb.ToString() : "_";
        }

        /// <summary>Adds new ids to content_ids.json (question 83); never removes any.</summary>
        private static bool UpdateLedger()
        {
            var errors = new List<string>();
            ContentFiles files = LoadFiles(errors);
            string path = $"{JsonRoot}/{ContentPacks.IdLedger}{FileContentSource.Extension}";
            string current = File.Exists(path) ? Normalize(File.ReadAllText(path, Encoding.UTF8)) : null;
            string updated = Normalize(JsonUtility.ToJson(ContentIdLedger.Updated(files), true));
            if (current != null && Canonical(updated) == Canonical(current))
            {
                return false;
            }
            File.WriteAllText(path, updated, new UTF8Encoding(false));
            Debug.Log(LogPrefix + $"New ids added to {ContentPacks.IdLedger}.json: {string.Join(", ", ContentIdLedger.Unlisted(files))}");
            return true;
        }

        private static string Canonical(string ledgerText) => Normalize(JsonUtility.ToJson(JsonUtility.FromJson<ContentIdLedgerDto>(ledgerText), true));

        private static void OnUpdate()
        {
            if (_dueAt > 0 && EditorApplication.timeSinceStartup >= _dueAt && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Run("change");
            }
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                switch (stream.GetEventType(i))
                {
                    // By the asset's path (GUID), not by instance id: the int instance-id APIs are being retired in Unity 6.3+.
                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        stream.GetChangeAssetObjectPropertiesEvent(i, out ChangeAssetObjectPropertiesEventArgs change);
                        ScheduleIfCard(AssetDatabase.GUIDToAssetPath(change.guid));
                        break;
                    case ObjectChangeKind.CreateAssetObject:
                        stream.GetCreateAssetObjectEvent(i, out CreateAssetObjectEventArgs created);
                        ScheduleIfCard(AssetDatabase.GUIDToAssetPath(created.guid));
                        break;
                    case ObjectChangeKind.DestroyAssetObject:
                        Schedule();
                        break;
                }
            }
        }

        private static void ScheduleIfCard(string path)
        {
            if (!string.IsNullOrEmpty(path) && path.StartsWith(CardsRoot + "/", StringComparison.Ordinal))
            {
                Schedule();
            }
        }

        /// <summary>JSON pulled from git or cards deleted in the Project window: sync shortly.</summary>
        private sealed class ContentAssetWatcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (_running)
                {
                    return;
                }
                if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))
                {
                    Schedule();
                }
            }

            private static bool Touches(string[] paths)
            {
                foreach (string path in paths)
                {
                    if ((path.StartsWith(JsonRoot, StringComparison.Ordinal) && path.EndsWith(FileContentSource.Extension, StringComparison.Ordinal))
                        || (path.StartsWith(CardsRoot, StringComparison.Ordinal) && !path.EndsWith(".json", StringComparison.Ordinal)))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>Last synced hash per pack file (committed with the cards, so every computer knows what was synced).</summary>
        [Serializable]
        private sealed class SyncState
        {
            public SyncEntry[] Entries = new SyncEntry[0];

            [NonSerialized]
            private Dictionary<string, string> _map;

            public static SyncState Load(string path)
            {
                SyncState state = File.Exists(path) ? JsonUtility.FromJson<SyncState>(File.ReadAllText(path, Encoding.UTF8)) : null;
                state = state ?? new SyncState();
                state._map = new Dictionary<string, string>();
                foreach (SyncEntry entry in state.Entries ?? new SyncEntry[0])
                {
                    state._map[entry.Path] = entry.Hash;
                }
                return state;
            }

            public string Get(string key) => _map.TryGetValue(key, out string hash) ? hash : null;

            public void Set(string key, string hash) => _map[key] = hash;

            public void Save(string path)
            {
                var keys = new List<string>(_map.Keys);
                keys.Sort(StringComparer.Ordinal);
                var entries = new List<SyncEntry>();
                foreach (string key in keys)
                {
                    entries.Add(new SyncEntry { Path = key, Hash = _map[key] });
                }
                Entries = entries.ToArray();
                string text = Normalize(JsonUtility.ToJson(this, true));
                if (File.Exists(path) && Normalize(File.ReadAllText(path, Encoding.UTF8)) == text)
                {
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, text, new UTF8Encoding(false));
            }
        }

        [Serializable]
        private sealed class SyncEntry
        {
            public string Path;
            public string Hash;
        }

        /// <summary>Menu labels and the "manual" reason (always reports the result).</summary>
        public static class Menu
        {
            public const string Root = "Game/Content/";
            public const string Manual = "manual";
        }

        [MenuItem(Menu.Root + "Sync cards and JSON", priority = 0)]
        private static void SyncNow() => Run(Menu.Manual);

        [MenuItem(Menu.Root + "Check content", priority = 1)]
        private static void CheckNow()
        {
            List<string> errors = Validate();
            foreach (string error in errors)
            {
                Debug.LogError(LogPrefix + error);
            }
            if (errors.Count == 0)
            {
                Debug.Log(LogPrefix + "Content is valid: " + string.Join(", ", PackIds()));
            }
        }
    }
}
