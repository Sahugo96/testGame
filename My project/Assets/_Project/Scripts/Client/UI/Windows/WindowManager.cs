using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Opens, closes and focuses mechanic windows built from the window registry (02, 7.13). Pressing the button or
    /// hotkey again closes a window; Esc closes the top one. New windows = a registry entry + a builder in <see cref="WindowFactory"/>.
    /// </summary>
    public sealed class WindowManager
    {
        private readonly Dictionary<string, HudWindow> _windows = new Dictionary<string, HudWindow>();
        private readonly List<HudWindow> _openOrder = new List<HudWindow>();
        private readonly RectTransform _layer;
        private readonly Canvas _canvas;
        private readonly ClientContext _ctx;
        private readonly Tooltip _tooltip;

        public WindowManager(RectTransform layer, Canvas canvas, ClientContext ctx, Tooltip tooltip)
        {
            _layer = layer;
            _canvas = canvas;
            _ctx = ctx;
            _tooltip = tooltip;
        }

        public bool AnyOpen => _openOrder.Count > 0;

        public void Toggle(string windowId)
        {
            HudWindow window = GetOrCreate(windowId);
            if (window == null)
            {
                return;
            }
            if (window.IsOpen)
            {
                Close(window);
            }
            else
            {
                window.SetOpen(true);
                _openOrder.Add(window);
            }
        }

        public void CloseTop()
        {
            if (_openOrder.Count > 0)
            {
                Close(_openOrder[_openOrder.Count - 1]);
            }
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            foreach (HudWindow window in _openOrder)
            {
                window.Refresh(state);
            }
        }

        private void Focus(HudWindow window)
        {
            window.BringToFront();
            if (_openOrder.Remove(window))
            {
                _openOrder.Add(window);
            }
        }

        private void Close(HudWindow window)
        {
            window.SetOpen(false);
            _openOrder.Remove(window);
        }

        private HudWindow GetOrCreate(string windowId)
        {
            if (_windows.TryGetValue(windowId, out HudWindow existing))
            {
                return existing;
            }
            HudWindowDto definition = _ctx.Content.HudWindows.Find(w => w.Id == windowId);
            if (definition == null)
            {
                return null;
            }
            HudWindow window = WindowFactory.Create(definition, _layer, _canvas, _ctx, _tooltip, Focus, Close);
            _windows[windowId] = window;
            return window;
        }
    }
}
