using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Insert an ingredient from the bag into a socket of an owned item, at a whetstone (04, 7.9).</summary>
    public struct InsertIngredientCommand : IGameCommand
    {
        public string ItemInstanceId;
        public int SocketIndex;
        public int IngredientBagIndex;
        public EntityId StationId;
    }
}
