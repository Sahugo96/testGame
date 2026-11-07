using System;
using System.IO;
using System.Text.Json;
using Game.Shared.Content;
using Game.Shared.Content.Dto;

namespace Game.LoadTest
{
    /// <summary>Reads the game's content JSON next to the executable (System.Text.Json, fields as in JsonUtility).</summary>
    public static class JsonContentLoader
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };

        public static ContentFiles Load() => new ContentFiles
        {
            Config = Read<GameConfig>(ContentFileNames.Config),
            Classes = Read<ClassesFileDto>(ContentFileNames.Classes),
            Skills = Read<SkillsFileDto>(ContentFileNames.Skills),
            SkillMastery = Read<SkillMasteryFileDto>(ContentFileNames.SkillMastery),
            Items = Read<ItemsFileDto>(ContentFileNames.Items),
            EquipmentSlots = Read<EquipmentSlotsFileDto>(ContentFileNames.EquipmentSlots),
            Mobs = Read<MobsFileDto>(ContentFileNames.Mobs),
            World = Read<WorldDto>(ContentFileNames.World),
            HudWindows = Read<HudWindowsFileDto>(ContentFileNames.HudWindows),
            BagSections = Read<BagSectionsFileDto>(ContentFileNames.BagSections),
            UpgradeRules = Read<UpgradeRulesFileDto>(ContentFileNames.UpgradeRules),
            Levels = Read<LevelsFileDto>(ContentFileNames.Levels),
            Recipes = Read<RecipesFileDto>(ContentFileNames.Recipes),
            StatusEffects = Read<StatusEffectsFileDto>(ContentFileNames.StatusEffects),
            AiProfiles = Read<AiProfilesFileDto>(ContentFileNames.AiProfiles),
            Currencies = Read<CurrenciesFileDto>(ContentFileNames.Currencies),
            AffixPools = Read<AffixPoolsFileDto>(ContentFileNames.AffixPools),
        };

        private static T Read<T>(string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, ContentFileNames.Folder, name + ".json");
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        }
    }
}
