using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Client.World;
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
    /// Character window (developer reference, 02 7.13): clothing slots on the left, jewelry on the right, weapons below,
    /// a rotating portrait in the middle; on the right, tabs "Main" (health, both mana pools, attributes, combat stats)
    /// and "Extra" (regeneration, speed). Slot columns come from the slot groups in equipment_slots.json.
    /// Click on an equipped slot unequips it.
    /// </summary>
    public sealed class CharacterWindow : HudWindow
    {
        private const string ClothingGroup = "Clothing";
        private const string JewelryGroup = "Jewelry";
        private const string WeaponGroup = "Weapon";
        private static readonly Vector2 Size = new Vector2(860f, 580f);
        private const float EquipmentWidth = 470f;
        private const float SlotSize = 64f;
        private const float TitleHeight = 46f;
        private const float TabHeight = 30f;
        private const float PlusButtonSize = 22f;
        private const string PlusCaption = "+";
        private static readonly StatId[] Attributes = { StatId.Strength, StatId.Intelligence, StatId.Stamina };
        private static readonly StatId[] Characteristics = { StatId.DamageBonus, StatId.Defense, StatId.CritChance, StatId.CritSize };
        private static readonly StatId[] Extra = { StatId.HealthRegen, StatId.MainManaRegen, StatId.MoveSpeed };

        private readonly Dictionary<string, SlotView> _slots = new Dictionary<string, SlotView>();
        private readonly Dictionary<string, string> _equipped = new Dictionary<string, string>();
        private readonly Dictionary<string, Text> _values = new Dictionary<string, Text>();
        private readonly List<Text> _attributeValues = new List<Text>();
        private readonly List<Button> _attributeButtons = new List<Button>();
        private readonly Text _points;
        private readonly List<Text> _characteristicValues = new List<Text>();
        private readonly List<Text> _extraValues = new List<Text>();
        private readonly RectTransform _equipmentArea;
        private readonly Text _title;
        private readonly RectTransform _mainTab;
        private readonly RectTransform _extraTab;
        private readonly CharacterPreview _preview;
        private string _layoutKey;
        private string _previewKey;

        public CharacterWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            _equipmentArea = UiFactory.Rect("Equipment", Content);
            _equipmentArea.anchorMin = Vector2.zero;
            _equipmentArea.anchorMax = new Vector2(0f, 1f);
            _equipmentArea.pivot = new Vector2(0f, 0.5f);
            _equipmentArea.offsetMin = Vector2.zero;
            _equipmentArea.offsetMax = new Vector2(EquipmentWidth, 0f);

            _title = UiFactory.Label("Title", _equipmentArea, ctx.Font, HudLayout.FontLarge, TextAnchor.UpperCenter, HudPalette.Text);
            UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
                new Vector2(EquipmentWidth - 2f * (SlotSize + HudLayout.Gap), TitleHeight));

            _preview = CharacterPreview.Create();
            _preview.SetBodyColor(WorldPalette.LocalPlayer);
            RectTransform portrait = UiFactory.Rect("Portrait", _equipmentArea);
            portrait.anchorMin = Vector2.zero;
            portrait.anchorMax = Vector2.one;
            portrait.offsetMin = new Vector2(SlotSize + HudLayout.Gap * 2f, SlotSize + HudLayout.Gap * 2f);
            portrait.offsetMax = new Vector2(-SlotSize - HudLayout.Gap * 2f, -TitleHeight);
            RawImage image = portrait.gameObject.AddComponent<RawImage>();
            image.texture = _preview.Texture;
            image.raycastTarget = false;

            foreach (EquipmentSlotDto slot in ctx.Content.EquipmentSlots)
            {
                var view = new SlotView($"Slot_{slot.Id}", _equipmentArea, ctx.Font, SlotSize);
                string slotId = slot.Id;
                view.Bind(() => ShowSlotTooltip(slotId), Tooltip.Hide, button => Unequip(slotId, button));
                _slots[slot.Id] = view;
            }

            RectTransform stats = UiFactory.Rect("Stats", Content);
            stats.anchorMin = Vector2.zero;
            stats.anchorMax = Vector2.one;
            stats.offsetMin = new Vector2(EquipmentWidth + HudLayout.Margin, 0f);
            stats.offsetMax = Vector2.zero;
            Button mainButton = TabButton(stats, UiKeys.TabMain, 0f);
            Button extraButton = TabButton(stats, UiKeys.TabExtra, 0.5f);
            _mainTab = TabPage("MainTab", stats);
            _extraTab = TabPage("ExtraTab", stats);
            mainButton.onClick.AddListener(() => ShowTab(true));
            extraButton.onClick.AddListener(() => ShowTab(false));

            RectTransform mainList = VerticalList("List", _mainTab);
            _values[UiKeys.Health] = Pair(mainList, ctx.Text.Get(UiKeys.Health));
            _values[UiKeys.MainMana] = Pair(mainList, ctx.Text.Get(UiKeys.MainMana));
            _values[UiKeys.ReserveMana] = Pair(mainList, ctx.Text.Get(UiKeys.ReserveMana));
            Header(mainList, UiKeys.ParamsHeader);
            _points = Row("Points", mainList);
            _points.color = HudPalette.Positive;
            foreach (StatId stat in Attributes)
            {
                Text value = Pair(mainList, StatFormat.Name(ctx.Text, stat));
                value.rectTransform.offsetMax = new Vector2(-PlusButtonSize - HudLayout.Gap, 0f);
                StatId attribute = stat;
                Button plus = UiFactory.Button("Plus", value.transform.parent, ctx.Font, PlusCaption, () => Ctx.Commands.SpendAttributePoint(attribute));
                UiFactory.Place((RectTransform)plus.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(PlusButtonSize, PlusButtonSize));
                _attributeValues.Add(value);
                _attributeButtons.Add(plus);
            }
            Header(mainList, UiKeys.CharacteristicsHeader);
            foreach (StatId stat in Characteristics)
            {
                _characteristicValues.Add(Pair(mainList, StatFormat.Name(ctx.Text, stat)));
            }

            RectTransform extraList = VerticalList("List", _extraTab);
            foreach (StatId stat in Extra)
            {
                _extraValues.Add(Pair(extraList, StatFormat.Name(ctx.Text, stat)));
            }
            _values[UiKeys.ReserveRegen] = Pair(extraList, ctx.Text.Get(UiKeys.ReserveRegen));
            ShowTab(true);
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            ILocalizationService text = Ctx.Text;
            string className = Ctx.Content.Classes.TryGetValue(state.ClassId, out ClassDefinition cls) ? text.Get(cls.NameKey) : state.ClassId;
            _title.text = className + "\n" + text.Format(UiKeys.LevelLong, state.Level);

            LayoutSlots(state);
            _equipped.Clear();
            var previewParts = new List<KeyValuePair<string, Color>>();
            foreach (EquipmentSlotSnapshot slot in state.Equipment)
            {
                if (!_slots.TryGetValue(slot.SlotId, out SlotView view))
                {
                    continue;
                }
                EquipmentSlotDto slotDefinition = Ctx.Content.EquipmentSlots.Find(s => s.Id == slot.SlotId);
                if (slot.DefinitionId != null)
                {
                    ItemDefinition item = Ctx.Content.Items[slot.DefinitionId];
                    _equipped[slot.SlotId] = slot.DefinitionId;
                    view.SetFilled(true);
                    view.SetCaption(TooltipText.ItemName(Ctx, item, slot.UpgradeLevel), HudPalette.ForRarity(item.Rarity));
                    previewParts.Add(new KeyValuePair<string, Color>(slotDefinition.SlotType, HudPalette.ForRarity(item.Rarity)));
                }
                else
                {
                    view.SetFilled(false);
                    view.SetCaption(text.Get(slotDefinition.NameKey), HudPalette.TextDim);
                }
            }
            string previewKey = string.Join("|", _equipped.Values);
            if (previewKey != _previewKey)
            {
                _previewKey = previewKey;
                _preview.ShowEquipment(previewParts);
            }

            _values[UiKeys.Health].text = text.Format(UiKeys.ValueOfMax, Mathf.CeilToInt(state.Health), Mathf.RoundToInt(state.MaxHealth));
            _values[UiKeys.MainMana].text = text.Format(UiKeys.ValueOfMax, Mathf.CeilToInt(state.MainMana), Mathf.RoundToInt(state.MaxMainMana));
            _values[UiKeys.ReserveMana].text = text.Format(UiKeys.ValueOfMax, Mathf.CeilToInt(state.ReserveMana), Mathf.RoundToInt(state.MaxReserveMana));
            Fill(state, Attributes, _attributeValues);
            _points.text = text.Format(UiKeys.AttributePoints, state.AttributePoints) + "   " + text.Format(UiKeys.ClassPoints, state.ClassPoints);
            foreach (Button plus in _attributeButtons)
            {
                plus.gameObject.SetActive(state.AttributePoints > 0);
            }
            Fill(state, Characteristics, _characteristicValues);
            Fill(state, Extra, _extraValues);
            float reserveRegen = StatValue(state, StatId.MainManaRegen) * Ctx.Content.Config.ReserveRegenOutOfCombatFactor;
            _values[UiKeys.ReserveRegen].text = reserveRegen.ToString("0.##", CultureInfo.InvariantCulture);
        }

        protected override void OnVisibilityChanged(bool open) => _preview.SetVisible(open);

        /// <summary>Places slots of the class: clothing on the left, jewelry on the right, weapons centered below.</summary>
        private void LayoutSlots(PlayerStateSnapshot state)
        {
            var available = new HashSet<string>();
            foreach (EquipmentSlotSnapshot slot in state.Equipment)
            {
                available.Add(slot.SlotId);
            }
            string key = state.ClassId + ":" + available.Count;
            if (key == _layoutKey)
            {
                return;
            }
            _layoutKey = key;
            int left = 0;
            int right = 0;
            var weapons = new List<string>();
            float step = SlotSize + HudLayout.Gap;
            foreach (EquipmentSlotDto slot in Ctx.Content.EquipmentSlots)
            {
                SlotView view = _slots[slot.Id];
                bool shown = available.Contains(slot.Id);
                view.Rect.gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }
                switch (slot.Group)
                {
                    case ClothingGroup:
                        UiFactory.Place(view.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -left++ * step), new Vector2(SlotSize, SlotSize));
                        break;
                    case JewelryGroup:
                        UiFactory.Place(view.Rect, Vector2.one, Vector2.one, new Vector2(0f, -right++ * step), new Vector2(SlotSize, SlotSize));
                        break;
                    case WeaponGroup:
                        weapons.Add(slot.Id);
                        break;
                    default:
                        UiFactory.Place(view.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -left++ * step), new Vector2(SlotSize, SlotSize));
                        break;
                }
            }
            float weaponsWidth = weapons.Count * step - HudLayout.Gap;
            for (int i = 0; i < weapons.Count; i++)
            {
                UiFactory.Place(_slots[weapons[i]].Rect, new Vector2(0.5f, 0f), Vector2.zero,
                    new Vector2(-weaponsWidth * 0.5f + i * step, 0f), new Vector2(SlotSize, SlotSize));
            }
        }

        private void ShowTab(bool main)
        {
            _mainTab.gameObject.SetActive(main);
            _extraTab.gameObject.SetActive(!main);
        }

        private void Unequip(string slotId, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Left && _equipped.ContainsKey(slotId))
            {
                Ctx.Commands.Unequip(slotId);
                Tooltip.Hide();
            }
        }

        private void ShowSlotTooltip(string slotId)
        {
            PlayerStateSnapshot state = Ctx.State.Player;
            if (state == null || !_equipped.TryGetValue(slotId, out string itemId))
            {
                return;
            }
            int level = 0;
            foreach (EquipmentSlotSnapshot slot in state.Equipment)
            {
                if (slot.SlotId == slotId)
                {
                    level = slot.UpgradeLevel;
                }
            }
            Tooltip.Show(TooltipText.Item(Ctx, Ctx.Content.Items[itemId], 1, UiKeys.ClickToUnequip, level));
        }

        private void Fill(PlayerStateSnapshot state, StatId[] stats, List<Text> values)
        {
            for (int i = 0; i < stats.Length; i++)
            {
                values[i].text = StatFormat.Value(Ctx.Text, stats[i], StatValue(state, stats[i]));
            }
        }

        private static float StatValue(PlayerStateSnapshot state, StatId stat)
        {
            foreach (StatSnapshot snapshot in state.Stats)
            {
                if (snapshot.Stat == stat)
                {
                    return snapshot.Value;
                }
            }
            return 0f;
        }

        private Button TabButton(RectTransform parent, string key, float anchorX)
        {
            Button button = UiFactory.Button($"Tab_{key}", parent, Ctx.Font, Ctx.Text.Get(key), () => { });
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(anchorX, 1f);
            rect.anchorMax = new Vector2(anchorX + 0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -TabHeight);
            rect.offsetMax = Vector2.zero;
            return button;
        }

        private static RectTransform TabPage(string name, RectTransform parent)
        {
            RectTransform page = UiFactory.Rect(name, parent);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = Vector2.zero;
            page.offsetMax = new Vector2(0f, -TabHeight - HudLayout.Gap);
            return page;
        }

        private void Header(RectTransform list, string key)
        {
            Text header = Row("Header", list);
            header.text = Ctx.Text.Get(key);
            header.color = HudPalette.Cast;
        }

        /// <summary>Row with a name on the left and a value on the right; returns the value label.</summary>
        private Text Pair(RectTransform list, string name)
        {
            Text label = Row("Name", list);
            label.text = name;
            Text value = UiFactory.Label("Value", label.transform, Ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleRight, HudPalette.Text);
            UiFactory.Stretch(value.rectTransform);
            return value;
        }
    }
}
