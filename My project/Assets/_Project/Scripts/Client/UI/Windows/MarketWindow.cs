using System;
using System.Collections.Generic;
using Game.Client.Localization;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Market (05, 7.14.3), opened at the market NPC: search by name (turned into item ids here — the client has the
    /// texts), category, class, level range, sockets, upgrade, rarity and price; sort; server pages; buy right from the
    /// list (temporarily DEFAULT: no walk to the shop). Columns: item, level, price per piece, shop. The server checks
    /// everything; the list refreshes when any lot changes.
    /// </summary>
    public sealed class MarketWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(820f, 650f);
        private const float RowHeight = 34f;
        private const float NameWidth = 300f;
        private const float LevelWidth = 50f;
        private const float PriceWidth = 110f;
        private const float ShopWidth = 160f;
        private const float CountWidth = 60f;
        private const float BuyWidth = 90f;
        private const int NameLimit = 40;
        private const int NumberLimit = 10;
        private static readonly MarketSort[] Sorts =
        {
            MarketSort.PriceAscending, MarketSort.PriceDescending, MarketSort.LevelAscending, MarketSort.LevelDescending, MarketSort.Newest,
        };

        private readonly InputField _name;
        private readonly InputField _minLevel;
        private readonly InputField _maxLevel;
        private readonly InputField _maxPrice;
        private readonly InputField _minSockets;
        private readonly InputField _minUpgrade;
        private readonly Text _sortLabel;
        private readonly Text _categoryLabel;
        private readonly Text _rarityLabel;
        private readonly Text _classLabel;
        private readonly Text _total;
        private readonly RectTransform[] _rows;
        private readonly Text[] _names;
        private readonly Text[] _levels;
        private readonly Text[] _prices;
        private readonly Text[] _shops;
        private readonly InputField[] _counts;
        private readonly Widgets.PagerWidget _pager;
        private readonly List<string> _classes = new List<string>();
        private LotSnapshot[] _lots = new LotSnapshot[0];
        private int _sort;
        private int _category = -1;
        private int _rarity = -1;
        private int _class = -1;
        private int _seenVersion = -1;

        public MarketWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            float h = HudLayout.MenuButtonHeight;
            float gap = HudLayout.Gap;
            Vector2 topLeft = new Vector2(0f, 1f);
            _name = MarketUi.Field(ctx, Content, topLeft, Vector2.zero, 220f, h, NameLimit, UiKeys.MarketNamePlaceholder, false);
            Button find = UiFactory.Button("Find", Content, ctx.Font, ctx.Text.Get(UiKeys.MarketFind), () => Search(0));
            UiFactory.Place((RectTransform)find.transform, topLeft, topLeft, new Vector2(224f, 0f), new Vector2(100f, h));
            _sortLabel = CycleButton("Sort", 328f, 0f, 170f, () => { _sort = (_sort + 1) % Sorts.Length; Search(0); });
            _categoryLabel = CycleButton("Category", 502f, 0f, 150f, () => { _category = Next(_category, Enum.GetValues(typeof(ItemCategory)).Length); Search(0); });
            _rarityLabel = CycleButton("Rarity", 656f, 0f, 140f, () => { _rarity = Next(_rarity, Enum.GetValues(typeof(Rarity)).Length); Search(0); });

            foreach (ClassDefinition cls in ctx.Content.Classes.Values)
            {
                _classes.Add(cls.Id);
            }
            float y2 = -(h + gap);
            _classLabel = CycleButton("Class", 0f, y2, 150f, () => { _class = Next(_class, _classes.Count); Search(0); });
            _minLevel = MarketUi.Field(ctx, Content, topLeft, new Vector2(154f, y2), 90f, h, NumberLimit, UiKeys.MarketMinLevel, true);
            _maxLevel = MarketUi.Field(ctx, Content, topLeft, new Vector2(248f, y2), 90f, h, NumberLimit, UiKeys.MarketMaxLevel, true);
            _maxPrice = MarketUi.Field(ctx, Content, topLeft, new Vector2(342f, y2), 130f, h, NumberLimit, UiKeys.MarketMaxPrice, true);
            _minSockets = MarketUi.Field(ctx, Content, topLeft, new Vector2(476f, y2), 150f, h, NumberLimit, UiKeys.MarketMinSockets, true);
            _minUpgrade = MarketUi.Field(ctx, Content, topLeft, new Vector2(630f, y2), 166f, h, NumberLimit, UiKeys.MarketMinUpgrade, true);

            float width = Size.x - HudLayout.Margin * 2f;
            float y = y2 - h - gap * 3f;
            RectTransform header = MarketUi.Row("Header", Content, y, width, HudLayout.RowHeight);
            Header(header, 0f, NameWidth, UiKeys.MarketColumnItem);
            Header(header, NameWidth, LevelWidth, UiKeys.MarketColumnLevel);
            Header(header, NameWidth + LevelWidth, PriceWidth, UiKeys.MarketColumnPrice);
            Header(header, NameWidth + LevelWidth + PriceWidth, ShopWidth, UiKeys.MarketColumnShop);
            y -= HudLayout.RowHeight + gap;

            int rows = Math.Max(1, ctx.Content.Config.MarketPageSize);
            _rows = new RectTransform[rows];
            _names = new Text[rows];
            _levels = new Text[rows];
            _prices = new Text[rows];
            _shops = new Text[rows];
            _counts = new InputField[rows];
            for (int i = 0; i < rows; i++)
            {
                int index = i;
                RectTransform row = MarketUi.Row($"Lot_{i}", Content, y - i * (RowHeight + gap), width, RowHeight);
                MarketUi.HoverItem(ctx, tooltip, row, () => index < _lots.Length ? _lots[index].Item : (ItemSlotSnapshot?)null, null);
                _names[i] = MarketUi.Cell(ctx, row, 0f, NameWidth);
                _levels[i] = MarketUi.Cell(ctx, row, NameWidth, LevelWidth);
                _prices[i] = MarketUi.Cell(ctx, row, NameWidth + LevelWidth, PriceWidth);
                _shops[i] = MarketUi.Cell(ctx, row, NameWidth + LevelWidth + PriceWidth, ShopWidth);
                _counts[i] = MarketUi.Field(ctx, row, new Vector2(1f, 0.5f), new Vector2(-BuyWidth - gap, 0f), CountWidth, RowHeight, NumberLimit, null, true);
                MarketUi.RowButton(ctx, row, UiKeys.MarketBuy, 0, BuyWidth, () => Buy(index));
                _rows[i] = row;
            }
            _total = UiFactory.Label("Total", Content, ctx.Font, HudLayout.FontSmall, TextAnchor.MiddleLeft, HudPalette.TextDim);
            UiFactory.Place(_total.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(width * 0.5f, h));
            _pager = new Widgets.PagerWidget(Content, ctx, new Vector2(1f, 0f), new Vector2(0f, h * 0.5f), () => Search(_pager.Page));
            ctx.State.MarketData += OnData;
            UpdateLabels();
            ShowPage(null);
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            if (!IsNearActiveStation(state))
            {
                RequestClose();
                return;
            }
            if (state.Market != null && state.Market.MarketVersion != _seenVersion)
            {
                _seenVersion = state.Market.MarketVersion;
                Search(_pager.Page);
            }
        }

        protected override void OnVisibilityChanged(bool open)
        {
            if (open)
            {
                _seenVersion = -1;
            }
        }

        private void Search(int page)
        {
            if (!IsOpen || Ctx.Commands == null)
            {
                return;
            }
            UpdateLabels();
            string[] ids = MatchingIds(_name.text);
            if (ids != null && ids.Length == 0)
            {
                ShowPage(new MarketPageMessage());
                return;
            }
            Ctx.Commands.SearchMarket(new MarketSearchCommand
            {
                DefinitionIds = ids,
                Category = _category < 0 ? null : ((ItemCategory)_category).ToString(),
                ClassId = _class < 0 ? null : _classes[_class],
                MinLevel = MarketUi.ReadInt(_minLevel, 0),
                MaxLevel = MarketUi.ReadInt(_maxLevel, 0),
                MinSockets = MarketUi.ReadInt(_minSockets, 0),
                MinUpgrade = MarketUi.ReadInt(_minUpgrade, 0),
                Rarity = _rarity,
                MaxPrice = MarketUi.ReadLong(_maxPrice, 0),
                Sort = Sorts[_sort],
                Page = page,
            });
        }

        /// <summary>Item ids whose localized name contains the text; null = no name filter.</summary>
        private string[] MatchingIds(string text)
        {
            string query = (text ?? string.Empty).Trim();
            if (query.Length == 0)
            {
                return null;
            }
            var ids = new List<string>();
            foreach (ItemDefinition item in Ctx.Content.Items.Values)
            {
                if (Ctx.Text.Get(item.NameKey).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ids.Add(item.Id);
                }
            }
            return ids.ToArray();
        }

        private void OnData()
        {
            if (IsOpen && Ctx.State.LastMarketPage != null)
            {
                ShowPage(Ctx.State.LastMarketPage);
            }
        }

        private void ShowPage(MarketPageMessage page)
        {
            _lots = page?.Lots ?? new LotSnapshot[0];
            for (int i = 0; i < _rows.Length; i++)
            {
                bool shown = i < _lots.Length;
                _rows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }
                LotSnapshot lot = _lots[i];
                _names[i].text = MarketUi.ItemLine(Ctx, lot.Item);
                _names[i].color = MarketUi.ItemColor(Ctx, lot.Item);
                _levels[i].text = MarketUi.TryGetItem(Ctx, lot.Item, out ItemDefinition item) ? item.RequiredLevel.ToString() : string.Empty;
                _prices[i].text = Ctx.Text.Format(UiKeys.MarketPrice, lot.PricePerUnit);
                _shops[i].text = lot.ShopName;
                _counts[i].text = lot.Item.Count.ToString();
            }
            int total = page?.Total ?? 0;
            _total.text = total > 0 ? Ctx.Text.Format(UiKeys.MarketTotal, total) : Ctx.Text.Get(UiKeys.MarketEmpty);
            _pager.Apply(total, _rows.Length);
        }

        private void Buy(int index)
        {
            if (index >= _lots.Length || Ctx.Commands == null)
            {
                return;
            }
            LotSnapshot lot = _lots[index];
            int count = Mathf.Clamp(MarketUi.ReadInt(_counts[index], lot.Item.Count), 1, lot.Item.Count);
            Ctx.Commands.BuyLot(lot.LotId, count, lot.PriceVersion);
        }

        private void UpdateLabels()
        {
            _sortLabel.text = Ctx.Text.Get(UiKeys.MarketSortPrefix + Sorts[_sort]);
            _categoryLabel.text = _category < 0 ? Ctx.Text.Get(UiKeys.MarketAnyCategory) : Ctx.Text.Get(UiKeys.CategoryPrefix + (ItemCategory)_category);
            _rarityLabel.text = _rarity < 0 ? Ctx.Text.Get(UiKeys.MarketAnyRarity) : Ctx.Text.Get(UiKeys.RarityPrefix + (Rarity)_rarity);
            _classLabel.text = _class < 0 ? Ctx.Text.Get(UiKeys.MarketAnyClass) : Ctx.Text.Get(Ctx.Content.Classes[_classes[_class]].NameKey);
        }

        /// <summary>-1 (any) → 0 → … → count-1 → -1.</summary>
        private static int Next(int value, int count) => value + 1 >= count ? -1 : value + 1;

        private Text CycleButton(string name, float x, float y, float width, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(name, Content, Ctx.Font, string.Empty, onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(width, HudLayout.MenuButtonHeight));
            Text label = button.GetComponentInChildren<Text>();
            label.fontSize = HudLayout.FontSmall;
            return label;
        }

        private void Header(RectTransform row, float x, float width, string key)
        {
            Text text = MarketUi.Cell(Ctx, row, x, width);
            text.text = Ctx.Text.Get(key);
            text.color = HudPalette.TextDim;
        }
    }
}
