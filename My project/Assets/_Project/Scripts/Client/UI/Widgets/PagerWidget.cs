using System;
using Game.Client.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>Page switcher for long lists (friends, raid members, trade bag): "&lt;", "page/total", "&gt;". Hidden when one page is enough.</summary>
    public sealed class PagerWidget
    {
        private const float ButtonWidth = 48f;
        private const float LabelWidth = 80f;

        private readonly RectTransform _root;
        private readonly Text _label;
        private readonly ClientContext _ctx;
        private readonly Action _changed;
        private int _pages = 1;

        public PagerWidget(RectTransform parent, ClientContext ctx, Vector2 anchor, Vector2 offset, Action changed)
        {
            _ctx = ctx;
            _changed = changed;
            float width = ButtonWidth * 2f + LabelWidth + HudLayout.Gap * 2f;
            _root = UiFactory.Place(UiFactory.Rect("Pager", parent), anchor, anchor, offset, new Vector2(width, HudLayout.MenuButtonHeight));
            Button prev = UiFactory.Button("Prev", _root, ctx.Font, ctx.Text.Get(UiKeys.PagePrev), () => Shift(-1));
            UiFactory.Place((RectTransform)prev.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(ButtonWidth, HudLayout.MenuButtonHeight));
            _label = UiFactory.Label("Page", _root, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleCenter, HudPalette.Text);
            UiFactory.Place(_label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(LabelWidth, HudLayout.MenuButtonHeight));
            Button next = UiFactory.Button("Next", _root, ctx.Font, ctx.Text.Get(UiKeys.PageNext), () => Shift(1));
            UiFactory.Place((RectTransform)next.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(ButtonWidth, HudLayout.MenuButtonHeight));
        }

        public int Page { get; private set; }

        /// <summary>Updates the page count for <paramref name="total"/> entries and returns the first index of the current page.</summary>
        public int Apply(int total, int perPage)
        {
            _pages = Math.Max(1, (total + perPage - 1) / perPage);
            Page = Mathf.Clamp(Page, 0, _pages - 1);
            _root.gameObject.SetActive(_pages > 1);
            _label.text = _ctx.Text.Format(UiKeys.PageLine, Page + 1, _pages);
            return Page * perPage;
        }

        private void Shift(int delta)
        {
            int page = Mathf.Clamp(Page + delta, 0, _pages - 1);
            if (page != Page)
            {
                Page = page;
                _changed?.Invoke();
            }
        }
    }
}
