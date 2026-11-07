using System.IO;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#else
using System;
using System.Text.Json;
#endif

namespace Game.Tests
{
    /// <summary>Loads the real content JSON: with JsonUtility inside Unity, with System.Text.Json in the .NET harness (sim-tests).</summary>
    public static class TestContentLoader
    {
        public static ContentFiles LoadFiles()
        {
            return new ContentFiles
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
            };
        }

#if UNITY_5_3_OR_NEWER
        private static T Read<T>(string name)
        {
            string path = Path.Combine(Application.dataPath, "_Project", "Resources", ContentFileNames.Folder, name + ".json");
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
#else
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };

        private static T Read<T>(string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, ContentFileNames.Folder, name + ".json");
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        }
#endif
    }
}
