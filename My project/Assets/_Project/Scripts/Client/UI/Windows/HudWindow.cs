using Game.Client.Localization;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Base of a mechanic window (02, 7.13): header with title, close button and drag handle; content area.
    /// Windows show model data and send commands; they hold no game logic.
    /// </summary>
    public abstract class HudWindow
    {
        protected readonly ClientContext Ctx;
        protected readonly Tooltip Tooltip;
        protected readonly RectTransform Content;
        private readonly RectTransform _root;

        protected HudWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip, System.Action<HudWindow> focus, System.Action<HudWindow> close)
        {
            Definition = definition;
            Ctx = ctx;
            Tooltip = tooltip;
            Image panel = UiFactory.Panel($"Window_{definition.Id}", parent, HudPalette.PanelBackground);
            _root = UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, HudLayout.WindowSize);

            Image header = UiFactory.Panel("Header", _root, HudPalette.WindowHeader);
            RectTransform headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, HudLayout.WindowHeaderHeight);
            WindowDragHandle drag = header.gameObject.AddComponent<WindowDragHandle>();
            drag.Init(_root, canvas);
            drag.Focused = () => focus(this);

            Text title = UiFactory.Label("Title", headerRect, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Text);
            UiFactory.Stretch(title.rectTransform, HudLayout.Gap * 2f);
            title.text = ctx.Text.Get(definition.NameKey);

            Button closeButton = UiFactory.Button("Close", headerRect, ctx.Font, ctx.Text.Get(UiKeys.Close), () => close(this));
            UiFactory.Place((RectTransform)closeButton.transform, Vector2.one, Vector2.one, Vector2.zero,
                new Vector2(HudLayout.WindowHeaderHeight, HudLayout.WindowHeaderHeight));

            Content = UiFactory.Rect("Content", _root);
            Content.anchorMin = Vector2.zero;
            Content.anchorMax = Vector2.one;
            Content.offsetMin = new Vector2(HudLayout.Margin, HudLayout.Margin);
            Content.offsetMax = new Vector2(-HudLayout.Margin, -HudLayout.WindowHeaderHeight - HudLayout.Gap);
            _root.gameObject.SetActive(false);
        }

        public HudWindowDto Definition { get; }

        public bool IsOpen => _root.gameObject.activeSelf;

        public void SetOpen(bool open)
        {
            _root.gameObject.SetActive(open);
            if (open)
            {
                _root.SetAsLastSibling();
                if (Ctx.State.Player != null)
                {
                    Refresh(Ctx.State.Player);
                }
            }
            else
            {
                Tooltip.Hide();
            }
        }

        public void BringToFront() => _root.SetAsLastSibling();

        /// <summary>Called on open and on every player snapshot while open (data subscription only while open, 02 7.13).</summary>
        public abstract void Refresh(PlayerStateSnapshot state);

        protected Text Row(string name, Transform parent, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Text text = UiFactory.Label(name, parent, Ctx.Font, HudLayout.FontNormal, alignment, HudPalette.Text);
            text.rectTransform.sizeDelta = new Vector2(0f, HudLayout.RowHeight);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = HudLayout.RowHeight;
            layout.preferredHeight = HudLayout.RowHeight;
            return text;
        }

        protected RectTransform VerticalList(string name, Transform parent)
        {
            RectTransform list = UiFactory.Rect(name, parent);
            UiFactory.Stretch(list);
            VerticalLayoutGroup layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = HudLayout.Gap;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return list;
        }
    }
}
