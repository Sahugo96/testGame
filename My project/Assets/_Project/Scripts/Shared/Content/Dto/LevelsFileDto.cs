using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/levels.json. The last level is the cap.</summary>
    [Serializable]
    public class LevelsFileDto
    {
        public LevelDto[] Levels;
    }
}
