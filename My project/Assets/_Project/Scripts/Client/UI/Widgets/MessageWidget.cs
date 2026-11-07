using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>Center message line for server rejections ("not enough mana", "out of range", ...).</summary>
    public sealed class MessageWidget
    {
        private const float Width = 700f;
        private const float OffsetFromTop = -160f;

        private readonly ClientContext _ctx;
        private readonly Text _text;
        private float _remaining;

        public MessageWidget(RectTransform root, ClientContext ctx)
        {
            _ctx = ctx;
            _text = UiFactory.Label("Message", root, ctx.Font, HudLayout.FontLarge, TextAnchor.MiddleCenter, HudPalette.Error);
            UiFactory.Place(_text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, OffsetFromTop),
                new Vector2(Width, HudLayout.FontHuge));
            _text.text = string.Empty;
        }

        /// <summary>Error line (command rejected by the server).</summary>
        public void Show(string key) => ShowText(_ctx.Text.Get(key), HudPalette.Error);

        /// <summary>Server notification: {0} = number, {1} = localized item name.</summary>
        public void Show(NotificationMessage message)
        {
            string item = string.IsNullOrEmpty(message.ItemNameKey) ? string.Empty : _ctx.Text.Get(message.ItemNameKey);
            ShowText(_ctx.Text.Format(message.TextKey, message.Number, item), message.IsPositive ? HudPalette.Positive : HudPalette.Error);
        }

        private void ShowText(string text, Color color)
        {
            _text.text = text;
            _text.color = color;
            _remaining = HudLayout.MessageSeconds;
        }

        public void Tick(float dt)
        {
            if (_remaining <= 0f)
            {
                return;
            }
            _remaining -= dt;
            if (_remaining <= 0f)
            {
                _text.text = string.Empty;
            }
        }
    }
}
