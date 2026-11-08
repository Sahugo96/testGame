using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Creates Assets/_Project/Resources/Materials/WorldLit.mat (URP Lit) when the editor loads, if it is missing. The world's
    /// placeholder shapes use it (Game.Client.World.WorldPrimitives): a player build has no URP default material for
    /// CreatePrimitive. Being in Resources, the material and its shader always get into the build.
    /// </summary>
    [InitializeOnLoad]
    public static class WorldMaterialAsset
    {
        private const string Folder = "Assets/_Project/Resources/Materials";
        private const string AssetPath = Folder + "/WorldLit.mat";
        private const string ShaderName = "Universal Render Pipeline/Lit";

        static WorldMaterialAsset() => EditorApplication.delayCall += () => Ensure();

        /// <summary>True if the material exists (or was just created).</summary>
        public static bool Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(AssetPath) != null)
            {
                return true;
            }
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[Build] Shader '{ShaderName}' not found: is the URP package installed?");
                return false;
            }
            Directory.CreateDirectory(Folder);
            AssetDatabase.CreateAsset(new Material(shader) { name = "WorldLit" }, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Build] Created {AssetPath} (URP Lit material for the world's shapes). Commit it to git.");
            return true;
        }
    }
}
