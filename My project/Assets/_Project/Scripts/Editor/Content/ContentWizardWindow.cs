using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.Shared.Content;
using Game.Shared.Content.Ids;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Content
{
    /// <summary>
    /// Wizards (module 4, question 84): a new item, mob, skill or class as a copy of an existing one (so every required field
    /// is filled), with a new id, the pack it belongs to, name and description keys and their Russian texts. The new card
    /// opens in the inspector; the sync writes it to JSON and validates. Zone and legendary wizards wait for their modules.
    /// </summary>
    public sealed class ContentWizardWindow : EditorWindow
    {
        public enum Kind
        {
            Item,
            Mob,
            Skill,
            Class,
        }

        /// <summary>Content file and localization key patterns per kind ({0} = id; no description key = none).</summary>
        private static readonly Dictionary<Kind, (string file, string nameKey, string descriptionKey)> Kinds = new Dictionary<Kind, (string, string, string)>
        {
            { Kind.Item, ("items", "item.{0}.name", "item.{0}.desc") },
            { Kind.Mob, ("mobs", "mob.{0}", null) },
            { Kind.Skill, ("skills", "skill.{0}.name", "skill.{0}.desc") },
            { Kind.Class, ("classes", "class.{0}.name", "class.{0}.desc") },
        };

        private static readonly Regex IdPattern = new Regex("^[a-z][a-z0-9_]*$");

        private Kind _kind;
        private int _pack;
        private int _template;
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private string _message;

        [MenuItem(ContentSync.Menu.Root + "New item…", priority = 20)]
        private static void NewItem() => Open(Kind.Item);

        [MenuItem(ContentSync.Menu.Root + "New mob…", priority = 21)]
        private static void NewMob() => Open(Kind.Mob);

        [MenuItem(ContentSync.Menu.Root + "New skill…", priority = 22)]
        private static void NewSkill() => Open(Kind.Skill);

        [MenuItem(ContentSync.Menu.Root + "New class…", priority = 23)]
        private static void NewClass() => Open(Kind.Class);

        private static void Open(Kind kind)
        {
            ContentSync.Run("wizard");
            var window = GetWindow<ContentWizardWindow>(true, "New " + kind.ToString().ToLowerInvariant());
            window._kind = kind;
            window._template = 0;
            window._message = null;
        }

        private void OnGUI()
        {
            List<string> packs = ContentSync.PackIds();
            ContentSyncTarget target = ContentSyncTarget.Find(Kinds[_kind].file);
            List<ContentCard> templates = AllCards(target, packs);
            if (packs.Count == 0 || templates.Count == 0)
            {
                EditorGUILayout.HelpBox("No packs or no existing records of this kind to copy from.", MessageType.Error);
                return;
            }
            _kind = (Kind)EditorGUILayout.EnumPopup("Kind", _kind);
            _pack = EditorGUILayout.Popup("Pack", Mathf.Clamp(_pack, 0, packs.Count - 1), packs.ToArray());
            string[] templateIds = templates.ConvertAll(c => target.Spec.IdOf(c.Record)).ToArray();
            _template = EditorGUILayout.Popup("Copy of", Mathf.Clamp(_template, 0, templateIds.Length - 1), templateIds);
            _id = EditorGUILayout.TextField("Id (a-z, 0-9, _)", _id).Trim();
            _name = EditorGUILayout.TextField("Name (ru)", _name);
            if (Kinds[_kind].descriptionKey != null)
            {
                _description = EditorGUILayout.TextField("Description (ru)", _description);
            }
            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, MessageType.Warning);
            }
            if (GUILayout.Button("Create"))
            {
                _message = Create(packs[_pack], target, templates[_template]);
                if (_message == null)
                {
                    Close();
                }
            }
        }

        private static List<ContentCard> AllCards(ContentSyncTarget target, List<string> packs)
        {
            var cards = new List<ContentCard>();
            foreach (string pack in packs)
            {
                cards.AddRange(ContentSync.FindCards(pack, target));
            }
            return cards;
        }

        /// <summary>Null on success, else what is wrong.</summary>
        private string Create(string pack, ContentSyncTarget target, ContentCard template)
        {
            if (!IdPattern.IsMatch(_id))
            {
                return "The id must start with a letter and contain only a-z, 0-9 and _.";
            }
            var errors = new List<string>();
            ContentFiles files = ContentSync.LoadFiles(errors);
            if (ContentIdLedger.Collect(files).TryGetValue(target.Name, out SortedSet<string> ids) && ids.Contains(_id))
            {
                return $"'{_id}' already exists.";
            }
            if (files.Ids?.Files != null && Array.Exists(files.Ids.Files, f => f.File == target.Name && f.Ids != null && Array.IndexOf(f.Ids, _id) >= 0))
            {
                return $"'{_id}' was used before (content_ids.json): ids are never reused.";
            }
            if (string.IsNullOrWhiteSpace(_name))
            {
                return "Enter the name.";
            }
            object record = JsonUtility.FromJson(JsonUtility.ToJson(template.Record), target.Spec.RecordType);
            (string file, string nameKey, string descriptionKey) kind = Kinds[_kind];
            string nameKey = string.Format(kind.nameKey, _id);
            string descriptionKey = kind.descriptionKey != null ? string.Format(kind.descriptionKey, _id) : null;
            SetField(record, "Id", _id);
            SetField(record, "NameKey", nameKey);
            SetField(record, "DescriptionKey", descriptionKey);
            SetField(record, "Deprecated", false);
            if (_kind == Kind.Item)
            {
                SetField(record, "Icon", _id);
            }

            List<ContentCard> siblings = ContentSync.FindCards(pack, target);
            string folder = ContentSync.CardFolder(pack, target);
            ContentSync.EnsureFolder(folder);
            ContentCard card = ContentSync.Create(target, $"{folder}/{ContentSync.AssetName(_id)}.asset");
            card.Record = record;
            card.Order = siblings.Count > 0 ? siblings[siblings.Count - 1].Order + 1 : 0;
            EditorUtility.SetDirty(card);
            AssetDatabase.SaveAssets();
            LocalizationSource.Set((nameKey, _name), (descriptionKey, _description));
            AssetDatabase.ImportAsset(LocalizationSource.Path);
            ContentSync.Run(ContentSync.Menu.Manual);
            Selection.activeObject = card;
            EditorGUIUtility.PingObject(card);
            Debug.Log($"[Content] Created {target.Name} '{_id}' in pack '{pack}' as a copy of '{target.Spec.IdOf(template.Record)}': edit it in the inspector.");
            return null;
        }

        private static void SetField(object record, string name, object value)
        {
            FieldInfo field = record.GetType().GetField(name);
            if (field != null && (value == null || field.FieldType.IsInstanceOfType(value)))
            {
                field.SetValue(record, value);
            }
        }
    }
}
