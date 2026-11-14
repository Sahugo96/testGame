using System.Collections.Generic;
using System.IO;
using Game.Shared.Content;
using Game.Shared.Content.Packs;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#else
using System;
#endif

namespace Game.Tests
{
    /// <summary>
    /// Loads the real content packs: with JsonUtility inside Unity, with System.Text.Json made JsonUtility-like in the .NET
    /// harness (sim-tests). Loading errors fail the caller's build of the content like any content error.
    /// </summary>
    public static class TestContentLoader
    {
#if UNITY_5_3_OR_NEWER
        public static string Root => Path.Combine(Application.dataPath, "_Project", "Resources", ContentPacks.Folder);

        /// <summary>Test data packs (Tests/EditMode/Fixtures).</summary>
        public static string FixturesRoot => Path.Combine(Application.dataPath, "_Project", "Tests", "EditMode", "Fixtures");

        /// <summary>Pictures of items, skills, statuses and window buttons (Resources/Icons).</summary>
        public static string IconsRoot => Path.Combine(Application.dataPath, "_Project", "Resources", "Icons");

        public static object Parse(string json, System.Type type) => JsonUtility.FromJson(json, type);
#else
        public static string Root => Path.Combine(AppContext.BaseDirectory, ContentPacks.Folder);

        /// <summary>Test data packs (copied next to the test assembly by sim-tests).</summary>
        public static string FixturesRoot => Path.Combine(AppContext.BaseDirectory, "Fixtures");

        /// <summary>Pictures (copied next to the test assembly by sim-tests).</summary>
        public static string IconsRoot => Path.Combine(AppContext.BaseDirectory, "Icons");

        public static object Parse(string json, Type type) => JsonUtilityLike.Parse(json, type);
#endif

        /// <summary>The packs; a broken layout (missing file, pack redefining a record) throws with the errors.</summary>
        public static ContentFiles LoadFiles()
        {
            var errors = new List<string>();
            ContentFiles files = LoadFiles(errors);
            if (errors.Count > 0)
            {
                throw new System.InvalidOperationException(string.Join("\n", errors));
            }
            return files;
        }

        public static ContentFiles LoadFiles(List<string> errors) => ContentPackLoader.Load(new FileContentSource(Root), Parse, errors);
    }
}
