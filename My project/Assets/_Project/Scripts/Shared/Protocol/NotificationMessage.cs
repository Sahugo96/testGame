namespace Game.Shared.Protocol
{
    /// <summary>Server notice for the player (level up, upgrade result, loot). TextKey is a localization key; Number and ItemNameKey fill it.</summary>
    public struct NotificationMessage
    {
        public string TextKey;
        public int Number;
        public string ItemNameKey;
        public bool IsPositive;
    }
}
