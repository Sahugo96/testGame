namespace Game.Shared.Protocol
{
    /// <summary>CmdUpgradeItem { itemInstanceId, stationId } (04, 7.14.1): upgrade an owned item at a station.</summary>
    public struct UpgradeItemCommand : IGameCommand
    {
        public string ItemInstanceId;
        public Game.Shared.Core.EntityId StationId;
    }
}
