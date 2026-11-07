using Game.Client.Localization;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Client.UI.Widgets
{
    /// <summary>Display names of entities the client knows (player name or localized content name) and loot mode names.</summary>
    public static class EntityNames
    {
        public static string Of(ClientContext ctx, EntityId id)
        {
            if (!ctx.State.TryGetEntity(id, out EntitySnapshot entity))
            {
                return ctx.Text.Get(UiKeys.UnknownName);
            }
            return string.IsNullOrEmpty(entity.DisplayName) ? ctx.Text.Get(entity.NameKey) : entity.DisplayName;
        }

        public static string LootMode(ILocalizationService text, LootMode mode) => text.Get(UiKeys.LootModePrefix + mode);
    }
}
