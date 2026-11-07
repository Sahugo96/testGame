using System.Collections.Generic;

namespace Game.Shared.Content
{
    /// <summary>Skill variant (03, 7.2). FROZEN structure: Costs, CastTime, Cooldown, Effects, Flags.</summary>
    public sealed class SkillVariant
    {
        public SkillVariantKind Kind;
        public readonly List<CostEntry> Costs = new List<CostEntry>();
        public float CastTime;
        public float Cooldown;
        public readonly List<EffectDefinition> Effects = new List<EffectDefinition>();
        public readonly HashSet<string> Flags = new HashSet<string>();

        public bool IsInterruptible => Flags.Contains(SkillFlags.Interruptible);
    }
}
