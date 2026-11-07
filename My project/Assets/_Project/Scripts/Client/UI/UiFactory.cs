using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>Builds uGUI elements in code (no prefabs yet). Text uses the built-in legacy font until TextMeshPro is set up.</summary>
    public static class UiFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Anchors and pivots a rect at one point of its parent with an offset and size.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        /// <summary>
        /// A single-line text field: background, text without rich text (player input is shown as typed), a dim
        /// placeholder and a character limit. Place the returned field's rect yourself.
        /// </summary>
        public static InputField Input(string name, Transform parent, Font font, int fontSize, int characterLimit, string placeholder)
        {
            Image background = Panel(name, parent, HudPalette.SlotFilled);
            Text text = Label("Text", background.transform, font, fontSize, TextAnchor.MiddleLeft, HudPalette.Text);
            Stretch(text.rectTransform, HudLayout.Gap * 2f);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text hint = Label("Placeholder", background.transform, font, fontSize, TextAnchor.MiddleLeft, HudPalette.TextDim);
            Stretch(hint.rectTransform, HudLayout.Gap * 2f);
            hint.text = placeholder ?? string.Empty;
            InputField field = background.gameObject.AddComponent<InputField>();
            field.targetGraphic = background;
            field.textComponent = text;
            field.placeholder = hint;
            field.characterLimit = characterLimit;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        public static Image Panel(string name, Transform parent, Color color, bool raycast = true)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Text Label(string name, Transform parent, Font font, int size, TextAnchor alignment, Color color)
        {
            RectTransform rect = Rect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button Button(string name, Transform parent, Font font, string caption, UnityAction onClick)
        {
            Image background = Panel(name, parent, HudPalette.Button);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(onClick);
            Text label = Label("Label", background.transform, font, HudLayout.FontNormal, TextAnchor.MiddleCenter, HudPalette.Text);
            Stretch(label.rectTransform);
            label.text = caption;
            return button;
        }

        /// <summary>A filled bar: the fill is scaled by anchors, so no sprite is required.</summary>
        public static UiBar Bar(string name, Transform parent, Font font, Color fill)
        {
            Image background = Panel(name, parent, HudPalette.BarBackground, false);
            Image fillImage = Panel("Fill", background.transform, fill, false);
            RectTransform fillRect = fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Text label = Label("Text", background.transform, font, HudLayout.FontSmall, TextAnchor.MiddleCenter, HudPalette.Text);
            Stretch(label.rectTransform);
            return new UiBar(background, fillImage, label);
        }
    }
}
