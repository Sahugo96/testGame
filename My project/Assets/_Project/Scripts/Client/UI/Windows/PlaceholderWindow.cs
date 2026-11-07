using System;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>Window registered in data whose mechanic is not implemented yet.</summary>
    public sealed class PlaceholderWindow : HudWindow
    {
        private const string NotImplementedKey = "ui.not_implemented";

        public PlaceholderWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close)
        {
            Text text = UiFactory.Label("Info", Content, ctx.Font, HudLayout.FontNormal, TextAnchor.UpperLeft, HudPalette.TextDim);
            UiFactory.Stretch(text.rectTransform);
            text.text = ctx.Text.Get(NotImplementedKey);
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
        }
    }
}
