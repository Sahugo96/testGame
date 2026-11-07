namespace Game.Shared.Protocol
{
    /// <summary>Account setting: auto-pickup of drops on or off (developer decision, question 77).</summary>
    public struct SetAutoLootCommand : IGameCommand
    {
        public bool Enabled;
    }
}
