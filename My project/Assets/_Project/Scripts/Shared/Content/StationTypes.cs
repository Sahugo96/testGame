namespace Game.Shared.Content
{
    /// <summary>Known station types of world NPC objects.</summary>
    public static class StationTypes
    {
        /// <summary>Crafting by recipes (05, 7.4; developer decision v15: the anvil crafts).</summary>
        public const string Anvil = "Anvil";
        /// <summary>Item upgrades, UpgradeLevel (04, 7.14.1; developer decision v15: upgrading is done at the whetstone).</summary>
        public const string Whetstone = "Whetstone";
    }
}
