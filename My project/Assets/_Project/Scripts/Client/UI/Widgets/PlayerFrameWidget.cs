using System.Globalization;
using Game.Client.Localization;
using Game.Shared.Content;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;
using EntityId = Game.Shared.Core.EntityId;

namespace Game.Client.UI.Widgets
{
    /// <summary>Top-left player frame (02, 7.13): name, level, HP, main and reserve mana, lock/combat state, cast bar, weapon swap timers.</summary>
    public sealed class PlayerFrameWidget
    {
        private readonly ClientContext _ctx;
        private readonly Text _title;
        private readonly UiBar _health;
        private readonly UiBar _main;
        private readonly UiBar _reserve;
        private readonly Text _status;
        private readonly UiBar _cast;

        public PlayerFrameWidget(RectTransform zone, ClientContext ctx)
        {
            _ctx = ctx;
            Font font = ctx.Font;
            Image panel = UiFactory.Panel("PlayerFrame", zone, HudPalette.PanelBackground, false);
            UiFactory.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, HudLayout.PlayerFrameSize);
            float width = HudLayout.PlayerFrameSize.x - HudLayout.Margin * 2f;
            float y = -HudLayout.Gap;

            _title = UiFactory.Label("Title", panel.transform, font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Text);
            UiFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudLayout.Margin, y), new Vector2(width, HudLayout.BarHeight));
            y -= HudLayout.BarHeight + HudLayout.Gap;

            _health = AddBar(panel.transform, HudPalette.Health, width, ref y);
            _main = AddBar(panel.transform, HudPalette.MainMana, width, ref y);
            _reserve = AddBar(panel.transform, HudPalette.ReserveMana, width, ref y);

            _status = UiFactory.Label("Status", panel.transform, font, HudLayout.FontSmall, TextAnchor.MiddleLeft, HudPalette.Warning);
            UiFactory.Place(_status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudLayout.Margin, y), new Vector2(width, HudLayout.BarHeight));

            _cast = UiFactory.Bar("CastBar", zone, font, HudPalette.Cast);
            UiFactory.Place(_cast.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -HudLayout.PlayerFrameSize.y - HudLayout.Gap), new Vector2(HudLayout.PlayerFrameSize.x, HudLayout.CastBarHeight));
            _cast.SetVisible(false);
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            ILocalizationService text = _ctx.Text;
            string className = _ctx.Content.Classes.TryGetValue(state.ClassId, out ClassDefinition cls) ? text.Get(cls.NameKey) : state.ClassId;
            _title.text = className + "  " + text.Format(UiKeys.Level, state.Level);
            if (state.Group != null && state.Group.InGroup && state.Group.LeaderId == state.EntityId)
            {
                _title.text = text.Format(UiKeys.GroupLeaderName, _title.text);
            }
            _health.Set(Fraction(state.Health, state.MaxHealth), Line(UiKeys.HealthShort, state.Health, state.MaxHealth));
            _main.Set(Fraction(state.MainMana, state.MaxMainMana), Line(UiKeys.MainManaShort, state.MainMana, state.MaxMainMana));
            _reserve.Set(Fraction(state.ReserveMana, state.MaxReserveMana), Line(UiKeys.ReserveManaShort, state.ReserveMana, state.MaxReserveMana));

            string status = string.Empty;
            if (state.IsDead)
            {
                status = text.Get(UiKeys.Dead);
            }
            else
            {
                if (state.InCombat)
                {
                    status = text.Get(UiKeys.InCombat);
                }
                if (state.MainManaLocked)
                {
                    status = Join(status, text.Get(UiKeys.MainLocked));
                }
                if (state.MainHandSwapLockRemaining > 0f)
                {
                    status = Join(status, text.Format(UiKeys.WeaponSwapLocked, Seconds(state.MainHandSwapLockRemaining)));
                }
                else if (state.MainHandSwapRiskRemaining > 0f)
                {
                    status = Join(status, text.Format(UiKeys.WeaponSwapRisk, Seconds(state.MainHandSwapRiskRemaining)));
                }
            }
            _status.text = status;

            bool casting = !string.IsNullOrEmpty(state.CastSkillId);
            _cast.SetVisible(casting);
            if (casting && _ctx.Content.Skills.TryGetValue(state.CastSkillId, out SkillDefinition skill))
            {
                _cast.Set(state.CastProgress, text.Get(skill.NameKey));
            }
        }

        private UiBar AddBar(Transform parent, Color color, float width, ref float y)
        {
            UiBar bar = UiFactory.Bar("Bar", parent, _ctx.Font, color);
            UiFactory.Place(bar.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudLayout.Margin, y), new Vector2(width, HudLayout.BarHeight));
            y -= HudLayout.BarHeight + HudLayout.Gap;
            return bar;
        }

        private string Line(string labelKey, float value, float max) =>
            _ctx.Text.Get(labelKey) + "  " + _ctx.Text.Format(UiKeys.ValueOfMax, Mathf.CeilToInt(value), Mathf.RoundToInt(max));

        private static float Fraction(float value, float max) => max > 0f ? value / max : 0f;

        private static string Seconds(float value) => value.ToString("0", CultureInfo.InvariantCulture);

        private static string Join(string a, string b) => string.IsNullOrEmpty(a) ? b : a + " · " + b;
    }
}
