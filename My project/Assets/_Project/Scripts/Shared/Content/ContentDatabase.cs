using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;

namespace Game.Shared.Content
{
    /// <summary>All game content by stable id. Built once by <see cref="ContentBuilder"/>; read-only afterwards.</summary>
    public sealed class ContentDatabase
    {
        public GameConfig Config;
        public WorldDto World;
        public readonly Dictionary<string, ClassDefinition> Classes = new Dictionary<string, ClassDefinition>();
        public readonly Dictionary<string, SkillDefinition> Skills = new Dictionary<string, SkillDefinition>();
        public readonly Dictionary<string, SkillMasteryRule> MasteryRules = new Dictionary<string, SkillMasteryRule>();
        public readonly Dictionary<string, ItemDefinition> Items = new Dictionary<string, ItemDefinition>();
        public readonly List<EquipmentSlotDto> EquipmentSlots = new List<EquipmentSlotDto>();
        public readonly Dictionary<string, MobDto> Mobs = new Dictionary<string, MobDto>();
        public readonly List<HudWindowDto> HudWindows = new List<HudWindowDto>();
        public readonly HashSet<(string, string)> HostileFactions = new HashSet<(string, string)>();
        public readonly List<BagSection> BagSections = new List<BagSection>();
        public readonly Dictionary<string, UpgradeRuleDto> UpgradeRules = new Dictionary<string, UpgradeRuleDto>();
        /// <summary>Recipes by id (05, 7.4).</summary>
        public readonly Dictionary<string, RecipeDto> Recipes = new Dictionary<string, RecipeDto>();
        /// <summary>Recipes in file order (for the crafting window).</summary>
        public readonly List<RecipeDto> RecipeList = new List<RecipeDto>();
        /// <summary>Status effects by id (03, 7.12).</summary>
        public readonly Dictionary<string, StatusEffectDefinition> StatusEffects = new Dictionary<string, StatusEffectDefinition>();
        /// <summary>Mob AI profiles by id (module 10).</summary>
        public readonly Dictionary<string, AiProfile> AiProfiles = new Dictionary<string, AiProfile>();
        /// <summary>Currencies (05, 7.14.2) and affix pools (04, 7.9).</summary>
        public readonly Dictionary<string, CurrencyDto> Currencies = new Dictionary<string, CurrencyDto>();
        public readonly Dictionary<string, AffixPoolDto> AffixPools = new Dictionary<string, AffixPoolDto>();

        /// <summary>Socket limit for an item: the SocketCap of its slot type (04, 7.9.1); 0 for items that are not worn.</summary>
        public int SocketCapFor(Items.ItemDefinition item)
        {
            Items.EquipSlotComponent slot = item.Get<Items.EquipSlotComponent>();
            if (slot == null)
            {
                return 0;
            }
            foreach (EquipmentSlotDto definition in EquipmentSlots)
            {
                if (definition.SlotType == slot.SlotType)
                {
                    return definition.SocketCap;
                }
            }
            return 0;
        }

        /// <summary>Experience to the next level by level (index = level - 1); the last level is the cap.</summary>
        public readonly List<int> ExperienceToNext = new List<int>();

        public int BagSize
        {
            get
            {
                int size = 0;
                foreach (BagSection section in BagSections)
                {
                    size += section.Size;
                }
                return size;
            }
        }

        public int MaxLevel => ExperienceToNext.Count;

        /// <summary>Section index for an item category, or -1 when no section takes it.</summary>
        public int SectionIndexFor(ItemCategory category)
        {
            for (int i = 0; i < BagSections.Count; i++)
            {
                if (BagSections[i].Categories.Contains(category))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Experience needed from this level to the next; 0 at the level cap.</summary>
        public int ExperienceFor(int level) => level >= 1 && level < MaxLevel ? ExperienceToNext[level - 1] : 0;

        public bool AreHostile(string factionA, string factionB) =>
            factionA != factionB && (HostileFactions.Contains((factionA, factionB)) || HostileFactions.Contains((factionB, factionA)));

        public SkillMasteryRule GetMasteryRule(string skillId) =>
            MasteryRules.TryGetValue(skillId, out SkillMasteryRule rule) ? rule : null;

        /// <summary>Equipment slots available to a class (extra ring slots depend on the class, question 36).</summary>
        public IEnumerable<EquipmentSlotDto> SlotsFor(ClassDefinition classDefinition)
        {
            foreach (EquipmentSlotDto slot in EquipmentSlots)
            {
                if (slot.MinRingSlots <= classDefinition.RingSlots)
                {
                    yield return slot;
                }
            }
        }
    }
}
