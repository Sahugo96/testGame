using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/classes.json (merged across packs).</summary>
    [Serializable]
    public class ClassesFileDto
    {
        public ClassDto[] Classes;
    }
}
