namespace Game.Shared.Content.Packs
{
    /// <summary>Fixed names of the pack layout (question 82): Content/packs.json lists packs; Content/&lt;pack&gt;/pack.json describes one.</summary>
    public static class ContentPacks
    {
        /// <summary>Resources folder of content.</summary>
        public const string Folder = "Content";
        public const string PackList = "packs";
        public const string Manifest = "pack";
        public const string IdLedger = "content_ids";
        /// <summary>The main game; always first; the only pack with config and balance limits.</summary>
        public const string Base = "base";

        public static string PathOf(string pack, string file) => pack + "/" + file;
    }
}
