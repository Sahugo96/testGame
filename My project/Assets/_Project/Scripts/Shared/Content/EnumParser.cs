using System;
using System.Collections.Generic;

namespace Game.Shared.Content
{
    /// <summary>Parses enum names from content files and collects readable errors instead of throwing.</summary>
    public static class EnumParser
    {
        public static T Parse<T>(string value, string context, List<string> errors, T fallback = default) where T : struct
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, false, out T result))
            {
                return result;
            }
            errors.Add($"{context}: unknown {typeof(T).Name} '{value}'");
            return fallback;
        }
    }
}
