using System;
using System.IO;
using Game.Shared.Core;
using UnityEngine;

namespace Game.Client.Settings
{
    /// <summary>Reads and writes <see cref="LocalSettings"/> in Application.persistentDataPath (temporarily, DEFAULT: a JSON file, not PlayerPrefs).</summary>
    public sealed class LocalSettingsStore
    {
        public const int Current = 1;
        private const string FileName = "settings_local.json";

        private string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Null on first start or when the file is unreadable or from a newer version.</summary>
        public LocalSettings Load()
        {
            try
            {
                if (!File.Exists(Path))
                {
                    return null;
                }
                LocalSettings settings = JsonUtility.FromJson<LocalSettings>(File.ReadAllText(Path));
                return settings != null && settings.SchemaVersion > 0 && settings.SchemaVersion <= Current ? settings : null;
            }
            catch (Exception exception)
            {
                Log.Warning(LogTags.Client, $"Local settings not read: {exception.Message}");
                return null;
            }
        }

        public void Save(LocalSettings settings)
        {
            try
            {
                settings.SchemaVersion = Current;
                File.WriteAllText(Path, JsonUtility.ToJson(settings, true));
            }
            catch (Exception exception)
            {
                Log.Warning(LogTags.Client, $"Local settings not saved: {exception.Message}");
            }
        }
    }
}
