namespace Game.Shared.Protocol
{
    /// <summary>One equipment slot; empty when DefinitionId is null.</summary>
    public struct EquipmentSlotSnapshot
    {
        public string SlotId;
        public string InstanceId;
        public string DefinitionId;
    }
}
