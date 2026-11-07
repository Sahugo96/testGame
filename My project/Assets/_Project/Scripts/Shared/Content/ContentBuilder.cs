using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Rules;

namespace Game.Shared.Content
{
    /// <summary>Turns raw content files into a typed <see cref="ContentDatabase"/>, collecting all errors (01, 7.10: no start with broken content).</summary>
    public static class ContentBuilder
    {
        public static ContentDatabase Build(ContentFiles files, ItemComponentRegistry registry, ICollection<string> knownEffectKeys, List<string> errors)
        {
            var db = new ContentDatabase { Config = files.Config, World = files.World };
            if (files.Config == null)
            {
                errors.Add("config: file is missing");
                return db;
            }
            foreach (FactionRelationDto relation in files.Config.HostileFactions ?? new FactionRelationDto[0])
            {
                db.HostileFactions.Add((relation.A, relation.B));
            }
            foreach (SkillDto dto in files.Skills?.Skills ?? new SkillDto[0])
            {
                AddUnique(db.Skills, dto.Id, BuildSkill(dto, errors), "skills", errors);
            }
            foreach (SkillMasteryDto dto in files.SkillMastery?.Rules ?? new SkillMasteryDto[0])
            {
                AddUnique(db.MasteryRules, dto.SkillId, new SkillMasteryRule
                {
                    SkillId = dto.SkillId,
                    EmpoweredUnlock = dto.EmpoweredUnlock,
                    WeakenedUnlock = dto.WeakenedUnlock,
                    MasteryPerUse = dto.MasteryPerUse,
                }, "skill_mastery", errors);
            }
            foreach (ClassDto dto in files.Classes?.Classes ?? new ClassDto[0])
            {
                AddUnique(db.Classes, dto.Id, BuildClass(dto, errors), "classes", errors);
            }
            foreach (ItemDto dto in files.Items?.Items ?? new ItemDto[0])
            {
                AddUnique(db.Items, dto.Id, BuildItem(dto, registry, errors), "items", errors);
            }
            foreach (MobDto dto in files.Mobs?.Mobs ?? new MobDto[0])
            {
                AddUnique(db.Mobs, dto.Id, dto, "mobs", errors);
            }
            db.EquipmentSlots.AddRange(files.EquipmentSlots?.Slots ?? new EquipmentSlotDto[0]);
            db.HudWindows.AddRange(files.HudWindows?.Windows ?? new HudWindowDto[0]);
            int start = 0;
            foreach (BagSectionDto dto in files.BagSections?.Sections ?? new BagSectionDto[0])
            {
                var section = new BagSection { Id = dto.Id, NameKey = dto.NameKey, Start = start, Size = dto.Size };
                foreach (string category in dto.Categories ?? new string[0])
                {
                    section.Categories.Add(EnumParser.Parse<ItemCategory>(category, $"bag section '{dto.Id}'", errors));
                }
                db.BagSections.Add(section);
                start += dto.Size;
            }
            foreach (UpgradeRuleDto dto in files.UpgradeRules?.Rules ?? new UpgradeRuleDto[0])
            {
                AddUnique(db.UpgradeRules, dto.Id, dto, "upgrade_rules", errors);
            }
            foreach (AiProfileDto dto in files.AiProfiles?.Profiles ?? new AiProfileDto[0])
            {
                var profile = new AiProfile
                {
                    Id = dto.Id,
                    Behavior = EnumParser.Parse<AiBehavior>(dto.Behavior, $"ai_profiles '{dto.Id}'", errors),
                    MoveSpeed = dto.MoveSpeed,
                    AggroRadius = dto.AggroRadius,
                    LeashRadius = dto.LeashRadius,
                };
                profile.Abilities.AddRange(dto.Abilities ?? new string[0]);
                AddUnique(db.AiProfiles, dto.Id, profile, "ai_profiles", errors);
            }
            foreach (StatusEffectDto dto in files.StatusEffects?.Statuses ?? new StatusEffectDto[0])
            {
                AddUnique(db.StatusEffects, dto.Id, BuildStatus(dto, errors), "status_effects", errors);
            }
            foreach (RecipeDto dto in files.Recipes?.Recipes ?? new RecipeDto[0])
            {
                if (AddUnique(db.Recipes, dto.Id, dto, "recipes", errors))
                {
                    db.RecipeList.Add(dto);
                }
            }
            var levels = new List<LevelDto>(files.Levels?.Levels ?? new LevelDto[0]);
            levels.Sort((a, b) => a.Level.CompareTo(b.Level));
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].Level != i + 1)
                {
                    errors.Add($"levels: expected level {i + 1}, found {levels[i].Level}");
                }
                db.ExperienceToNext.Add(levels[i].ExperienceToNext);
            }
            ContentValidator.Validate(db, registry, knownEffectKeys, errors);
            return db;
        }

        /// <summary>Adds an entry with a unique, non-empty id; returns false (and reports) otherwise.</summary>
        private static bool AddUnique<T>(Dictionary<string, T> target, string id, T value, string file, List<string> errors)
        {
            if (string.IsNullOrEmpty(id))
            {
                errors.Add($"{file}: entry without Id");
                return false;
            }
            if (target.ContainsKey(id))
            {
                errors.Add($"{file}: duplicate Id '{id}'");
                return false;
            }
            target.Add(id, value);
            return true;
        }

        private static StatusEffectDefinition BuildStatus(StatusEffectDto dto, List<string> errors)
        {
            string ctx = $"status_effects '{dto.Id}'";
            var status = new StatusEffectDefinition
            {
                Id = dto.Id,
                NameKey = dto.NameKey,
                DescriptionKey = dto.DescriptionKey,
                Icon = dto.Icon,
                Kind = EnumParser.Parse<StatusKind>(dto.Kind, ctx, errors),
                Category = EnumParser.Parse<StatusCategory>(dto.Category, ctx, errors),
                Duration = dto.Duration,
                TickInterval = dto.TickInterval,
                TickAmount = dto.TickAmount,
                Stacking = string.IsNullOrEmpty(dto.Stacking) ? StatusStacking.Refresh : EnumParser.Parse<StatusStacking>(dto.Stacking, ctx, errors),
                MaxStacks = dto.MaxStacks > 0 ? dto.MaxStacks : 1,
                Dispellable = dto.Dispellable,
                DispelType = dto.DispelType,
                ExclusiveGroup = dto.ExclusiveGroup,
                ShieldAmount = dto.ShieldAmount,
                HandlerKey = dto.HandlerKey,
            };
            status.ImmunityTags.AddRange(dto.ImmunityTags ?? new string[0]);
            foreach (string control in dto.Control ?? new string[0])
            {
                status.Control |= EnumParser.Parse<ControlType>(control, ctx, errors);
            }
            foreach (StatModifierDto modifier in dto.Modifiers ?? new StatModifierDto[0])
            {
                status.Modifiers.Add(new StatModifier(
                    EnumParser.Parse<StatId>(modifier.Stat, ctx, errors),
                    EnumParser.Parse<ModifierOp>(modifier.Op, ctx, errors),
                    modifier.Value,
                    null));
            }
            return status;
        }

        private static SkillDefinition BuildSkill(SkillDto dto, List<string> errors)
        {
            string ctx = $"skill '{dto.Id}'";
            var skill = new SkillDefinition
            {
                Id = dto.Id,
                NameKey = dto.NameKey,
                DescriptionKey = dto.DescriptionKey,
                Icon = dto.Icon,
                TargetType = EnumParser.Parse<TargetType>(dto.TargetType, ctx, errors),
                Range = dto.Range,
                UsesGlobalCooldown = dto.UsesGlobalCooldown,
            };
            foreach (SkillVariantDto variantDto in dto.Variants ?? new SkillVariantDto[0])
            {
                SkillVariant variant = BuildVariant(variantDto, ctx, errors);
                switch (variant.Kind)
                {
                    case SkillVariantKind.Default:
                        skill.Default = variant;
                        break;
                    case SkillVariantKind.Empowered:
                        skill.Empowered = variant;
                        break;
                    case SkillVariantKind.Weakened:
                        skill.Weakened = variant;
                        break;
                }
            }
            return skill;
        }

        private static SkillVariant BuildVariant(SkillVariantDto dto, string ctx, List<string> errors)
        {
            var variant = new SkillVariant
            {
                Kind = EnumParser.Parse<SkillVariantKind>(dto.Kind, ctx, errors),
                CastTime = dto.CastTime,
                Cooldown = dto.Cooldown,
            };
            foreach (CostEntryDto cost in dto.Costs ?? new CostEntryDto[0])
            {
                variant.Costs.Add(new CostEntry
                {
                    Resource = EnumParser.Parse<ResourceKind>(cost.Resource, ctx, errors),
                    Amount = cost.Amount,
                    Mode = EnumParser.Parse<CostMode>(cost.Mode, ctx, errors),
                });
            }
            foreach (EffectDto effect in dto.Effects ?? new EffectDto[0])
            {
                variant.Effects.Add(new EffectDefinition
                {
                    Key = effect.Key,
                    Amount = effect.Amount,
                    Resource = string.IsNullOrEmpty(effect.Resource)
                        ? ResourceKind.Health
                        : EnumParser.Parse<ResourceKind>(effect.Resource, ctx, errors),
                    StatusId = effect.StatusId,
                    DispelType = effect.DispelType,
                });
            }
            foreach (string flag in dto.Flags ?? new string[0])
            {
                variant.Flags.Add(flag);
            }
            return variant;
        }

        private static ClassDefinition BuildClass(ClassDto dto, List<string> errors)
        {
            string ctx = $"class '{dto.Id}'";
            var cls = new ClassDefinition
            {
                Id = dto.Id,
                NameKey = dto.NameKey,
                DescriptionKey = dto.DescriptionKey,
                Role = EnumParser.Parse<ClassRole>(dto.Role, ctx, errors),
                PrimaryAttribute = EnumParser.Parse<StatId>(dto.PrimaryAttribute, ctx, errors),
                RingSlots = dto.EquipmentRules?.RingSlots ?? 0,
            };
            foreach (StatValueDto stat in dto.BaseStats ?? new StatValueDto[0])
            {
                cls.BaseStats[EnumParser.Parse<StatId>(stat.Stat, ctx, errors)] = stat.Value;
            }
            foreach (StatValueDto stat in dto.StartingAttributes ?? new StatValueDto[0])
            {
                cls.StartingAttributes[EnumParser.Parse<StatId>(stat.Stat, ctx, errors)] = stat.Value;
            }
            foreach (AttributeEffectDto effect in dto.AttributeEffects ?? new AttributeEffectDto[0])
            {
                cls.AttributeEffects.Add(new AttributeEffect
                {
                    Attribute = EnumParser.Parse<StatId>(effect.Attribute, ctx, errors),
                    Stat = EnumParser.Parse<StatId>(effect.Stat, ctx, errors),
                    PerPoint = effect.PerPoint,
                });
            }
            foreach (ClassSkillDto skill in dto.Skills ?? new ClassSkillDto[0])
            {
                cls.Skills.Add(new ClassSkill { SkillId = skill.SkillId, LevelUnlock = skill.LevelUnlock, StartingMastery = skill.StartingMastery });
            }
            foreach (string armor in dto.EquipmentRules?.AllowedArmorTypes ?? new string[0])
            {
                cls.AllowedArmorTypes.Add(armor);
            }
            foreach (string weapon in dto.EquipmentRules?.AllowedWeaponTypes ?? new string[0])
            {
                cls.AllowedWeaponTypes.Add(weapon);
            }
            cls.StartingItems.AddRange(dto.StartingItems ?? new StartingItemDto[0]);
            return cls;
        }

        private static ItemDefinition BuildItem(ItemDto dto, ItemComponentRegistry registry, List<string> errors)
        {
            string ctx = $"item '{dto.Id}'";
            var item = new ItemDefinition
            {
                Id = dto.Id,
                NameKey = dto.NameKey,
                DescriptionKey = dto.DescriptionKey,
                Icon = dto.Icon,
                Category = EnumParser.Parse<ItemCategory>(dto.Category, ctx, errors),
                Rarity = EnumParser.Parse<Rarity>(dto.Rarity, ctx, errors),
                ItemLevel = dto.ItemLevel,
                RequiredLevel = dto.RequiredLevel,
                Binding = string.IsNullOrEmpty(dto.Binding) ? BindingType.None : EnumParser.Parse<BindingType>(dto.Binding, ctx, errors),
            };
            foreach (string classId in dto.ClassRestriction ?? new string[0])
            {
                item.ClassRestriction.Add(classId);
            }
            foreach (ItemComponentDto componentDto in dto.Components ?? new ItemComponentDto[0])
            {
                ItemComponent component = registry.Create(componentDto, ctx, errors);
                if (component != null)
                {
                    item.Components.Add(component);
                }
            }
            return item;
        }
    }
}
