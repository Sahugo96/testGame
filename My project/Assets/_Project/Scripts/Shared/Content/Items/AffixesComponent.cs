namespace Game.Shared.Content.Items
{
    /// <summary>AffixPools (04, 7.9): a new instance rolls as many affixes from Pool as its rarity allows (config AffixCountByRarity). Requires Equipment.</summary>
    public sealed class AffixesComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.Affixes;

        public string PoolId;
    }
}
