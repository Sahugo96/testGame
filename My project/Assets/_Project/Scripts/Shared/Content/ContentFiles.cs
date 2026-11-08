using Game.Shared.Content.Dto;

namespace Game.Shared.Content
{
    /// <summary>
    /// Raw content as loaded from JSON and merged across packs (module 4): by Unity's JsonUtility in the game, by
    /// System.Text.Json made JsonUtility-like in .NET tools. File types are listed in Packs.ContentFileTable.
    /// </summary>
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
        public AiProfilesFileDto AiProfiles;
        public CurrenciesFileDto Currencies;
        public AffixPoolsFileDto AffixPools;
        public BalanceLimitsDto BalanceLimits;

        /// <summary>Loaded packs in order ("base" first) and the hash of everything loaded (compared at login).</summary>
        public string[] Packs = new string[0];
        public string Hash;
        /// <summary>Every id ever released (content_ids.json); null when the file is missing.</summary>
        public ContentIdLedgerDto Ids;
    }
}
