using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Chat channels (module 14; 02: chat windows with tabs).</summary>
    public enum ChatChannel : byte
    {
        /// <summary>Players nearby (InterestRadius).</summary>
        Say = 0,
        /// <summary>Everyone on the server.</summary>
        World = 1,
        /// <summary>Group or raid.</summary>
        Group = 2,
        /// <summary>A private message to one player by name.</summary>
        Whisper = 3,
        /// <summary>Server lines (loot votes, joins); localized by TextKey.</summary>
        System = 4,
    }

    /// <summary>A player writes to a channel; the server checks length, rate and recipients.</summary>
    public struct SendChatCommand : IGameCommand
    {
        public ChatChannel Channel;
        public string Text;
        /// <summary>Whisper: the recipient's character name.</summary>
        public string Target;
    }

    /// <summary>A chat line delivered to a player. Player lines carry Text; system lines carry TextKey (+ Number, Name).</summary>
    public struct ChatMessage
    {
        public ChatChannel Channel;
        public EntityId SenderId;
        public string SenderName;
        /// <summary>Whisper: the other side's name (sender sees "to X", recipient "from X").</summary>
        public string TargetName;
        public string Text;
        public string TextKey;
    }
}
