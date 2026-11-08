using System;

namespace Game.Server.Persistence
{
    /// <summary>
    /// Saved data (module 13). Plain [Serializable] classes with public fields and arrays only, so the same JSON is read by
    /// Unity's JsonUtility and by System.Text.Json. SchemaVersion drives migrations (CharacterMigrations).
    /// </summary>
    [Serializable]
    public class AccountRecord
    {
        public int SchemaVersion;
        public string Login;
        public string PasswordHash;
        public string PasswordSalt;
        public int PasswordIterations;
        public string CreatedAt;
        /// <summary>Account setting (question 77): manual pickup. Stored inverted so older accounts (field missing = false) keep auto-pickup.</summary>
        public bool ManualLoot;
        /// <summary>Friends (module 14): character names; requests in both directions (friendship is mutual).</summary>
        public string[] Friends;
        public string[] FriendRequestsIn;
        public string[] FriendRequestsOut;
        /// <summary>Chat tabs (decision of the developer); none saved = the defaults from config.</summary>
        public ChatTabRecord[] ChatTabs;
        /// <summary>Controls, interface and game settings (module 17); missing = the defaults.</summary>
        public AccountSettingsRecord Settings;
    }

    /// <summary>The saved account settings document (02, 7.6): SchemaVersion drives future migrations.</summary>
    [Serializable]
    public class AccountSettingsRecord
    {
        public int SchemaVersion;
        public KeyBindingRecord[] Bindings;
        public float CameraSensitivity;
        public bool InvertY;
        public float ZoomSpeed;
        public float UiScale;
        public float PanelOpacity;
        public bool DamageNumbers;
        public bool Nameplates;
        public bool NameplateHealth;
    }

    [Serializable]
    public class KeyBindingRecord
    {
        public string BindingId;
        public string Path;
    }

    /// <summary>A chat tab: its own name or the default's NameKey, and the channels it shows (ChatChannel names).</summary>
    [Serializable]
    public class ChatTabRecord
    {
        public string Name;
        public string NameKey;
        public string[] Channels;
    }

    [Serializable]
    public class CharacterRecord
    {
        public int SchemaVersion;
        public string AccountLogin;
        public string ClassId;
        public string DisplayName;
        public string CharacterId;
        public int Level;
        public int Experience;
        public int AttributePoints;
        public int ClassPoints;
        public StatValueRecord[] Attributes;
        public SkillValueRecord[] Mastery;
        public ItemRecord[] Bag;
        public ItemRecord[] Equipment;
        public float Health;
        public float MainMana;
        public float ReserveMana;
        public float PositionX;
        public float PositionZ;
        /// <summary>Statuses are kept between sessions; their time runs only online (question 26).</summary>
        public StatusRecord[] Statuses;
        /// <summary>Wallet balances (05, 7.14.2), schema v2.</summary>
        public CurrencyRecord[] Currencies;
        public string SavedAt;
    }

    [Serializable]
    public class StatValueRecord
    {
        public string Stat;
        public float Value;
    }

    [Serializable]
    public class SkillValueRecord
    {
        public string SkillId;
        public float Value;
    }

    /// <summary>An item instance with its place: bag slot index or equipment slot id.</summary>
    [Serializable]
    public class ItemRecord
    {
        public int BagIndex;
        public string SlotId;
        public string InstanceId;
        public string DefinitionId;
        public int Count;
        public int UpgradeLevel;
        public string BoundTo;
        public SocketRecord[] Sockets;
        public AffixRecord[] Affixes;
    }

    [Serializable]
    public class SocketRecord
    {
        public string Type;
        public bool IsOvercap;
        public string IngredientId;
    }

    [Serializable]
    public class AffixRecord
    {
        public string Stat;
        public string Op;
        public float Value;
    }

    [Serializable]
    public class CurrencyRecord
    {
        public string Id;
        public long Amount;
    }

    [Serializable]
    public class StatusRecord
    {
        public string StatusId;
        public int Stacks;
        public float Remaining;
        public float Power;
        public float ShieldLeft;
    }

    /// <summary>Character names are unique per world: name → owning account.</summary>
    [Serializable]
    public class NameIndexRecord
    {
        public NameEntryRecord[] Entries;
    }

    [Serializable]
    public class NameEntryRecord
    {
        public string Name;
        public string Login;
    }

    /// <summary>Central registry of unique (divine) items: one instance per world (04, 7.9 / 7.11).</summary>
    [Serializable]
    public class UniqueRegistryRecord
    {
        public UniqueEntryRecord[] Entries;
    }

    [Serializable]
    public class UniqueEntryRecord
    {
        public string DefinitionId;
        public string InstanceId;
        public string OwnerId;
        public string Since;
    }
}
