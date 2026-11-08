#if GAME_ADDRESSABLES
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.EditorTools.Content
{
    /// <summary>
    /// Addressables (only for the Localization tables, module 4) creates groups with bundle names that end in a long hash;
    /// Addressables 2.x warns that such paths may be too long on Windows. The groups and the "Packed Assets" template get
    /// NoHash names (after Set up and before a build); bundles are rebuilt with every player build anyway.
    /// </summary>
    public static class AddressablesNaming
    {
        public static void UseShortNames()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return;
            }
            int changed = 0;
            foreach (AddressableAssetGroup group in settings.groups)
            {
                changed += Fix(group != null ? group.GetSchema<BundledAssetGroupSchema>() : null);
            }
            foreach (ScriptableObject template in settings.GroupTemplateObjects)
            {
                if (template is AddressableAssetGroupTemplate groupTemplate)
                {
                    changed += Fix(groupTemplate.GetSchemaByType(typeof(BundledAssetGroupSchema)) as BundledAssetGroupSchema);
                }
            }
            if (changed > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[Localization] Addressables: short bundle names (NoHash) in {changed} groups/templates.");
            }
        }

        private static int Fix(BundledAssetGroupSchema schema)
        {
            if (schema == null || (!schema.UseDefaultSchemaSettings && schema.BundleNaming == BundledAssetGroupSchema.BundleNamingStyle.NoHash))
            {
                return 0;
            }
            schema.UseDefaultSchemaSettings = false;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
            EditorUtility.SetDirty(schema);
            return 1;
        }
    }
}
#endif
