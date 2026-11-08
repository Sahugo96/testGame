using System.Text;

namespace Game.Server.Systems
{
    /// <summary>Player-typed text of module 20 (shop names, letters): no control characters, trimmed, length-limited.</summary>
    public static class MarketText
    {
        public static string Clean(string text, int max)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (!char.IsControl(c))
                {
                    sb.Append(c);
                }
            }
            string clean = sb.ToString().Trim();
            return clean.Length > max ? clean.Substring(0, max) : clean;
        }
    }
}
