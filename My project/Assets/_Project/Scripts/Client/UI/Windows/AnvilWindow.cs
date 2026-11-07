using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Core;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Anvil (04, 7.14.1; developer request v11): pick an equipped item or one from the equipment section, see the next
    /// level, success chance and material, press "Upgrade". The server decides the result. The window closes when the
    /// player walks away from the anvil or enters combat (02, 7.13).
    /// </summary>
    public sealed class AnvilWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(760f, 470f);
        private const float ListWidth = 360f;
        private const float PercentFactor = 100f;
        private const string EquipmentSectionId = "equipment";

        private readonly RectTransform _list;
        private readonly List<Text> _rows = new List<Text>();
        private readonly List<string> _rowInstances = new List<string>();
        private readonly Text _details;
        private readonly Button _upgradeButton;
        private string _selected;

        public AnvilWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            RectTransform left = UiFactory.Rect("Items", Content);
            left.anchorMin = Vector2.zero;
            left.anchorMax = new Vector2(0f, 1f);
            left.pivot = new Vector2(0f, 0.5f);
            left.offsetMin = Vector2.zero;
            left.offsetMax = new Vector2(ListWidth, 0f);
            _list = VerticalList("List", left);

            RectTransform right = UiFactory.Rect("Details", Content);
            right.anchorMin = Vector2.zero;
            right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(ListWidth + HudLayout.Margin, 0f);
            right.offsetMax = Vector2.zero;
            _details = UiFactory.Label("Text", right, ctx.Font, HudLayout.FontNormal, TextAnchor.UpperLeft, HudPalette.Text);
            _details.supportRichText = true;
            UiFactory.Stretch(_details.rectTransform);
            _details.rectTransform.offsetMin = new Vector2(0f, HudLayout.MenuButtonHeight + HudLayout.Gap);
            _upgradeButton = UiFactory.Button("Upgrade", right, ctx.Font, ctx.Text.Get(UiKeys.UpgradeButton), Upgrade);
            UiFactory.Place((RectTransform)_upgradeButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero,
                new Vector2(HudLayout.MenuButtonWidth * 2f, HudLayout.MenuButtonHeight));
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            if (!IsNearStation(state) || state.InCombat)
            {
                RequestClose();
                return;
            }
            var entries = new List<(string instanceId, string definitionId, int level)>();
            foreach (EquipmentSlotSnapshot slot in state.Equipment)
            {
                if (IsUpgradeable(slot.DefinitionId))
                {
                    entries.Add((slot.InstanceId, slot.DefinitionId, slot.UpgradeLevel));
                }
            }
            BagSection section = Ctx.Content.BagSections.Find(s => s.Id == EquipmentSectionId);
            foreach (ItemSlotSnapshot slot in state.Bag)
            {
                if ((section == null || section.Contains(slot.Index)) && IsUpgradeable(slot.DefinitionId))
                {
                    entries.Add((slot.InstanceId, slot.DefinitionId, slot.UpgradeLevel));
                }
            }

            while (_rows.Count < entries.Count)
            {
                Text row = Row("Item", _list);
                row.raycastTarget = true;
                HoverTrigger hover = row.gameObject.AddComponent<HoverTrigger>();
                int index = _rows.Count;
                hover.Clicked = button =>
                {
                    if (button == PointerEventData.InputButton.Left && index < _rowInstances.Count)
                    {
                        _selected = _rowInstances[index];
                        Refresh(Ctx.State.Player);
                    }
                };
                _rows.Add(row);
            }
            _rowInstances.Clear();
            bool selectedFound = false;
            for (int i = 0; i < _rows.Count; i++)
            {
                bool visible = i < entries.Count;
                _rows[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }
                var (instanceId, definitionId, level) = entries[i];
                ItemDefinition item = Ctx.Content.Items[definitionId];
                _rowInstances.Add(instanceId);
                bool selected = instanceId == _selected;
                selectedFound |= selected;
                _rows[i].text = (selected ? "▶ " : string.Empty) + TooltipText.ItemName(Ctx, item, level);
                _rows[i].color = HudPalette.ForRarity(item.Rarity);
            }
            if (!selectedFound)
            {
                _selected = entries.Count > 0 ? entries[0].instanceId : null;
            }
            ShowDetails(entries);
        }

        private void ShowDetails(List<(string instanceId, string definitionId, int level)> entries)
        {
            var entry = entries.Find(e => e.instanceId == _selected);
            if (entry.instanceId == null)
            {
                _details.text = Ctx.Text.Get(UiKeys.AnvilEmpty);
                _upgradeButton.interactable = false;
                return;
            }
            ItemDefinition item = Ctx.Content.Items[entry.definitionId];
            UpgradeRuleDto rule = Ctx.Content.UpgradeRules[item.Get<UpgradeableComponent>().RulesId];
            ILocalizationService text = Ctx.Text;
            string name = TooltipText.ItemName(Ctx, item, entry.level);
            if (entry.level >= rule.MaxLevel)
            {
                _details.text = "<b>" + name + "</b>\n\n" + text.Get(UiKeys.AnvilMaxLevel);
                _upgradeButton.interactable = false;
                return;
            }
            UpgradeStepDto step = rule.Steps[entry.level];
            int have = CountInBag(rule.MaterialItemId);
            string material = text.Get(Ctx.Content.Items[rule.MaterialItemId].NameKey);
            _details.text = "<b>" + name + "</b>\n\n"
                + text.Format(UiKeys.AnvilNextLevel, entry.level, entry.level + 1) + "\n"
                + text.Format(UiKeys.AnvilChance, (step.SuccessChance * PercentFactor).ToString("0", CultureInfo.InvariantCulture)) + "\n"
                + text.Format(UiKeys.AnvilMaterial, material, have, rule.MaterialCount) + "\n"
                + (step.LevelLossOnFail > 0 ? text.Format(UiKeys.AnvilFailLoss, step.LevelLossOnFail) + "\n" : string.Empty)
                + text.Format(UiKeys.AnvilStatBonus, (rule.StatBonusPerLevel * PercentFactor).ToString("0", CultureInfo.InvariantCulture));
            _upgradeButton.interactable = have >= rule.MaterialCount;
        }

        private void Upgrade()
        {
            if (_selected != null)
            {
                Ctx.Commands.UpgradeItem(_selected, Ctx.ActiveStation);
            }
        }

        private bool IsUpgradeable(string definitionId) =>
            definitionId != null && Ctx.Content.Items.TryGetValue(definitionId, out ItemDefinition item)
            && item.Has<UpgradeableComponent>() && item.Rarity != Rarity.Divine;

        private int CountInBag(string definitionId)
        {
            int total = 0;
            foreach (ItemSlotSnapshot slot in Ctx.State.Player.Bag)
            {
                if (slot.DefinitionId == definitionId)
                {
                    total += slot.Count;
                }
            }
            return total;
        }

        private bool IsNearStation(PlayerStateSnapshot state)
        {
            if (!Ctx.State.TryGetEntity(Ctx.ActiveStation, out EntitySnapshot station)
                || !Ctx.State.TryGetEntity(state.EntityId, out EntitySnapshot player))
            {
                return false;
            }
            return Vec3.DistanceXZ(station.Position, player.Position) <= Ctx.Content.Config.InteractRange;
        }
    }
}
