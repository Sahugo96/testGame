using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// A square slot: icon (or a caption when there is no art), hotkey, counter, cooldown overlay and a corner mark. Used
    /// by skill, consumable and bag slots. With an icon the slot background is seen as a thin frame around it.
    /// </summary>
    public sealed class SlotView
    {
        private const float CornerInset = 3f;
        private const float IconInset = 2f;
        private const float CornerLabelHeight = 16f;
        private const int MinCaptionFont = 8;

        public readonly Image Background;
        private readonly Image _icon;
        private readonly Text _caption;
        private readonly Text _hotkey;
        private readonly Text _count;
        private readonly Text _mark;
        private readonly Image _cooldown;
        private readonly HoverTrigger _hover;

        public SlotView(string name, Transform parent, Font font, float size)
        {
            Background = UiFactory.Panel(name, parent, HudPalette.SlotBackground);
            Background.rectTransform.sizeDelta = new Vector2(size, size);
            _icon = UiFactory.Panel("Icon", Background.transform, Color.white, false);
            UiFactory.Stretch(_icon.rectTransform, IconInset);
            _icon.enabled = false;
            _cooldown = UiFactory.Panel("Cooldown", Background.transform, HudPalette.CooldownOverlay, false);
            RectTransform cooldownRect = _cooldown.rectTransform;
            cooldownRect.anchorMin = Vector2.zero;
            cooldownRect.anchorMax = new Vector2(1f, 0f);
            cooldownRect.pivot = new Vector2(0.5f, 0f);
            cooldownRect.offsetMin = Vector2.zero;
            cooldownRect.offsetMax = Vector2.zero;
            _caption = UiFactory.Label("Caption", Background.transform, font, HudLayout.FontSmall, TextAnchor.MiddleCenter, HudPalette.Text);
            UiFactory.Stretch(_caption.rectTransform, CornerInset);
            _caption.resizeTextForBestFit = true;
            _caption.resizeTextMinSize = MinCaptionFont;
            _caption.resizeTextMaxSize = HudLayout.FontSmall;
            _hotkey = Corner("Hotkey", font, new Vector2(0f, 1f), TextAnchor.UpperLeft, HudPalette.TextDim);
            _count = Corner("Count", font, new Vector2(1f, 0f), TextAnchor.LowerRight, HudPalette.Text);
            _mark = Corner("Mark", font, new Vector2(1f, 1f), TextAnchor.UpperRight, HudPalette.Cast);
            _hover = Background.gameObject.AddComponent<HoverTrigger>();
        }

        public RectTransform Rect => Background.rectTransform;

        public void Bind(Action onEnter, Action onExit, Action<PointerEventData.InputButton> onClick)
        {
            _hover.Entered = onEnter;
            _hover.Exited = onExit;
            _hover.Clicked = onClick;
        }

        /// <summary>Makes the slot draggable and a drop target.</summary>
        public void BindDrag(Action began, Action ended, Action dropped)
        {
            // Not "??": in the editor GetComponent returns a fake-null object that "??" treats as present.
            DragTrigger drag = Background.gameObject.GetComponent<DragTrigger>();
            if (drag == null)
            {
                drag = Background.gameObject.AddComponent<DragTrigger>();
            }
            drag.Began = began;
            drag.Ended = ended;
            drag.Dropped = dropped;
        }

        public void SetCaption(string caption, Color color)
        {
            _caption.text = caption;
            _caption.color = color;
        }

        /// <summary>Shows a picture instead of the caption; null — no art, the caption stays.</summary>
        public void SetIcon(Sprite icon)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _caption.enabled = icon == null;
        }

        /// <summary>Colours the picture (white — as drawn): not enough of a resource, nothing left.</summary>
        public void SetIconTint(Color color) => _icon.color = color;

        public void SetHotkey(string text) => _hotkey.text = text;

        public void SetCount(string text) => _count.text = text;

        public void SetMark(string text) => _mark.text = text;

        public void SetFilled(bool filled) => Background.color = filled ? HudPalette.SlotFilled : HudPalette.SlotBackground;

        public void SetTint(Color color) => Background.color = color;

        /// <summary>0 = ready, 1 = full cooldown.</summary>
        public void SetCooldown(float fraction) => _cooldown.rectTransform.anchorMax = new Vector2(1f, Mathf.Clamp01(fraction));

        private Text Corner(string name, Font font, Vector2 anchor, TextAnchor alignment, Color color)
        {
            Text label = UiFactory.Label(name, Background.transform, font, HudLayout.FontSmall, alignment, color);
            float size = Background.rectTransform.sizeDelta.x - CornerInset * 2f;
            Vector2 offset = new Vector2(anchor.x > 0.5f ? -CornerInset : CornerInset, anchor.y > 0.5f ? -CornerInset : CornerInset);
            UiFactory.Place(label.rectTransform, anchor, anchor, offset, new Vector2(size, CornerLabelHeight));
            // Corner texts lie over the picture: a dark outline keeps them readable on any icon.
            Outline outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = HudPalette.TextOutline;
            return label;
        }
    }
}
