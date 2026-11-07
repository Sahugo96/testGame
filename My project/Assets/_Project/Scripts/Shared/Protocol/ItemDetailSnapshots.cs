using Game.Shared.Content;

namespace Game.Shared.Protocol
{
    /// <summary>A socket of an item instance (04, 7.9): type, over-the-limit flag, inserted ingredient (definition id) or null.</summary>
    public struct SocketSnapshot
    {
        public string Type;
        public bool IsOvercap;
        public string IngredientId;
    }

    /// <summary>A rolled affix of an item instance (04, 7.9).</summary>
    public struct AffixSnapshot
    {
        public StatId Stat;
        public ModifierOp Op;
        public float Value;
    }

    /// <summary>A wallet balance (05, 7.14.2).</summary>
    public struct CurrencySnapshot
    {
        public string CurrencyId;
        public long Amount;
    }
}
