using Game.Shared.Content.Dto;

namespace Game.Shared.Content
{
    /// <summary>Raw content files as loaded from JSON (by Unity's JsonUtility in the game, by System.Text.Json in .NET tests).</summary>
    public sealed class ContentFiles
    {
        public GameConfig Config;
        public ClassesFileDto Classes;
        public SkillsFileDto Skills;
        public SkillMasteryFileDto SkillMastery;
        public ItemsFileDto Items;
        public EquipmentSlotsFileDto EquipmentSlots;
        public MobsFileDto Mobs;
        public WorldDto World;
        public HudWindowsFileDto HudWindows;
        public BagSectionsFileDto BagSections;
        public UpgradeRulesFileDto UpgradeRules;
        public LevelsFileDto Levels;
        public RecipesFileDto Recipes;
        public StatusEffectsFileDto StatusEffects;
    }
}
