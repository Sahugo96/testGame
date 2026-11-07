namespace Game.Shared.Protocol
{
    /// <summary>Movement input: desired direction on the XZ plane, length ≤ 1 (server clamps).</summary>
    public struct MoveCommand : IGameCommand
    {
        public float DirX;
        public float DirZ;
    }
}
