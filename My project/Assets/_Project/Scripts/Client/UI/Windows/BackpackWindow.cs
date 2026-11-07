using System;
using System.Collections.Generic;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>Backpack (02, 7.13): grid of bag slots; click equips equipment or uses a consumable; tooltip with item details.</summary>
    public sealed class BackpackWindow : HudWindow
    {
        private readonly List<SlotView> _slots = new List<SlotView>();

        public BackpackWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close)
        {
            GridLayoutGroup grid = Content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(HudLayout.BagSlotSize, HudLayout.BagSlotSize);
            grid.spacing = new Vector2(HudLayout.Gap, HudLayout.Gap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = HudLayout.BagColumns;
            for (int i = 0; i < ctx.Content.Config.BagSize; i++)
            {
                var slot = new SlotView($"Bag{i}", Content, ctx.Font, HudLayout.BagSlotSize);
                int index = i;
                slot.Bind(() => ShowTooltip(index), Tooltip.Hide, button => OnClick(index, button));
                _slots.Add(slot);
            }
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            for (int i = 0; i < _slots.Count && i < state.Bag.Length; i++)
            {
                ItemSlotSnapshot bagSlot = state.Bag[i];
                SlotView view = _slots[i];
                if (bagSlot.DefinitionId == null)
                {
                    view.SetFilled(false);
                    view.SetCaption(string.Empty, HudPalette.Text);
                    view.SetCount(string.Empty);
                    continue;
                }
                ItemDefinition item = Ctx.Content.Items[bagSlot.DefinitionId];
                view.SetFilled(true);
                view.SetCaption(Ctx.Text.Get(item.NameKey), HudPalette.ForRarity(item.Rarity));
                view.SetCount(bagSlot.Count > 1 ? bagSlot.Count.ToString() : string.Empty);
            }
        }

        private void ShowTooltip(int index)
        {
            PlayerStateSnapshot state = Ctx.State.Player;
            if (state == null || index >= state.Bag.Length || state.Bag[index].DefinitionId == null)
            {
                return;
            }
            ItemDefinition item = Ctx.Content.Items[state.Bag[index].DefinitionId];
            string hint = item.Has<EquipmentComponent>() ? UiKeys.ClickToEquip : item.Has<ConsumableComponent>() ? UiKeys.ClickToUse : null;
            Tooltip.Show(TooltipText.Item(Ctx, item, state.Bag[index].Count, hint));
        }

        private void OnClick(int index, PointerEventData.InputButton button)
        {
            PlayerStateSnapshot state = Ctx.State.Player;
            if (button != PointerEventData.InputButton.Left || state == null || index >= state.Bag.Length || state.Bag[index].DefinitionId == null)
            {
                return;
            }
            ItemDefinition item = Ctx.Content.Items[state.Bag[index].DefinitionId];
            if (item.Has<EquipmentComponent>())
            {
                Ctx.Commands.Equip(index);
            }
            else if (item.Has<ConsumableComponent>())
            {
                Ctx.Commands.UseConsumable(index);
            }
            Tooltip.Hide();
        }
    }
}
