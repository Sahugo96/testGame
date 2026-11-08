namespace Game.Shared.Content.Packs
{
    /// <summary>What the editor does with one content file (module 4: cards in Unity, JSON for the server and git).</summary>
    public enum ContentSyncAction
    {
        /// <summary>Neither side exists.</summary>
        Nothing,
        /// <summary>Cards and JSON hold the same data.</summary>
        InSync,
        /// <summary>The JSON changed outside Unity (git pull, a text edit): read it into the cards.</summary>
        Import,
        /// <summary>The cards changed in Unity: write them to the JSON.</summary>
        Export,
        /// <summary>Both changed since the last sync: the JSON wins, the cards' version is kept as a backup file.</summary>
        Conflict,
    }

    /// <summary>
    /// The sync decision for one file from three hashes of canonical text: the JSON now, the cards now, and the last synced
    /// version (null = missing). Pure, so it is tested outside Unity; the editor's ContentSync carries it out.
    /// </summary>
    public static class ContentSyncRule
    {
        public static ContentSyncAction Decide(string json, string cards, string lastSynced)
        {
            if (json == null && cards == null)
            {
                return ContentSyncAction.Nothing;
            }
            if (cards == null)
            {
                return ContentSyncAction.Import;
            }
            if (json == null)
            {
                return ContentSyncAction.Export;
            }
            if (json == cards)
            {
                return ContentSyncAction.InSync;
            }
            if (json == lastSynced)
            {
                return ContentSyncAction.Export;
            }
            if (cards == lastSynced)
            {
                return ContentSyncAction.Import;
            }
            return ContentSyncAction.Conflict;
        }
    }
}
