using System.Collections.Generic;
using Game.Shared.Rules;

namespace Game.Shared.Content
{
    /// <summary>
    /// StatusEffectDefinition (03, 7.12): kind, category, duration (0 = until removed), tick, stacking, dispel, immunity,
    /// stat modifiers and category parameters. New statuses are data only; unique behaviour goes through HandlerKey (7.10, later).
    /// </summary>
    public sealed class StatusEffectDefinition
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public string Icon;
        public StatusKind Kind;
        public StatusCategory Category;
        public float Duration;
        public float TickInterval;
        /// <summary>DoT damage / HoT healing per tick and stack (scaled by the source's efficiency).</summary>
        public float TickAmount;
        public StatusStacking Stacking;
        public int MaxStacks;
        public bool Dispellable;
        public string DispelType;
        /// <summary>While this status is on a target, statuses with these tags (DispelType, category name or Id) are not applied; "Damage" blocks all damage.</summary>
        public readonly List<string> ImmunityTags = new List<string>();
        /// <summary>Statuses of one group replace each other (03, 7.12: buff potions of one category).</summary>
        public string ExclusiveGroup;
        /// <summary>Shield: damage absorbed (scaled by the source's efficiency).</summary>
        public float ShieldAmount;
        public ControlType Control;
        /// <summary>Per stack; SourceId is set per status instance.</summary>
        public readonly List<StatModifier> Modifiers = new List<StatModifier>();
        public string HandlerKey;
    }
}
