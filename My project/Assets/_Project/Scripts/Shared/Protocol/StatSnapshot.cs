using Game.Shared.Content;

namespace Game.Shared.Protocol
{
    /// <summary>Final value of a stat.</summary>
    public struct StatSnapshot
    {
        public StatId Stat;
        public float Value;
    }
}
