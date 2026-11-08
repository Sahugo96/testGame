using UnityEngine;

namespace Game.Client.World
{
    /// <summary>
    /// Primitives for the placeholder world with a URP material. In a player build URP gives CreatePrimitive no default
    /// material (UniversalRenderPipelineAsset.defaultMaterial is editor-only), so every shape would be magenta: the Lit
    /// material comes from Resources (created by the editor, Game.EditorTools.WorldMaterialAsset) and is shared by all.
    /// </summary>
    public static class WorldPrimitives
    {
        /// <summary>Resources path of the material (without extension); the editor creates it on load.</summary>
        public const string LitMaterialPath = "Materials/WorldLit";

        private static Material _lit;
        private static bool _warned;

        public static GameObject Create(PrimitiveType type)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Material lit = Lit();
            if (lit != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = lit;
            }
            return go;
        }

        private static Material Lit()
        {
            if (_lit == null)
            {
                _lit = Resources.Load<Material>(LitMaterialPath);
                if (_lit == null && !_warned)
                {
                    _warned = true;
                    Debug.LogWarning($"[World] Resources/{LitMaterialPath}.mat is missing: shapes may be magenta in a build. Reopen the project in the editor to create it.");
                }
            }
            return _lit;
        }
    }
}
