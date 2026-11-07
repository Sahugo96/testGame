using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Serialized EffectDefinition: handler key + parameters (01, 7.10).</summary>
    [Serializable]
    public class EffectDto
    {
        public string Key;
        public float Amount;
        public string Resource;
    }
}
