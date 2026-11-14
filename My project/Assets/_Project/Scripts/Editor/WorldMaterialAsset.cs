using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Creates the materials of the world's shapes in Assets/_Project/Resources/Materials when the editor loads, if they
    /// are missing: WorldLit.mat (URP Lit) and WorldUnlit.mat (URP Unlit, for parts that glow). The shapes use them
    /// through Game.Client.World.WorldPrimitives: a player build has no URP default material for CreatePrimitive. Being
    /// in Resources, the materials and their shaders always get into the build.
    /// </summary>
    [InitializeOnLoad]
    public static class WorldMaterialAsset
    {
        private const string Folder = "Assets/_Project/Resources/Materials";
        private const string LitName = "WorldLit";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitName = "WorldUnlit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";

        static WorldMaterialAsset() => EditorApplication.delayCall += () => Ensure();

        /// <summary>True if both materials exist (or were just created).</summary>
        public static bool Ensure()
        {
            bool lit = Ensure(LitName, LitShader);
            bool unlit = Ensure(UnlitName, UnlitShader);
            return lit && unlit;
        }

        private static bool Ensure(string name, string shaderName)
        {
            string path = Folder + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                return true;
            }
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[Build] Shader '{shaderName}' not found: is the URP package installed?");
                return false;
            }
            Directory.CreateDirectory(Folder);
            AssetDatabase.CreateAsset(new Material(shader) { name = name }, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Build] Created {path} (material for the world's shapes). Commit it to git.");
            return true;
        }
    }
}
