using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/skills.json (merged across packs).</summary>
    [Serializable]
    public class SkillsFileDto
    {
        public SkillDto[] Skills;
    }
}
