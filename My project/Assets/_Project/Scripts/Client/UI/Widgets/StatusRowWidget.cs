using System.Globalization;
using System.Text;
using Game.Client.Localization;
using Game.Shared.Content;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// A row of status chips (03, 7.12 UI): picture, short name, stacks and seconds left; buffs and debuffs differ by
    /// colour; a tooltip shows the description. A status without art is shown by its name only; the number of chips is limited.
    /// </summary>
    public sealed class StatusRowWidget
    {
        private static readonly Vector2 ChipSize = new Vector2(104f, 24f);
        private const float IconInset = 1f;
        private const int MinFont = 9;

        private readonly ClientContext _ctx;
        private readonly Tooltip _tooltip;
        private readonly Image[] _chips;
        private readonly Text[] _labels;
        private readonly Image[] _icons;
        private readonly string[] _ids;
        private readonly StringBuilder _sb = new StringBuilder();

        /// <summary>anchor/offset place the first chip; chips go right, `columns` per row, at most `maxChips`.</summary>
        public StatusRowWidget(RectTransform parent, ClientContext ctx, Tooltip tooltip, Vector2 anchor, Vector2 offset, int columns, int maxChips)
        {
            _ctx = ctx;
            _tooltip = tooltip;
            _chips = new Image[maxChips];
            _labels = new Text[maxChips];
            _icons = new Image[maxChips];
            _ids = new string[maxChips];
            for (int i = 0; i < maxChips; i++)
            {
                int index = i;
                Image chip = UiFactory.Panel($"Status_{i}", parent, HudPalette.StatusBuff);
                var cell = new Vector2((i % columns) * (ChipSize.x + HudLayout.Gap), -(i / columns) * (ChipSize.y + HudLayout.Gap));
                UiFactory.Place(chip.rectTransform, anchor, new Vector2(0f, 1f), offset + cell, ChipSize);
                Image icon = UiFactory.Panel("Icon", chip.transform, Color.white, false);
                float iconSize = ChipSize.y - IconInset * 2f;
                UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(IconInset, 0f), new Vector2(iconSize, iconSize));
                Text label = UiFactory.Label("Label", chip.transform, ctx.Font, HudLayout.FontSmall, TextAnchor.MiddleCenter, HudPalette.Text);
                UiFactory.Stretch(label.rectTransform);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = MinFont;
                label.resizeTextMaxSize = HudLayout.FontSmall;
                _icons[i] = icon;
                HoverTrigger hover = chip.gameObject.AddComponent<HoverTrigger>();
                hover.Entered = () => ShowTooltip(index);
                hover.Exited = _tooltip.Hide;
                chip.gameObject.SetActive(false);
                _chips[i] = chip;
                _labels[i] = label;
            }
        }

        public void Refresh(StatusSnapshot[] statuses)
        {
            int count = statuses == null ? 0 : Mathf.Min(statuses.Length, _chips.Length);
            for (int i = 0; i < _chips.Length; i++)
            {
                bool shown = i < count;
                _chips[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    _ids[i] = null;
                    continue;
                }
                StatusSnapshot status = statuses[i];
                _ids[i] = status.StatusId;
                bool known = _ctx.Content.StatusEffects.TryGetValue(status.StatusId, out StatusEffectDefinition definition);
                _chips[i].color = known && definition.Kind == StatusKind.Debuff ? HudPalette.StatusDebuff : HudPalette.StatusBuff;
                Sprite sprite = known ? _ctx.Icons.Status(definition.Icon) : null;
                if (_icons[i].sprite != sprite || _icons[i].enabled != (sprite != null))
                {
                    _icons[i].sprite = sprite;
                    _icons[i].enabled = sprite != null;
                    // The text gives way to the picture.
                    _labels[i].rectTransform.offsetMin = new Vector2(sprite != null ? ChipSize.y : 0f, 0f);
                }
                _sb.Clear().Append(known ? _ctx.Text.Get(definition.NameKey) : status.StatusId);
                if (status.Stacks > 1)
                {
                    _sb.Append(" ×").Append(status.Stacks);
                }
                if (status.Remaining > 0f)
                {
                    _sb.Append(' ').Append(Mathf.CeilToInt(status.Remaining).ToString(CultureInfo.InvariantCulture));
                }
                _labels[i].text = _sb.ToString();
            }
        }

        private void ShowTooltip(int index)
        {
            if (_ids[index] == null || !_ctx.Content.StatusEffects.TryGetValue(_ids[index], out StatusEffectDefinition definition))
            {
                return;
            }
            string description = string.IsNullOrEmpty(definition.DescriptionKey) ? string.Empty : "\n" + _ctx.Text.Get(definition.DescriptionKey);
            _tooltip.Show(_ctx.Text.Get(definition.NameKey) + description);
        }
    }
}
