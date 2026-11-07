using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Pair of mutually hostile factions (hostility via factions, 01 §13 hook).</summary>
    [Serializable]
    public class FactionRelationDto
    {
        public string A;
        public string B;
    }
}
