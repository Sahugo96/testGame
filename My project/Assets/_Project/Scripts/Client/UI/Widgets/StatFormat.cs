using System.Globalization;
using Game.Client.Localization;
using Game.Shared.Content;

namespace Game.Client.UI.Widgets
{
    /// <summary>Formats stat values: fractional stats (defense, crit, bonuses) as percents (stored as fractions, shown as %, 03 7.12).</summary>
    public static class StatFormat
    {
        private const float PercentFactor = 100f;

        public static bool IsFraction(StatId stat) =>
            stat == StatId.Defense || stat == StatId.CritChance || stat == StatId.CritSize || stat == StatId.DamageBonus;

        public static string Value(ILocalizationService text, StatId stat, float value) =>
            IsFraction(stat)
                ? text.Format(UiKeys.Percent, (value * PercentFactor).ToString("0.#", CultureInfo.InvariantCulture))
                : value.ToString("0.#", CultureInfo.InvariantCulture);

        public static string Name(ILocalizationService text, StatId stat) => text.Get(UiKeys.StatPrefix + stat);
    }
}
