namespace Game.Shared.Protocol
{
    /// <summary>Spend one free attribute point on a primary attribute (question 52).</summary>
    public struct SpendAttributePointCommand : IGameCommand
    {
        public Game.Shared.Content.StatId Attribute;
    }
}
