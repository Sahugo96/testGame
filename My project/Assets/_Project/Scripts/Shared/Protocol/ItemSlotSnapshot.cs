namespace Game.Shared.Protocol
{
    /// <summary>One bag slot; empty when DefinitionId is null.</summary>
    public struct ItemSlotSnapshot
    {
        public int Index;
        public string InstanceId;
        public string DefinitionId;
        public int Count;
        public int UpgradeLevel;
    }
}
