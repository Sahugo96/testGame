using System;
using System.Collections.Generic;
using System.IO;
using Game.Shared.Content;
using Game.Shared.Content.Packs;
using Game.Tests;

namespace Game.LoadTest
{
    /// <summary>Reads the game's content packs next to the executable (System.Text.Json made JsonUtility-like, as the game reads them).</summary>
    public static class JsonContentLoader
    {
        public static ContentFiles Load(List<string> errors) =>
            ContentPackLoader.Load(new FileContentSource(Path.Combine(AppContext.BaseDirectory, ContentPacks.Folder)), JsonUtilityLike.Parse, errors);
    }
}
