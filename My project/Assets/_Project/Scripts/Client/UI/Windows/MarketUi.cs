using System;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>Shared pieces of the module 20 windows (market, shop, mail, upgrade orders): texts, item captions, table rows.</summary>
    public static class MarketUi
    {
        private const long SecondsPerMinute = 60;
        private const long SecondsPerHour = 3600;
        private const long SecondsPerDay = 86400;

        /// <summary>"N d H h", "H h M min" or "never" (-1).</summary>
        public static string TimeLeft(ClientContext ctx, long seconds)
        {
            if (seconds < 0)
            {
                return ctx.Text.Get(UiKeys.NeverExpires);
            }
            if (seconds >= SecondsPerDay)
            {
                return ctx.Text.Format(UiKeys.TimeDays, seconds / SecondsPerDay, seconds % SecondsPerDay / SecondsPerHour);
            }
            return ctx.Text.Format(UiKeys.TimeHours, seconds / SecondsPerHour, seconds % SecondsPerHour / SecondsPerMinute);
        }

        public static bool TryGetItem(ClientContext ctx, ItemSlotSnapshot slot, out ItemDefinition item)
        {
            item = null;
            return slot.DefinitionId != null && ctx.Content.Items.TryGetValue(slot.DefinitionId, out item);
        }

        /// <summary>Name with +level and ×count.</summary>
        public static string ItemLine(ClientContext ctx, ItemSlotSnapshot slot)
        {
            if (!TryGetItem(ctx, slot, out ItemDefinition item))
            {
                return slot.DefinitionId ?? string.Empty;
            }
            string name = TooltipText.ItemName(ctx, item, slot.UpgradeLevel);
            return slot.Count > 1 ? ctx.Text.Format(UiKeys.DragStack, name, slot.Count) : name;
        }

        public static Color ItemColor(ClientContext ctx, ItemSlotSnapshot slot) =>
            TryGetItem(ctx, slot, out ItemDefinition item) ? HudPalette.ForRarity(item.Rarity) : HudPalette.Text;

        public static void ShowTooltip(ClientContext ctx, Tooltip tooltip, ItemSlotSnapshot slot, string hintKey)
        {
            if (TryGetItem(ctx, slot, out ItemDefinition item))
            {
                tooltip.Show(TooltipText.Item(ctx, item, slot.Count, hintKey, slot.UpgradeLevel, slot.Sockets, slot.Affixes));
            }
        }

        /// <summary>A system letter's subject from its key ({0} item, {1} number, {2} detail) or the typed subject.</summary>
        public static string Subject(ClientContext ctx, MailSnapshot mail)
        {
            if (string.IsNullOrEmpty(mail.SubjectKey))
            {
                return string.IsNullOrEmpty(mail.Subject) ? ctx.Text.Get(UiKeys.MailNoSubject) : mail.Subject;
            }
            string item = mail.ItemDefinitionId != null && ctx.Content.Items.TryGetValue(mail.ItemDefinitionId, out ItemDefinition definition)
                ? ctx.Text.Get(definition.NameKey) : string.Empty;
            return ctx.Text.Format(mail.SubjectKey, item, mail.Number, mail.Detail ?? string.Empty);
        }

        public static long ReadLong(InputField field, long fallback) => long.TryParse(field.text, out long value) ? value : fallback;

        public static int ReadInt(InputField field, int fallback) => int.TryParse(field.text, out int value) ? value : fallback;

        /// <summary>A row of a table anchored at the top-left of the parent.</summary>
        public static RectTransform Row(string name, RectTransform parent, float y, float width, float height) =>
            UiFactory.Place(UiFactory.Rect(name, parent), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(width, height));

        /// <summary>A text cell of a row at x with a width.</summary>
        public static Text Cell(ClientContext ctx, RectTransform row, float x, float width, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Text text = UiFactory.Label("Cell", row, ctx.Font, HudLayout.FontSmall, alignment, HudPalette.Text);
            UiFactory.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(width, row.sizeDelta.y));
            return text;
        }

        /// <summary>A button at the right edge of a row; slot 0 = rightmost.</summary>
        public static Button RowButton(ClientContext ctx, RectTransform row, string key, int slot, float width, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(key, row, ctx.Font, ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-slot * (width + HudLayout.Gap), 0f), new Vector2(width, row.sizeDelta.y));
            button.GetComponentInChildren<Text>().fontSize = HudLayout.FontSmall;
            return button;
        }

        /// <summary>A small input placed in a row or a panel.</summary>
        public static InputField Field(ClientContext ctx, RectTransform parent, Vector2 anchor, Vector2 offset, float width, float height, int limit, string placeholderKey, bool numbers)
        {
            InputField field = UiFactory.Input("Field", parent, ctx.Font, HudLayout.FontSmall, limit, placeholderKey != null ? ctx.Text.Get(placeholderKey) : string.Empty);
            UiFactory.Place((RectTransform)field.transform, anchor, anchor, offset, new Vector2(width, height));
            if (numbers)
            {
                field.contentType = InputField.ContentType.IntegerNumber;
            }
            return field;
        }

        /// <summary>
        /// Hover over a row (a plain RectTransform without a graphic) shows the tooltip of whatever the getter returns.
        /// A clear Image makes the row hit-testable; one graphic per object, so never call it on a Text or Button.
        /// </summary>
        public static void HoverItem(ClientContext ctx, Tooltip tooltip, RectTransform row, Func<ItemSlotSnapshot?> item, string hintKey)
        {
            Image hit = row.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            HoverTrigger hover = row.gameObject.AddComponent<HoverTrigger>();
            hover.Entered = () =>
            {
                ItemSlotSnapshot? slot = item();
                if (slot.HasValue)
                {
                    ShowTooltip(ctx, tooltip, slot.Value, hintKey);
                }
            };
            hover.Exited = tooltip.Hide;
        }
    }
}
