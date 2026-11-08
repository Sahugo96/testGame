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
    /// Whetstone (04, 7.14.1; developer requests v11 and v15 — upgrading is done at the whetstone, the anvil crafts): pick an
    /// equipped item or one from the equipment section, see the next level, success chance and material, press "Upgrade".
    /// The server decides the result. The window closes when the player walks away from the station or enters combat (02, 7.13).
    /// </summary>
    public sealed class WhetstoneWindow : HudWindow
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

        public WhetstoneWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
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
            _details.rectTransform.offsetMin = new Vector2(0f, (HudLayout.MenuButtonHeight + HudLayout.Gap) * 2f);
            _socketButton = UiFactory.Button("Socket", right, ctx.Font, string.Empty, InsertIngredient);
            UiFactory.Place((RectTransform)_socketButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, HudLayout.MenuButtonHeight + HudLayout.Gap),
                new Vector2(HudLayout.MenuButtonWidth * 3f, HudLayout.MenuButtonHeight));
            _socketLabel = _socketButton.GetComponentInChildren<Text>();
            _socketLabel.resizeTextForBestFit = true;
            _socketLabel.resizeTextMaxSize = HudLayout.FontNormal;
            _upgradeButton = UiFactory.Button("Upgrade", right, ctx.Font, ctx.Text.Get(UiKeys.UpgradeButton), Upgrade);
            UiFactory.Place((RectTransform)_upgradeButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero,
                new Vector2(HudLayout.MenuButtonWidth * 2f, HudLayout.MenuButtonHeight));
            // Module 20: upgrade orders with escrow at the same whetstone.
            Button orders = UiFactory.Button("Orders", right, ctx.Font, ctx.Text.Get(UiKeys.OrdersButton), () => Ctx.RequestWindow(WindowFactory.UpgradeOrders));
            UiFactory.Place((RectTransform)orders.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(HudLayout.MenuButtonWidth, HudLayout.MenuButtonHeight));
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            if (!IsNearActiveStation(state) || state.InCombat)
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

        private readonly Button _socketButton;
        private readonly Text _socketLabel;
        private int _socketIndex = -1;
        private int _ingredientIndex = -1;

        /// <summary>
        /// Sockets of the selected item (04, 7.9): the button inserts the first fitting ingredient from the bag into the first
        /// empty fitting socket, or replaces one (the old ingredient is destroyed — the label says so).
        /// </summary>
        private string ShowSockets(string instanceId)
        {
            _socketIndex = -1;
            _ingredientIndex = -1;
            SocketSnapshot[] sockets = SocketsOf(instanceId);
            _socketButton.gameObject.SetActive(sockets.Length > 0);
            if (sockets.Length == 0)
            {
                return string.Empty;
            }
            ILocalizationService text = Ctx.Text;
            var sb = new System.Text.StringBuilder("\n");
            bool replacing = false;
            for (int pass = 0; pass < 2 && _socketIndex < 0; pass++)
            {
                for (int i = 0; i < sockets.Length && _socketIndex < 0; i++)
                {
                    bool empty = sockets[i].IngredientId == null;
                    if ((pass == 0) != empty)
                    {
                        continue;
                    }
                    int ingredient = FindIngredient(sockets[i].Type);
                    if (ingredient >= 0)
                    {
                        _socketIndex = i;
                        _ingredientIndex = ingredient;
                        replacing = !empty;
                    }
                }
            }
            foreach (SocketSnapshot socket in sockets)
            {
                string inside = socket.IngredientId != null ? text.Get(Ctx.Content.Items[socket.IngredientId].NameKey) : text.Get(UiKeys.SocketEmpty);
                sb.Append(text.Format(UiKeys.SocketLine, text.Get(UiKeys.SocketTypePrefix + socket.Type), inside)).Append('\n');
            }
            if (_socketIndex < 0)
            {
                _socketLabel.text = text.Get(UiKeys.NoIngredient);
                _socketButton.interactable = false;
            }
            else
            {
                string name = text.Get(Ctx.Content.Items[Ctx.State.Player.Bag[_ingredientIndex].DefinitionId].NameKey);
                _socketLabel.text = text.Format(replacing ? UiKeys.ReplaceIngredient : UiKeys.InsertIngredient, name, _socketIndex + 1);
                _socketButton.interactable = true;
            }
            return sb.ToString();
        }

        private SocketSnapshot[] SocketsOf(string instanceId)
        {
            PlayerStateSnapshot state = Ctx.State.Player;
            foreach (EquipmentSlotSnapshot slot in state.Equipment)
            {
                if (slot.InstanceId == instanceId)
                {
                    return slot.Sockets ?? new SocketSnapshot[0];
                }
            }
            foreach (ItemSlotSnapshot slot in state.Bag)
            {
                if (slot.InstanceId == instanceId)
                {
                    return slot.Sockets ?? new SocketSnapshot[0];
                }
            }
            return new SocketSnapshot[0];
        }

        private int FindIngredient(string socketType)
        {
            ItemSlotSnapshot[] bag = Ctx.State.Player.Bag;
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i].DefinitionId == null)
                {
                    continue;
                }
                SocketIngredientComponent ingredient = Ctx.Content.Items[bag[i].DefinitionId].Get<SocketIngredientComponent>();
                if (ingredient != null && (ingredient.SocketType == socketType || socketType == Ctx.Content.Config.UniversalSocketType))
                {
                    return i;
                }
            }
            return -1;
        }

        private void InsertIngredient()
        {
            if (_selected != null && _socketIndex >= 0 && _ingredientIndex >= 0)
            {
                Ctx.Commands?.InsertIngredient(_selected, _socketIndex, _ingredientIndex, Ctx.ActiveStation);
            }
        }

        private void ShowDetails(List<(string instanceId, string definitionId, int level)> entries)
        {
            var entry = entries.Find(e => e.instanceId == _selected);
            if (entry.instanceId == null)
            {
                _details.text = Ctx.Text.Get(UiKeys.WhetstoneEmpty);
                _upgradeButton.interactable = false;
                _socketButton.gameObject.SetActive(false);
                return;
            }
            string socketsText = ShowSockets(entry.instanceId);
            ItemDefinition item = Ctx.Content.Items[entry.definitionId];
            UpgradeRuleDto rule = Ctx.Content.UpgradeRules[item.Get<UpgradeableComponent>().RulesId];
            ILocalizationService text = Ctx.Text;
            string name = TooltipText.ItemName(Ctx, item, entry.level);
            if (entry.level >= rule.MaxLevel)
            {
                _details.text = "<b>" + name + "</b>\n\n" + text.Get(UiKeys.WhetstoneMaxLevel) + "\n" + socketsText;
                _upgradeButton.interactable = false;
                return;
            }
            UpgradeStepDto step = rule.Steps[entry.level];
            int have = CountInBag(rule.MaterialItemId);
            string material = text.Get(Ctx.Content.Items[rule.MaterialItemId].NameKey);
            _details.text = "<b>" + name + "</b>\n\n"
                + text.Format(UiKeys.WhetstoneNextLevel, entry.level, entry.level + 1) + "\n"
                + text.Format(UiKeys.WhetstoneChance, (step.SuccessChance * PercentFactor).ToString("0", CultureInfo.InvariantCulture)) + "\n"
                + text.Format(UiKeys.WhetstoneMaterial, material, have, rule.MaterialCount) + "\n"
                + (step.LevelLossOnFail > 0 ? text.Format(UiKeys.WhetstoneFailLoss, step.LevelLossOnFail) + "\n" : string.Empty)
                + text.Format(UiKeys.GoldCostLine, step.GoldCost, Gold()) + "\n"
                + text.Format(UiKeys.WhetstoneStatBonus, (rule.StatBonusPerLevel * PercentFactor).ToString("0", CultureInfo.InvariantCulture))
                + "\n" + socketsText;
            _upgradeButton.interactable = have >= rule.MaterialCount && Gold() >= step.GoldCost;
        }

        private long Gold()
        {
            foreach (CurrencySnapshot currency in Ctx.State.Player.Currencies ?? new CurrencySnapshot[0])
            {
                if (currency.CurrencyId == Ctx.Content.Config.GoldCurrencyId)
                {
                    return currency.Amount;
                }
            }
            return 0L;
        }

        private void Upgrade()
        {
            if (_selected != null)
            {
                Ctx.Commands?.UpgradeItem(_selected, Ctx.ActiveStation);
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
    }
}
