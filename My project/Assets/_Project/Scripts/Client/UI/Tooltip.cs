using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>Single tooltip that follows the pointer.</summary>
    public sealed class Tooltip
    {
        private readonly RectTransform _root;
        private readonly Text _text;
        private readonly RectTransform _canvasRect;

        public Tooltip(Transform canvas, Font font)
        {
            _canvasRect = (RectTransform)canvas;
            Image panel = UiFactory.Panel("Tooltip", canvas, HudPalette.PanelBackground, false);
            _root = panel.rectTransform;
            _root.pivot = new Vector2(0f, 1f);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.zero;
            ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            _root.sizeDelta = new Vector2(HudLayout.TooltipWidth, 0f);
            _text = UiFactory.Label("Text", _root, font, HudLayout.FontSmall, TextAnchor.UpperLeft, HudPalette.Text);
            _text.supportRichText = true;
            Hide();
        }

        public void Show(string text)
        {
            _text.text = text;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
        }

        public void Hide() => _root.gameObject.SetActive(false);

        public void Follow(Vector2 screenPosition)
        {
            if (!_root.gameObject.activeSelf)
            {
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, null, out Vector2 local);
            Vector2 size = _canvasRect.rect.size;
            _root.anchoredPosition = local + size * 0.5f + new Vector2(HudLayout.TooltipOffset, -HudLayout.TooltipOffset);
        }
    }
}
