using System;
using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Packs;

namespace Game.EditorTools.Content
{
    /// <summary>One file kind the editor keeps in sync: its JSON name, file DTO, card type, and whether it is a list of records.</summary>
    public sealed class ContentSyncTarget
    {
        private ContentSyncTarget(string name, Type fileType, Type cardType, ContentFileSpec spec)
        {
            Name = name;
            FileType = fileType;
            CardType = cardType;
            Spec = spec;
        }

        public string Name { get; }

        public Type FileType { get; }

        public Type CardType { get; }

        /// <summary>Null for pack.json (not a content file of ContentFileTable).</summary>
        public ContentFileSpec Spec { get; }

        public bool IsRecords => Spec != null && Spec.Kind == ContentFileKind.Records;

        public static readonly ContentSyncTarget Manifest = new ContentSyncTarget(ContentPacks.Manifest, typeof(PackManifestDto), typeof(PackCard), null);

        /// <summary>Card class per content file (ContentFileTable order, then pack.json).</summary>
        public static readonly IReadOnlyList<ContentSyncTarget> All = Build();

        public static ContentSyncTarget Find(string name) => ((List<ContentSyncTarget>)All).Find(t => t.Name == name);

        private static List<ContentSyncTarget> Build()
        {
            var cards = new Dictionary<Type, Type>
            {
                { typeof(GameConfig), typeof(ConfigCard) },
                { typeof(BalanceLimitsDto), typeof(BalanceLimitsCard) },
                { typeof(WorldDto), typeof(WorldCard) },
                { typeof(ClassDto), typeof(ClassCard) },
                { typeof(SkillDto), typeof(SkillCard) },
                { typeof(SkillMasteryDto), typeof(SkillMasteryCard) },
                { typeof(ItemDto), typeof(ItemCard) },
                { typeof(EquipmentSlotDto), typeof(EquipmentSlotCard) },
                { typeof(MobDto), typeof(MobCard) },
                { typeof(HudWindowDto), typeof(HudWindowCard) },
                { typeof(BagSectionDto), typeof(BagSectionCard) },
                { typeof(UpgradeRuleDto), typeof(UpgradeRuleCard) },
                { typeof(LevelDto), typeof(LevelCard) },
                { typeof(RecipeDto), typeof(RecipeCard) },
                { typeof(StatusEffectDto), typeof(StatusEffectCard) },
                { typeof(AiProfileDto), typeof(AiProfileCard) },
                { typeof(CurrencyDto), typeof(CurrencyCard) },
                { typeof(AffixPoolDto), typeof(AffixPoolCard) },
            };
            var result = new List<ContentSyncTarget>();
            foreach (ContentFileSpec spec in ContentFileTable.All)
            {
                Type record = spec.Kind == ContentFileKind.Records ? spec.RecordType : spec.FileType;
                if (!cards.TryGetValue(record, out Type card))
                {
                    throw new InvalidOperationException($"No content card class for {record.Name}: add one in Editor/Content/Cards");
                }
                result.Add(new ContentSyncTarget(spec.Name, spec.FileType, card, spec));
            }
            result.Add(Manifest);
            return result;
        }
    }
}
