using System;
using System.Collections.Generic;
using Game.Client.Input;
using Game.Client.Localization;
using Game.Shared.Content.Dto;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Square window buttons to the left of the skill panel (developer reference: character, backpack, talents, map).
    /// Built from the window registry entries of one menu zone; the button shows the window's picture (a short
    /// localized name when there is no art), the tooltip shows the full name and hotkey.
    /// </summary>
    public sealed class IconMenuWidget
    {
        private const string ShortSuffix = ".short";

        public IconMenuWidget(RectTransform zone, ClientContext ctx, Tooltip tooltip, string menuZone, float rightEdgeX, Action<string> toggleWindow)
        {
            var windows = new List<HudWindowDto>();
            foreach (HudWindowDto window in ctx.Content.HudWindows)
            {
                if (window.MenuZone == menuZone)
                {
                    windows.Add(window);
                }
            }
            windows.Sort((a, b) => b.MenuOrder.CompareTo(a.MenuOrder));
            float step = HudLayout.SkillSlotSize + HudLayout.Gap;
            for (int i = 0; i < windows.Count; i++)
            {
                HudWindowDto window = windows[i];
                var slot = new SlotView($"WindowButton_{window.Id}", zone, ctx.Font, HudLayout.SkillSlotSize);
                UiFactory.Place(slot.Rect, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(rightEdgeX - i * step, 0f),
                    new Vector2(HudLayout.SkillSlotSize, HudLayout.SkillSlotSize));
                slot.SetFilled(true);
                slot.SetCaption(ctx.Text.Get(window.NameKey + ShortSuffix), HudPalette.Text);
                slot.SetIcon(ctx.Icons.Window(window.Icon));
                slot.SetHotkey(GameInput.WindowKey(ctx, window));
                if (ctx.Settings != null)
                {
                    ctx.Settings.Keys.KeysChanged += () => slot.SetHotkey(GameInput.WindowKey(ctx, window));
                }
                string windowId = window.Id;
                slot.Bind(() =>
                {
                    string hotkey = GameInput.WindowKey(ctx, window);
                    tooltip.Show(ctx.Text.Get(window.NameKey) + (string.IsNullOrEmpty(hotkey) ? string.Empty : "\n" + ctx.Text.Format(UiKeys.Hotkey, hotkey)));
                }, tooltip.Hide, button =>
                {
                    if (button == PointerEventData.InputButton.Left)
                    {
                        toggleWindow(windowId);
                    }
                });
            }
        }
    }
}
