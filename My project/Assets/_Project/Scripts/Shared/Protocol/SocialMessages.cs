namespace Game.Shared.Protocol
{
    /// <summary>Friendship is mutual (decision of the developer): a request waits until the other side accepts.</summary>
    public enum FriendState : byte
    {
        Friend = 0,
        /// <summary>They asked me; I can accept or decline.</summary>
        Incoming = 1,
        /// <summary>I asked them; waiting (I can withdraw).</summary>
        Outgoing = 2,
    }

    /// <summary>An entry of the friends window: a friend or a pending request, with the online state (module 14).</summary>
    public struct FriendSnapshot
    {
        public string Name;
        public bool Online;
        public FriendState State;
    }

    /// <summary>Send a friend request by character name (accepts at once if that character already asked me).</summary>
    public struct FriendAddCommand : IGameCommand
    {
        public string Name;
    }

    /// <summary>Answer an incoming friend request.</summary>
    public struct FriendRespondCommand : IGameCommand
    {
        public string Name;
        public bool Accept;
    }

    /// <summary>Remove a friend (from both lists) or withdraw my request.</summary>
    public struct FriendRemoveCommand : IGameCommand
    {
        public string Name;
    }
}
