namespace Game.Client.Localization
{
    /// <summary>
    /// Picks the text service (module 4, question 81): Unity Localization when the package and its settings are in the project,
    /// otherwise the JSON table alone. Both answer the same keys.
    /// </summary>
    public static class LocalizationFactory
    {
        /// <summary>The JSON table every setup keeps (source of the string table, fallback for missing keys).</summary>
        public const string JsonLanguage = "ru";

        public static (ILocalizationService text, ILanguageService languages) Create(string preferredLanguage)
        {
            var json = new JsonTableLocalizationService(JsonLanguage);
#if GAME_UNITY_LOCALIZATION
            UnityLocalizationService unity = UnityLocalizationService.TryCreate(json, preferredLanguage);
            if (unity != null)
            {
                return (unity, unity);
            }
#endif
            return (json, json);
        }
    }
}
