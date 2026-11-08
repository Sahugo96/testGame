using System;
using System.Collections.Generic;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Personal shop (05, 7.14.3; question 34). At a free shop spot: name, shop level (commission, lots, time, rent) and
    /// "Open". At a merchant: its lots; another player buys a number of pieces; the owner changes prices, takes lots back,
    /// lists bag stacks at a price per piece and may close the shop early. Everything is checked by the server.
    /// </summary>
    public sealed class ShopWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(720f, 660f);
        private const float RowHeight = 32f;
        private const int LotRows = 10;
        private const float ItemWidth = 300f;
        private const float PriceWidth = 120f;
        private const float SmallButton = 80f;
        private const float FieldWidth = 70f;
        private const float SlotSize = 52f;
        private const int BagSlots = 10;
        private const int NumberLimit = 10;

        private readonly RectTransform _spotPanel;
        private readonly RectTransform _shopPanel;
        private readonly InputField _shopName;
        private readonly Button[] _tierButtons;
        private readonly Text[] _tierLabels;
        private int _tier;

        private readonly Text _header;
        private readonly RectTransform[] _rows = new RectTransform[LotRows];
        private readonly Text[] _items = new Text[LotRows];
        private readonly Text[] _prices = new Text[LotRows];
        private readonly InputField[] _fields = new InputField[LotRows];
        private readonly Text[] _primaryLabels = new Text[LotRows];
        private readonly Button[] _secondary = new Button[LotRows];
        private readonly PagerWidget _lotPager;
        private readonly RectTransform _ownerPanel;
        private readonly InputField _listPrice;
        private readonly SlotView[] _bag = new SlotView[BagSlots];
        private readonly List<ItemSlotSnapshot> _bagItems = new List<ItemSlotSnapshot>();
        private readonly PagerWidget _bagPager;
        private ShopViewMessage _shop;
        private int _lotFirst;
        private int _bagFirst;
        private int _seenVersion = -1;

        public ShopWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            float width = Size.x - HudLayout.Margin * 2f;
            float h = HudLayout.MenuButtonHeight;
            float gap = HudLayout.Gap;

            // A free spot: open a shop.
            _spotPanel = UiFactory.Stretch(UiFactory.Rect("Spot", Content));
            Text spotTitle = MarketUi.Cell(ctx, MarketUi.Row("Title", _spotPanel, 0f, width, HudLayout.RowHeight), 0f, width);
            spotTitle.text = ctx.Text.Get(UiKeys.ShopFreeSpot);
            _shopName = MarketUi.Field(ctx, _spotPanel, new Vector2(0f, 1f), new Vector2(0f, -HudLayout.RowHeight - gap), width, h, ctx.Content.Config.ShopNameMax, UiKeys.ShopNamePlaceholder, false);
            ShopTierDto[] tiers = ctx.Content.Config.ShopTiers ?? new ShopTierDto[0];
            _tierButtons = new Button[tiers.Length];
            _tierLabels = new Text[tiers.Length];
            float y = -HudLayout.RowHeight - h - gap * 4f;
            for (int i = 0; i < tiers.Length; i++)
            {
                int index = i;
                _tierButtons[i] = UiFactory.Button($"Tier_{tiers[i].Id}", _spotPanel, ctx.Font, string.Empty, () => { _tier = index; UpdateTiers(); });
                UiFactory.Place((RectTransform)_tierButtons[i].transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(width, h));
                _tierLabels[i] = _tierButtons[i].GetComponentInChildren<Text>();
                _tierLabels[i].fontSize = HudLayout.FontSmall;
                y -= h + gap;
            }
            Button open = UiFactory.Button("Open", _spotPanel, ctx.Font, ctx.Text.Get(UiKeys.ShopOpen), OpenShop);
            UiFactory.Place((RectTransform)open.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y - gap * 2f), new Vector2(200f, h));

            // A merchant: its lots; for the owner also the bag to list from.
            _shopPanel = UiFactory.Stretch(UiFactory.Rect("Shop", Content));
            _header = MarketUi.Cell(ctx, MarketUi.Row("Header", _shopPanel, 0f, width, HudLayout.RowHeight * 2f), 0f, width, TextAnchor.UpperLeft);
            _header.fontSize = HudLayout.FontNormal;
            float rowsTop = -HudLayout.RowHeight * 2f - gap;
            for (int i = 0; i < LotRows; i++)
            {
                int index = i;
                RectTransform row = MarketUi.Row($"Lot_{i}", _shopPanel, rowsTop - i * (RowHeight + gap), width, RowHeight);
                MarketUi.HoverItem(ctx, tooltip, row, () => LotAt(index)?.Item, null);
                _items[i] = MarketUi.Cell(ctx, row, 0f, ItemWidth);
                _prices[i] = MarketUi.Cell(ctx, row, ItemWidth, PriceWidth);
                Button secondary = MarketUi.RowButton(ctx, row, UiKeys.ShopUnlist, 0, SmallButton, () => Secondary(index));
                Button primary = MarketUi.RowButton(ctx, row, UiKeys.MarketBuy, 1, SmallButton, () => Primary(index));
                _primaryLabels[i] = primary.GetComponentInChildren<Text>();
                _secondary[i] = secondary;
                _fields[i] = MarketUi.Field(ctx, row, new Vector2(1f, 0.5f), new Vector2(-(SmallButton + gap) * 2f, 0f), FieldWidth, RowHeight, NumberLimit, null, true);
                _rows[i] = row;
            }
            float afterRows = rowsTop - LotRows * (RowHeight + gap);
            _lotPager = new PagerWidget(_shopPanel, ctx, new Vector2(1f, 1f), new Vector2(0f, afterRows), Redraw);

            _ownerPanel = UiFactory.Place(UiFactory.Rect("Owner", _shopPanel), new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(width, SlotSize + h * 2f + gap * 3f));
            Text hint = MarketUi.Cell(ctx, MarketUi.Row("Hint", _ownerPanel, 0f, width - 200f, HudLayout.RowHeight), 0f, width - 200f);
            hint.text = ctx.Text.Get(UiKeys.ShopListHint);
            hint.color = HudPalette.TextDim;
            _listPrice = MarketUi.Field(ctx, _ownerPanel, new Vector2(1f, 1f), Vector2.zero, 190f, HudLayout.RowHeight, NumberLimit, UiKeys.ShopListPrice, true);
            for (int i = 0; i < BagSlots; i++)
            {
                int index = i;
                var slot = new SlotView($"Bag_{i}", _ownerPanel, ctx.Font, SlotSize);
                UiFactory.Place(slot.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (SlotSize + gap), -HudLayout.RowHeight - gap), Vector2.one * SlotSize);
                slot.Bind(() => { if (index + _bagFirst < _bagItems.Count) MarketUi.ShowTooltip(Ctx, Tooltip, _bagItems[index + _bagFirst], UiKeys.ShopClickToList); }, Tooltip.Hide,
                    button => ListFromBag(index, button));
                _bag[i] = slot;
            }
            _bagPager = new PagerWidget(_ownerPanel, ctx, new Vector2(1f, 0f), new Vector2(-210f, h * 0.5f), () => { if (Ctx.State.Player != null) Refresh(Ctx.State.Player); });
            Button closeShop = UiFactory.Button("CloseShop", _ownerPanel, ctx.Font, ctx.Text.Get(UiKeys.ShopClose), () => Ctx.Commands?.CloseShop());
            UiFactory.Place((RectTransform)closeShop.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(200f, h));

            ctx.State.MarketData += OnData;
            UpdateTiers();
        }

        private bool AtMerchant => Ctx.State.TryGetEntity(Ctx.ActiveStation, out EntitySnapshot entity) && entity.DefinitionId == Ctx.Content.World.MerchantVisual?.Id;

        public override void Refresh(PlayerStateSnapshot state)
        {
            // Just opened a shop on this spot: its merchant stands here now.
            if (!AtMerchant && state.Market != null && state.Market.MyMerchantId.IsValid && Ctx.State.TryGetEntity(state.Market.MyMerchantId, out _))
            {
                Ctx.ActiveStation = state.Market.MyMerchantId;
                _seenVersion = -1;
            }
            if (!IsNearActiveStation(state))
            {
                RequestClose();
                return;
            }
            bool merchant = AtMerchant;
            _spotPanel.gameObject.SetActive(!merchant);
            _shopPanel.gameObject.SetActive(merchant);
            if (merchant && state.Market != null && state.Market.MarketVersion != _seenVersion)
            {
                _seenVersion = state.Market.MarketVersion;
                Ctx.Commands?.ViewShop(Ctx.ActiveStation);
            }
            FillBag(state);
        }

        protected override void OnVisibilityChanged(bool open)
        {
            if (open)
            {
                _seenVersion = -1;
                _shop = null;
                Redraw();
            }
        }

        private void OnData()
        {
            ShopViewMessage shop = Ctx.State.LastShop;
            if (IsOpen && shop != null && shop.MerchantId == Ctx.ActiveStation)
            {
                _shop = shop;
                Redraw();
            }
        }

        private void Redraw()
        {
            bool mine = _shop != null && _shop.IsMine;
            _header.text = _shop == null ? string.Empty
                : Ctx.Text.Format(UiKeys.ShopHeader, _shop.Name, _shop.OwnerName, MarketUi.TimeLeft(Ctx, _shop.SecondsLeft));
            LotSnapshot[] lots = _shop?.Lots ?? new LotSnapshot[0];
            _lotFirst = _lotPager.Apply(lots.Length, LotRows);
            for (int i = 0; i < LotRows; i++)
            {
                LotSnapshot? lot = LotAt(i);
                _rows[i].gameObject.SetActive(lot.HasValue);
                if (!lot.HasValue)
                {
                    continue;
                }
                _items[i].text = MarketUi.ItemLine(Ctx, lot.Value.Item);
                _items[i].color = MarketUi.ItemColor(Ctx, lot.Value.Item);
                _prices[i].text = Ctx.Text.Format(UiKeys.MarketPrice, lot.Value.PricePerUnit);
                _primaryLabels[i].text = Ctx.Text.Get(mine ? UiKeys.ShopSetPrice : UiKeys.MarketBuy);
                _secondary[i].gameObject.SetActive(mine);
                _fields[i].text = mine ? lot.Value.PricePerUnit.ToString() : lot.Value.Item.Count.ToString();
            }
            _ownerPanel.gameObject.SetActive(mine);
        }

        private LotSnapshot? LotAt(int row)
        {
            LotSnapshot[] lots = _shop?.Lots ?? new LotSnapshot[0];
            int index = _lotFirst + row;
            return index < lots.Length ? lots[index] : (LotSnapshot?)null;
        }

        /// <summary>Buyer: buy the typed number of pieces. Owner: set the typed price.</summary>
        private void Primary(int row)
        {
            LotSnapshot? lot = LotAt(row);
            if (!lot.HasValue || Ctx.Commands == null)
            {
                return;
            }
            if (_shop.IsMine)
            {
                Ctx.Commands.SetLotPrice(lot.Value.LotId, MarketUi.ReadLong(_fields[row], lot.Value.PricePerUnit));
                return;
            }
            int count = Mathf.Clamp(MarketUi.ReadInt(_fields[row], lot.Value.Item.Count), 1, lot.Value.Item.Count);
            Ctx.Commands.BuyLot(lot.Value.LotId, count, lot.Value.PriceVersion);
        }

        private void Secondary(int row)
        {
            LotSnapshot? lot = LotAt(row);
            if (lot.HasValue && _shop.IsMine)
            {
                Ctx.Commands?.UnlistLot(lot.Value.LotId);
            }
        }

        private void FillBag(PlayerStateSnapshot state)
        {
            _bagItems.Clear();
            foreach (ItemSlotSnapshot slot in state.Bag ?? new ItemSlotSnapshot[0])
            {
                if (slot.DefinitionId != null && !string.IsNullOrEmpty(slot.InstanceId))
                {
                    _bagItems.Add(slot);
                }
            }
            _bagFirst = _bagPager.Apply(_bagItems.Count, BagSlots);
            for (int i = 0; i < BagSlots; i++)
            {
                int index = _bagFirst + i;
                SlotView view = _bag[i];
                bool filled = index < _bagItems.Count;
                view.SetFilled(filled);
                view.SetCaption(filled ? MarketUi.ItemLine(Ctx, _bagItems[index]) : string.Empty, filled ? MarketUi.ItemColor(Ctx, _bagItems[index]) : HudPalette.Text);
                view.SetCount(filled && _bagItems[index].Count > 1 ? _bagItems[index].Count.ToString() : string.Empty);
            }
        }

        private void ListFromBag(int slot, PointerEventData.InputButton button)
        {
            int index = _bagFirst + slot;
            if (button != PointerEventData.InputButton.Left || index >= _bagItems.Count || Ctx.Commands == null)
            {
                return;
            }
            Ctx.Commands.ListLot(_bagItems[index].InstanceId, MarketUi.ReadLong(_listPrice, 0));
            Tooltip.Hide();
        }

        private void OpenShop()
        {
            ShopTierDto[] tiers = Ctx.Content.Config.ShopTiers ?? new ShopTierDto[0];
            if (_tier < tiers.Length)
            {
                Ctx.Commands?.OpenShop(Ctx.ActiveStation, tiers[_tier].Id, _shopName.text);
            }
        }

        private void UpdateTiers()
        {
            ShopTierDto[] tiers = Ctx.Content.Config.ShopTiers ?? new ShopTierDto[0];
            for (int i = 0; i < tiers.Length; i++)
            {
                ShopTierDto tier = tiers[i];
                _tierLabels[i].text = Ctx.Text.Format(UiKeys.ShopTierLine, Ctx.Text.Get(tier.NameKey), Mathf.RoundToInt(tier.Commission * 100f), tier.MaxLots, tier.DurationHours, tier.RentCost);
                _tierLabels[i].color = i == _tier ? HudPalette.Cast : HudPalette.Text;
            }
        }
    }
}
