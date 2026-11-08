namespace Game.Shared.Content
{
    /// <summary>Known station types of world NPC objects.</summary>
    public static class StationTypes
    {
        /// <summary>Crafting by recipes (05, 7.4; developer decision v15: the anvil crafts).</summary>
        public const string Anvil = "Anvil";
        /// <summary>Item upgrades, UpgradeLevel (04, 7.14.1; developer decision v15: upgrading is done at the whetstone).</summary>
        public const string Whetstone = "Whetstone";
        /// <summary>"Главный управляющий рынком": opens the market window (05, 7.14.3).</summary>
        public const string Market = "Market";
        public const string Mailbox = "Mailbox";
        /// <summary>A place in town where a player may put a personal merchant (question 58).</summary>
        public const string ShopSpot = "ShopSpot";
        /// <summary>A player's merchant standing on a shop spot (spawned while the shop is open).</summary>
        public const string Merchant = "Merchant";
    }
}
