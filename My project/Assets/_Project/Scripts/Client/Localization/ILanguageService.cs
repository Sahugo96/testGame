using System.Collections.Generic;

namespace Game.Client.Localization
{
    /// <summary>Languages the game can show and the chosen one (module 4 with Unity Localization, question 81; picked in Settings → Game).</summary>
    public interface ILanguageService
    {
        /// <summary>Locale codes ("ru", …) in display order.</summary>
        IReadOnlyList<string> Codes { get; }

        string Current { get; }

        /// <summary>The language's own name for the selector (from the locale, not a UI key).</summary>
        string NameOf(string code);

        /// <summary>Switches the texts; windows opened later show the new language, open ones after a restart. False if unknown.</summary>
        bool Select(string code);
    }
}
