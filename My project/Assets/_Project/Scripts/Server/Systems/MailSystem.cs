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
    /// Mail (05, 7.14.3; module 20). Letters to a character name, also offline; attachments (bag items, gold) are held by
    /// the server and claimed all at once at a mailbox (same GUIDs, one wallet transaction). Player letters expire after
    /// MailExpireDays and go back to the sender; system and returned letters never expire (question 50). Sending costs
    /// MailFeePerAttachment per attachment (gold sink). Shops and upgrade orders deliver through <see cref="SendSystem"/>.
    /// </summary>
    public sealed class MailSystem
    {
        private const long SecondsPerDay = 24 * 60 * 60;

        private readonly ServerContext _ctx;
        private readonly MarketState _market;
        private readonly WalletSystem _wallet;
        private readonly InventorySystem _inventory;
        private readonly StationSystem _stations;
        private readonly ChatSystem _chat;
        private readonly FriendSystem _friends;
        private readonly RequestMemory _requests;
        private readonly List<long> _ids = new List<long>();

        public MailSystem(ServerContext ctx, MarketState market, WalletSystem wallet, InventorySystem inventory, StationSystem stations, ChatSystem chat, FriendSystem friends)
        {
            _ctx = ctx;
            _market = market;
            _wallet = wallet;
            _inventory = inventory;
            _stations = stations;
            _chat = chat;
            _friends = friends;
            _requests = new RequestMemory(ctx.Content.Config.MarketRequestMemory);
        }

        /// <summary>A letter from the system (sale income, returns, purchases, orders): always delivered, never expires.</summary>
        public void SendSystem(string recipient, MailType type, string subjectKey, string itemDefinitionId, int number, string detail, List<ItemInstance> items, long gold)
        {
            Deliver(new MailData
            {
                Type = type,
                Recipient = recipient,
                SubjectKey = subjectKey,
                ItemDefinitionId = itemDefinitionId,
                Number = number,
                Detail = detail,
                Items = items ?? new List<ItemInstance>(),
                Gold = gold,
                SentAt = _ctx.Now,
            });
        }

        public void Send(PlayerCharacter player, SendMailCommand command)
        {
            if (!NearMailbox(player))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return;
            }
            string recipient = (command.Recipient ?? string.Empty).Trim();
            string self = NameOf(player);
            if (recipient.Length == 0 || string.Equals(recipient, self, StringComparison.OrdinalIgnoreCase) || _friends.Store.AccountOf(recipient) == null)
            {
                CombatEvents.Reject(player, RejectReasons.MailNoRecipient);
                return;
            }
            PlayerCharacter online = _chat.FindByName(recipient);
            if (online != null)
            {
                recipient = NameOf(online);
            }
            string subject = MarketText.Clean(command.Subject, _ctx.Content.Config.MailSubjectMax);
            string body = MarketText.Clean(command.Body, _ctx.Content.Config.MailBodyMax);
            string[] ids = command.InstanceIds ?? new string[0];
            if (subject.Length == 0 && body.Length == 0 && ids.Length == 0 && command.Gold <= 0)
            {
                CombatEvents.Reject(player, RejectReasons.MailBadText);
                return;
            }
            if (ids.Length > _ctx.Content.Config.MailMaxAttachments)
            {
                CombatEvents.Reject(player, RejectReasons.MailTooManyItems);
                return;
            }
            var items = new List<ItemInstance>();
            foreach (string id in ids)
            {
                ItemInstance item = _inventory.FindInBag(player, id);
                if (item == null || items.Contains(item) || !_inventory.IsTradable(item))
                {
                    CombatEvents.Reject(player, RejectReasons.ItemNotTradable);
                    return;
                }
                items.Add(item);
            }
            if (command.Gold < 0 || command.Gold > _ctx.Content.Config.MarketPriceMax)
            {
                CombatEvents.Reject(player, RejectReasons.MarketBadPrice);
                return;
            }
            if (CountFor(recipient) >= _ctx.Content.Config.MailboxMax)
            {
                CombatEvents.Reject(player, RejectReasons.MailRecipientFull);
                return;
            }
            if (!_requests.TryAdd($"mail:{player.CharacterId}:{command.RequestId}"))
            {
                return;
            }
            long cost = command.Gold + _ctx.Content.Config.MailFeePerAttachment * items.Count;
            if (cost > 0)
            {
                var debit = new[] { new WalletEntry { Player = player, CurrencyId = _ctx.Content.Config.GoldCurrencyId, Amount = cost } };
                WalletResult paid = _wallet.Transact(debit, null, $"mail:{player.CharacterId}:{command.RequestId}", "mail");
                if (paid == WalletResult.Duplicate)
                {
                    return;
                }
                if (paid != WalletResult.Ok)
                {
                    CombatEvents.Reject(player, RejectReasons.NotEnoughGold);
                    return;
                }
            }
            foreach (ItemInstance item in items)
            {
                _inventory.RemoveFromBag(player, item);
                Audit(AuditActions.MailSend, player.CharacterId, item, recipient);
            }
            if (command.Gold >= _ctx.Content.Config.MarketSuspiciousGold)
            {
                Log.Warning(LogTags.Items, $"Large mail: {player.CharacterId} sent {command.Gold} gold to '{recipient}'");
            }
            Deliver(new MailData
            {
                Type = MailType.Player,
                Recipient = recipient,
                Sender = self,
                Subject = subject,
                Body = body,
                Items = items,
                Gold = command.Gold,
                SentAt = _ctx.Now,
                ExpiresAt = _ctx.Now + (long)(_ctx.Content.Config.MailExpireDays * SecondsPerDay),
            });
            _market.Changed(player.EntityId);
            Notify(player, NotificationKeys.MailSent, 0, null, true);
        }

        /// <summary>Everything attached, all or nothing: room for every item and the gold within the wallet cap.</summary>
        public void Claim(PlayerCharacter player, ClaimMailCommand command)
        {
            if (!NearMailbox(player))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return;
            }
            if (!TryGetOwn(player, command.MailId, out MailData mail) || mail.Claimed)
            {
                CombatEvents.Reject(player, RejectReasons.MailNotFound);
                return;
            }
            if (!_inventory.CanPlaceAll(player, mail.Items))
            {
                CombatEvents.Reject(player, RejectReasons.BagFull);
                return;
            }
            if (mail.Gold > 0)
            {
                var credit = new[] { new WalletEntry { Player = player, CurrencyId = _ctx.Content.Config.GoldCurrencyId, Amount = mail.Gold } };
                WalletResult result = _wallet.Transact(null, credit, $"claim:{mail.Id}", "mail");
                if (result != WalletResult.Ok && result != WalletResult.Duplicate)
                {
                    CombatEvents.Reject(player, RejectReasons.WalletFull);
                    return;
                }
            }
            foreach (ItemInstance item in mail.Items)
            {
                _inventory.Place(player, item);
                Audit(AuditActions.MailClaim, player.CharacterId, item, mail.Sender);
            }
            mail.Items = new List<ItemInstance>();
            mail.Gold = 0;
            mail.Claimed = true;
            mail.Read = true;
            Store(mail);
            _market.Changed(player.EntityId);
            Notify(player, NotificationKeys.MailClaimed, 0, null, true);
        }

        public void Read(PlayerCharacter player, ReadMailCommand command)
        {
            if (TryGetOwn(player, command.MailId, out MailData mail) && !mail.Read)
            {
                mail.Read = true;
                Store(mail);
            }
        }

        public void Delete(PlayerCharacter player, DeleteMailCommand command)
        {
            if (!TryGetOwn(player, command.MailId, out MailData mail))
            {
                CombatEvents.Reject(player, RejectReasons.MailNotFound);
                return;
            }
            if (mail.Items.Count > 0 || mail.Gold > 0)
            {
                CombatEvents.Reject(player, RejectReasons.MailHasAttachments);
                return;
            }
            _market.Mail.Remove(mail.Id);
            _market.MailChanged(mail.Recipient);
        }

        public void View(PlayerCharacter player)
        {
            if (!NearMailbox(player))
            {
                CombatEvents.Reject(player, RejectReasons.NotNearStation);
                return;
            }
            string name = NameOf(player);
            long now = _ctx.Now;
            var letters = new List<MailSnapshot>();
            foreach (MailData mail in _market.Mail.Values)
            {
                if (!string.Equals(mail.Recipient, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var items = new ItemSlotSnapshot[mail.Items.Count];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = SnapshotBuilder.Slot(mail.Items[i], i);
                }
                letters.Add(new MailSnapshot
                {
                    Id = mail.Id, Type = mail.Type, From = mail.Sender, Subject = mail.Subject, Body = mail.Body, SubjectKey = mail.SubjectKey,
                    ItemDefinitionId = mail.ItemDefinitionId, Number = mail.Number, Detail = mail.Detail, Items = items, Gold = mail.Gold,
                    SecondsLeft = mail.ExpiresAt > 0 ? Math.Max(0, mail.ExpiresAt - now) : -1, Read = mail.Read, Claimed = mail.Claimed,
                });
            }
            letters.Sort((a, b) => b.Id.CompareTo(a.Id));
            player.Outbox.Replies.Add(new MailboxMessage { Mail = letters.ToArray() });
        }

        /// <summary>Unread letters and the mailbox version for the player snapshot.</summary>
        public void Fill(PlayerCharacter player, MarketStateSnapshot snapshot)
        {
            // Called for every player's state: the letters are counted again only when this mailbox changed (its version).
            if (!_unread.TryGetValue(player.EntityId, out UnreadCache cache))
            {
                cache = new UnreadCache { Name = NameOf(player), Version = -1 };
                cache.Key = cache.Name.ToLowerInvariant();
            }
            _market.MailVersions.TryGetValue(cache.Key, out int version);
            if (version != cache.Version)
            {
                cache.Version = version;
                cache.Count = 0;
                foreach (MailData mail in _market.Mail.Values)
                {
                    if (!mail.Read && string.Equals(mail.Recipient, cache.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        cache.Count++;
                    }
                }
            }
            _unread[player.EntityId] = cache;
            snapshot.UnreadMail = cache.Count;
            snapshot.MailVersion = version;
        }

        public void OnPlayerRemoved(EntityId id) => _unread.Remove(id);

        /// <summary>Unread letters of an online player, valid for one version of the mailbox.</summary>
        private struct UnreadCache
        {
            public string Name;
            public string Key;
            public int Version;
            public int Count;
        }

        private readonly Dictionary<EntityId, UnreadCache> _unread = new Dictionary<EntityId, UnreadCache>();

        /// <summary>Player letters past their time go back to the sender with what is still attached.</summary>
        public void Update()
        {
            long now = _ctx.Now;
            _ids.Clear();
            foreach (MailData mail in _market.Mail.Values)
            {
                if (mail.Type == MailType.Player && mail.ExpiresAt > 0 && now >= mail.ExpiresAt)
                {
                    _ids.Add(mail.Id);
                }
            }
            foreach (long id in _ids)
            {
                MailData mail = _market.Mail[id];
                _market.Mail.Remove(id);
                _market.MailChanged(mail.Recipient);
                if (mail.Items.Count == 0 && mail.Gold <= 0)
                {
                    continue;
                }
                foreach (ItemInstance item in mail.Items)
                {
                    Audit(AuditActions.MailReturn, null, item, mail.Sender);
                }
                SendSystem(mail.Sender, MailType.Return, MailSubjects.Returned, null, 0, mail.Recipient, mail.Items, mail.Gold);
            }
        }

        private void Deliver(MailData mail)
        {
            mail.Id = _market.NextMailId++;
            mail.Items = mail.Items ?? new List<ItemInstance>();
            _market.Mail[mail.Id] = mail;
            _market.MailChanged(mail.Recipient);
            PlayerCharacter online = _chat.FindByName(mail.Recipient);
            if (online != null)
            {
                Notify(online, NotificationKeys.MailNew, 0, null, true);
            }
        }

        private void Store(MailData mail)
        {
            _market.Mail[mail.Id] = mail;
            _market.MailChanged(mail.Recipient);
        }

        private bool TryGetOwn(PlayerCharacter player, long id, out MailData mail) =>
            _market.Mail.TryGetValue(id, out mail) && string.Equals(mail.Recipient, NameOf(player), StringComparison.OrdinalIgnoreCase);

        private int CountFor(string recipient)
        {
            int count = 0;
            foreach (MailData mail in _market.Mail.Values)
            {
                count += string.Equals(mail.Recipient, recipient, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            }
            return count;
        }

        private bool NearMailbox(PlayerCharacter player) => _stations.IsNearAny(player, StationTypes.Mailbox);

        private string NameOf(PlayerCharacter player) => _ctx.NameOf(player);

        private static void Notify(PlayerCharacter player, string key, int number, string itemNameKey, bool positive) =>
            player.Outbox.Notifications.Add(new NotificationMessage { TextKey = key, Number = number, ItemNameKey = itemNameKey, IsPositive = positive });

        private void Audit(string action, string actor, ItemInstance item, string other) =>
            _ctx.Audit.Write(new AuditEntry
            {
                Time = _ctx.Time,
                Action = action,
                ActorId = actor,
                InstanceId = item.SourceId,
                DefinitionId = item.DefinitionId,
                Count = item.Count,
                Reason = other,
            });
    }
}
