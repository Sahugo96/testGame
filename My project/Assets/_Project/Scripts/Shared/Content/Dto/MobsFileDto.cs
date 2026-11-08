using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/mobs.json (merged across packs).</summary>
    [Serializable]
    public class MobsFileDto
    {
        public MobDto[] Mobs;
    }
}
