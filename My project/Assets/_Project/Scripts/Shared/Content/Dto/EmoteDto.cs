using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>
    /// An emote a player may show (02, 7.7 Г: only ids from this white list travel over the network).
    /// `Animation` is the name of a motion clip of the figure (the client's Resources/Animations/&lt;name&gt;.txt, BVH text);
    /// empty — the emote has no animation.
    /// </summary>
    [Serializable]
    public sealed class EmoteDto
    {
        public string Id;
        public string NameKey;
        public string Icon;
        public string Animation;
    }
}
