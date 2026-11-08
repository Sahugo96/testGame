using System.Collections.Generic;

namespace Game.Server.Players
{
    /// <summary>
    /// Friends of an account (module 14, mutual by decision of the developer): accepted friends and pending requests in
    /// both directions, as character names. Saved with the account (AccountRecord).
    /// </summary>
    public sealed class FriendLists
    {
        public readonly List<string> Friends = new List<string>();
        /// <summary>Characters that asked this account.</summary>
        public readonly List<string> Incoming = new List<string>();
        /// <summary>Characters this account asked.</summary>
        public readonly List<string> Outgoing = new List<string>();
    }
}
