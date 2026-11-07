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
        public EntityId TargetId;
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
