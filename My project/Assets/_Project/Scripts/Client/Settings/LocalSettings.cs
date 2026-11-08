using System;

namespace Game.Client.Settings
{
    /// <summary>
    /// Settings kept on this device (02, 7.6; question 11): graphics and diagnostics. A [Serializable] document with a
    /// schema version, saved as JSON next to the other local data of the game.
    /// </summary>
    [Serializable]
    public class LocalSettings
    {
        public int SchemaVersion;
        public string PresetId;
        public int Width;
        public int Height;
        /// <summary>UnityEngine.FullScreenMode name: FullScreenWindow, ExclusiveFullScreen or Windowed.</summary>
        public string WindowMode;
        public bool VSync;
        /// <summary>0 = no limit.</summary>
        public int FpsLimit;
        public bool ShowFps;

        public LocalSettings Copy() => (LocalSettings)MemberwiseClone();
    }
}
