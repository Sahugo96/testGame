using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;

namespace Game.Shared.Content
{
    /// <summary>Cross-reference checks (01, 7.10 principle 6): broken links, missing required fields and component dependencies.</summary>
    public static class ContentValidator
    {
        public static void Validate(ContentDatabase db, ItemComponentRegistry registry, ICollection<string> knownEffectKeys, List<string> errors)
        {
            GameConfig cfg = db.Config;
            if (cfg.TickRate <= 0)
            {
                errors.Add("config: TickRate must be > 0");
            }
            if (cfg.BagSize <= 0)
            {
                errors.Add("config: BagSize must be > 0");
            }
            if (!db.Classes.ContainsKey(cfg.DefaultClassId ?? string.Empty))
            {
                errors.Add($"config: DefaultClassId '{cfg.DefaultClassId}' is not a class");
            }

            foreach (SkillDefinition skill in db.Skills.Values)
            {
                if (skill.Default == null)
                {
                    errors.Add($"skill '{skill.Id}': Default variant is required");
                }
                if (string.IsNullOrEmpty(skill.NameKey))
                {
                    errors.Add($"skill '{skill.Id}': NameKey is empty");
                }
                foreach (SkillVariant variant in new[] { skill.Default, skill.Empowered, skill.Weakened })
                {
                    if (variant == null)
                    {
                        continue;
                    }
                    foreach (EffectDefinition effect in variant.Effects)
                    {
                        if (effect.Key == null || !knownEffectKeys.Contains(effect.Key))
                        {
                            errors.Add($"skill '{skill.Id}': unknown effect '{effect.Key}'");
                        }
                    }
                }
            }
            foreach (SkillMasteryRule rule in db.MasteryRules.Values)
            {
                if (!db.Skills.ContainsKey(rule.SkillId))
                {
                    errors.Add($"skill_mastery: unknown skill '{rule.SkillId}'");
                }
            }

            var slotTypes = new HashSet<string>();
            foreach (EquipmentSlotDto slot in db.EquipmentSlots)
            {
                slotTypes.Add(slot.SlotType);
            }
            foreach (ItemDefinition item in db.Items.Values)
            {
                if (string.IsNullOrEmpty(item.NameKey))
                {
                    errors.Add($"item '{item.Id}': NameKey is empty");
                }
                foreach (ItemComponent component in item.Components)
                {
                    foreach (string required in registry.GetRequirements(component.Type))
                    {
                        if (!item.HasType(required))
                        {
                            errors.Add($"item '{item.Id}': component '{component.Type}' requires '{required}'");
                        }
                    }
                }
                EquipSlotComponent equipSlot = item.Get<EquipSlotComponent>();
                if (equipSlot != null && !slotTypes.Contains(equipSlot.SlotType))
                {
                    errors.Add($"item '{item.Id}': unknown slot type '{equipSlot.SlotType}'");
                }
                if (item.Has<EquipmentComponent>() && equipSlot == null)
                {
                    errors.Add($"item '{item.Id}': Equipment needs an EquipSlot");
                }
                foreach (string classId in item.ClassRestriction)
                {
                    if (!db.Classes.ContainsKey(classId))
                    {
                        errors.Add($"item '{item.Id}': unknown class '{classId}' in ClassRestriction");
                    }
                }
            }

            foreach (ClassDefinition cls in db.Classes.Values)
            {
                foreach (ClassSkill skill in cls.Skills)
                {
                    if (!db.Skills.ContainsKey(skill.SkillId))
                    {
                        errors.Add($"class '{cls.Id}': unknown skill '{skill.SkillId}'");
                    }
                }
                foreach (StartingItemDto start in cls.StartingItems)
                {
                    if (!db.Items.ContainsKey(start.ItemId))
                    {
                        errors.Add($"class '{cls.Id}': unknown starting item '{start.ItemId}'");
                    }
                }
            }

            if (db.World != null)
            {
                foreach (SpawnDto spawn in db.World.Spawns ?? new SpawnDto[0])
                {
                    if (!db.Mobs.ContainsKey(spawn.MobId))
                    {
                        errors.Add($"world: unknown mob '{spawn.MobId}'");
                    }
                }
            }
            else
            {
                errors.Add("world: file is missing");
            }
        }
    }
}
