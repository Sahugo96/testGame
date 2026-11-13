using System.Collections.Generic;
using Game.Server.Items;
using Game.Server.Players;
using Game.Shared.Content.Dto;

namespace Game.Server.Systems
{
    /// <summary>One posting of a wallet transaction: whose wallet, which currency, how much (always positive).</summary>
    public struct WalletEntry
    {
        public PlayerCharacter Player;
        public string CurrencyId;
        public long Amount;
    }

    public enum WalletResult
    {
        Ok,
        /// <summary>The same idempotency key was already applied: nothing happens again (treated as success).</summary>
        Duplicate,
        Insufficient,
        OverCap,
        Invalid,
    }

    /// <summary>
    /// Wallets (05, 7.14.2; module 11): balances are whole, never negative, capped per currency. Every change is one atomic
    /// Transact(debits, credits, idempotencyKey): all postings or none; a credit over Cap rejects the whole transaction; a
    /// repeated key does not apply twice. Each posting goes to the audit log.
    /// </summary>
    public sealed class WalletSystem
    {
        private readonly ServerContext _ctx;
        private readonly HashSet<string> _appliedKeys = new HashSet<string>();
        private readonly Queue<string> _keyOrder = new Queue<string>();
        private readonly Dictionary<(PlayerCharacter, string), long> _after = new Dictionary<(PlayerCharacter, string), long>();

        public WalletSystem(ServerContext ctx) => _ctx = ctx;

        public long Balance(PlayerCharacter player, string currencyId) =>
            player.Wallet.TryGetValue(currencyId, out long amount) ? amount : 0L;

        public WalletResult Transact(IList<WalletEntry> debits, IList<WalletEntry> credits, string idempotencyKey, string reason)
        {
            if (!string.IsNullOrEmpty(idempotencyKey) && _appliedKeys.Contains(idempotencyKey))
            {
                return WalletResult.Duplicate;
            }
            _after.Clear();
            debits = debits ?? new WalletEntry[0];
            credits = credits ?? new WalletEntry[0];
            foreach (WalletEntry entry in debits)
            {
                if (!Check(entry, -entry.Amount, out WalletResult error))
                {
                    return error;
                }
            }
            foreach (WalletEntry entry in credits)
            {
                if (!Check(entry, entry.Amount, out WalletResult error))
                {
                    return error;
                }
            }
            foreach (KeyValuePair<(PlayerCharacter player, string currency), long> pair in _after)
            {
                if (pair.Value < 0)
                {
                    return WalletResult.Insufficient;
                }
                if (pair.Value > _ctx.Content.Currencies[pair.Key.currency].Cap)
                {
                    return WalletResult.OverCap;
                }
            }
            foreach (KeyValuePair<(PlayerCharacter player, string currency), long> pair in _after)
            {
                pair.Key.player.Wallet[pair.Key.currency] = pair.Value;
            }
            Audit(debits, AuditActions.CurrencyDebit, reason, idempotencyKey);
            Audit(credits, AuditActions.CurrencyCredit, reason, idempotencyKey);
            Remember(idempotencyKey);
            return WalletResult.Ok;
        }

        /// <summary>
        /// Shortcut for a single credit (rewards, refunds): true only if the money is in the wallet now (or was already
        /// credited under this key). Callers send the money by mail instead when this is false.
        /// </summary>
        public bool Credit(PlayerCharacter player, string currencyId, long amount, string key, string reason)
        {
            if (amount <= 0)
            {
                return true;
            }
            WalletResult result = Transact(null, new[] { new WalletEntry { Player = player, CurrencyId = currencyId, Amount = amount } }, key, reason);
            return result == WalletResult.Ok || result == WalletResult.Duplicate;
        }

        private bool Check(WalletEntry entry, long delta, out WalletResult error)
        {
            error = WalletResult.Ok;
            if (entry.Player == null || entry.Amount <= 0 || !_ctx.Content.Currencies.ContainsKey(entry.CurrencyId ?? string.Empty))
            {
                error = WalletResult.Invalid;
                return false;
            }
            var key = (entry.Player, entry.CurrencyId);
            if (!_after.TryGetValue(key, out long balance))
            {
                balance = Balance(entry.Player, entry.CurrencyId);
            }
            _after[key] = balance + delta;
            return true;
        }

        private void Remember(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            _appliedKeys.Add(key);
            _keyOrder.Enqueue(key);
            while (_keyOrder.Count > _ctx.Content.Config.WalletIdempotencyMemory)
            {
                _appliedKeys.Remove(_keyOrder.Dequeue());
            }
        }

        private void Audit(IList<WalletEntry> entries, string action, string reason, string key)
        {
            foreach (WalletEntry entry in entries)
            {
                _ctx.Audit.Write(new AuditEntry
                {
                    Time = _ctx.Time,
                    Action = action,
                    ActorId = entry.Player.CharacterId,
                    InstanceId = key,
                    DefinitionId = entry.CurrencyId,
                    Amount = entry.Amount,
                    Reason = reason,
                });
            }
        }
    }
}
