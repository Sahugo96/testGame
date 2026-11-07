namespace Game.Network
{
    /// <summary>
    /// Client → server, first message: protocol version, account (login + password, or the session token from a previous
    /// Welcome), character name (used when the account has no character of this class yet) and class (module 13).
    /// </summary>
    public struct HelloMessage
    {
        public int ProtocolVersion;
        public string PlayerName;
        public string ClassId;
        public string Login;
        public string Password;
        public string Token;
    }
}
