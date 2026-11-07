using System;

namespace Game.Client.Localization
{
    /// <summary>One key → text pair of a language table.</summary>
    [Serializable]
    public class LocalizationEntryDto
    {
        public string Key;
        public string Value;
    }
}
