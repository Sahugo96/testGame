using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Interactive NPC object in the world (whetstone, anvil, ...). Type selects the behaviour (StationTypes).</summary>
    [Serializable]
    public class StationDto
    {
        public string Id;
        public string Type;
        public string NameKey;
        public float X;
        public float Z;
        public string VisualColor;
        /// <summary>Placeholder primitive: "Cube" or "Cylinder".</summary>
        public string VisualShape;
    }
}
