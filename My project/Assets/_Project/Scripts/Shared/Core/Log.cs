namespace Game.Shared.Core
{
    /// <summary>Single logging entry point with module tags (01_DEV_PLAN §6).</summary>
    public static class Log
    {
        private static ILogSink _sink;

        public static void SetSink(ILogSink sink) => _sink = sink;

        public static void Info(string tag, string message) => _sink?.Info(tag, message);
        public static void Warning(string tag, string message) => _sink?.Warning(tag, message);
        public static void Error(string tag, string message) => _sink?.Error(tag, message);
    }
}
