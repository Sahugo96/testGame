namespace Game.Shared.Protocol
{
    /// <summary>Select a target by id (click).</summary>
    public struct SelectTargetCommand : IGameCommand
    {
        public Game.Shared.Core.EntityId TargetId;
    }
}
