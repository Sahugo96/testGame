using System;

namespace Game.Client.Localization
{
    /// <summary>Language table file: Resources/Localization/&lt;language&gt;.json.</summary>
    [Serializable]
    public class LocalizationTableDto
    {
        public LocalizationEntryDto[] Entries;
    }
}
