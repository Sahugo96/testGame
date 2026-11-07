using Game.Shared.Content;
using Game.Shared.Content.Dto;
using UnityEngine;

namespace Game.Host
{
    /// <summary>Loads content JSON from Resources/Content with JsonUtility (Addressables and content packs come with module 4).</summary>
    public static class UnityContentLoader
    {
        public static ContentFiles LoadAll()
        {
            return new ContentFiles
            {
                Config = Load<GameConfig>(ContentFileNames.Config),
                Classes = Load<ClassesFileDto>(ContentFileNames.Classes),
                Skills = Load<SkillsFileDto>(ContentFileNames.Skills),
                SkillMastery = Load<SkillMasteryFileDto>(ContentFileNames.SkillMastery),
                Items = Load<ItemsFileDto>(ContentFileNames.Items),
                EquipmentSlots = Load<EquipmentSlotsFileDto>(ContentFileNames.EquipmentSlots),
                Mobs = Load<MobsFileDto>(ContentFileNames.Mobs),
                World = Load<WorldDto>(ContentFileNames.World),
                HudWindows = Load<HudWindowsFileDto>(ContentFileNames.HudWindows),
                BagSections = Load<BagSectionsFileDto>(ContentFileNames.BagSections),
                UpgradeRules = Load<UpgradeRulesFileDto>(ContentFileNames.UpgradeRules),
                Levels = Load<LevelsFileDto>(ContentFileNames.Levels),
                Recipes = Load<RecipesFileDto>(ContentFileNames.Recipes),
                StatusEffects = Load<StatusEffectsFileDto>(ContentFileNames.StatusEffects),
                AiProfiles = Load<AiProfilesFileDto>(ContentFileNames.AiProfiles),
            };
        }

        public static T Load<T>(string name) where T : class
        {
            var asset = Resources.Load<TextAsset>(ContentFileNames.Folder + "/" + name);
            return asset != null ? JsonUtility.FromJson<T>(asset.text) : null;
        }
    }
}
