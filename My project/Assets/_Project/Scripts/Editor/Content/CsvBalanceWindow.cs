using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Shared.Content.Csv;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Content
{
    /// <summary>
    /// Balance tables (module 4, question 84): one content type of one pack ↔ a CSV for Excel or Google Sheets. Export writes
    /// UTF-8 with BOM (Excel shows Cyrillic) with ';' and decimal commas; import changes the cards (only existing records, all
    /// or nothing) and the sync writes the JSON.
    /// </summary>
    public sealed class CsvBalanceWindow : EditorWindow
    {
        private int _pack;
        private int _file;
        private string _message;
        private MessageType _messageType;

        [MenuItem(ContentSync.Menu.Root + "Balance tables (CSV)…", priority = 40)]
        private static void Open() => GetWindow<CsvBalanceWindow>(true, "Balance tables (CSV)");

        private void OnGUI()
        {
            List<string> packs = ContentSync.PackIds();
            var targets = new List<ContentSyncTarget>();
            foreach (ContentSyncTarget target in ContentSyncTarget.All)
            {
                if (target.IsRecords)
                {
                    targets.Add(target);
                }
            }
            if (packs.Count == 0)
            {
                EditorGUILayout.HelpBox("No packs.", MessageType.Error);
                return;
            }
            _pack = EditorGUILayout.Popup("Pack", Mathf.Clamp(_pack, 0, packs.Count - 1), packs.ToArray());
            _file = EditorGUILayout.Popup("Table", Mathf.Clamp(_file, 0, targets.Count - 1), targets.ConvertAll(t => t.Name).ToArray());
            ContentSyncTarget chosen = targets[_file];
            string pack = packs[_pack];
            EditorGUILayout.HelpBox("Columns: simple fields and one level of nested fields (\"Field.Sub\"). Lists (components, drops, variants) are edited in the cards. Rows change existing records only.", MessageType.None);
            if (GUILayout.Button("Export to CSV…"))
            {
                Export(pack, chosen);
            }
            if (GUILayout.Button("Import from CSV…"))
            {
                Import(pack, chosen);
            }
            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, _messageType);
            }
        }

        private void Export(string pack, ContentSyncTarget target)
        {
            ContentSync.Run("csv export");
            string path = EditorUtility.SaveFilePanel("Export balance table", string.Empty, $"{pack}_{target.Name}.csv", "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            List<ContentCard> cards = ContentSync.FindCards(pack, target);
            string csv = CsvBalanceMapper.Export(target.Spec.RecordType, cards.ConvertAll(c => c.Record));
            File.WriteAllText(path, csv, new UTF8Encoding(true));
            Show($"Exported {cards.Count} rows to {path}", MessageType.Info);
        }

        private void Import(string pack, ContentSyncTarget target)
        {
            string path = EditorUtility.OpenFilePanel("Import balance table", string.Empty, "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            List<ContentCard> cards = ContentSync.FindCards(pack, target);
            var records = cards.ConvertAll(c => c.Record);
            var errors = new List<string>();
            Undo.RecordObjects(cards.ToArray(), "Import balance table");
            List<object> changed = CsvBalanceMapper.Import(target.Spec.RecordType, File.ReadAllText(path, Encoding.UTF8), records, errors);
            if (errors.Count > 0)
            {
                Show("Nothing changed:\n" + string.Join("\n", errors), MessageType.Error);
                return;
            }
            foreach (ContentCard card in cards)
            {
                if (changed.Contains(card.Record))
                {
                    EditorUtility.SetDirty(card);
                }
            }
            AssetDatabase.SaveAssets();
            List<string> contentErrors = ContentSync.Run(ContentSync.Menu.Manual);
            Show(contentErrors.Count == 0
                ? $"Changed {changed.Count} records; content is valid."
                : $"Changed {changed.Count} records, but the content now has errors (see the Console):\n" + string.Join("\n", contentErrors), contentErrors.Count == 0 ? MessageType.Info : MessageType.Warning);
        }

        private void Show(string message, MessageType type)
        {
            _message = message;
            _messageType = type;
            Repaint();
        }
    }
}
