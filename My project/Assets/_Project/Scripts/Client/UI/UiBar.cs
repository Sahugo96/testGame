using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>Horizontal bar built by <see cref="UiFactory.Bar"/>.</summary>
    public sealed class UiBar
    {
        public readonly Image Background;
        public readonly Image Fill;
        public readonly Text Label;

        public UiBar(Image background, Image fill, Text label)
        {
            Background = background;
            Fill = fill;
            Label = label;
        }

        public RectTransform Rect => Background.rectTransform;

        public void Set(float fraction, string text)
        {
            Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            Label.text = text;
        }

        public void SetVisible(bool visible) => Background.gameObject.SetActive(visible);
    }
}
