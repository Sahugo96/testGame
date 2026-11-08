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
        /// <summary>A unique item already exists in the world: a second one is not created (04, 7.11).</summary>
        public const string UniqueRefused = "unique_refused";
        public const string CurrencyDebit = "currency_debit";
        public const string CurrencyCredit = "currency_credit";
        public const string SocketInsert = "socket_insert";
        public const string SocketDestroy = "socket_destroy";
        /// <summary>An item changed hands in a trade (Reason = receiver).</summary>
        public const string TradeGive = "trade_give";
        public const string StackSplit = "stack_split";
        public const string StackMerge = "stack_merge";
    }
}
