namespace Game.Server.Persistence
{
    /// <summary>JSON for saves, provided by the host (JsonUtility in Unity, System.Text.Json in tests and tools).</summary>
    public interface IJsonCodec
    {
        string ToJson(object value);
        T FromJson<T>(string json);
    }

    /// <summary>Accounts (module 13). The file store now; PostgreSQL behind the same interface later (question 76).</summary>
    public interface IAccountStore
    {
        AccountRecord LoadAccount(string login);
        void SaveAccount(AccountRecord account);
    }

    /// <summary>Characters by (account, class) and the world-wide name index.</summary>
    public interface ICharacterStore
    {
        CharacterRecord LoadCharacter(string login, string classId);
        void SaveCharacter(CharacterRecord character);
        /// <summary>Claims a character name for an account; false if another account owns it. Atomic.</summary>
        bool TryClaimName(string name, string login);

        /// <summary>Whether a character with this name exists in the world (any account).</summary>
        bool NameExists(string name);

        /// <summary>Account login that owns a character name (any case), or null.</summary>
        string LoginOfName(string name);
    }

    /// <summary>The market of module 20 as one record (file store now; tables in PostgreSQL later, question 76).</summary>
    public interface IMarketStore
    {
        MarketRecord LoadMarket();
        void SaveMarket(MarketRecord market);
    }

    /// <summary>
    /// Several characters and the market written as one unit: after a crash either all of them are the new version or all
    /// the old one (FileStorage: a journal file replayed on start; a database transaction later, question 76).
    /// </summary>
    public interface ICommitStore
    {
        /// <param name="market">Null when the market did not change.</param>
        void Commit(CharacterRecord[] characters, MarketRecord market);
    }

    /// <summary>One instance per unique definition (04, 7.11): TryReserve is atomic across threads (and later across servers).</summary>
    public interface IUniqueItemRegistry
    {
        bool TryReserve(string definitionId, string instanceId, string ownerId);
        void Release(string definitionId, string instanceId);
        string OwnerOf(string definitionId);
    }
}
