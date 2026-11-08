using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Persistence
{
    /// <summary>
    /// Accounts, login and saved characters (module 13). Registration happens on the first login with a new account name
    /// (temporarily, DEFAULT). An account keeps one character per class; character names are unique in the world. On login the
    /// server gives a session token that can replace the password until it expires. Characters are saved on leaving,
    /// every AutosaveSeconds and on shutdown; the state survives a server restart.
    /// </summary>
    public sealed class CharacterPersistence : Systems.IFriendStore
    {
        /// <summary>The local player of single player and host mode: no password.</summary>
        public const string LocalLogin = "local";

        private const int MinLoginLength = 3;
        private const int MaxLoginLength = 20;
        private const int MinNameLength = 2;
        private const int TokenBytes = 24;

        private readonly GameServer _server;
        private readonly IAccountStore _accounts;
        private readonly ICharacterStore _characters;
        private readonly GameConfig _config;
        private readonly Dictionary<EntityId, (string login, string classId)> _online = new Dictionary<EntityId, (string, string)>();
        private readonly Dictionary<string, (string login, DateTime expires)> _tokens = new Dictionary<string, (string, DateTime)>();
        private float _autosaveTimer;

        public CharacterPersistence(GameServer server, IAccountStore accounts, ICharacterStore characters, IMarketStore market = null)
        {
            _server = server;
            _accounts = accounts;
            _characters = characters;
            _config = server.Context.Content.Config;
            _server.AccountSettingsChanged += SaveAccountSettings;
            _server.Friends.Store = this;
            _market = market;
            if (market != null)
            {
                MarketSerializer.Import(_server.Market, market.LoadMarket(), _server.Context.Content, _server.Context.Time);
                _server.MarketLoaded();
            }
            _server.Committed += OnCommitted;
        }

        private readonly IMarketStore _market;

        /// <summary>
        /// Items or gold changed owners (trade, shop, mail, order): the characters involved and the market are written as one
        /// commit — atomically when the store supports it (ICommitStore), otherwise characters first, then the market.
        /// </summary>
        private void OnCommitted(IReadOnlyList<EntityId> players, bool marketChanged)
        {
            var records = new List<CharacterRecord>();
            foreach (EntityId id in players)
            {
                CharacterRecord record = Record(id);
                if (record != null)
                {
                    records.Add(record);
                }
            }
            MarketRecord market = marketChanged && _market != null ? MarketSerializer.Export(_server.Market) : null;
            if (_characters is ICommitStore commits)
            {
                commits.Commit(records.ToArray(), market);
                return;
            }
            foreach (CharacterRecord record in records)
            {
                _characters.SaveCharacter(record);
            }
            if (market != null)
            {
                _market.SaveMarket(market);
            }
        }

        public int Online => _online.Count;

        /// <summary>A second login of the same account replaced this character (it was saved and removed); close its connection.</summary>
        public event Action<EntityId> Replaced;

        /// <summary>Logs in (or registers) and brings the account's character of this class into the world.</summary>
        public EntityId Enter(string login, string password, string token, string name, string classId, bool trusted, out string error, out string newToken)
        {
            newToken = null;
            error = null;
            // One account per spelling: file names are case-insensitive, so "Bob" and "bob" are the same account and must
            // also be the same online session (otherwise the same character could be loaded twice).
            login = Canonical(login);
            if (!trusted && !Authenticate(login, password, token, out error))
            {
                return EntityId.None;
            }
            if (trusted)
            {
                login = LocalLogin;
            }
            if (!_server.HasClass(classId))
            {
                error = Game.Network.NetworkConstants.ErrorUnknownClass;
                return EntityId.None;
            }
            // A new character's name is checked before the account's live session is replaced: a typo must not kick it.
            if (_characters.LoadCharacter(login, classId) == null && !CanCreate(login, name, out error))
            {
                return EntityId.None;
            }
            if (!trusted)
            {
                foreach (KeyValuePair<EntityId, (string login, string classId)> online in new List<KeyValuePair<EntityId, (string, string)>>(_online))
                {
                    if (online.Value.login == login)
                    {
                        Leave(online.Key);
                        Replaced?.Invoke(online.Key);
                    }
                }
            }
            CharacterRecord record = _characters.LoadCharacter(login, classId);
            EntityId id;
            if (record != null)
            {
                id = _server.AddPlayer(record);
            }
            else
            {
                name = (name ?? string.Empty).Trim();
                if (!IsValidName(name))
                {
                    error = LoginErrors.BadName;
                    return EntityId.None;
                }
                if (!_characters.TryClaimName(name, login))
                {
                    error = LoginErrors.NameTaken;
                    return EntityId.None;
                }
                id = _server.AddPlayer(classId, name, Guid.NewGuid().ToString("N"));
                _online[id] = (login, classId);
                Save(id);
            }
            _online[id] = (login, classId);
            AccountRecord account = _accounts.LoadAccount(login);
            _server.GetPlayer(id).AutoLoot = account == null || !account.ManualLoot;
            ReadFriends(account, _server.GetPlayer(id).Friends);
            _server.LoadChatTabs(id, account?.ChatTabs);
            _server.LoadAccountSettings(id, account?.Settings);
            newToken = trusted ? null : IssueToken(login);
            return id;
        }

        /// <summary>Saves the character and removes it from the world (disconnect, leaving to the menu, switching class).</summary>
        public void Leave(EntityId id)
        {
            Save(id);
            _online.Remove(id);
            _server.RemovePlayer(id);
        }

        public void Save(EntityId id)
        {
            CharacterRecord record = Record(id);
            if (record != null)
            {
                _characters.SaveCharacter(record);
            }
        }

        /// <summary>The save of an online character, or null.</summary>
        private CharacterRecord Record(EntityId id)
        {
            if (!_online.TryGetValue(id, out var owner))
            {
                return null;
            }
            CharacterRecord record = _server.ExportPlayer(id);
            if (record == null)
            {
                return null;
            }
            record.AccountLogin = owner.login;
            record.SavedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            return record;
        }

        /// <summary>Account settings (auto-pickup) are saved at once; the local account gets a record on first change.</summary>
        private void SaveAccountSettings(EntityId id)
        {
            if (!_online.TryGetValue(id, out var owner))
            {
                return;
            }
            AccountRecord account = _accounts.LoadAccount(owner.login) ?? new AccountRecord { SchemaVersion = 1, Login = owner.login };
            account.ManualLoot = !_server.GetPlayer(id).AutoLoot;
            WriteFriends(account, _server.GetPlayer(id).Friends);
            account.ChatTabs = _server.ExportChatTabs(id);
            account.Settings = _server.ExportAccountSettings(id);
            _accounts.SaveAccount(account);
        }

        /// <summary>The canonical form of a login: trimmed, lower case (accounts are case-insensitive).</summary>
        public static string Canonical(string login) => (login ?? string.Empty).Trim().ToLowerInvariant();

        private bool CanCreate(string login, string name, out string error)
        {
            error = null;
            name = (name ?? string.Empty).Trim();
            if (!IsValidName(name))
            {
                error = LoginErrors.BadName;
                return false;
            }
            string owner = _characters.LoginOfName(name);
            if (owner != null && Canonical(owner) != login)
            {
                error = LoginErrors.NameTaken;
                return false;
            }
            return true;
        }

        string Systems.IFriendStore.AccountOf(string characterName)
        {
            string login = _characters.LoginOfName(characterName);
            return login == null ? null : Canonical(login);
        }

        string Systems.IFriendStore.AccountOf(EntityId player) => _online.TryGetValue(player, out var owner) ? owner.login : null;

        Players.FriendLists Systems.IFriendStore.Load(string account, out EntityId online)
        {
            foreach (KeyValuePair<EntityId, (string login, string classId)> pair in _online)
            {
                if (pair.Value.login == account)
                {
                    online = pair.Key;
                    return _server.GetPlayer(pair.Key).Friends;
                }
            }
            online = EntityId.None;
            var lists = new Players.FriendLists();
            ReadFriends(_accounts.LoadAccount(account), lists);
            return lists;
        }

        void Systems.IFriendStore.Save(string account, Players.FriendLists lists)
        {
            AccountRecord record = _accounts.LoadAccount(account) ?? new AccountRecord { SchemaVersion = 1, Login = account };
            WriteFriends(record, lists);
            _accounts.SaveAccount(record);
        }

        private static void ReadFriends(AccountRecord account, Players.FriendLists lists)
        {
            lists.Friends.AddRange(account?.Friends ?? new string[0]);
            lists.Incoming.AddRange(account?.FriendRequestsIn ?? new string[0]);
            lists.Outgoing.AddRange(account?.FriendRequestsOut ?? new string[0]);
        }

        private static void WriteFriends(AccountRecord account, Players.FriendLists lists)
        {
            account.Friends = lists.Friends.ToArray();
            account.FriendRequestsIn = lists.Incoming.ToArray();
            account.FriendRequestsOut = lists.Outgoing.ToArray();
        }

        public void SaveAll()
        {
            foreach (EntityId id in new List<EntityId>(_online.Keys))
            {
                Save(id);
            }
        }

        /// <summary>Autosave every AutosaveSeconds (call once per frame or tick).</summary>
        public void Update(float dt)
        {
            _autosaveTimer += dt;
            if (_autosaveTimer >= _config.AutosaveSeconds)
            {
                _autosaveTimer = 0f;
                SaveAll();
            }
        }

        private bool Authenticate(string login, string password, string token, out string error)
        {
            error = null;
            if (!string.IsNullOrEmpty(token))
            {
                if (_tokens.TryGetValue(token, out var session) && session.login == login && session.expires > DateTime.UtcNow)
                {
                    return true;
                }
                if (string.IsNullOrEmpty(password))
                {
                    error = LoginErrors.TokenExpired;
                    return false;
                }
            }
            if (!IsValidLogin(login) || login == LocalLogin || password == null || password.Length < _config.MinPasswordLength)
            {
                error = LoginErrors.BadLogin;
                return false;
            }
            AccountRecord account = _accounts.LoadAccount(login);
            if (account == null)
            {
                PasswordHasher.Hash(password, _config.PasswordIterations, out string hash, out string salt);
                _accounts.SaveAccount(new AccountRecord
                {
                    SchemaVersion = 1,
                    Login = login,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    PasswordIterations = _config.PasswordIterations,
                    CreatedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                });
                Log.Info(LogTags.Server, $"Account '{login}' registered");
                return true;
            }
            if (!PasswordHasher.Verify(password, account.PasswordHash, account.PasswordSalt, account.PasswordIterations))
            {
                error = LoginErrors.WrongPassword;
                return false;
            }
            return true;
        }

        private string IssueToken(string login)
        {
            var bytes = new byte[TokenBytes];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }
            string token = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            _tokens[token] = (login, DateTime.UtcNow.AddHours(_config.SessionTokenHours));
            return token;
        }

        private static bool IsValidLogin(string login)
        {
            if (login.Length < MinLoginLength || login.Length > MaxLoginLength)
            {
                return false;
            }
            foreach (char c in login)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsValidName(string name)
        {
            if (name.Length < MinNameLength || name.Length > _config.MaxPlayerNameLength)
            {
                return false;
            }
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '_' && c != '-')
                {
                    return false;
                }
            }
            return true;
        }
    }
}
