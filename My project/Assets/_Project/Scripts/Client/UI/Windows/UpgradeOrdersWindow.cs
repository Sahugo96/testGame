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
    /// <summary>
    /// Upgrade orders with escrow (04, 7.14.1), opened from the whetstone window: my orders (cancel) and orders open to me
    /// as a smith (fulfil: one attempt with my gold and materials, the payment is mine). Below: pick an upgradeable bag
    /// item, the payment and optionally a smith's name, then "Create" — the server holds item and payment until it is done.
    /// </summary>
    public sealed class UpgradeOrdersWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(720f, 600f);
        private const float RowHeight = 34f;
        private const int Rows = 8;
        private const float ItemWidth = 250f;
        private const float WhoWidth = 210f;
        private const float PayWidth = 90f;
        private const float ActionWidth = 110f;
        private const float SlotSize = 52f;
        private const int BagSlots = 10;
        private const int NameLimit = 32;
        private const int NumberLimit = 10;

        private readonly RectTransform[] _rows = new RectTransform[Rows];
        private readonly Text[] _items = new Text[Rows];
        private readonly Text[] _who = new Text[Rows];
        private readonly Text[] _pay = new Text[Rows];
        private readonly Text[] _actionLabels = new Text[Rows];
        private readonly PagerWidget _pager;
        private readonly SlotView[] _bag = new SlotView[BagSlots];
        private readonly List<ItemSlotSnapshot> _bagItems = new List<ItemSlotSnapshot>();
        private readonly InputField _payment;
        private readonly InputField _smith;
        private UpgradeOrderSnapshot[] _orders = new UpgradeOrderSnapshot[0];
        private string _chosen;
        private int _first;
        private int _seenVersion = -1;

        public UpgradeOrdersWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            float width = Size.x - HudLayout.Margin * 2f;
            float h = HudLayout.MenuButtonHeight;
            float gap = HudLayout.Gap;
            for (int i = 0; i < Rows; i++)
            {
                int index = i;
                RectTransform row = MarketUi.Row($"Order_{i}", Content, -i * (RowHeight + gap), width, RowHeight);
                MarketUi.HoverItem(ctx, tooltip, row, () => _first + index < _orders.Length ? _orders[_first + index].Item : (ItemSlotSnapshot?)null, null);
                _items[i] = MarketUi.Cell(ctx, row, 0f, ItemWidth);
                _who[i] = MarketUi.Cell(ctx, row, ItemWidth, WhoWidth);
                _pay[i] = MarketUi.Cell(ctx, row, ItemWidth + WhoWidth, PayWidth);
                _actionLabels[i] = MarketUi.RowButton(ctx, row, UiKeys.OrderFulfil, 0, ActionWidth, () => Act(index)).GetComponentInChildren<Text>();
                _rows[i] = row;
            }
            float y = -Rows * (RowHeight + gap);
            _pager = new PagerWidget(Content, ctx, new Vector2(1f, 1f), new Vector2(0f, y), ShowOrders);
            y -= h + gap * 3f;

            Text hint = MarketUi.Cell(ctx, MarketUi.Row("Hint", Content, y, width, HudLayout.RowHeight), 0f, width);
            hint.text = ctx.Text.Get(UiKeys.OrderCreateHint);
            hint.color = HudPalette.TextDim;
            y -= HudLayout.RowHeight + gap;
            for (int i = 0; i < BagSlots; i++)
            {
                int index = i;
                _bag[i] = new SlotView($"Bag_{i}", Content, ctx.Font, SlotSize);
                UiFactory.Place(_bag[i].Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (SlotSize + gap), y), Vector2.one * SlotSize);
                _bag[i].Bind(() => { if (index < _bagItems.Count) MarketUi.ShowTooltip(Ctx, Tooltip, _bagItems[index], UiKeys.OrderClickToChoose); }, Tooltip.Hide,
                    button => Choose(index, button));
            }
            y -= SlotSize + gap * 2f;
            _payment = MarketUi.Field(ctx, Content, new Vector2(0f, 1f), new Vector2(0f, y), 180f, h, NumberLimit, UiKeys.OrderPayment, true);
            _smith = MarketUi.Field(ctx, Content, new Vector2(0f, 1f), new Vector2(184f, y), 240f, h, NameLimit, UiKeys.OrderSmith, false);
            Button create = UiFactory.Button("Create", Content, ctx.Font, ctx.Text.Get(UiKeys.OrderCreate), Create);
            UiFactory.Place((RectTransform)create.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(428f, y), new Vector2(200f, h));
            ctx.State.MarketData += OnData;
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            if (!IsNearActiveStation(state))
            {
                RequestClose();
                return;
            }
            if (state.Market != null && state.Market.OrdersVersion != _seenVersion)
            {
                _seenVersion = state.Market.OrdersVersion;
                Ctx.Commands?.ViewUpgradeOrders();
            }
            FillBag(state);
        }

        protected override void OnVisibilityChanged(bool open)
        {
            if (open)
            {
                _seenVersion = -1;
                _chosen = null;
            }
        }

        private void OnData()
        {
            if (IsOpen && Ctx.State.LastOrders != null)
            {
                _orders = Ctx.State.LastOrders.Orders ?? new UpgradeOrderSnapshot[0];
                ShowOrders();
            }
        }

        private void ShowOrders()
        {
            _first = _pager.Apply(_orders.Length, Rows);
            for (int i = 0; i < Rows; i++)
            {
                int index = _first + i;
                bool shown = index < _orders.Length;
                _rows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }
                UpgradeOrderSnapshot order = _orders[index];
                _items[i].text = MarketUi.ItemLine(Ctx, order.Item);
                _items[i].color = MarketUi.ItemColor(Ctx, order.Item);
                string smith = string.IsNullOrEmpty(order.SmithName) ? Ctx.Text.Get(UiKeys.OrderAnySmith) : order.SmithName;
                _who[i].text = Ctx.Text.Format(UiKeys.OrderWho, order.OwnerName, smith, MarketUi.TimeLeft(Ctx, order.SecondsLeft));
                _pay[i].text = Ctx.Text.Format(UiKeys.MarketPrice, order.Payment);
                _actionLabels[i].text = Ctx.Text.Get(order.IsMine ? UiKeys.OrderCancel : UiKeys.OrderFulfil);
            }
        }

        private void Act(int row)
        {
            int index = _first + row;
            if (index >= _orders.Length || Ctx.Commands == null)
            {
                return;
            }
            UpgradeOrderSnapshot order = _orders[index];
            if (order.IsMine)
            {
                Ctx.Commands.CancelUpgradeOrder(order.Id);
            }
            else
            {
                Ctx.Commands.FulfillUpgradeOrder(order.Id, Ctx.ActiveStation);
            }
        }

        private void FillBag(PlayerStateSnapshot state)
        {
            _bagItems.Clear();
            foreach (ItemSlotSnapshot slot in state.Bag ?? new ItemSlotSnapshot[0])
            {
                if (_bagItems.Count < BagSlots && MarketUi.TryGetItem(Ctx, slot, out ItemDefinition item) && item.Has<UpgradeableComponent>())
                {
                    _bagItems.Add(slot);
                }
            }
            for (int i = 0; i < BagSlots; i++)
            {
                bool filled = i < _bagItems.Count;
                _bag[i].SetCaption(filled ? MarketUi.ItemLine(Ctx, _bagItems[i]) : string.Empty, filled ? MarketUi.ItemColor(Ctx, _bagItems[i]) : HudPalette.Text);
                if (filled && _bagItems[i].InstanceId == _chosen)
                {
                    _bag[i].SetTint(HudPalette.Cast);
                }
                else
                {
                    _bag[i].SetFilled(filled);
                }
            }
        }

        private void Choose(int index, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Left && index < _bagItems.Count)
            {
                _chosen = _bagItems[index].InstanceId;
                if (Ctx.State.Player != null)
                {
                    FillBag(Ctx.State.Player);
                }
            }
        }

        private void Create()
        {
            if (_chosen == null || Ctx.Commands == null)
            {
                return;
            }
            Ctx.Commands.CreateUpgradeOrder(_chosen, MarketUi.ReadLong(_payment, 0), _smith.text, Ctx.ActiveStation);
            _chosen = null;
        }
    }
}
