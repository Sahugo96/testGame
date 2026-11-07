namespace Game.Server.Items
{
    /// <summary>Audit log action names.</summary>
    public static class AuditActions
    {
        public const string Create = "create";
        public const string Equip = "equip";
        public const string Unequip = "unequip";
        public const string Consume = "consume";
        public const string Destroy = "destroy";
        public const string StackAdd = "stack_add";
        public const string UpgradeSuccess = "upgrade_success";
        public const string UpgradeFail = "upgrade_fail";
        public const string MaterialSpent = "material_spent";
    }
}
