using Game.Shared.Core;

namespace Game.Server.Players
{
    /// <summary>A craft in progress (05, 7.4): which recipe, at which station, how far.</summary>
    public struct CraftData
    {
        public bool Active;
        public string RecipeId;
        public EntityId StationId;
        public float Elapsed;
        public float Duration;
    }
}
