using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Serialized StatModifier.</summary>
    [Serializable]
    public class StatModifierDto
    {
        public string Stat;
        public string Op;
        public float Value;
    }
}
