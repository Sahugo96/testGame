namespace Game.Network
{
    /// <summary>Client → server, first message: protocol version, chosen class and player name.</summary>
    public struct HelloMessage
    {
        public int ProtocolVersion;
        public string PlayerName;
        public string ClassId;
    }
}
