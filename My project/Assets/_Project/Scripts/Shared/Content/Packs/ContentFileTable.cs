using System.Collections.Generic;
using System.Globalization;
using Game.Shared.Content.Dto;

namespace Game.Shared.Content.Packs
{
    /// <summary>
    /// The one place that lists content file types (module 4): file name, DTO, merge rule and stable id. Loaders (Unity,
    /// tests, LoadTest, the editor's cards) all go through it — a new file type is one line here plus a field in ContentFiles.
    /// </summary>
    public static class ContentFileTable
    {
        public const string Config = "config";
        public const string World = "world";
        public const string BalanceLimits = "balance_limits";

        public static readonly IReadOnlyList<ContentFileSpec> All = new[]
        {
            new ContentFileSpec(Config, typeof(GameConfig), ContentFileKind.BaseOnly, null, null),
            new ContentFileSpec(BalanceLimits, typeof(BalanceLimitsDto), ContentFileKind.BaseOnly, null, null),
            new ContentFileSpec(World, typeof(WorldDto), ContentFileKind.Extendable, null, null),
            new ContentFileSpec("classes", typeof(ClassesFileDto), ContentFileKind.Records, nameof(ClassesFileDto.Classes), r => ((ClassDto)r).Id),
            new ContentFileSpec("skills", typeof(SkillsFileDto), ContentFileKind.Records, nameof(SkillsFileDto.Skills), r => ((SkillDto)r).Id),
            new ContentFileSpec("skill_mastery", typeof(SkillMasteryFileDto), ContentFileKind.Records, nameof(SkillMasteryFileDto.Rules), r => ((SkillMasteryDto)r).SkillId),
            new ContentFileSpec("items", typeof(ItemsFileDto), ContentFileKind.Records, nameof(ItemsFileDto.Items), r => ((ItemDto)r).Id),
            new ContentFileSpec("equipment_slots", typeof(EquipmentSlotsFileDto), ContentFileKind.Records, nameof(EquipmentSlotsFileDto.Slots), r => ((EquipmentSlotDto)r).Id),
            new ContentFileSpec("mobs", typeof(MobsFileDto), ContentFileKind.Records, nameof(MobsFileDto.Mobs), r => ((MobDto)r).Id),
            new ContentFileSpec("hud_windows", typeof(HudWindowsFileDto), ContentFileKind.Records, nameof(HudWindowsFileDto.Windows), r => ((HudWindowDto)r).Id),
            new ContentFileSpec("bag_sections", typeof(BagSectionsFileDto), ContentFileKind.Records, nameof(BagSectionsFileDto.Sections), r => ((BagSectionDto)r).Id),
            new ContentFileSpec("upgrade_rules", typeof(UpgradeRulesFileDto), ContentFileKind.Records, nameof(UpgradeRulesFileDto.Rules), r => ((UpgradeRuleDto)r).Id),
            new ContentFileSpec("levels", typeof(LevelsFileDto), ContentFileKind.Records, nameof(LevelsFileDto.Levels), r => ((LevelDto)r).Level.ToString(CultureInfo.InvariantCulture)),
            new ContentFileSpec("recipes", typeof(RecipesFileDto), ContentFileKind.Records, nameof(RecipesFileDto.Recipes), r => ((RecipeDto)r).Id),
            new ContentFileSpec("status_effects", typeof(StatusEffectsFileDto), ContentFileKind.Records, nameof(StatusEffectsFileDto.Statuses), r => ((StatusEffectDto)r).Id),
            new ContentFileSpec("ai_profiles", typeof(AiProfilesFileDto), ContentFileKind.Records, nameof(AiProfilesFileDto.Profiles), r => ((AiProfileDto)r).Id),
            new ContentFileSpec("currencies", typeof(CurrenciesFileDto), ContentFileKind.Records, nameof(CurrenciesFileDto.Currencies), r => ((CurrencyDto)r).Id),
            new ContentFileSpec("affixes", typeof(AffixPoolsFileDto), ContentFileKind.Records, nameof(AffixPoolsFileDto.Pools), r => ((AffixPoolDto)r).Id),
        };

        public static ContentFileSpec Find(string name)
        {
            foreach (ContentFileSpec spec in All)
            {
                if (spec.Name == name)
                {
                    return spec;
                }
            }
            return null;
        }

        public static ContentFileSpec ForRecord(System.Type recordType)
        {
            foreach (ContentFileSpec spec in All)
            {
                if (spec.RecordType == recordType || (spec.Kind != ContentFileKind.Records && spec.FileType == recordType))
                {
                    return spec;
                }
            }
            return null;
        }
    }
}
