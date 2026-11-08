using Game.Client.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>Diagnostics (02, 7.6, device setting): frames per second, averaged over a short window, top-right.</summary>
    public sealed class FpsCounterWidget
    {
        private const float WindowSeconds = 0.5f;
        private static readonly Vector2 Size = new Vector2(160f, 24f);

        private readonly ClientContext _ctx;
        private readonly Text _text;
        private float _elapsed;
        private int _frames;

        public FpsCounterWidget(RectTransform zone, ClientContext ctx, float offsetY)
        {
            _ctx = ctx;
            _text = UiFactory.Label("Fps", zone, ctx.Font, HudLayout.FontSmall, TextAnchor.MiddleRight, HudPalette.TextDim);
            UiFactory.Place(_text.rectTransform, Vector2.one, Vector2.one, new Vector2(0f, offsetY), Size);
            SetVisible(false);
        }

        public void SetVisible(bool visible) => _text.gameObject.SetActive(visible);

        public void Tick(float unscaledDt)
        {
            if (!_text.gameObject.activeSelf)
            {
                return;
            }
            _elapsed += unscaledDt;
            _frames++;
            if (_elapsed >= WindowSeconds)
            {
                _text.text = _ctx.Text.Format(UiKeys.FpsLine, Mathf.RoundToInt(_frames / _elapsed));
                _elapsed = 0f;
                _frames = 0;
            }
        }
    }
}
