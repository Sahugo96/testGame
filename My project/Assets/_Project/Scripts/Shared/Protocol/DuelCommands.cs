namespace Game.Shared.Protocol
{
    /// <summary>Challenge another player to a duel (the challenger's target must be a player nearby).</summary>
    public struct DuelRequestCommand : IGameCommand
    {
        public Game.Shared.Core.EntityId TargetId;
    }

    /// <summary>Answer to an incoming duel request.</summary>
    public struct DuelResponseCommand : IGameCommand
    {
        public bool Accept;
    }

    /// <summary>Withdraw an own request, cancel during the countdown or surrender an active duel.</summary>
    public struct DuelSurrenderCommand : IGameCommand
    {
    }
}
