namespace Game.Network
{
    /// <summary>Transport limits. Bump ProtocolVersion on any wire format change: old builds are then refused politely.</summary>
    public static class NetworkConstants
    {
        public const int ProtocolVersion = 1;
        public const int MaxFrameBytes = 4 * 1024 * 1024;
        public const int ConnectTimeoutMs = 5000;
        public const string ErrorVersionMismatch = "net.version_mismatch";
        public const string ErrorUnknownClass = "net.unknown_class";
    }
}
