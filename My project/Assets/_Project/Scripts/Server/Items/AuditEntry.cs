namespace Game.Server.Items
{
    /// <summary>One audited item/currency operation (principle 10): who, what, how many, when.</summary>
    public struct AuditEntry
    {
        public float Time;
        public string Action;
        public string ActorId;
        public string InstanceId;
        public string DefinitionId;
        public int Count;
    }
}
