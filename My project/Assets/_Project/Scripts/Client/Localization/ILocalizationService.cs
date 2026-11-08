namespace Game.Client.Localization
{
    /// <summary>
    /// Thin localization interface (question 42, hybrid): code and data use keys only. Unity Localization (string table
    /// "Game", module 4, question 81) backs it when the package is set up; the JSON key table is the fallback.
    /// </summary>
    public interface ILocalizationService
    {
        string Get(string key);

        string Format(string key, params object[] args);
    }
}
