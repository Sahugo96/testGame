using Game.Client.Localization;
using Game.Shared.Protocol;
using UnityEngine;

namespace Game.Client.UI.Widgets
{
    /// <summary>Experience bar bottom-right (02, 7.13): level, experience to the next level.</summary>
    public sealed class ExperienceBarWidget
    {
        private const float Width = 420f;

        private readonly ClientContext _ctx;
        private readonly UiBar _bar;

        public ExperienceBarWidget(RectTransform zone, ClientContext ctx, float bottomOffset)
        {
            _ctx = ctx;
            _bar = UiFactory.Bar("ExperienceBar", zone, ctx.Font, HudPalette.Experience);
            UiFactory.Place(_bar.Rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, bottomOffset), new Vector2(Width, HudLayout.BarHeight));
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            string level = _ctx.Text.Format(UiKeys.Level, state.Level);
            if (state.ExperienceToNext <= 0)
            {
                _bar.Set(1f, level + "  " + _ctx.Text.Get(UiKeys.ExperienceMax));
                return;
            }
            _bar.Set((float)state.Experience / state.ExperienceToNext,
                level + "  " + _ctx.Text.Format(UiKeys.ExperienceLine, state.Experience, state.ExperienceToNext));
        }
    }
}
