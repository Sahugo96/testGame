using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>
    /// File: Resources/Content/content_ids.json — every id ever released, per content file (question 83). An id here that is
    /// missing from the data is an error: saves may hold it; mark the record Deprecated instead of deleting it.
    /// </summary>
    [Serializable]
    public class ContentIdLedgerDto
    {
        public ContentIdListDto[] Files;
    }

    [Serializable]
    public class ContentIdListDto
    {
        /// <summary>Content file name ("items", "mobs", …; "world.stations" for stations).</summary>
        public string File;
        public string[] Ids;
    }
}
