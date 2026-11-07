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
            if (db.BagSections.Count == 0 || db.BagSize <= 0)
            {
                errors.Add("bag_sections: at least one section with Size > 0 is required");
            }
            if (cfg.DefaultPort <= 0 || cfg.NetworkWorldEveryTicks <= 0 || cfg.NetworkStateEveryTicks <= 0 || cfg.MaxPlayerNameLength <= 0)
            {
                errors.Add("config: DefaultPort, NetworkWorldEveryTicks, NetworkStateEveryTicks, MaxPlayerNameLength must be > 0");
            }
            if (cfg.InteractRange <= 0f)
            {
                errors.Add("config: InteractRange must be > 0");
            }
            if (db.MaxLevel == 0)
            {
                errors.Add("levels: the level table is empty");
            }
            ValidateUpgradeRules(db, errors);
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
                if (db.SectionIndexFor(item.Category) < 0)
                {
                    errors.Add($"item '{item.Id}': no backpack section takes category '{item.Category}'");
                }
                UpgradeableComponent upgradeable = item.Get<UpgradeableComponent>();
                if (upgradeable != null && !db.UpgradeRules.ContainsKey(upgradeable.RulesId ?? string.Empty))
                {
                    errors.Add($"item '{item.Id}': unknown upgrade rules '{upgradeable.RulesId}'");
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

            foreach (MobDto mob in db.Mobs.Values)
            {
                foreach (MobDropDto drop in mob.Drops ?? new MobDropDto[0])
                {
                    if (!db.Items.ContainsKey(drop.ItemId ?? string.Empty))
                    {
                        errors.Add($"mob '{mob.Id}': unknown drop item '{drop.ItemId}'");
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
                foreach (StationDto station in db.World.Stations ?? new StationDto[0])
                {
                    if (station.Type != StationTypes.Anvil)
                    {
                        errors.Add($"world: unknown station type '{station.Type}' of '{station.Id}'");
                    }
                }
            }
            else
            {
                errors.Add("world: file is missing");
            }
        }

        private static void ValidateUpgradeRules(ContentDatabase db, List<string> errors)
        {
            foreach (UpgradeRuleDto rule in db.UpgradeRules.Values)
            {
                if (!db.Items.ContainsKey(rule.MaterialItemId ?? string.Empty))
                {
                    errors.Add($"upgrade_rules '{rule.Id}': unknown material '{rule.MaterialItemId}'");
                }
                if (rule.Steps == null || rule.Steps.Length != rule.MaxLevel)
                {
                    errors.Add($"upgrade_rules '{rule.Id}': Steps must have exactly MaxLevel ({rule.MaxLevel}) entries");
                }
            }
        }
    }
}
