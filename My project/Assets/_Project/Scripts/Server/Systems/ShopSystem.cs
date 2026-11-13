using System;
using System.Collections.Generic;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Personal shops and the market (05, 7.14.3; question 34; module 20). A player puts a merchant on a free shop spot for
    /// the paid time (rent is a gold sink; one shop per character; the first to come gets the spot, question 58) and lists
    /// whole bag stacks with a price per piece. The listed instance is held by the server, so it cannot be sold twice or
    /// used. Anyone buys at the merchant or at the market NPC: the server checks the lot, the price version the buyer saw,
    /// his gold and the request id (a repeated command does nothing); a part of a stack becomes a new instance. The buyer
    /// gets the item in the bag (or by mail when full); the seller gets the gold minus the shop's commission by mail. When
    /// the time is over or the owner closes the shop, unsold lots come back by mail.
    /// </summary>
    public sealed class ShopSystem
    {
        private readonly ServerContext _ctx;
        private readonly MarketState _market;
        private readonly WalletSystem _wallet;
        private readonly InventorySystem _inventory;
        private readonly StationSystem _stations;
        private readonly MailSystem _mail;
        private readonly RequestMemory _requests;
        private readonly Dictionary<EntityId, float> _lastSearch = new Dictionary<EntityId, float>();
        private readonly List<int> _expired = new List<int>();

        public ShopSystem(ServerContext ctx, MarketState market, WalletSystem wallet, InventorySystem inventory, StationSystem stations, MailSystem mail)
        {
            _ctx = ctx;
            _market = market;
            _wallet = wallet;
            _inventory = inventory;
            _stations = stations;
            _mail = mail;
            _requests = new RequestMemory(ctx.Content.Config.MarketRequestMemory);
        }

        public void Open(PlayerCharacter player, OpenShopCommand command)
        {
            if (!_stations.IsNear(player, command.SpotId, StationTypes.ShopSpot))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return;
            }
            if (TryGetShopOf(player, out _))
            {
                CombatEvents.Reject(player, RejectReasons.ShopAlreadyOpen);
                return;
            }
            string spotId = _ctx.World.Stations.Get(command.SpotId).StationId;
            foreach (ShopData other in _market.Shops.Values)
            {
                if (other.SpotId == spotId)
                {
                    CombatEvents.Reject(player, RejectReasons.ShopSpotTaken);
                    return;
                }
            }
            ShopTierDto tier = Tier(command.TierId);
            string name = MarketText.Clean(command.Name, _ctx.Content.Config.ShopNameMax);
            if (tier == null || name.Length == 0)
            {
                CombatEvents.Reject(player, RejectReasons.ShopBadName);
                return;
            }
            if (tier.RentCost > 0)
            {
                var rent = new[] { new WalletEntry { Player = player, CurrencyId = _ctx.Content.Config.GoldCurrencyId, Amount = tier.RentCost } };
                if (_wallet.Transact(rent, null, null, "shop_rent") != WalletResult.Ok)
                {
                    CombatEvents.Reject(player, RejectReasons.NotEnoughGold);
                    return;
                }
            }
            long now = _ctx.Now;
            var shop = new ShopData
            {
                Id = _market.NextShopId++,
                OwnerCharacterId = player.CharacterId,
                OwnerName = NameOf(player),
                Name = name,
                TierId = tier.Id,
                SpotId = spotId,
                OpenedAt = now,
                ExpiresAt = now + (long)(tier.DurationHours * 3600f),
                Lots = new List<LotData>(),
            };
            shop.MerchantId = SpawnMerchant(shop);
            _market.Shops[shop.Id] = shop;
            Changed(player.EntityId);
            Notify(player, NotificationKeys.ShopOpened, (int)tier.DurationHours, null, true);
        }

        public void Close(PlayerCharacter player)
        {
            if (!TryGetShopOf(player, out ShopData shop))
            {
                CombatEvents.Reject(player, RejectReasons.ShopNotFound);
                return;
            }
            CloseShop(shop);
            Notify(player, NotificationKeys.ShopClosed, 0, null, true);
        }

        public void List(PlayerCharacter player, ListLotCommand command)
        {
            if (!OwnShopNearby(player, out ShopData shop))
            {
                return;
            }
            if (shop.Lots.Count >= Tier(shop.TierId).MaxLots)
            {
                CombatEvents.Reject(player, RejectReasons.ShopLotsFull);
                return;
            }
            if (!ValidPrice(command.PricePerUnit))
            {
                CombatEvents.Reject(player, RejectReasons.MarketBadPrice);
                return;
            }
            ItemInstance item = _inventory.FindInBag(player, command.ItemInstanceId);
            if (!_inventory.IsTradable(item))
            {
                CombatEvents.Reject(player, RejectReasons.ItemNotTradable);
                return;
            }
            _inventory.RemoveFromBag(player, item);
            shop.Lots.Add(new LotData { Id = _market.NextLotId++, Item = item, PricePerUnit = command.PricePerUnit, PriceVersion = 1, ListedAt = _ctx.Now });
            Audit(AuditActions.ShopList, player.CharacterId, item, item.Count, command.PricePerUnit, shop.Name);
            Changed(player.EntityId);
        }

        public void Unlist(PlayerCharacter player, UnlistLotCommand command)
        {
            if (!OwnShopNearby(player, out ShopData shop))
            {
                return;
            }
            int index = shop.Lots.FindIndex(l => l.Id == command.LotId);
            if (index < 0)
            {
                CombatEvents.Reject(player, RejectReasons.MarketLotGone);
                return;
            }
            ItemInstance item = shop.Lots[index].Item;
            if (!_inventory.Place(player, item))
            {
                CombatEvents.Reject(player, RejectReasons.BagFull);
                return;
            }
            shop.Lots.RemoveAt(index);
            Audit(AuditActions.ShopUnlist, player.CharacterId, item, item.Count, 0, shop.Name);
            Changed(player.EntityId);
        }

        public void SetPrice(PlayerCharacter player, SetLotPriceCommand command)
        {
            if (!OwnShopNearby(player, out ShopData shop))
            {
                return;
            }
            int index = shop.Lots.FindIndex(l => l.Id == command.LotId);
            if (index < 0)
            {
                CombatEvents.Reject(player, RejectReasons.MarketLotGone);
                return;
            }
            if (!ValidPrice(command.PricePerUnit))
            {
                CombatEvents.Reject(player, RejectReasons.MarketBadPrice);
                return;
            }
            LotData lot = shop.Lots[index];
            lot.PricePerUnit = command.PricePerUnit;
            lot.PriceVersion++;
            shop.Lots[index] = lot;
            Changed(EntityId.None);
        }

        public void Buy(PlayerCharacter player, BuyLotCommand command)
        {
            if (!FindLot(command.LotId, out ShopData shop, out int index))
            {
                CombatEvents.Reject(player, RejectReasons.MarketLotGone);
                return;
            }
            if (!_stations.IsNearAny(player, StationTypes.Market) && !_stations.IsNear(player, shop.MerchantId, StationTypes.Merchant))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return;
            }
            if (shop.OwnerCharacterId == player.CharacterId)
            {
                CombatEvents.Reject(player, RejectReasons.MarketOwnLot);
                return;
            }
            LotData lot = shop.Lots[index];
            if (command.PriceVersion != lot.PriceVersion)
            {
                CombatEvents.Reject(player, RejectReasons.MarketPriceChanged);
                return;
            }
            if (command.Count <= 0 || command.Count > lot.Item.Count)
            {
                CombatEvents.Reject(player, RejectReasons.MarketLotGone);
                return;
            }
            string key = $"buy:{player.CharacterId}:{command.RequestId}";
            if (!_requests.TryAdd(key))
            {
                return;
            }
            long total = lot.PricePerUnit * command.Count;
            var payment = new[] { new WalletEntry { Player = player, CurrencyId = _ctx.Content.Config.GoldCurrencyId, Amount = total } };
            WalletResult paid = _wallet.Transact(payment, null, key, "market_buy");
            if (paid == WalletResult.Duplicate)
            {
                return;
            }
            if (paid != WalletResult.Ok)
            {
                CombatEvents.Reject(player, RejectReasons.NotEnoughGold);
                return;
            }

            ItemInstance bought;
            if (command.Count == lot.Item.Count)
            {
                bought = lot.Item;
                shop.Lots.RemoveAt(index);
            }
            else
            {
                bought = new ItemInstance
                {
                    InstanceId = Guid.NewGuid(),
                    DefinitionId = lot.Item.DefinitionId,
                    Count = command.Count,
                    UpgradeLevel = lot.Item.UpgradeLevel,
                    CreatedAt = _ctx.Time,
                };
                lot.Item.Count -= command.Count;
                shop.Lots[index] = lot;
                Audit(AuditActions.StackSplit, shop.OwnerCharacterId, bought, command.Count, 0, lot.Item.SourceId);
            }
            Audit(AuditActions.ShopSale, shop.OwnerCharacterId, bought, command.Count, total, player.CharacterId);
            if (total >= _ctx.Content.Config.MarketSuspiciousGold)
            {
                Log.Warning(LogTags.Items, $"Large sale: '{shop.OwnerName}' sold {bought.DefinitionId} x{command.Count} for {total} to {player.CharacterId}");
            }

            ItemDefinition definition = _ctx.Content.Items[bought.DefinitionId];
            if (_inventory.Place(player, bought))
            {
                Notify(player, NotificationKeys.PurchaseDone, command.Count, definition.NameKey, true);
            }
            else
            {
                _mail.SendSystem(NameOf(player), MailType.System, MailSubjects.Purchase, bought.DefinitionId, command.Count, shop.Name, new List<ItemInstance> { bought }, 0);
                Notify(player, NotificationKeys.PurchaseByMail, command.Count, definition.NameKey, true);
            }
            long commission = (long)Math.Floor(total * (double)Tier(shop.TierId).Commission);
            _mail.SendSystem(shop.OwnerName, MailType.Sale, MailSubjects.Sold, bought.DefinitionId, command.Count, shop.Name, null, total - commission);
            PlayerCharacter owner = FindOwner(shop);
            if (owner != null)
            {
                Notify(owner, NotificationKeys.ShopSold, command.Count, definition.NameKey, true);
            }
            Changed(player.EntityId);
        }

        public void View(PlayerCharacter player, ViewShopCommand command)
        {
            if (!TryGetByMerchant(command.MerchantId, out ShopData shop))
            {
                CombatEvents.Reject(player, RejectReasons.ShopNotFound);
                return;
            }
            var lots = new LotSnapshot[shop.Lots.Count];
            for (int i = 0; i < lots.Length; i++)
            {
                lots[i] = Snapshot(shop, shop.Lots[i]);
            }
            player.Outbox.Replies.Add(new ShopViewMessage
            {
                MerchantId = shop.MerchantId,
                Name = shop.Name,
                OwnerName = shop.OwnerName,
                TierId = shop.TierId,
                IsMine = shop.OwnerCharacterId == player.CharacterId,
                SecondsLeft = Math.Max(0, shop.ExpiresAt - _ctx.Now),
                Lots = lots,
            });
        }

        /// <summary>Market search at the market NPC: filters, sort, one page (MarketPageSize); limited in rate.</summary>
        public void Search(PlayerCharacter player, MarketSearchCommand command)
        {
            if (!_stations.IsNearAny(player, StationTypes.Market))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return;
            }
            if (_lastSearch.TryGetValue(player.EntityId, out float last) && _ctx.Time - last < _ctx.Content.Config.MarketSearchMinIntervalSeconds)
            {
                CombatEvents.Reject(player, RejectReasons.MarketTooFast);
                return;
            }
            _lastSearch[player.EntityId] = _ctx.Time;
            var ids = command.DefinitionIds != null && command.DefinitionIds.Length > 0 ? new HashSet<string>(command.DefinitionIds) : null;
            var found = new List<(ShopData shop, LotData lot, ItemDefinition definition)>();
            foreach (ShopData shop in _market.Shops.Values)
            {
                foreach (LotData lot in shop.Lots)
                {
                    ItemDefinition definition = _ctx.Content.Items[lot.Item.DefinitionId];
                    if (Matches(command, ids, lot, definition))
                    {
                        found.Add((shop, lot, definition));
                    }
                }
            }
            found.Sort((a, b) => Compare(command.Sort, a.lot, a.definition, b.lot, b.definition));
            int size = Math.Max(1, _ctx.Content.Config.MarketPageSize);
            int pages = Math.Max(1, (found.Count + size - 1) / size);
            int page = Math.Max(0, Math.Min(command.Page, pages - 1));
            var lots = new List<LotSnapshot>();
            for (int i = page * size; i < found.Count && i < (page + 1) * size; i++)
            {
                lots.Add(Snapshot(found[i].shop, found[i].lot));
            }
            player.Outbox.Replies.Add(new MarketPageMessage { Total = found.Count, Page = page, PageCount = pages, Lots = lots.ToArray() });
        }

        /// <summary>The player's merchant (for the snapshot), or None.</summary>
        public EntityId MerchantOf(PlayerCharacter player) => TryGetShopOf(player, out ShopData shop) ? shop.MerchantId : EntityId.None;

        /// <summary>Shops whose paid time is over close (lots back by mail).</summary>
        public void Update()
        {
            long now = _ctx.Now;
            _expired.Clear();
            foreach (ShopData shop in _market.Shops.Values)
            {
                if (now >= shop.ExpiresAt)
                {
                    _expired.Add(shop.Id);
                }
            }
            foreach (int id in _expired)
            {
                CloseShop(_market.Shops[id]);
            }
        }

        /// <summary>After loading the saved market: merchants of the open shops stand on their spots again.</summary>
        public void SpawnMerchants()
        {
            foreach (int id in new List<int>(_market.Shops.Keys))
            {
                ShopData shop = _market.Shops[id];
                shop.MerchantId = SpawnMerchant(shop);
                _market.Shops[id] = shop;
            }
        }

        public void OnPlayerRemoved(EntityId id) => _lastSearch.Remove(id);

        private void CloseShop(ShopData shop)
        {
            var items = shop.Lots.ConvertAll(l => l.Item);
            if (items.Count > 0)
            {
                _mail.SendSystem(shop.OwnerName, MailType.Return, MailSubjects.ShopClosed, null, items.Count, shop.Name, items, 0);
            }
            if (shop.MerchantId.IsValid && _ctx.World.Exists(shop.MerchantId))
            {
                _ctx.World.DestroyEntity(shop.MerchantId);
            }
            _market.Shops.Remove(shop.Id);
            Changed(EntityId.None);
        }

        private EntityId SpawnMerchant(ShopData shop)
        {
            StationDto look = _ctx.Content.World.MerchantVisual;
            Vec3 position = SpotPosition(shop.SpotId);
            ServerWorld world = _ctx.World;
            EntityId id = world.CreateEntity();
            world.Identities.Add(id, new IdentityData { Kind = EntityKind.Npc, DefinitionId = look?.Id, NameKey = look?.NameKey, DisplayName = shop.Name });
            world.Transforms.Add(id, new TransformData { Position = position });
            world.Stations.Add(id, new StationData { StationId = shop.SpotId, Type = StationTypes.Merchant });
            return id;
        }

        private Vec3 SpotPosition(string spotId)
        {
            foreach (StationDto station in _ctx.Content.World.Stations ?? new StationDto[0])
            {
                if (station.Id == spotId)
                {
                    return new Vec3(station.X, 0f, station.Z);
                }
            }
            return default;
        }

        private bool OwnShopNearby(PlayerCharacter player, out ShopData shop)
        {
            if (!TryGetShopOf(player, out shop))
            {
                CombatEvents.Reject(player, RejectReasons.ShopNotFound);
                return false;
            }
            if (!_stations.IsNear(player, shop.MerchantId, StationTypes.Merchant))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return false;
            }
            return true;
        }

        private bool TryGetShopOf(PlayerCharacter player, out ShopData shop)
        {
            foreach (ShopData candidate in _market.Shops.Values)
            {
                if (candidate.OwnerCharacterId == player.CharacterId)
                {
                    shop = candidate;
                    return true;
                }
            }
            shop = default;
            return false;
        }

        private bool TryGetByMerchant(EntityId merchant, out ShopData shop)
        {
            foreach (ShopData candidate in _market.Shops.Values)
            {
                if (candidate.MerchantId == merchant)
                {
                    shop = candidate;
                    return true;
                }
            }
            shop = default;
            return false;
        }

        private bool FindLot(long lotId, out ShopData shop, out int index)
        {
            foreach (ShopData candidate in _market.Shops.Values)
            {
                index = candidate.Lots.FindIndex(l => l.Id == lotId);
                if (index >= 0)
                {
                    shop = candidate;
                    return true;
                }
            }
            shop = default;
            index = -1;
            return false;
        }

        private PlayerCharacter FindOwner(ShopData shop)
        {
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                if (player.CharacterId == shop.OwnerCharacterId)
                {
                    return player;
                }
            }
            return null;
        }

        private bool Matches(MarketSearchCommand command, HashSet<string> ids, LotData lot, ItemDefinition definition)
        {
            if (ids != null && !ids.Contains(definition.Id))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(command.Category) && definition.Category.ToString() != command.Category)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(command.ClassId) && !definition.IsUsableBy(command.ClassId))
            {
                return false;
            }
            if ((command.MinLevel > 0 && definition.RequiredLevel < command.MinLevel) || (command.MaxLevel > 0 && definition.RequiredLevel > command.MaxLevel))
            {
                return false;
            }
            if (lot.Item.Sockets.Count < command.MinSockets || lot.Item.UpgradeLevel < command.MinUpgrade)
            {
                return false;
            }
            if (command.Rarity >= 0 && (int)definition.Rarity != command.Rarity)
            {
                return false;
            }
            return command.MaxPrice <= 0 || lot.PricePerUnit <= command.MaxPrice;
        }

        private static int Compare(MarketSort sort, LotData a, ItemDefinition da, LotData b, ItemDefinition db)
        {
            switch (sort)
            {
                case MarketSort.PriceDescending:
                    return b.PricePerUnit.CompareTo(a.PricePerUnit);
                case MarketSort.LevelAscending:
                    return da.RequiredLevel != db.RequiredLevel ? da.RequiredLevel.CompareTo(db.RequiredLevel) : a.PricePerUnit.CompareTo(b.PricePerUnit);
                case MarketSort.LevelDescending:
                    return da.RequiredLevel != db.RequiredLevel ? db.RequiredLevel.CompareTo(da.RequiredLevel) : a.PricePerUnit.CompareTo(b.PricePerUnit);
                case MarketSort.Newest:
                    return b.Id.CompareTo(a.Id);
                default:
                    return a.PricePerUnit != b.PricePerUnit ? a.PricePerUnit.CompareTo(b.PricePerUnit) : a.Id.CompareTo(b.Id);
            }
        }

        private static LotSnapshot Snapshot(ShopData shop, LotData lot) => new LotSnapshot
        {
            LotId = lot.Id,
            ShopName = shop.Name,
            MerchantId = shop.MerchantId,
            Item = SnapshotBuilder.Slot(lot.Item, 0),
            PricePerUnit = lot.PricePerUnit,
            PriceVersion = lot.PriceVersion,
        };

        private bool ValidPrice(long price) => price > 0 && price <= _ctx.Content.Config.MarketPriceMax;

        private ShopTierDto Tier(string id) => Array.Find(_ctx.Content.Config.ShopTiers ?? new ShopTierDto[0], t => t.Id == id);

        private void Changed(EntityId player)
        {
            _market.MarketVersion++;
            _market.Changed(player);
        }

        private string NameOf(PlayerCharacter player) => _ctx.NameOf(player);

        private static void Notify(PlayerCharacter player, string key, int number, string itemNameKey, bool positive) =>
            player.Outbox.Notifications.Add(new NotificationMessage { TextKey = key, Number = number, ItemNameKey = itemNameKey, IsPositive = positive });

        private void Audit(string action, string actor, ItemInstance item, int count, long amount, string reason) =>
            _ctx.Audit.Write(new AuditEntry
            {
                Time = _ctx.Time,
                Action = action,
                ActorId = actor,
                InstanceId = item.SourceId,
                DefinitionId = item.DefinitionId,
                Count = count,
                Amount = amount,
                Reason = reason,
            });
    }
}
