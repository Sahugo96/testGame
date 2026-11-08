using System.Collections.Generic;
using Game.Server.Items;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using Game.Shared.Rules;

namespace Game.Server.Players
{
    /// <summary>Server-side state of a player character that is not per-tick simulation data: class, stats, items, mastery, cooldowns.</summary>
    public sealed class PlayerCharacter
    {
        public EntityId EntityId;
        public string CharacterId;
        public ClassDefinition Class;
        public int Level;
        public int Experience;
        public int AttributePoints;
        public int ClassPoints;
        public readonly StatSheet Stats = new StatSheet();
        public readonly Dictionary<StatId, float> Attributes = new Dictionary<StatId, float>();
        public Inventory Bag;
        public readonly Dictionary<string, ItemInstance> Equipment = new Dictionary<string, ItemInstance>();
        public readonly Dictionary<string, float> Mastery = new Dictionary<string, float>();
        public readonly Dictionary<string, SkillCooldown> SkillCooldowns = new Dictionary<string, SkillCooldown>();
        public readonly Dictionary<string, float> ConsumableCooldowns = new Dictionary<string, float>();
        public readonly WeaponSwapState WeaponSwap = new WeaponSwapState();
        public CraftData Craft;
        public EntityId TargetId;
        /// <summary>Account setting (developer decision, question 77): drops go straight into the bag; off — they lie on the ground to be picked up.</summary>
        public bool AutoLoot = true;
        /// <summary>Wallet { Balances: { CurrencyId -> long } } (05, 7.14.2); changed only through WalletSystem.Transact.</summary>
        public readonly Dictionary<string, long> Wallet = new Dictionary<string, long>();
        /// <summary>Friends and friend requests (module 14); an account setting, saved with the account.</summary>
        public readonly FriendLists Friends = new FriendLists();
        /// <summary>Chat tabs of the account (decision of the developer); null = the defaults from config.</summary>
        public ChatTabSnapshot[] ChatTabs;
        /// <summary>Controls, interface and game settings of the account (module 17); null = the defaults from config.</summary>
        public AccountSettingsSnapshot AccountSettings;
        /// <summary>The settings document is to be sent to the client (after login or a change).</summary>
        public bool SettingsToSend = true;
        public MoveCommand LastMove;
        public readonly PlayerOutbox Outbox = new PlayerOutbox();
        public readonly Queue<IGameCommand> PendingCommands = new Queue<IGameCommand>();

        public float GetMastery(string skillId) => Mastery.TryGetValue(skillId, out float value) ? value : 0f;

        public bool KnowsSkill(string skillId)
        {
            foreach (ClassSkill skill in Class.Skills)
            {
                if (skill.SkillId == skillId && skill.LevelUnlock <= Level)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
