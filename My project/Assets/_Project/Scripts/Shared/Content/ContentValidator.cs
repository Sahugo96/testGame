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
            if (cfg.DuelRequestRange <= 0f || cfg.DuelRequestSeconds <= 0f || cfg.DuelCountdownSeconds < 0f || cfg.DuelMaxSeconds <= 0f
                || cfg.DuelRadius <= 0f || cfg.DuelLoserHealth <= 0f || cfg.PvpDamageMultiplier <= 0f)
            {
                errors.Add("config: Duel* values and PvpDamageMultiplier must be > 0 (DuelCountdownSeconds ≥ 0)");
            }
            if (cfg.GroupMaxSize < 2 || cfg.GroupInviteSeconds <= 0f || cfg.LootVoteSeconds <= 0f || cfg.LootVoteRepeatSeconds < 0f
                || cfg.LootRadius <= 0f || cfg.GroupLogSize <= 0)
            {
                errors.Add("config: GroupMaxSize ≥ 2; GroupInviteSeconds, LootVoteSeconds, LootRadius, GroupLogSize > 0; LootVoteRepeatSeconds ≥ 0");
            }
            if (cfg.ThreatPerDamage <= 0f || cfg.HealThreatFactor < 0f)
            {
                errors.Add("config: ThreatPerDamage must be > 0 and HealThreatFactor ≥ 0");
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
            ValidateRecipes(db, errors);
            ValidateStatuses(db, errors);
            ValidateAi(db, errors);
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
                    if (station.Type != StationTypes.Anvil && station.Type != StationTypes.Whetstone)
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

        private static void ValidateAi(ContentDatabase db, List<string> errors)
        {
            GameConfig cfg = db.Config;
            if (cfg.MobGlobalCooldownSeconds < 0f || cfg.MobAggroThreat <= 0f || cfg.MobReturnArriveDistance <= 0f)
            {
                errors.Add("config: MobGlobalCooldownSeconds ≥ 0, MobAggroThreat and MobReturnArriveDistance > 0");
            }
            foreach (AiProfile profile in db.AiProfiles.Values)
            {
                if (profile.Behavior != AiBehavior.Passive && (profile.MoveSpeed <= 0f || profile.LeashRadius <= 0f || profile.Abilities.Count == 0))
                {
                    errors.Add($"ai_profiles '{profile.Id}': a fighting profile needs MoveSpeed, LeashRadius and at least one ability");
                }
                if (profile.Behavior == AiBehavior.Aggressive && profile.AggroRadius <= 0f)
                {
                    errors.Add($"ai_profiles '{profile.Id}': Aggressive needs AggroRadius > 0");
                }
                foreach (string ability in profile.Abilities)
                {
                    if (!db.Skills.TryGetValue(ability ?? string.Empty, out SkillDefinition skill) || skill.TargetType != TargetType.Enemy)
                    {
                        errors.Add($"ai_profiles '{profile.Id}': ability '{ability}' must be an Enemy-target skill");
                    }
                }
            }
            foreach (MobDto mob in db.Mobs.Values)
            {
                if (!string.IsNullOrEmpty(mob.AiProfile) && !db.AiProfiles.ContainsKey(mob.AiProfile))
                {
                    errors.Add($"mob '{mob.Id}': unknown AI profile '{mob.AiProfile}'");
                }
            }
        }

        private static void ValidateStatuses(ContentDatabase db, List<string> errors)
        {
            foreach (StatusEffectDefinition status in db.StatusEffects.Values)
            {
                if (string.IsNullOrEmpty(status.NameKey))
                {
                    errors.Add($"status_effects '{status.Id}': NameKey is required");
                }
                if (status.Duration < 0f)
                {
                    errors.Add($"status_effects '{status.Id}': Duration must be ≥ 0 (0 = until removed)");
                }
                bool ticking = status.Category == StatusCategory.DoT || status.Category == StatusCategory.HoT;
                if (ticking && (status.TickInterval <= 0f || status.TickAmount <= 0f))
                {
                    errors.Add($"status_effects '{status.Id}': DoT/HoT need TickInterval > 0 and TickAmount > 0");
                }
                if (status.Category == StatusCategory.Shield && status.ShieldAmount <= 0f)
                {
                    errors.Add($"status_effects '{status.Id}': Shield needs ShieldAmount > 0");
                }
                if (status.Category == StatusCategory.Control && status.Control == ControlType.None)
                {
                    errors.Add($"status_effects '{status.Id}': Control needs at least one Control type");
                }
            }
            foreach (SkillDefinition skill in db.Skills.Values)
            {
                foreach (SkillVariantKind kind in new[] { SkillVariantKind.Default, SkillVariantKind.Empowered, SkillVariantKind.Weakened })
                {
                    SkillVariant variant = skill.GetVariant(kind);
                    if (variant == null || variant.Kind != kind)
                    {
                        continue;
                    }
                    foreach (EffectDefinition effect in variant.Effects)
                    {
                        if (effect.Key == EffectKeys.ApplyStatus && !db.StatusEffects.ContainsKey(effect.StatusId ?? string.Empty))
                        {
                            errors.Add($"skills '{skill.Id}': unknown status '{effect.StatusId}'");
                        }
                    }
                }
            }
            foreach (ItemDefinition item in db.Items.Values)
            {
                ConsumableComponent consumable = item.Get<ConsumableComponent>();
                if (consumable != null && consumable.EffectKey == EffectKeys.ApplyStatus && !db.StatusEffects.ContainsKey(consumable.StatusId ?? string.Empty))
                {
                    errors.Add($"items '{item.Id}': unknown status '{consumable.StatusId}'");
                }
            }
        }

        private static void ValidateRecipes(ContentDatabase db, List<string> errors)
        {
            foreach (RecipeDto recipe in db.Recipes.Values)
            {
                if (string.IsNullOrEmpty(recipe.NameKey))
                {
                    errors.Add($"recipes '{recipe.Id}': NameKey is required");
                }
                if (recipe.Station != StationTypes.Anvil && recipe.Station != StationTypes.Whetstone)
                {
                    errors.Add($"recipes '{recipe.Id}': unknown station type '{recipe.Station}'");
                }
                if (recipe.CraftTime < 0f || recipe.SuccessChance <= 0f || recipe.SuccessChance > 1f)
                {
                    errors.Add($"recipes '{recipe.Id}': CraftTime must be ≥ 0 and SuccessChance in (0, 1]");
                }
                if (recipe.Inputs == null || recipe.Inputs.Length == 0 || recipe.Outputs == null || recipe.Outputs.Length == 0)
                {
                    errors.Add($"recipes '{recipe.Id}': at least one input and one output are required");
                    continue;
                }
                foreach (ItemCountDto entry in recipe.Inputs)
                {
                    ValidateItemCount(db, recipe.Id, entry, errors);
                }
                foreach (ItemCountDto entry in recipe.Outputs)
                {
                    ValidateItemCount(db, recipe.Id, entry, errors);
                }
            }
        }

        private static void ValidateItemCount(ContentDatabase db, string recipeId, ItemCountDto entry, List<string> errors)
        {
            if (entry == null || !db.Items.ContainsKey(entry.ItemId ?? string.Empty) || entry.Count <= 0)
            {
                errors.Add($"recipes '{recipeId}': unknown item '{entry?.ItemId}' or Count ≤ 0");
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
