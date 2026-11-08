using System.Collections.Generic;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Items;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Player-to-player trade (module 14): a request to a nearby player; both sides put bag items and gold; any change of an
    /// offer resets both confirmations; when both confirm, the server re-checks everything (ownership, tradability, bag room,
    /// gold) and swaps items (the same instances, GUIDs kept) and gold in one step — all or nothing — with audit entries.
    /// Bound and divine items cannot be traded (04 7.9). Walking away, combat, death or leaving cancels the trade.
    /// </summary>
    public sealed class TradeSystem
    {
        private readonly ServerContext _ctx;
        private readonly InventorySystem _inventory;
        private readonly WalletSystem _wallet;
        private readonly ResourceSystem _resources;
        private readonly Dictionary<int, TradeData> _trades = new Dictionary<int, TradeData>();
        private readonly Dictionary<EntityId, int> _byPlayer = new Dictionary<EntityId, int>();
        private readonly List<int> _ids = new List<int>();
        private int _nextId = 1;

        public TradeSystem(ServerContext ctx, InventorySystem inventory, WalletSystem wallet, ResourceSystem resources)
        {
            _ctx = ctx;
            _inventory = inventory;
            _wallet = wallet;
            _resources = resources;
        }

        public void Request(PlayerCharacter player, EntityId targetId)
        {
            PlayerCharacter target = _ctx.GetPlayer(targetId);
            if (target == null || target == player)
            {
                CombatEvents.Reject(player, RejectReasons.TradeInvalid);
                return;
            }
            if (_byPlayer.ContainsKey(player.EntityId) || _byPlayer.ContainsKey(targetId))
            {
                CombatEvents.Reject(player, RejectReasons.TradeBusy);
                return;
            }
            if (!CanStay(player.EntityId, targetId))
            {
                CombatEvents.Reject(player, RejectReasons.TradeTooFar);
                return;
            }
            var trade = new TradeData
            {
                Id = _nextId++,
                A = player.EntityId,
                B = targetId,
                RequestExpiresAt = _ctx.Time + _ctx.Content.Config.TradeRequestSeconds,
                OfferA = NewOffer(),
                OfferB = NewOffer(),
                PositionA = PositionOf(player.EntityId),
                PositionB = PositionOf(targetId),
            };
            _trades.Add(trade.Id, trade);
            _byPlayer[trade.A] = trade.Id;
            _byPlayer[trade.B] = trade.Id;
            target.Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.TradeRequested, IsPositive = true });
        }

        public void Respond(PlayerCharacter player, bool accept)
        {
            if (!TryGet(player.EntityId, out TradeData trade) || trade.Open || trade.B != player.EntityId)
            {
                CombatEvents.Reject(player, RejectReasons.TradeInvalid);
                return;
            }
            if (!accept)
            {
                Close(trade, NotificationKeys.TradeCancelled);
                return;
            }
            trade.Open = true;
            // From now on neither may move (the requester already had to stand still while waiting).
            trade.PositionB = PositionOf(trade.B);
            _trades[trade.Id] = trade;
        }

        public void SetOffer(PlayerCharacter player, TradeOfferCommand command)
        {
            if (!TryGet(player.EntityId, out TradeData trade) || !trade.Open)
            {
                CombatEvents.Reject(player, RejectReasons.TradeInvalid);
                return;
            }
            if (command.InstanceIds != null && command.InstanceIds.Length > _ctx.Content.Config.TradeMaxItems)
            {
                CombatEvents.Reject(player, RejectReasons.TradeInvalid);
                return;
            }
            var ids = new List<string>();
            foreach (string id in command.InstanceIds ?? new string[0])
            {
                if (ids.Contains(id))
                {
                    continue;
                }
                if (!IsTradable(player, id))
                {
                    CombatEvents.Reject(player, RejectReasons.TradeItemNotTradable);
                    return;
                }
                ids.Add(id);
            }
            if (command.Gold < 0 || command.Gold > _wallet.Balance(player, _ctx.Content.Config.GoldCurrencyId))
            {
                CombatEvents.Reject(player, RejectReasons.NotEnoughGold);
                return;
            }
            var offer = new TradeOffer { InstanceIds = ids, Counts = ids.ConvertAll(id => BagItem(player, id, out _).Count), Gold = command.Gold };
            if (trade.A == player.EntityId)
            {
                trade.OfferA = offer;
            }
            else
            {
                trade.OfferB = offer;
            }
            // Any change of an offer: both have to confirm again.
            trade.OfferA.Confirmed = false;
            trade.OfferB.Confirmed = false;
            _trades[trade.Id] = trade;
        }

        public void Confirm(PlayerCharacter player)
        {
            if (!TryGet(player.EntityId, out TradeData trade) || !trade.Open)
            {
                CombatEvents.Reject(player, RejectReasons.TradeInvalid);
                return;
            }
            if (trade.A == player.EntityId)
            {
                trade.OfferA.Confirmed = true;
            }
            else
            {
                trade.OfferB.Confirmed = true;
            }
            _trades[trade.Id] = trade;
            if (trade.OfferA.Confirmed && trade.OfferB.Confirmed)
            {
                Execute(trade);
            }
        }

        public void Cancel(PlayerCharacter player)
        {
            if (TryGet(player.EntityId, out TradeData trade))
            {
                Close(trade, NotificationKeys.TradeCancelled);
            }
        }

        public void OnPlayerGone(EntityId id)
        {
            if (TryGet(id, out TradeData trade))
            {
                Close(trade, NotificationKeys.TradeCancelled);
            }
        }

        public void Update()
        {
            _ids.Clear();
            _ids.AddRange(_trades.Keys);
            foreach (int id in _ids)
            {
                if (!_trades.TryGetValue(id, out TradeData trade))
                {
                    continue;
                }
                bool expired = !trade.Open && _ctx.Time >= trade.RequestExpiresAt;
                if (expired || !CanStay(trade.A, trade.B) || Moved(trade))
                {
                    Close(trade, NotificationKeys.TradeCancelled);
                    continue;
                }
                if (trade.Open)
                {
                    bool changedA = Refresh(_ctx.GetPlayer(trade.A), ref trade.OfferA);
                    bool changedB = Refresh(_ctx.GetPlayer(trade.B), ref trade.OfferB);
                    if (changedA || changedB)
                    {
                        trade.OfferA.Confirmed = false;
                        trade.OfferB.Confirmed = false;
                        _trades[trade.Id] = trade;
                    }
                }
            }
        }

        public TradeSnapshot Snapshot(EntityId id)
        {
            var snapshot = new TradeSnapshot();
            if (!TryGet(id, out TradeData trade))
            {
                return snapshot;
            }
            bool isA = trade.A == id;
            snapshot.PartnerId = isA ? trade.B : trade.A;
            snapshot.Phase = trade.Open ? TradePhase.Open : isA ? TradePhase.OutgoingRequest : TradePhase.IncomingRequest;
            snapshot.Remaining = trade.Open ? 0f : System.Math.Max(0f, trade.RequestExpiresAt - _ctx.Time);
            TradeOffer mine = isA ? trade.OfferA : trade.OfferB;
            TradeOffer theirs = isA ? trade.OfferB : trade.OfferA;
            snapshot.MyItems = Items(_ctx.GetPlayer(id), mine);
            snapshot.MyGold = mine.Gold;
            snapshot.MyConfirmed = mine.Confirmed;
            snapshot.TheirItems = Items(_ctx.GetPlayer(snapshot.PartnerId), theirs);
            snapshot.TheirGold = theirs.Gold;
            snapshot.TheirConfirmed = theirs.Confirmed;
            return snapshot;
        }

        /// <summary>Both confirmed: check everything again, then swap items and gold in one step (all or nothing).</summary>
        private void Execute(TradeData trade)
        {
            PlayerCharacter a = _ctx.GetPlayer(trade.A);
            PlayerCharacter b = _ctx.GetPlayer(trade.B);
            List<ItemInstance> fromA = Collect(a, trade.OfferA);
            List<ItemInstance> fromB = Collect(b, trade.OfferB);
            if (fromA == null || fromB == null)
            {
                // Something changed after the confirmations: both see the new offer and confirm again.
                Refresh(a, ref trade.OfferA);
                Refresh(b, ref trade.OfferB);
                trade.OfferA.Confirmed = false;
                trade.OfferB.Confirmed = false;
                _trades[trade.Id] = trade;
                CombatEvents.Reject(a, RejectReasons.TradeInvalid);
                CombatEvents.Reject(b, RejectReasons.TradeInvalid);
                return;
            }
            if (!HasRoom(b, fromA, fromB) || !HasRoom(a, fromB, fromA))
            {
                Close(trade, NotificationKeys.TradeNoRoom);
                return;
            }
            string gold = _ctx.Content.Config.GoldCurrencyId;
            var debits = new List<WalletEntry>();
            var credits = new List<WalletEntry>();
            AddGold(a, b, trade.OfferA.Gold, gold, debits, credits);
            AddGold(b, a, trade.OfferB.Gold, gold, debits, credits);
            if ((debits.Count > 0 || credits.Count > 0)
                && _wallet.Transact(debits, credits, $"trade:{trade.Id}", "trade") != WalletResult.Ok)
            {
                Close(trade, NotificationKeys.TradeFailed);
                return;
            }
            Move(a, b, fromA);
            Move(b, a, fromB);
            // Both sides are saved together right after the swap (GameServer.Committed): if one logged out and the server
            // then stopped, the other side's old save must not bring the given items back.
            _ctx.Commit(a.EntityId);
            _ctx.Commit(b.EntityId);
            Log.Info(LogTags.Items, $"Trade {trade.Id}: {a.CharacterId} gave {fromA.Count} items and {trade.OfferA.Gold} gold, {b.CharacterId} gave {fromB.Count} items and {trade.OfferB.Gold} gold");
            Close(trade, NotificationKeys.TradeDone);
        }

        private static void AddGold(PlayerCharacter from, PlayerCharacter to, long amount, string gold, List<WalletEntry> debits, List<WalletEntry> credits)
        {
            if (amount <= 0)
            {
                return;
            }
            debits.Add(new WalletEntry { Player = from, CurrencyId = gold, Amount = amount });
            credits.Add(new WalletEntry { Player = to, CurrencyId = gold, Amount = amount });
        }

        /// <summary>
        /// The offered instances, still in the giver's bag, tradable and of the confirmed size; null if anything changed
        /// (a stack split or drunk in the same tick as the last confirmation is not seen by Update yet).
        /// </summary>
        private List<ItemInstance> Collect(PlayerCharacter player, TradeOffer offer)
        {
            var items = new List<ItemInstance>();
            for (int i = 0; i < offer.InstanceIds.Count; i++)
            {
                string id = offer.InstanceIds[i];
                if (!IsTradable(player, id))
                {
                    return null;
                }
                ItemInstance item = BagItem(player, id, out _);
                if (item.Count != offer.Counts[i])
                {
                    return null;
                }
                items.Add(item);
            }
            return items;
        }

        /// <summary>Room check by bag section: free slots plus the slots the receiver gives away must hold what comes in.</summary>
        private bool HasRoom(PlayerCharacter receiver, List<ItemInstance> incoming, List<ItemInstance> outgoing)
        {
            var need = new Dictionary<BagSection, int>();
            foreach (ItemInstance item in incoming)
            {
                BagSection section = _inventory.SectionOf(_ctx.Content.Items[item.DefinitionId]);
                need.TryGetValue(section, out int count);
                need[section] = count + 1;
            }
            foreach (KeyValuePair<BagSection, int> pair in need)
            {
                int freed = 0;
                foreach (ItemInstance item in outgoing)
                {
                    freed += _inventory.SectionOf(_ctx.Content.Items[item.DefinitionId]) == pair.Key ? 1 : 0;
                }
                if (receiver.Bag.FreeSlotCount(pair.Key.Start, pair.Key.Size) + freed < pair.Value)
                {
                    return false;
                }
            }
            return true;
        }

        private void Move(PlayerCharacter from, PlayerCharacter to, List<ItemInstance> items)
        {
            foreach (ItemInstance item in items)
            {
                from.Bag.Set(from.Bag.IndexOf(item), null);
            }
            foreach (ItemInstance item in items)
            {
                BagSection section = _inventory.SectionOf(_ctx.Content.Items[item.DefinitionId]);
                to.Bag.Set(to.Bag.FindFreeSlot(section.Start, section.Size), item);
                _ctx.Audit.Write(new AuditEntry
                {
                    Time = _ctx.Time,
                    Action = AuditActions.TradeGive,
                    ActorId = from.CharacterId,
                    InstanceId = item.SourceId,
                    DefinitionId = item.DefinitionId,
                    Count = item.Count,
                    Reason = to.CharacterId,
                });
            }
        }

        private bool IsTradable(PlayerCharacter player, string instanceId)
        {
            ItemInstance item = BagItem(player, instanceId, out _);
            if (item == null || item.BoundTo != null)
            {
                return false;
            }
            return _ctx.Content.Items[item.DefinitionId].Rarity != Rarity.Divine;
        }

        private static ItemInstance BagItem(PlayerCharacter player, string instanceId, out int index)
        {
            for (index = 0; index < player.Bag.Size; index++)
            {
                ItemInstance item = player.Bag.Get(index);
                if (item != null && item.SourceId == instanceId)
                {
                    return item;
                }
            }
            index = -1;
            return null;
        }

        private ItemSlotSnapshot[] Items(PlayerCharacter owner, TradeOffer offer)
        {
            var result = new List<ItemSlotSnapshot>();
            if (owner == null)
            {
                return result.ToArray();
            }
            foreach (string id in offer.InstanceIds)
            {
                ItemInstance item = BagItem(owner, id, out int index);
                if (item != null)
                {
                    result.Add(new ItemSlotSnapshot { Index = index, InstanceId = item.SourceId, DefinitionId = item.DefinitionId, Count = item.Count, UpgradeLevel = item.UpgradeLevel });
                }
            }
            return result.ToArray();
        }

        /// <summary>Both alive, not in combat, within TradeRange.</summary>
        private bool CanStay(EntityId a, EntityId b)
        {
            ServerWorld world = _ctx.World;
            if (_ctx.GetPlayer(a) == null || _ctx.GetPlayer(b) == null || !world.IsAlive(a) || !world.IsAlive(b)
                || _resources.IsInCombat(a) || _resources.IsInCombat(b))
            {
                return false;
            }
            return Vec3.DistanceXZ(world.Transforms.Get(a).Position, world.Transforms.Get(b).Position) <= _ctx.Content.Config.TradeRange;
        }

        private void Close(TradeData trade, string key)
        {
            _trades.Remove(trade.Id);
            _byPlayer.Remove(trade.A);
            _byPlayer.Remove(trade.B);
            bool positive = key == NotificationKeys.TradeDone;
            _ctx.GetPlayer(trade.A)?.Outbox.Notifications.Add(new NotificationMessage { TextKey = key, IsPositive = positive });
            _ctx.GetPlayer(trade.B)?.Outbox.Notifications.Add(new NotificationMessage { TextKey = key, IsPositive = positive });
        }

        private bool TryGet(EntityId id, out TradeData trade)
        {
            if (_byPlayer.TryGetValue(id, out int tradeId))
            {
                trade = _trades[tradeId];
                return true;
            }
            trade = default;
            return false;
        }

        private static TradeOffer NewOffer() => new TradeOffer { InstanceIds = new List<string>(), Counts = new List<int>() };

        /// <summary>The requester moved, or (once open) either side moved: decision of the developer, cancel at once.</summary>
        private bool Moved(TradeData trade)
        {
            float tolerance = _ctx.Content.Config.TradeMoveTolerance;
            if (Vec3.DistanceXZ(PositionOf(trade.A), trade.PositionA) > tolerance)
            {
                return true;
            }
            return trade.Open && Vec3.DistanceXZ(PositionOf(trade.B), trade.PositionB) > tolerance;
        }

        private Vec3 PositionOf(EntityId id) => _ctx.World.Transforms.Has(id) ? _ctx.World.Transforms.Get(id).Position : default;

        /// <summary>
        /// Drops offered instances that left the bag and notes changed stack sizes (a potion drunk, a stack split) so a side
        /// never confirms one thing and gives another. True if the offer changed.
        /// </summary>
        private static bool Refresh(PlayerCharacter owner, ref TradeOffer offer)
        {
            if (owner == null)
            {
                return false;
            }
            bool changed = false;
            for (int i = offer.InstanceIds.Count - 1; i >= 0; i--)
            {
                ItemInstance item = BagItem(owner, offer.InstanceIds[i], out _);
                if (item == null)
                {
                    offer.InstanceIds.RemoveAt(i);
                    offer.Counts.RemoveAt(i);
                    changed = true;
                }
                else if (item.Count != offer.Counts[i])
                {
                    offer.Counts[i] = item.Count;
                    changed = true;
                }
            }
            return changed;
        }
    }
}
