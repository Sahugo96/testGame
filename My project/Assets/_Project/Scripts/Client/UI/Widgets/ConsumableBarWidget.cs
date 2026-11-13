using System;
using System.Collections.Generic;
using Game.Client.Localization;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Consumable panel (02, 7.7 Б): slots hold an item kind (definition id) from the character's layout on the server
    /// (module 18) — any stack of that kind is used, so an empty stack is replaced by the next one (auto-substitution);
    /// counter and group cooldown. Filled by dragging a consumable from the backpack; dragging a slot onto another swaps
    /// them, onto nothing clears it. Until the layout arrives the bag's consumables are shown as before.
    /// </summary>
    public sealed class ConsumableBarWidget
    {
        private readonly ClientContext _ctx;
        private readonly Tooltip _tooltip;
        private readonly List<SlotView> _slots = new List<SlotView>();
        private readonly string[] _itemBySlot;

        public ConsumableBarWidget(RectTransform zone, ClientContext ctx, Tooltip tooltip, float bottomOffset)
        {
            _ctx = ctx;
            _tooltip = tooltip;
            int count = ctx.Content.Config.ConsumableBarSlots;
            _itemBySlot = new string[count];
            float step = HudLayout.ConsumableSlotSize + HudLayout.Gap;
            float width = step * count - HudLayout.Gap;
            for (int i = 0; i < count; i++)
            {
                var slot = new SlotView($"ConsumableSlot{i}", zone, ctx.Font, HudLayout.ConsumableSlotSize);
                UiFactory.Place(slot.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                    new Vector2(-width * 0.5f + i * step, bottomOffset), new Vector2(HudLayout.ConsumableSlotSize, HudLayout.ConsumableSlotSize));
                int captured = i;
                slot.Bind(() => OnEnter(captured), _tooltip.Hide, button => OnClick(captured, button));
                slot.BindDrag(() => BeginDrag(captured), () => EndDrag(captured), () => Drop(captured));
                _slots.Add(slot);
            }
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            if (state == null)
            {
                return;
            }
            if (_ctx.State.Panels == null)
            {
                FillFromBag(state);
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                string itemId = _itemBySlot[i];
                SlotView view = _slots[i];
                if (itemId == null || !_ctx.Content.Items.TryGetValue(itemId, out ItemDefinition item))
                {
                    view.SetFilled(false);
                    view.SetCaption(string.Empty, HudPalette.Text);
                    view.SetCount(string.Empty);
                    view.SetCooldown(0f);
                    continue;
                }
                int total = TotalCount(state, itemId);
                view.SetFilled(total > 0);
                view.SetCaption(_ctx.Text.Get(item.NameKey), total > 0 ? HudPalette.Text : HudPalette.TextDim);
                view.SetCount(total.ToString());
                ConsumableComponent consumable = item.Get<ConsumableComponent>();
                view.SetCooldown(consumable != null ? GroupCooldown(state, consumable) : 0f);
            }
        }

        /// <summary>The character's layout from the server (module 18).</summary>
        public void ApplyPanels(PanelsMessage panels)
        {
            for (int i = 0; i < _itemBySlot.Length; i++)
            {
                string itemId = i < panels.ConsumableBar.Length ? panels.ConsumableBar[i] : null;
                _itemBySlot[i] = string.IsNullOrEmpty(itemId) ? null : itemId;
            }
            Refresh(_ctx.State.Player);
        }

        /// <summary>Before the server's layout: the distinct consumables of the bag, in bag order.</summary>
        private void FillFromBag(PlayerStateSnapshot state)
        {
            Array.Clear(_itemBySlot, 0, _itemBySlot.Length);
            int slot = 0;
            foreach (ItemSlotSnapshot bagSlot in state.Bag)
            {
                if (slot >= _itemBySlot.Length)
                {
                    break;
                }
                if (bagSlot.DefinitionId == null || Array.IndexOf(_itemBySlot, bagSlot.DefinitionId) >= 0
                    || !_ctx.Content.Items[bagSlot.DefinitionId].Has<ConsumableComponent>())
                {
                    continue;
                }
                _itemBySlot[slot++] = bagSlot.DefinitionId;
            }
        }

        private static int TotalCount(PlayerStateSnapshot state, string definitionId)
        {
            int total = 0;
            foreach (ItemSlotSnapshot slot in state.Bag)
            {
                if (slot.DefinitionId == definitionId)
                {
                    total += slot.Count;
                }
            }
            return total;
        }

        /// <summary>The first bag stack of the kind (auto-substitution: when one runs out, the next is used); -1 if none.</summary>
        private static int FirstStack(PlayerStateSnapshot state, string definitionId)
        {
            foreach (ItemSlotSnapshot slot in state.Bag)
            {
                if (slot.DefinitionId == definitionId && slot.Count > 0)
                {
                    return slot.Index;
                }
            }
            return -1;
        }

        private static float GroupCooldown(PlayerStateSnapshot state, ConsumableComponent consumable)
        {
            if (consumable.Cooldown <= 0f)
            {
                return 0f;
            }
            foreach (CooldownGroupSnapshot group in state.ConsumableCooldowns)
            {
                if (group.Group == consumable.CooldownGroup)
                {
                    return group.Remaining / consumable.Cooldown;
                }
            }
            return 0f;
        }

        private void OnEnter(int index)
        {
            PlayerStateSnapshot state = _ctx.State.Player;
            string itemId = _itemBySlot[index];
            if (state == null || itemId == null || !_ctx.Content.Items.TryGetValue(itemId, out ItemDefinition item))
            {
                return;
            }
            _tooltip.Show(TooltipText.Item(_ctx, item, TotalCount(state, itemId), UiKeys.ClickToUse));
        }

        private void OnClick(int index, PointerEventData.InputButton button)
        {
            PlayerStateSnapshot state = _ctx.State.Player;
            string itemId = _itemBySlot[index];
            if (state == null || itemId == null || button != PointerEventData.InputButton.Left)
            {
                return;
            }
            int bagIndex = FirstStack(state, itemId);
            if (bagIndex >= 0)
            {
                _ctx.Commands?.UseConsumable(bagIndex);
            }
        }

        // ---- Drag and drop (module 18) ----

        private void BeginDrag(int index)
        {
            string itemId = _itemBySlot[index];
            if (itemId != null && _ctx.State.Panels != null && _ctx.Content.Items.TryGetValue(itemId, out ItemDefinition item))
            {
                _ctx.Drag.Begin(DragKind.ConsumableSlot, itemId, index);
                _tooltip.Show(_ctx.Text.Format(UiKeys.DragSkill, _ctx.Text.Get(item.NameKey)));
            }
        }

        private void EndDrag(int index)
        {
            if (_ctx.Drag.Kind == DragKind.ConsumableSlot && _ctx.Drag.Index == index && !_ctx.Drag.Dropped)
            {
                _ctx.Commands?.SetConsumableSlot(index, null);
            }
            _ctx.Drag.End();
            _tooltip.Hide();
        }

        private void Drop(int index)
        {
            DragDropService drag = _ctx.Drag;
            if (_ctx.Commands == null)
            {
                return;
            }
            if (drag.Kind == DragKind.Item && drag.Id != null && _ctx.Content.Items.TryGetValue(drag.Id, out ItemDefinition item) && item.Has<ConsumableComponent>())
            {
                _ctx.Commands.SetConsumableSlot(index, drag.Id);
                drag.MarkDropped();
            }
            else if (drag.Kind == DragKind.ConsumableSlot)
            {
                drag.MarkDropped();
                if (drag.Index != index)
                {
                    string here = _itemBySlot[index];
                    _ctx.Commands.SetConsumableSlot(index, drag.Id);
                    _ctx.Commands.SetConsumableSlot(drag.Index, here);
                }
            }
        }
    }
}
