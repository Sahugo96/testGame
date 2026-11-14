using System.Globalization;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using UnityEngine;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// How an item looks in a slot, the same in every window: its picture in a frame of the rarity colour, "+N" of the
    /// upgrade in the corner and the stack size. An item without art is shown by its name in the rarity colour.
    /// </summary>
    public static class ItemSlots
    {
        private const string UpgradePrefix = "+";

        /// <summary>Shows the stack of a bag slot; an empty or unknown one clears the slot.</summary>
        public static void Show(ClientContext ctx, SlotView view, ItemSlotSnapshot slot)
        {
            if (slot.DefinitionId == null || !ctx.Content.Items.TryGetValue(slot.DefinitionId, out ItemDefinition item))
            {
                Clear(view);
                return;
            }
            Show(ctx, view, item, slot.UpgradeLevel, slot.Count);
        }

        public static void Show(ClientContext ctx, SlotView view, ItemDefinition item, int upgradeLevel, int count)
        {
            Sprite icon = ctx.Icons.Item(item.Icon);
            view.SetIcon(icon);
            view.SetIconTint(Color.white);
            if (icon != null)
            {
                view.SetCaption(string.Empty, HudPalette.Text);
                view.SetMark(upgradeLevel > 0 ? UpgradePrefix + upgradeLevel.ToString(CultureInfo.InvariantCulture) : string.Empty);
                view.SetTint(HudPalette.FrameForRarity(item.Rarity));
            }
            else
            {
                view.SetCaption(TooltipText.ItemName(ctx, item, upgradeLevel), HudPalette.ForRarity(item.Rarity));
                view.SetMark(string.Empty);
                view.SetFilled(true);
            }
            view.SetCount(count > 1 ? count.ToString(CultureInfo.InvariantCulture) : string.Empty);
        }

        /// <summary>An empty slot.</summary>
        public static void Clear(SlotView view) => Clear(view, string.Empty);

        /// <summary>An empty slot with a dim hint (the name of an equipment slot).</summary>
        public static void Clear(SlotView view, string hint)
        {
            view.SetIcon(null);
            view.SetFilled(false);
            view.SetCaption(hint, HudPalette.TextDim);
            view.SetMark(string.Empty);
            view.SetCount(string.Empty);
        }
    }
}
