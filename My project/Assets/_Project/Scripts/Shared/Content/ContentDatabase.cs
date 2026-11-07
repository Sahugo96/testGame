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
