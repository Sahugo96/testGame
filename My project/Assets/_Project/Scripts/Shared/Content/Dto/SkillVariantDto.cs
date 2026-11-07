using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Serialized SkillVariant (FROZEN structure, 03 7.2).</summary>
    [Serializable]
    public class SkillVariantDto
    {
        public string Kind;
        public CostEntryDto[] Costs;
        public float CastTime;
        public float Cooldown;
        public EffectDto[] Effects;
        public string[] Flags;
    }
}
