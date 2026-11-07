using Game.Client.Localization;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>Top-center target frame (02, 7.13): name and health of the selected target.</summary>
    public sealed class TargetFrameWidget
    {
        private readonly ClientContext _ctx;
        private readonly Image _panel;
        private readonly Text _name;
        private readonly UiBar _health;

        public TargetFrameWidget(RectTransform zone, ClientContext ctx)
        {
            _ctx = ctx;
            _panel = UiFactory.Panel("TargetFrame", zone, HudPalette.PanelBackground, false);
            UiFactory.Place(_panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, HudLayout.TargetFrameSize);
            float width = HudLayout.TargetFrameSize.x - HudLayout.Margin * 2f;
            _name = UiFactory.Label("Name", _panel.transform, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleCenter, HudPalette.Text);
            UiFactory.Place(_name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -HudLayout.Gap), new Vector2(width, HudLayout.BarHeight));
            _health = UiFactory.Bar("Health", _panel.transform, ctx.Font, HudPalette.Health);
            UiFactory.Place(_health.Rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -HudLayout.BarHeight - HudLayout.Gap * 2f), new Vector2(width, HudLayout.BarHeight));
            _panel.gameObject.SetActive(false);
        }

        public void Refresh(PlayerStateSnapshot player)
        {
            bool has = player != null && player.TargetId.IsValid && _ctx.State.TryGetEntity(player.TargetId, out _);
            _panel.gameObject.SetActive(has);
            if (!has)
            {
                return;
            }
            _ctx.State.TryGetEntity(player.TargetId, out EntitySnapshot target);
            _name.text = _ctx.Text.Get(target.NameKey);
            _health.Set(target.MaxHealth > 0f ? target.Health / target.MaxHealth : 0f,
                _ctx.Text.Format(UiKeys.ValueOfMax, Mathf.CeilToInt(target.Health), Mathf.RoundToInt(target.MaxHealth)));
        }
    }
}
