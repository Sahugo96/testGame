namespace Game.Shared.Content
{
    /// <summary>One point of Attribute gives PerPoint of Stat (question 52).</summary>
    public sealed class AttributeEffect
    {
        public StatId Attribute;
        public StatId Stat;
        public float PerPoint;
    }
}
