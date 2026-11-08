using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/levels.json (merged across packs). The last level is the cap.</summary>
    [Serializable]
    public class LevelsFileDto
    {
        public LevelDto[] Levels;
    }
}
