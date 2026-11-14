using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Show an emote (02, 7.7 Г: CmdEmoji): only an id from the content's white list, rate-limited by the server.</summary>
    public struct EmoteCommand : IGameCommand
    {
        public string EmoteId;
    }

    /// <summary>An entity started an emote; sent to the players who see it (and to the player themselves).</summary>
    public sealed class EmoteMessage
    {
        public EntityId EntityId;
        public string EmoteId;
    }
}
