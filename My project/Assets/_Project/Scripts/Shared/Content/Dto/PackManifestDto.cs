using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File: Resources/Content/&lt;pack&gt;/pack.json — what a content pack is and which packs it needs before it.</summary>
    [Serializable]
    public class PackManifestDto
    {
        public string Id;
        public string Version;
        public string NameKey;
        public string[] Dependencies;
    }
}
