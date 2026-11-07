using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Craft a recipe at a station (05, 7.4). The server checks the station, distance and ingredients and rolls the chance.</summary>
    public struct CraftItemCommand : IGameCommand
    {
        public string RecipeId;
        public EntityId StationId;
    }

    /// <summary>Stop the current craft (nothing is spent).</summary>
    public struct CancelCraftCommand : IGameCommand
    {
    }
}
