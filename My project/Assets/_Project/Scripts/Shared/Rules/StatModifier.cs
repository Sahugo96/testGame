using Game.Shared.Content;

namespace Game.Shared.Rules
{
    /// <summary>One modifier of a stat (03, 7.12). SourceId lets the server remove all modifiers of one source (item, buff, potion).</summary>
    public struct StatModifier
    {
        public StatId Stat;
        public ModifierOp Op;
        public float Value;
        public string SourceId;

        public StatModifier(StatId stat, ModifierOp op, float value, string sourceId)
        {
            Stat = stat;
            Op = op;
            Value = value;
            SourceId = sourceId;
        }
    }
}
