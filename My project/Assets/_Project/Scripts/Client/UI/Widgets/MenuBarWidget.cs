using System;
using System.Collections.Generic;
using Game.Client.Input;
using Game.Shared.Content.Dto;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>Menu of mechanics (02, 7.13): buttons are built from the window registry data for one menu zone.</summary>
    public sealed class MenuBarWidget
    {
        public MenuBarWidget(RectTransform zone, ClientContext ctx, string menuZone, bool alignRight, Action<string> toggleWindow)
        {
            var windows = new List<HudWindowDto>();
            foreach (HudWindowDto window in ctx.Content.HudWindows)
            {
                if (window.MenuZone == menuZone)
                {
                    windows.Add(window);
                }
            }
            windows.Sort((a, b) => a.MenuOrder.CompareTo(b.MenuOrder));
            float step = HudLayout.MenuButtonWidth + HudLayout.Gap;
            for (int i = 0; i < windows.Count; i++)
            {
                HudWindowDto window = windows[i];
                var button = UiFactory.Button($"Menu_{window.Id}", zone, ctx.Font, Caption(ctx, window), () => toggleWindow(window.Id));
                Text label = button.GetComponentInChildren<Text>();
                if (ctx.Settings != null)
                {
                    ctx.Settings.Keys.KeysChanged += () => label.text = Caption(ctx, window);
                }
                var rect = (RectTransform)button.transform;
                float x = alignRight ? -i * step : i * step;
                Vector2 anchor = alignRight ? new Vector2(1f, 0f) : Vector2.zero;
                UiFactory.Place(rect, anchor, anchor, new Vector2(x, 0f), new Vector2(HudLayout.MenuButtonWidth, HudLayout.MenuButtonHeight));
            }
        }

        /// <summary>"Name (key)"; the key follows rebinding (module 17).</summary>
        private static string Caption(ClientContext ctx, HudWindowDto window)
        {
            string hotkey = GameInput.WindowKey(ctx, window);
            return ctx.Text.Get(window.NameKey) + (string.IsNullOrEmpty(hotkey) ? string.Empty : $" ({hotkey})");
        }
    }
}
