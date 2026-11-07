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
    /// <summary>Character window (02, 7.13): equipment slots (click to unequip) and final stats (defense, crit, both mana pools, ...).</summary>
    public sealed class CharacterWindow : HudWindow
    {
        private const float EquipmentColumnShare = 0.55f;

        private readonly Dictionary<string, Text> _slotRows = new Dictionary<string, Text>();
        private readonly Dictionary<string, string> _slotItems = new Dictionary<string, string>();
        private readonly RectTransform _statsList;
        private readonly List<Text> _statRows = new List<Text>();

        public CharacterWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close)
        {
            RectTransform left = UiFactory.Rect("Equipment", Content);
            left.anchorMin = Vector2.zero;
            left.anchorMax = new Vector2(EquipmentColumnShare, 1f);
            left.offsetMin = Vector2.zero;
            left.offsetMax = new Vector2(-HudLayout.Gap, 0f);
            RectTransform equipmentList = VerticalList("List", left);
            Text header = Row("Header", equipmentList);
            header.text = ctx.Text.Get(UiKeys.EquipmentHeader);
            header.color = HudPalette.Cast;
            foreach (EquipmentSlotDto slot in ctx.Content.EquipmentSlots)
            {
                Text row = Row(slot.Id, equipmentList);
                row.raycastTarget = true;
                HoverTrigger hover = row.gameObject.AddComponent<HoverTrigger>();
                string slotId = slot.Id;
                hover.Entered = () => ShowSlotTooltip(slotId);
                hover.Exited = Tooltip.Hide;
                hover.Clicked = button =>
                {
                    if (button == PointerEventData.InputButton.Left && _slotItems.ContainsKey(slotId))
                    {
                        Ctx.Commands.Unequip(slotId);
                        Tooltip.Hide();
                    }
                };
                _slotRows[slot.Id] = row;
            }

            RectTransform right = UiFactory.Rect("Stats", Content);
            right.anchorMin = new Vector2(EquipmentColumnShare, 0f);
            right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(HudLayout.Gap, 0f);
            right.offsetMax = Vector2.zero;
            _statsList = VerticalList("List", right);
            Text statsHeader = Row("Header", _statsList);
            statsHeader.text = ctx.Text.Get(UiKeys.StatsHeader);
            statsHeader.color = HudPalette.Cast;
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            _slotItems.Clear();
            var available = new HashSet<string>();
            foreach (EquipmentSlotSnapshot slot in state.Equipment)
            {
                available.Add(slot.SlotId);
                if (slot.DefinitionId != null)
                {
                    _slotItems[slot.SlotId] = slot.DefinitionId;
                }
            }
            foreach (EquipmentSlotDto slot in Ctx.Content.EquipmentSlots)
            {
                Text row = _slotRows[slot.Id];
                row.gameObject.SetActive(available.Contains(slot.Id));
                string slotName = Ctx.Text.Get(slot.NameKey);
                if (_slotItems.TryGetValue(slot.Id, out string itemId))
                {
                    ItemDefinition item = Ctx.Content.Items[itemId];
                    row.text = slotName + ": " + Ctx.Text.Get(item.NameKey);
                    row.color = HudPalette.ForRarity(item.Rarity);
                }
                else
                {
                    row.text = slotName + ": " + Ctx.Text.Get(UiKeys.Empty);
                    row.color = HudPalette.TextDim;
                }
            }

            while (_statRows.Count < state.Stats.Length)
            {
                _statRows.Add(Row("Stat", _statsList));
            }
            for (int i = 0; i < state.Stats.Length; i++)
            {
                StatSnapshot stat = state.Stats[i];
                _statRows[i].text = StatFormat.Name(Ctx.Text, stat.Stat) + ": " + StatFormat.Value(Ctx.Text, stat.Stat, stat.Value);
            }
        }

        private void ShowSlotTooltip(string slotId)
        {
            if (_slotItems.TryGetValue(slotId, out string itemId))
            {
                Tooltip.Show(TooltipText.Item(Ctx, Ctx.Content.Items[itemId], 1, UiKeys.ClickToUnequip));
            }
        }
    }
}
