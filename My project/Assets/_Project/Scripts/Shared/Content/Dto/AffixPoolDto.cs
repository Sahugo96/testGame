using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/affixes.json.</summary>
    [Serializable]
    public class AffixPoolsFileDto
    {
        public AffixPoolDto[] Pools;
    }

    /// <summary>AffixPools (04, 7.9): stats an item of this pool may roll, each with a range.</summary>
    [Serializable]
    public class AffixPoolDto
    {
        public string Id;
        public AffixStatDto[] Stats;
    }

    [Serializable]
    public class AffixStatDto
    {
        public string Stat;
        public string Op;
        public float Min;
        public float Max;
    }
}
