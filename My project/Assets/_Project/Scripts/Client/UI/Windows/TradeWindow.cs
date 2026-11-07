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
    /// Player-to-player trade (module 14). Opens when the server opens a trade and closes when it ends (HudRoot).
    /// Left: what I give (click to take back), right: what the partner gives; gold on both sides; both confirm, any change
    /// resets the confirmations. Below: my bag — a click offers the item. Closing the window cancels the trade.
    /// The server checks everything (ownership, tradability, room, gold) and swaps atomically.
    /// </summary>
    public sealed class TradeWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(640f, 700f);
        private const float SlotSize = 56f;
        private const int OfferColumns = 4;
        private const int BagColumns = 10;
        private const int BagRows = 2;
        private const float ButtonWidth = 150f;
        private const float GoldFieldWidth = 140f;
        private const int GoldDigits = 12;

        private readonly Text _mineTitle;
        private readonly Text _theirsTitle;
        private readonly SlotView[] _mine;
        private readonly SlotView[] _theirs;
        private readonly Text _myGold;
        private readonly Text _theirGold;
        private readonly Text _myState;
        private readonly Text _theirState;
        private readonly InputField _goldField;
        private readonly Button _confirm;
        private readonly SlotView[] _bag = new SlotView[BagColumns * BagRows];
        private readonly List<ItemSlotSnapshot> _bagItems = new List<ItemSlotSnapshot>();
        private readonly PagerWidget _pager;
        private TradeSnapshot _trade = new TradeSnapshot();
        private int _bagFirst;

        public TradeWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            float width = Size.x - HudLayout.Margin * 2f;
            float half = width * 0.5f;
            float y = 0f;
            _mineTitle = Label("MineTitle", 0f, y, half, HudPalette.Text);
            _theirsTitle = Label("TheirsTitle", half, y, half, HudPalette.Text);
            _mineTitle.text = ctx.Text.Get(UiKeys.TradeMine);
            y -= HudLayout.RowHeight + HudLayout.Gap;

            int max = ctx.Content.Config.TradeMaxItems;
            _mine = OfferGrid("Mine", 0f, y, max, true);
            _theirs = OfferGrid("Theirs", half, y, max, false);
            int rows = (max + OfferColumns - 1) / OfferColumns;
            y -= rows * (SlotSize + HudLayout.Gap) + HudLayout.Gap;

            _myGold = Label("MyGold", 0f, y, half, HudPalette.Cast);
            _theirGold = Label("TheirGold", half, y, half, HudPalette.Cast);
            y -= HudLayout.RowHeight + HudLayout.Gap;
            _myState = Label("MyState", 0f, y, half, HudPalette.TextDim);
            _theirState = Label("TheirState", half, y, half, HudPalette.TextDim);
            y -= HudLayout.RowHeight + HudLayout.Gap * 2f;

            _goldField = UiFactory.Input("Gold", Content, ctx.Font, HudLayout.FontNormal, GoldDigits, string.Empty);
            _goldField.contentType = InputField.ContentType.IntegerNumber;
            UiFactory.Place((RectTransform)_goldField.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y),
                new Vector2(GoldFieldWidth, HudLayout.MenuButtonHeight));
            AddButton("SetGold", UiKeys.TradeSetGold, GoldFieldWidth + HudLayout.Gap, y, SetGold);
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap * 2f;

            _confirm = AddButton("Confirm", UiKeys.TradeConfirm, 0f, y, () => Ctx.Commands?.ConfirmTrade());
            AddButton("Cancel", UiKeys.TradeCancel, ButtonWidth + HudLayout.Gap, y, () => Ctx.Commands?.CancelTrade());
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap * 3f;

            Text hint = Label("BagHint", 0f, y, width, HudPalette.TextDim);
            hint.text = ctx.Text.Get(UiKeys.TradeBagHint);
            y -= HudLayout.RowHeight + HudLayout.Gap;
            for (int i = 0; i < _bag.Length; i++)
            {
                int index = i;
                SlotView slot = new SlotView($"Bag_{i}", Content, ctx.Font, SlotSize);
                UiFactory.Place(slot.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2((i % BagColumns) * (SlotSize + HudLayout.Gap), y - (i / BagColumns) * (SlotSize + HudLayout.Gap)), Vector2.one * SlotSize);
                slot.Bind(() => ShowBagTooltip(index), Tooltip.Hide, button => OnBagClick(index, button));
                _bag[i] = slot;
            }
            _pager = new PagerWidget(Content, ctx, new Vector2(1f, 0f), new Vector2(-HudLayout.Gap, HudLayout.Gap), () =>
            {
                if (Ctx.State.Player != null)
                {
                    Refresh(Ctx.State.Player);
                }
            });
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            _trade = state.Trade ?? new TradeSnapshot();
            string partner = _trade.PartnerId.IsValid ? EntityNames.Of(Ctx, _trade.PartnerId) : string.Empty;
            _theirsTitle.text = Ctx.Text.Format(UiKeys.TradeTheirs, partner);
            FillOffer(_mine, _trade.MyItems);
            FillOffer(_theirs, _trade.TheirItems);
            _myGold.text = Ctx.Text.Format(UiKeys.TradeGoldLine, _trade.MyGold);
            _theirGold.text = Ctx.Text.Format(UiKeys.TradeGoldLine, _trade.TheirGold);
            SetState(_myState, _trade.MyConfirmed);
            SetState(_theirState, _trade.TheirConfirmed);
            _confirm.interactable = _trade.Phase == TradePhase.Open && !_trade.MyConfirmed;
            FillBag(state);
        }

        protected override void OnVisibilityChanged(bool open)
        {
            // The player closed the window (X / Esc) during an open trade: cancel it. HudRoot closes it after the trade ends.
            if (!open && Ctx.State.Player?.Trade != null && Ctx.State.Player.Trade.Phase == TradePhase.Open)
            {
                Ctx.Commands?.CancelTrade();
            }
        }

        private void FillOffer(SlotView[] slots, ItemSlotSnapshot[] items)
        {
            items = items ?? new ItemSlotSnapshot[0];
            for (int i = 0; i < slots.Length; i++)
            {
                Show(slots[i], i < items.Length ? items[i] : default);
            }
        }

        private void FillBag(PlayerStateSnapshot state)
        {
            _bagItems.Clear();
            foreach (ItemSlotSnapshot slot in state.Bag ?? new ItemSlotSnapshot[0])
            {
                if (slot.DefinitionId != null && !string.IsNullOrEmpty(slot.InstanceId) && !Offered(slot.InstanceId))
                {
                    _bagItems.Add(slot);
                }
            }
            _bagFirst = _pager.Apply(_bagItems.Count, _bag.Length);
            for (int i = 0; i < _bag.Length; i++)
            {
                int index = _bagFirst + i;
                Show(_bag[i], index < _bagItems.Count ? _bagItems[index] : default);
            }
        }

        private void Show(SlotView view, ItemSlotSnapshot slot)
        {
            if (slot.DefinitionId == null || !Ctx.Content.Items.TryGetValue(slot.DefinitionId, out ItemDefinition item))
            {
                view.SetFilled(false);
                view.SetCaption(string.Empty, HudPalette.Text);
                view.SetCount(string.Empty);
                return;
            }
            view.SetFilled(true);
            view.SetCaption(TooltipText.ItemName(Ctx, item, slot.UpgradeLevel), HudPalette.ForRarity(item.Rarity));
            view.SetCount(slot.Count > 1 ? slot.Count.ToString() : string.Empty);
        }

        private bool Offered(string instanceId)
        {
            foreach (ItemSlotSnapshot item in _trade.MyItems ?? new ItemSlotSnapshot[0])
            {
                if (item.InstanceId == instanceId)
                {
                    return true;
                }
            }
            return false;
        }

        private List<string> OfferedIds()
        {
            var ids = new List<string>();
            foreach (ItemSlotSnapshot item in _trade.MyItems ?? new ItemSlotSnapshot[0])
            {
                ids.Add(item.InstanceId);
            }
            return ids;
        }

        private void OnBagClick(int index, PointerEventData.InputButton button)
        {
            int item = _bagFirst + index;
            if (button != PointerEventData.InputButton.Left || item >= _bagItems.Count || Ctx.Commands == null)
            {
                return;
            }
            List<string> ids = OfferedIds();
            ids.Add(_bagItems[item].InstanceId);
            Ctx.Commands.OfferTrade(ids.ToArray(), _trade.MyGold);
            Tooltip.Hide();
        }

        private void OnMineClick(int index, PointerEventData.InputButton button)
        {
            ItemSlotSnapshot[] mine = _trade.MyItems ?? new ItemSlotSnapshot[0];
            if (button != PointerEventData.InputButton.Left || index >= mine.Length || Ctx.Commands == null)
            {
                return;
            }
            List<string> ids = OfferedIds();
            ids.Remove(mine[index].InstanceId);
            Ctx.Commands.OfferTrade(ids.ToArray(), _trade.MyGold);
            Tooltip.Hide();
        }

        private void SetGold()
        {
            if (Ctx.Commands == null || !long.TryParse(_goldField.text, out long gold) || gold < 0)
            {
                return;
            }
            Ctx.Commands.OfferTrade(OfferedIds().ToArray(), gold);
        }

        private void ShowBagTooltip(int index)
        {
            int item = _bagFirst + index;
            if (item < _bagItems.Count)
            {
                ShowItemTooltip(_bagItems[item], UiKeys.ClickToOffer);
            }
        }

        private void ShowOfferTooltip(ItemSlotSnapshot[] items, int index, string hint)
        {
            if (items != null && index < items.Length)
            {
                ShowItemTooltip(items[index], hint);
            }
        }

        private void ShowItemTooltip(ItemSlotSnapshot slot, string hint)
        {
            if (slot.DefinitionId != null && Ctx.Content.Items.TryGetValue(slot.DefinitionId, out ItemDefinition item))
            {
                Tooltip.Show(TooltipText.Item(Ctx, item, slot.Count, hint, slot.UpgradeLevel, slot.Sockets, slot.Affixes));
            }
        }

        private void SetState(Text label, bool confirmed)
        {
            label.text = Ctx.Text.Get(confirmed ? UiKeys.TradeConfirmed : UiKeys.TradeNotConfirmed);
            label.color = confirmed ? HudPalette.Positive : HudPalette.TextDim;
        }

        private SlotView[] OfferGrid(string name, float x, float y, int count, bool mine)
        {
            var slots = new SlotView[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                SlotView slot = new SlotView($"{name}_{i}", Content, Ctx.Font, SlotSize);
                UiFactory.Place(slot.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x + (i % OfferColumns) * (SlotSize + HudLayout.Gap), y - (i / OfferColumns) * (SlotSize + HudLayout.Gap)), Vector2.one * SlotSize);
                if (mine)
                {
                    slot.Bind(() => ShowOfferTooltip(_trade.MyItems, index, UiKeys.ClickToWithdraw), Tooltip.Hide, button => OnMineClick(index, button));
                }
                else
                {
                    slot.Bind(() => ShowOfferTooltip(_trade.TheirItems, index, null), Tooltip.Hide, null);
                }
                slots[i] = slot;
            }
            return slots;
        }

        private Text Label(string name, float x, float y, float width, Color color)
        {
            Text text = UiFactory.Label(name, Content, Ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, color);
            UiFactory.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(width, HudLayout.RowHeight));
            return text;
        }

        private Button AddButton(string name, string key, float x, float y, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(name, Content, Ctx.Font, Ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(ButtonWidth, HudLayout.MenuButtonHeight));
            return button;
        }
    }
}
