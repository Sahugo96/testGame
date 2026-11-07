using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>How many stat units one point of a primary attribute gives for a class (question 52).</summary>
    [Serializable]
    public class AttributeEffectDto
    {
        public string Attribute;
        public string Stat;
        public float PerPoint;
    }
}
