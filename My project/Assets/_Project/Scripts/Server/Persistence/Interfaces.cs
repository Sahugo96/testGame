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
    }

    /// <summary>One instance per unique definition (04, 7.11): TryReserve is atomic across threads (and later across servers).</summary>
    public interface IUniqueItemRegistry
    {
        bool TryReserve(string definitionId, string instanceId, string ownerId);
        void Release(string definitionId, string instanceId);
        string OwnerOf(string definitionId);
    }
}
