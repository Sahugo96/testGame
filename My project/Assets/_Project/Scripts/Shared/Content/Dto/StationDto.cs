using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Interactive NPC object in the world (anvil, ...). Type selects the behaviour.</summary>
    [Serializable]
    public class StationDto
    {
        public string Id;
        public string Type;
        public string NameKey;
        public float X;
        public float Z;
        public string VisualColor;
    }
}
