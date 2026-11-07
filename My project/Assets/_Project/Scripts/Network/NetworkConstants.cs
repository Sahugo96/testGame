namespace Game.Network
{
    /// <summary>Transport limits. Bump ProtocolVersion on any wire format change: old builds are then refused politely.</summary>
    public static class NetworkConstants
    {
        public const int ProtocolVersion = 5;
        public const int MaxFrameBytes = 4 * 1024 * 1024;
        public const int ConnectTimeoutMs = 5000;
        /// <summary>Server error reasons are localization keys with this prefix; transport reasons are plain text.</summary>
        public const string ErrorKeyPrefix = "net.";
        public const string ErrorVersionMismatch = "net.version_mismatch";
        public const string ErrorUnknownClass = "net.unknown_class";
    }
}
