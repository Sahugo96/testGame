using System;
using System.Collections.Generic;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Packs;
using UnityEngine;

namespace Game.Host
{
    /// <summary>Loads content packs from Resources/Content with JsonUtility (module 4: ContentPackLoader decides the layout).</summary>
    public static class UnityContentLoader
    {
        public static ContentFiles LoadAll(List<string> errors) => ContentPackLoader.Load(new ResourcesContentSource(), Parse, errors);

        public static GameConfig LoadConfig() => ContentPackLoader.LoadConfig(new ResourcesContentSource(), Parse);

        public static object Parse(string json, Type type) => JsonUtility.FromJson(json, type);

        /// <summary>Content TextAssets under Resources/Content.</summary>
        private sealed class ResourcesContentSource : IContentSource
        {
            public string ReadText(string path)
            {
                var asset = Resources.Load<TextAsset>(ContentPacks.Folder + "/" + path);
                return asset != null ? asset.text : null;
            }
        }
    }
}
