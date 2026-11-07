using System.Collections.Generic;
using Game.Shared.Rules;

namespace Game.Shared.Content.Items
{
    /// <summary>Stat bonuses applied while the item is equipped (SourceId = item instance). Requires Equipment.</summary>
    public sealed class StatsComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Stats;

        /// <summary>Templates; SourceId is filled when the item is equipped.</summary>
        public readonly List<StatModifier> Modifiers = new List<StatModifier>();
    }
}
