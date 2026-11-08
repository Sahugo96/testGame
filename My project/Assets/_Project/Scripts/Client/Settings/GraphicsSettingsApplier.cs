using System;
using System.Collections.Generic;
using Game.Shared.Content.Dto;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Client.Settings
{
    /// <summary>
    /// Applies graphics settings at once (02, 7.6): the preset's quality level (by name) plus its URP values — set on a
    /// runtime copy of that level's pipeline asset, so the project asset never changes — then resolution and window mode,
    /// VSync and the FPS limit. The original pipeline assets are put back when the application quits.
    /// </summary>
    public static class GraphicsSettingsApplier
    {
        public static readonly FullScreenMode[] WindowModes = { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.Windowed };

        private static readonly Dictionary<int, RenderPipelineAsset> Originals = new Dictionary<int, RenderPipelineAsset>();
        private static readonly Dictionary<int, UniversalRenderPipelineAsset> Copies = new Dictionary<int, UniversalRenderPipelineAsset>();
        private static bool _restoreHooked;
        /// <summary>Quality values before our first change, per level (restored on quit; see RememberLevel).</summary>
        private static readonly Dictionary<int, (float lodBias, int vSync)> Touched = new Dictionary<int, (float, int)>();
        private static int _initialLevel = -1;

        /// <summary>First start: the highest preset the video memory allows, the current screen resolution, borderless, VSync.</summary>
        public static LocalSettings Detect(GameConfig config)
        {
            GraphicsPresetDto chosen = null;
            foreach (GraphicsPresetDto preset in config.GraphicsPresets ?? new GraphicsPresetDto[0])
            {
                if (SystemInfo.graphicsMemorySize >= preset.MinVideoMemoryMb && (chosen == null || preset.MinVideoMemoryMb >= chosen.MinVideoMemoryMb))
                {
                    chosen = preset;
                }
            }
            return new LocalSettings
            {
                SchemaVersion = LocalSettingsStore.Current,
                PresetId = chosen?.Id,
                Width = Screen.currentResolution.width,
                Height = Screen.currentResolution.height,
                WindowMode = FullScreenMode.FullScreenWindow.ToString(),
                VSync = true,
                FpsLimit = 0,
                ShowFps = false,
            };
        }

        public static GraphicsPresetDto Preset(GameConfig config, string id) =>
            Array.Find(config.GraphicsPresets ?? new GraphicsPresetDto[0], p => p.Id == id) ?? (config.GraphicsPresets != null && config.GraphicsPresets.Length > 0 ? config.GraphicsPresets[0] : null);

        public static FullScreenMode ModeOf(LocalSettings settings) =>
            Enum.TryParse(settings.WindowMode, out FullScreenMode mode) ? mode : FullScreenMode.FullScreenWindow;

        /// <summary>Distinct sizes the monitor offers, smallest first.</summary>
        public static List<Vector2Int> Resolutions()
        {
            var sizes = new List<Vector2Int>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!sizes.Contains(size))
                {
                    sizes.Add(size);
                }
            }
            if (sizes.Count == 0)
            {
                sizes.Add(new Vector2Int(Screen.width, Screen.height));
            }
            sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return sizes;
        }

        public static void Apply(LocalSettings settings, GameConfig config)
        {
            if (_initialLevel < 0)
            {
                _initialLevel = QualitySettings.GetQualityLevel();
            }
            GraphicsPresetDto preset = Preset(config, settings.PresetId);
            if (preset != null)
            {
                int level = Array.IndexOf(QualitySettings.names, preset.QualityLevel);
                if (level >= 0 && level != QualitySettings.GetQualityLevel())
                {
                    QualitySettings.SetQualityLevel(level, true);
                }
                RememberLevel();
                QualitySettings.lodBias = preset.LodBias;
                ApplyPipeline(preset);
            }
            RememberLevel();
            FullScreenMode mode = ModeOf(settings);
            if (settings.Width > 0 && settings.Height > 0 && (Screen.width != settings.Width || Screen.height != settings.Height || Screen.fullScreenMode != mode))
            {
                Screen.SetResolution(settings.Width, settings.Height, mode);
            }
            QualitySettings.vSyncCount = settings.VSync ? 1 : 0;
            Application.targetFrameRate = settings.FpsLimit > 0 ? settings.FpsLimit : -1;
        }

        /// <summary>URP values of the preset on a copy of the current level's pipeline asset (the level's own asset or the default).</summary>
        private static void ApplyPipeline(GraphicsPresetDto preset)
        {
            int level = QualitySettings.GetQualityLevel();
            if (!Copies.TryGetValue(level, out UniversalRenderPipelineAsset copy))
            {
                RenderPipelineAsset original = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
                if (!(original is UniversalRenderPipelineAsset urp))
                {
                    return;
                }
                Originals[level] = QualitySettings.renderPipeline;
                copy = UnityEngine.Object.Instantiate(urp);
                Copies[level] = copy;
                HookRestore();
            }
            copy.renderScale = preset.RenderScale;
            copy.shadowDistance = preset.ShadowDistance;
            copy.msaaSampleCount = preset.Msaa;
            QualitySettings.renderPipeline = copy;
        }

        /// <summary>
        /// In the editor, QualitySettings changed in Play mode stay in ProjectSettings/QualitySettings.asset: the values of each
        /// level we touch are remembered once and put back on quit (with the level the editor had), so the project file is unchanged.
        /// </summary>
        private static void RememberLevel()
        {
            int level = QualitySettings.GetQualityLevel();
            if (!Touched.ContainsKey(level))
            {
                Touched[level] = (QualitySettings.lodBias, QualitySettings.vSyncCount);
                HookRestore();
            }
        }

        private static void HookRestore()
        {
            if (_restoreHooked)
            {
                return;
            }
            _restoreHooked = true;
            Application.quitting += () =>
            {
                int current = QualitySettings.GetQualityLevel();
                foreach (KeyValuePair<int, RenderPipelineAsset> original in Originals)
                {
                    QualitySettings.SetQualityLevel(original.Key, false);
                    QualitySettings.renderPipeline = original.Value;
                }
                foreach (KeyValuePair<int, (float lodBias, int vSync)> touched in Touched)
                {
                    QualitySettings.SetQualityLevel(touched.Key, false);
                    QualitySettings.lodBias = touched.Value.lodBias;
                    QualitySettings.vSyncCount = touched.Value.vSync;
                }
                QualitySettings.SetQualityLevel(Application.isEditor && _initialLevel >= 0 ? _initialLevel : current, false);
            };
        }
    }
}
