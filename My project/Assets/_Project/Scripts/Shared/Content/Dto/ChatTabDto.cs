using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>A default chat tab (config.ChatDefaultTabs): localized name and the ChatChannel names it shows.</summary>
    [Serializable]
    public class ChatTabDto
    {
        public string NameKey;
        public string[] Channels;
    }
}
