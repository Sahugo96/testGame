using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Experience needed to go from Level to Level + 1.</summary>
    [Serializable]
    public class LevelDto
    {
        public int Level;
        public int ExperienceToNext;
    }
}
