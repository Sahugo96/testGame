using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>
    /// Building blocks of the world's look: primitive shapes with URP materials. In a player build URP gives
    /// CreatePrimitive no default material (UniversalRenderPipelineAsset.defaultMaterial is editor-only), so every shape
    /// would be magenta: the Lit and Unlit materials come from Resources (created by the editor,
    /// Game.EditorTools.WorldMaterialAsset). Coloured copies are cached per colour and shared by all shapes, so a
    /// thousand parts of the same colour use one material and nothing leaks when a figure is destroyed.
    /// </summary>
    public static class WorldPrimitives
    {
        /// <summary>Resources paths of the materials (without extension); the editor creates them on load.</summary>
        public const string LitMaterialPath = "Materials/WorldLit";
        public const string UnlitMaterialPath = "Materials/WorldUnlit";

        private const float Smoothness = 0.12f;
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        private static readonly Dictionary<Color32, Material> LitByColor = new Dictionary<Color32, Material>();
        private static readonly Dictionary<Color32, Material> GlowByColor = new Dictionary<Color32, Material>();
        private static readonly Dictionary<PrimitiveType, Mesh> Meshes = new Dictionary<PrimitiveType, Mesh>();
        private static Material _lit;
        private static Material _unlit;
        /// <summary>The material Unity itself gives a primitive: the last resort when ours are missing (works in the editor).</summary>
        private static Material _default;
        private static bool _warned;

        /// <summary>A primitive with its collider and the shared Lit material (ground, click targets).</summary>
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

        /// <summary>A part of a figure: mesh and material only, no collider (a figure has one collider on its root).</summary>
        public static Transform Part(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 euler, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = MeshOf(type);
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            return go.transform;
        }

        /// <summary>A lit material with a repeating texture (the ground); owned by the caller.</summary>
        public static Material Textured(Texture2D texture, float repeats, Color tint)
        {
            Material source = Lit();
            if (source == null)
            {
                return null;
            }
            var material = new Material(source) { color = tint, mainTexture = texture, mainTextureScale = new Vector2(repeats, repeats) };
            Matte(material);
            return material;
        }

        /// <summary>The shared lit material of a colour.</summary>
        public static Material Colored(Color color) => Cached(LitByColor, Lit(), color);

        /// <summary>The shared material of a colour that ignores light: reads as glowing (eyes, sparks, markers).</summary>
        public static Material Glow(Color color) => Cached(GlowByColor, Unlit() ?? Lit(), color);

        private static Material Cached(Dictionary<Color32, Material> cache, Material source, Color color)
        {
            Color32 key = color;
            if (!cache.TryGetValue(key, out Material material) || material == null)
            {
                source = source != null ? source : _default;
                if (source == null)
                {
                    return null;
                }
                material = new Material(source) { color = color };
                Matte(material);
                cache[key] = material;
            }
            return material;
        }

        /// <summary>URP Lit is glossy by default, which makes everything look like plastic: turn the gloss down.</summary>
        private static void Matte(Material material)
        {
            if (material.HasProperty(SmoothnessId))
            {
                material.SetFloat(SmoothnessId, Smoothness);
            }
        }

        private static Mesh MeshOf(PrimitiveType type)
        {
            if (!Meshes.TryGetValue(type, out Mesh mesh) || mesh == null)
            {
                // The built-in meshes are reachable only through a primitive: take the mesh once and drop the object.
                GameObject sample = GameObject.CreatePrimitive(type);
                mesh = sample.GetComponent<MeshFilter>().sharedMesh;
                if (_default == null)
                {
                    _default = sample.GetComponent<Renderer>().sharedMaterial;
                }
                sample.SetActive(false);   // Destroy acts at the end of the frame: the sample must not be seen or clicked
                Object.Destroy(sample);
                Meshes[type] = mesh;
            }
            return mesh;
        }

        private static Material Lit()
        {
            if (_lit == null)
            {
                _lit = Load(LitMaterialPath);
            }
            return _lit;
        }

        private static Material Unlit()
        {
            if (_unlit == null)
            {
                _unlit = Load(UnlitMaterialPath);
            }
            return _unlit;
        }

        private static Material Load(string path)
        {
            Material material = Resources.Load<Material>(path);
            if (material == null && !_warned)
            {
                _warned = true;
                Debug.LogWarning($"[World] Resources/{path}.mat is missing: shapes may be magenta in a build. Reopen the project in the editor to create it.");
            }
            return material;
        }
    }
}
