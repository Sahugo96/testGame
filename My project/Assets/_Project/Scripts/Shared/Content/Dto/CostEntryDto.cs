using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Serialized CostEntry (FROZEN structure, 03 7.1).</summary>
    [Serializable]
    public class CostEntryDto
    {
        public string Resource;
        public float Amount;
        public string Mode;
    }
}
