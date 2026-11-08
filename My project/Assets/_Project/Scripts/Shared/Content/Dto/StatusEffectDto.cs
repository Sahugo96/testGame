using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/status_effects.json (merged across packs).</summary>
    [Serializable]
    public class StatusEffectsFileDto
    {
        public StatusEffectDto[] Statuses;
    }

    /// <summary>Serialized StatusEffectDefinition (03, 7.12). Enums are strings; Control is a list of ControlType names.</summary>
    [Serializable]
    public class StatusEffectDto
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public string Icon;
        public string Kind;
        public string Category;
        public float Duration;
        public float TickInterval;
        public float TickAmount;
        public string Stacking;
        public int MaxStacks;
        public bool Dispellable;
        public string DispelType;
        public string[] ImmunityTags;
        public string ExclusiveGroup;
        public float ShieldAmount;
        public string[] Control;
        public StatModifierDto[] Modifiers;
        public string HandlerKey;
    }
}
