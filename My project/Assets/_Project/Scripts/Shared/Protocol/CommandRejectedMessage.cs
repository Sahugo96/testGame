namespace Game.Shared.Protocol
{
    /// <summary>Server refused a command; ReasonKey is a localization key shown to the player.</summary>
    public struct CommandRejectedMessage
    {
        public string ReasonKey;
    }
}
