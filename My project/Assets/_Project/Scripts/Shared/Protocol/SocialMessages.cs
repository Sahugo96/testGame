namespace Game.Shared.Protocol
{
    /// <summary>A friend and whether they are online (module 14).</summary>
    public struct FriendSnapshot
    {
        public string Name;
        public bool Online;
    }

    public struct FriendAddCommand : IGameCommand
    {
        public string Name;
    }

    public struct FriendRemoveCommand : IGameCommand
    {
        public string Name;
    }
}
