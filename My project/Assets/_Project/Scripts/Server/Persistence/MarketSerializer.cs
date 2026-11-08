using System;
using System.Collections.Generic;
using Game.Server.Items;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Persistence
{
    /// <summary>MarketState ↔ MarketRecord. Items keep their GUIDs; an item whose definition left the content is skipped (logged).</summary>
    public static class MarketSerializer
    {
        public const int Current = 1;

        public static MarketRecord Export(MarketState market)
        {
            var shops = new List<ShopRecord>();
            foreach (ShopData shop in market.Shops.Values)
            {
                shops.Add(new ShopRecord
                {
                    Id = shop.Id, OwnerCharacterId = shop.OwnerCharacterId, OwnerName = shop.OwnerName, Name = shop.Name, TierId = shop.TierId,
                    SpotId = shop.SpotId, OpenedAt = shop.OpenedAt, ExpiresAt = shop.ExpiresAt,
                    Lots = shop.Lots.ConvertAll(l => new LotRecord { Id = l.Id, PricePerUnit = l.PricePerUnit, PriceVersion = l.PriceVersion, ListedAt = l.ListedAt, Item = ItemRecords.Export(l.Item, -1, null) }).ToArray(),
                });
            }
            var mail = new List<MailRecord>();
            foreach (MailData letter in market.Mail.Values)
            {
                mail.Add(new MailRecord
                {
                    Id = letter.Id, Type = letter.Type.ToString(), Recipient = letter.Recipient, Sender = letter.Sender, Subject = letter.Subject, Body = letter.Body,
                    SubjectKey = letter.SubjectKey, ItemDefinitionId = letter.ItemDefinitionId, Number = letter.Number, Detail = letter.Detail,
                    Items = letter.Items.ConvertAll(i => ItemRecords.Export(i, -1, null)).ToArray(),
                    Gold = letter.Gold, SentAt = letter.SentAt, ExpiresAt = letter.ExpiresAt, Read = letter.Read, Claimed = letter.Claimed,
                });
            }
            var orders = new List<UpgradeOrderRecord>();
            foreach (UpgradeOrderData order in market.Orders.Values)
            {
                orders.Add(new UpgradeOrderRecord
                {
                    Id = order.Id, OwnerCharacterId = order.OwnerCharacterId, OwnerName = order.OwnerName, SmithName = order.SmithName,
                    Item = ItemRecords.Export(order.Item, -1, null), Payment = order.Payment, CreatedAt = order.CreatedAt, ExpiresAt = order.ExpiresAt,
                });
            }
            return new MarketRecord
            {
                SchemaVersion = Current,
                NextShopId = market.NextShopId,
                NextLotId = market.NextLotId,
                NextMailId = market.NextMailId,
                NextOrderId = market.NextOrderId,
                Shops = shops.ToArray(),
                Mail = mail.ToArray(),
                Orders = orders.ToArray(),
            };
        }

        public static void Import(MarketState market, MarketRecord record, ContentDatabase content, float time)
        {
            if (record == null)
            {
                return;
            }
            market.NextShopId = Math.Max(1, record.NextShopId);
            market.NextLotId = Math.Max(1, record.NextLotId);
            market.NextMailId = Math.Max(1, record.NextMailId);
            market.NextOrderId = Math.Max(1, record.NextOrderId);
            foreach (ShopRecord shop in record.Shops ?? new ShopRecord[0])
            {
                var lots = new List<LotData>();
                foreach (LotRecord lot in shop.Lots ?? new LotRecord[0])
                {
                    ItemInstance item = ItemRecords.Restore(content, lot.Item, time);
                    if (item != null)
                    {
                        lots.Add(new LotData { Id = lot.Id, PricePerUnit = lot.PricePerUnit, PriceVersion = lot.PriceVersion, ListedAt = lot.ListedAt, Item = item });
                    }
                }
                market.Shops[shop.Id] = new ShopData
                {
                    Id = shop.Id, OwnerCharacterId = shop.OwnerCharacterId, OwnerName = shop.OwnerName, Name = shop.Name, TierId = shop.TierId,
                    SpotId = shop.SpotId, OpenedAt = shop.OpenedAt, ExpiresAt = shop.ExpiresAt, Lots = lots, MerchantId = EntityId.None,
                };
            }
            foreach (MailRecord letter in record.Mail ?? new MailRecord[0])
            {
                market.Mail[letter.Id] = new MailData
                {
                    Id = letter.Id, Type = Enum.TryParse(letter.Type, out MailType type) ? type : MailType.System, Recipient = letter.Recipient, Sender = letter.Sender,
                    Subject = letter.Subject, Body = letter.Body, SubjectKey = letter.SubjectKey, ItemDefinitionId = letter.ItemDefinitionId, Number = letter.Number,
                    Detail = letter.Detail, Items = Restore(content, letter.Items, time), Gold = letter.Gold, SentAt = letter.SentAt, ExpiresAt = letter.ExpiresAt,
                    Read = letter.Read, Claimed = letter.Claimed,
                };
            }
            foreach (UpgradeOrderRecord order in record.Orders ?? new UpgradeOrderRecord[0])
            {
                ItemInstance item = ItemRecords.Restore(content, order.Item, time);
                if (item == null)
                {
                    continue;
                }
                market.Orders[order.Id] = new UpgradeOrderData
                {
                    Id = order.Id, OwnerCharacterId = order.OwnerCharacterId, OwnerName = order.OwnerName, SmithName = string.IsNullOrEmpty(order.SmithName) ? null : order.SmithName,
                    Item = item, Payment = order.Payment, CreatedAt = order.CreatedAt, ExpiresAt = order.ExpiresAt,
                };
            }
        }

        private static List<ItemInstance> Restore(ContentDatabase content, ItemRecord[] records, float time)
        {
            var items = new List<ItemInstance>();
            foreach (ItemRecord record in records ?? new ItemRecord[0])
            {
                ItemInstance item = ItemRecords.Restore(content, record, time);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            return items;
        }
    }
}
