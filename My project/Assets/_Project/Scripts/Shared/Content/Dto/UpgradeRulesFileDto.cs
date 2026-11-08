using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/upgrade_rules.json (merged across packs).</summary>
    [Serializable]
    public class UpgradeRulesFileDto
    {
        public UpgradeRuleDto[] Rules;
    }
}
