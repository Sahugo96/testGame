using System;
using System.Collections.Generic;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Upgrade orders with escrow (04, 7.14.1; question 30; module 20). At a whetstone the owner hands the server a bag item
    /// and the payment; the order is open to any smith or to the one named. A smith at a whetstone makes one attempt with
    /// his own gold and materials (UpgradeSystem.Attempt); the item — upgraded, downgraded or, if it broke, a notice — goes
    /// back to the owner by mail, the payment goes to the smith. The smith never holds the item. The owner may cancel an
    /// open order; after UpgradeOrderHours it expires; both return item and payment. Until professions (module 12) any
    /// player may act as a smith (question 66).
    /// </summary>
    public sealed class UpgradeOrderSystem
    {
        private readonly ServerContext _ctx;
        private readonly MarketState _market;
        private readonly WalletSystem _wallet;
        private readonly InventorySystem _inventory;
        private readonly UpgradeSystem _upgrade;
        private readonly MailSystem _mail;
        private readonly FriendSystem _friends;
        private readonly List<int> _expired = new List<int>();

        public UpgradeOrderSystem(ServerContext ctx, MarketState market, WalletSystem wallet, InventorySystem inventory, UpgradeSystem upgrade, MailSystem mail, FriendSystem friends)
        {
            _ctx = ctx;
            _market = market;
            _wallet = wallet;
            _inventory = inventory;
            _upgrade = upgrade;
            _mail = mail;
            _friends = friends;
        }

        public void Create(PlayerCharacter player, CreateUpgradeOrderCommand command)
        {
            if (!_upgrade.IsNearWhetstone(player, command.StationId))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return;
            }
            int mine = 0;
            foreach (UpgradeOrderData order in _market.Orders.Values)
            {
                mine += order.OwnerCharacterId == player.CharacterId ? 1 : 0;
            }
            if (mine >= _ctx.Content.Config.UpgradeOrdersMax)
            {
                CombatEvents.Reject(player, RejectReasons.OrderTooMany);
                return;
            }
            ItemInstance item = _inventory.FindInBag(player, command.ItemInstanceId);
            // The item never changes owner, so an item bound to this character may be ordered too.
            if (item == null || (item.BoundTo != null && item.BoundTo != player.CharacterId))
            {
                CombatEvents.Reject(player, RejectReasons.ItemNotFound);
                return;
            }
            if (!_upgrade.CanUpgrade(item))
            {
                CombatEvents.Reject(player, RejectReasons.CannotUpgrade);
                return;
            }
            if (command.Payment < 0 || command.Payment > _ctx.Content.Config.MarketPriceMax)
            {
                CombatEvents.Reject(player, RejectReasons.MarketBadPrice);
                return;
            }
            string smith = string.IsNullOrWhiteSpace(command.SmithName) ? null : command.SmithName.Trim();
            if (smith != null && (_friends.Store.AccountOf(smith) == null || string.Equals(smith, NameOf(player), StringComparison.OrdinalIgnoreCase)))
            {
                CombatEvents.Reject(player, RejectReasons.MailNoRecipient);
                return;
            }
            if (command.Payment > 0)
            {
                var payment = new[] { new WalletEntry { Player = player, CurrencyId = _ctx.Content.Config.GoldCurrencyId, Amount = command.Payment } };
                if (_wallet.Transact(payment, null, null, "order_escrow") != WalletResult.Ok)
                {
                    CombatEvents.Reject(player, RejectReasons.NotEnoughGold);
                    return;
                }
            }
            _inventory.RemoveFromBag(player, item);
            long now = _ctx.Now;
            var created = new UpgradeOrderData
            {
                Id = _market.NextOrderId++,
                OwnerCharacterId = player.CharacterId,
                OwnerName = NameOf(player),
                SmithName = smith,
                Item = item,
                Payment = command.Payment,
                CreatedAt = now,
                ExpiresAt = now + (long)(_ctx.Content.Config.UpgradeOrderHours * 3600f),
            };
            _market.Orders[created.Id] = created;
            Audit(AuditActions.EscrowIn, player.CharacterId, item, command.Payment, $"order:{created.Id}");
            Changed(player.EntityId);
            Notify(player, NotificationKeys.OrderCreated, created.Id);
        }

        /// <summary>The owner takes back an open order: the item into the bag (or by mail), the payment into the wallet (or by mail).</summary>
        public void Cancel(PlayerCharacter player, CancelUpgradeOrderCommand command)
        {
            if (!_market.Orders.TryGetValue(command.OrderId, out UpgradeOrderData order) || order.OwnerCharacterId != player.CharacterId)
            {
                CombatEvents.Reject(player, RejectReasons.OrderNotFound);
                return;
            }
            _market.Orders.Remove(order.Id);
            Audit(AuditActions.EscrowOut, player.CharacterId, order.Item, order.Payment, $"order:{order.Id}:cancel");
            bool placed = _inventory.Place(player, order.Item);
            bool paid = order.Payment <= 0 || _wallet.Credit(player, _ctx.Content.Config.GoldCurrencyId, order.Payment, $"order:{order.Id}:refund", "order_refund");
            if (!placed || !paid)
            {
                _mail.SendSystem(order.OwnerName, MailType.Return, MailSubjects.OrderReturned, order.Item.DefinitionId, order.Item.UpgradeLevel, null,
                    placed ? null : new List<ItemInstance> { order.Item }, paid ? 0 : order.Payment);
            }
            Changed(player.EntityId);
            Notify(player, NotificationKeys.OrderCancelled, order.Id);
        }

        public void Fulfill(PlayerCharacter smith, FulfillUpgradeOrderCommand command)
        {
            if (!_upgrade.IsNearWhetstone(smith, command.StationId))
            {
                CombatEvents.Reject(smith, RejectReasons.NotNearStation);
                return;
            }
            if (!_market.Orders.TryGetValue(command.OrderId, out UpgradeOrderData order))
            {
                CombatEvents.Reject(smith, RejectReasons.OrderNotFound);
                return;
            }
            if (!OpenTo(order, smith))
            {
                CombatEvents.Reject(smith, RejectReasons.OrderNotForYou);
                return;
            }
            UpgradeOutcome outcome = _upgrade.Attempt(smith, order.Item);
            if (outcome == UpgradeOutcome.Refused)
            {
                return;
            }
            _market.Orders.Remove(order.Id);
            Audit(AuditActions.EscrowOut, smith.CharacterId, order.Item, order.Payment, $"order:{order.Id}:{outcome}");
            if (outcome == UpgradeOutcome.Broken)
            {
                Audit(AuditActions.Destroy, smith.CharacterId, order.Item, 0, $"order:{order.Id}");
                _mail.SendSystem(order.OwnerName, MailType.System, MailSubjects.OrderBroken, order.Item.DefinitionId, 0, NameOf(smith), null, 0);
            }
            else
            {
                string subject = outcome == UpgradeOutcome.Success ? MailSubjects.OrderSuccess : MailSubjects.OrderFailed;
                _mail.SendSystem(order.OwnerName, MailType.System, subject, order.Item.DefinitionId, order.Item.UpgradeLevel, NameOf(smith),
                    new List<ItemInstance> { order.Item }, 0);
            }
            if (order.Payment > 0 && !_wallet.Credit(smith, _ctx.Content.Config.GoldCurrencyId, order.Payment, $"order:{order.Id}:pay", "order_payment"))
            {
                _mail.SendSystem(NameOf(smith), MailType.System, MailSubjects.OrderPayment, order.Item.DefinitionId, 0, order.OwnerName, null, order.Payment);
            }
            Changed(smith.EntityId);
            Notify(smith, NotificationKeys.OrderFulfilled, order.Id);
        }

        /// <summary>My orders and the ones open to me as a smith.</summary>
        public void View(PlayerCharacter player)
        {
            long now = _ctx.Now;
            var result = new List<UpgradeOrderSnapshot>();
            foreach (UpgradeOrderData order in _market.Orders.Values)
            {
                bool isMine = order.OwnerCharacterId == player.CharacterId;
                if (!isMine && !OpenTo(order, player))
                {
                    continue;
                }
                result.Add(new UpgradeOrderSnapshot
                {
                    Id = order.Id,
                    OwnerName = order.OwnerName,
                    SmithName = order.SmithName,
                    Item = SnapshotBuilder.Slot(order.Item, 0),
                    Payment = order.Payment,
                    SecondsLeft = Math.Max(0, order.ExpiresAt - now),
                    IsMine = isMine,
                });
            }
            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            player.Outbox.Replies.Add(new UpgradeOrdersMessage { Orders = result.ToArray() });
        }

        /// <summary>Expired orders return item and payment to the owner by mail.</summary>
        public void Update()
        {
            long now = _ctx.Now;
            _expired.Clear();
            foreach (UpgradeOrderData order in _market.Orders.Values)
            {
                if (now >= order.ExpiresAt)
                {
                    _expired.Add(order.Id);
                }
            }
            foreach (int id in _expired)
            {
                UpgradeOrderData order = _market.Orders[id];
                _market.Orders.Remove(id);
                Audit(AuditActions.EscrowOut, null, order.Item, order.Payment, $"order:{order.Id}:expired");
                _mail.SendSystem(order.OwnerName, MailType.Return, MailSubjects.OrderReturned, order.Item.DefinitionId, order.Item.UpgradeLevel, null,
                    new List<ItemInstance> { order.Item }, order.Payment);
                Changed(EntityId.None);
            }
        }

        private bool OpenTo(UpgradeOrderData order, PlayerCharacter smith) =>
            order.OwnerCharacterId != smith.CharacterId && (order.SmithName == null || string.Equals(order.SmithName, NameOf(smith), StringComparison.OrdinalIgnoreCase));

        private void Changed(EntityId player)
        {
            _market.OrdersVersion++;
            _market.Changed(player);
        }

        private string NameOf(PlayerCharacter player) => _ctx.NameOf(player);

        private static void Notify(PlayerCharacter player, string key, int number) =>
            player.Outbox.Notifications.Add(new NotificationMessage { TextKey = key, Number = number, IsPositive = true });

        private void Audit(string action, string actor, ItemInstance item, long amount, string reason) =>
            _ctx.Audit.Write(new AuditEntry
            {
                Time = _ctx.Time,
                Action = action,
                ActorId = actor,
                InstanceId = item.SourceId,
                DefinitionId = item.DefinitionId,
                Count = item.Count,
                Amount = amount,
                Reason = reason,
            });
    }
}
