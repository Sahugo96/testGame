namespace Game.Client.Localization
{
    /// <summary>
    /// Thin localization interface (question 42, hybrid): code and data use keys only. Today a JSON key table backs it;
    /// Unity Localization (String Tables) plugs in behind the same interface in module 4.
    /// </summary>
    public interface ILocalizationService
    {
        string Get(string key);

        string Format(string key, params object[] args);
    }
}
