namespace Game.Network
{
    /// <summary>First byte of every frame: what message follows.</summary>
    public enum MessageType : byte
    {
        Hello = 1,
        Welcome = 2,
        Move = 10,
        UseSkill = 11,
        SelectTarget = 12,
        CycleTarget = 13,
        Equip = 14,
        Unequip = 15,
        UseConsumable = 16,
        UpgradeItem = 17,
        SpendAttributePoint = 18,
        World = 40,
        PlayerState = 41,
        CombatEvent = 42,
        Rejected = 43,
        Notification = 44,
    }
}
