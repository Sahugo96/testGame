using System;
using System.Collections.Generic;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Backpack with sections as tabs (developer request v11): equipment, consumables, loot, resources, quest items —
    /// from bag_sections.json. Each section has its own capacity; the server puts items into the section of their category.
    /// Click equips equipment or uses a consumable; tooltip shows details.
    /// </summary>
    public sealed class BackpackWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(460f, 560f);
        private const float TabHeight = 30f;

        private readonly List<SlotView> _slots = new List<SlotView>();
        private readonly List<Text> _tabLabels = new List<Text>();
        private int _section;
        private readonly Text _currencies;

        public BackpackWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            int sections = ctx.Content.BagSections.Count;
            for (int i = 0; i < sections; i++)
            {
                int index = i;
                Button tab = UiFactory.Button($"Tab_{ctx.Content.BagSections[i].Id}", Content, ctx.Font,
                    ctx.Text.Get(ctx.Content.BagSections[i].NameKey), () => SelectSection(index));
                var rect = (RectTransform)tab.transform;
                rect.anchorMin = new Vector2((float)i / sections, 1f);
                rect.anchorMax = new Vector2((float)(i + 1) / sections, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(HudLayout.Gap * 0.5f, -TabHeight);
                rect.offsetMax = new Vector2(-HudLayout.Gap * 0.5f, 0f);
                Text label = tab.GetComponentInChildren<Text>();
                label.resizeTextForBestFit = true;
                label.resizeTextMaxSize = HudLayout.FontNormal;
                _tabLabels.Add(label);
            }

            RectTransform gridRect = UiFactory.Rect("Grid", Content);
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(0f, HudLayout.RowHeight + HudLayout.Gap);
            _currencies = UiFactory.Label("Currencies", Content, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Cast);
            UiFactory.Place(_currencies.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(Size.x - HudLayout.Margin * 2f, HudLayout.RowHeight));
            gridRect.offsetMax = new Vector2(0f, -TabHeight - HudLayout.Gap);
            GridLayoutGroup grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(HudLayout.BagSlotSize, HudLayout.BagSlotSize);
            grid.spacing = new Vector2(HudLayout.Gap, HudLayout.Gap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = HudLayout.BagColumns;
            int largest = 0;
            foreach (BagSection section in ctx.Content.BagSections)
            {
                largest = Math.Max(largest, section.Size);
            }
            for (int i = 0; i < largest; i++)
            {
                var slot = new SlotView($"Bag{i}", gridRect, ctx.Font, HudLayout.BagSlotSize);
                int local = i;
                slot.Bind(() => ShowTooltip(local), Tooltip.Hide, button => OnClick(local, button));
                _slots.Add(slot);
            }
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            // 05, 7.14.2: the backpack shows every currency.
            var line = new System.Text.StringBuilder();
            foreach (CurrencySnapshot currency in state.Currencies ?? new CurrencySnapshot[0])
            {
                string name = Ctx.Content.Currencies.TryGetValue(currency.CurrencyId, out var definition) ? Ctx.Text.Get(definition.NameKey) : currency.CurrencyId;
                line.Append(Ctx.Text.Format(UiKeys.CurrencyLine, name, currency.Amount)).Append("   ");
            }
            _currencies.text = line.ToString();
            BagSection section = Ctx.Content.BagSections[_section];
            for (int t = 0; t < _tabLabels.Count; t++)
            {
                BagSection tabSection = Ctx.Content.BagSections[t];
                _tabLabels[t].text = Ctx.Text.Get(tabSection.NameKey) + " " + Ctx.Text.Format(UiKeys.ValueOfMax, Used(state, tabSection), tabSection.Size);
                _tabLabels[t].color = t == _section ? HudPalette.Cast : HudPalette.Text;
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                SlotView view = _slots[i];
                bool inSection = i < section.Size;
                view.Rect.gameObject.SetActive(inSection);
                if (!inSection)
                {
                    continue;
                }
                ItemSlotSnapshot bagSlot = state.Bag[section.Start + i];
                if (bagSlot.DefinitionId == null)
                {
                    view.SetFilled(false);
                    view.SetCaption(string.Empty, HudPalette.Text);
                    view.SetCount(string.Empty);
                    continue;
                }
                ItemDefinition item = Ctx.Content.Items[bagSlot.DefinitionId];
                view.SetFilled(true);
                view.SetCaption(TooltipText.ItemName(Ctx, item, bagSlot.UpgradeLevel), HudPalette.ForRarity(item.Rarity));
                view.SetCount(bagSlot.Count > 1 ? bagSlot.Count.ToString() : string.Empty);
            }
        }

        private void SelectSection(int index)
        {
            _section = index;
            Tooltip.Hide();
            if (Ctx.State.Player != null)
            {
                Refresh(Ctx.State.Player);
            }
        }

        private static int Used(PlayerStateSnapshot state, BagSection section)
        {
            int used = 0;
            for (int i = section.Start; i < section.Start + section.Size && i < state.Bag.Length; i++)
            {
                if (state.Bag[i].DefinitionId != null)
                {
                    used++;
                }
            }
            return used;
        }

        private bool TryGetSlot(int local, out ItemSlotSnapshot slot)
        {
            slot = default;
            PlayerStateSnapshot state = Ctx.State.Player;
            BagSection section = Ctx.Content.BagSections[_section];
            if (state == null || local >= section.Size)
            {
                return false;
            }
            slot = state.Bag[section.Start + local];
            return slot.DefinitionId != null;
        }

        private void ShowTooltip(int local)
        {
            if (!TryGetSlot(local, out ItemSlotSnapshot slot))
            {
                return;
            }
            ItemDefinition item = Ctx.Content.Items[slot.DefinitionId];
            string hint = item.Has<EquipmentComponent>() ? UiKeys.ClickToEquip : item.Has<ConsumableComponent>() ? UiKeys.ClickToUse : null;
            Tooltip.Show(TooltipText.Item(Ctx, item, slot.Count, hint, slot.UpgradeLevel, slot.Sockets, slot.Affixes));
        }

        private void OnClick(int local, PointerEventData.InputButton button)
        {
            if (button != PointerEventData.InputButton.Left || !TryGetSlot(local, out ItemSlotSnapshot slot))
            {
                return;
            }
            ItemDefinition item = Ctx.Content.Items[slot.DefinitionId];
            if (item.Has<EquipmentComponent>())
            {
                Ctx.Commands?.Equip(slot.Index);
            }
            else if (item.Has<ConsumableComponent>())
            {
                Ctx.Commands?.UseConsumable(slot.Index);
            }
            Tooltip.Hide();
        }
    }
}
