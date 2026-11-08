using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Packs;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Content
{
    /// <summary>
    /// A new content pack (question 82): Resources/Content/&lt;id&gt;/pack.json (depends on base) and the pack added to the end
    /// of packs.json. Records are then made with the wizards choosing this pack.
    /// </summary>
    public sealed class ContentPackWindow : EditorWindow
    {
        private static readonly Regex IdPattern = new Regex("^[a-z][a-z0-9_]*$");

        private string _id = string.Empty;
        private string _message;

        [MenuItem(ContentSync.Menu.Root + "New pack…", priority = 30)]
        private static void Open() => GetWindow<ContentPackWindow>(true, "New content pack");

        private void OnGUI()
        {
            _id = EditorGUILayout.TextField("Pack id (a-z, 0-9, _)", _id).Trim();
            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, MessageType.Warning);
            }
            if (GUILayout.Button("Create"))
            {
                _message = Create(_id);
                if (_message == null)
                {
                    Close();
                }
            }
        }

        private static string Create(string id)
        {
            List<string> packs = ContentSync.PackIds();
            if (!IdPattern.IsMatch(id))
            {
                return "The id must start with a letter and contain only a-z, 0-9 and _.";
            }
            if (packs.Contains(id))
            {
                return $"Pack '{id}' already exists.";
            }
            string folder = $"{ContentSync.JsonRoot}/{id}";
            Directory.CreateDirectory(folder);
            var manifest = new PackManifestDto { Id = id, Version = "1.0.0", NameKey = $"pack.{id}.name", Dependencies = new[] { ContentPacks.Base } };
            File.WriteAllText($"{folder}/{ContentPacks.Manifest}{FileContentSource.Extension}", JsonUtility.ToJson(manifest, true) + "\n", new UTF8Encoding(false));
            packs.Add(id);
            File.WriteAllText($"{ContentSync.JsonRoot}/{ContentPacks.PackList}{FileContentSource.Extension}",
                JsonUtility.ToJson(new PackListDto { Packs = packs.ToArray() }, true) + "\n", new UTF8Encoding(false));
            AssetDatabase.Refresh();
            ContentSync.Run(ContentSync.Menu.Manual);
            Debug.Log($"[Content] Pack '{id}' created and added to packs.json. Make records with Game → Content → New … choosing this pack.");
            return null;
        }
    }
}
