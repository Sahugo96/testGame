using System.Collections.Generic;
using Game.Client.Localization;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Consumable panel (02, 7.7 Б): slots auto-filled with the consumable stacks from the bag; counter and group cooldown.
    /// Manual assignment arrives with drag &amp; drop (module 18).
    /// </summary>
    public sealed class ConsumableBarWidget
    {
        private readonly ClientContext _ctx;
        private readonly Tooltip _tooltip;
        private readonly List<SlotView> _slots = new List<SlotView>();
        private readonly int[] _bagIndexBySlot;

        public ConsumableBarWidget(RectTransform zone, ClientContext ctx, Tooltip tooltip, float bottomOffset)
        {
            _ctx = ctx;
            _tooltip = tooltip;
            int count = ctx.Content.Config.ConsumableBarSlots;
            _bagIndexBySlot = new int[count];
            float step = HudLayout.ConsumableSlotSize + HudLayout.Gap;
            float width = step * count - HudLayout.Gap;
            for (int i = 0; i < count; i++)
            {
                var slot = new SlotView($"ConsumableSlot{i}", zone, ctx.Font, HudLayout.ConsumableSlotSize);
                UiFactory.Place(slot.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                    new Vector2(-width * 0.5f + i * step, bottomOffset), new Vector2(HudLayout.ConsumableSlotSize, HudLayout.ConsumableSlotSize));
                int captured = i;
                slot.Bind(() => OnEnter(captured), _tooltip.Hide, button => OnClick(captured, button));
                _slots.Add(slot);
                _bagIndexBySlot[i] = -1;
            }
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            if (state == null)
            {
                return;
            }
            int slot = 0;
            var seen = new HashSet<string>();
            foreach (ItemSlotSnapshot bagSlot in state.Bag)
            {
                if (slot >= _slots.Count)
                {
                    break;
                }
                if (bagSlot.DefinitionId == null || seen.Contains(bagSlot.DefinitionId))
                {
                    continue;
                }
                ItemDefinition item = _ctx.Content.Items[bagSlot.DefinitionId];
                ConsumableComponent consumable = item.Get<ConsumableComponent>();
                if (consumable == null)
                {
                    continue;
                }
                seen.Add(bagSlot.DefinitionId);
                _bagIndexBySlot[slot] = bagSlot.Index;
                SlotView view = _slots[slot];
                view.SetFilled(true);
                view.SetCaption(_ctx.Text.Get(item.NameKey), HudPalette.Text);
                view.SetCount(TotalCount(state, bagSlot.DefinitionId).ToString());
                view.SetCooldown(GroupCooldown(state, consumable));
                slot++;
            }
            for (; slot < _slots.Count; slot++)
            {
                _bagIndexBySlot[slot] = -1;
                _slots[slot].SetFilled(false);
                _slots[slot].SetCaption(string.Empty, HudPalette.Text);
                _slots[slot].SetCount(string.Empty);
                _slots[slot].SetCooldown(0f);
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
            int bagIndex = _bagIndexBySlot[index];
            if (state == null || bagIndex < 0)
            {
                return;
            }
            ItemSlotSnapshot slot = state.Bag[bagIndex];
            _tooltip.Show(TooltipText.Item(_ctx, _ctx.Content.Items[slot.DefinitionId], slot.Count, UiKeys.ClickToUse));
        }

        private void OnClick(int index, PointerEventData.InputButton button)
        {
            int bagIndex = _bagIndexBySlot[index];
            if (bagIndex >= 0 && button == PointerEventData.InputButton.Left)
            {
                _ctx.Commands.UseConsumable(bagIndex);
            }
        }
    }
}
