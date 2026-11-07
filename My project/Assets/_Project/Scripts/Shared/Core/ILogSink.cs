namespace Game.Shared.Core
{
    /// <summary>Output target for <see cref="Log"/>; the host plugs in Unity's console.</summary>
    public interface ILogSink
    {
        void Info(string tag, string message);
        void Warning(string tag, string message);
        void Error(string tag, string message);
    }
}
