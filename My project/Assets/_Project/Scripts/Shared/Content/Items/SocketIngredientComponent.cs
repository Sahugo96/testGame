using System.Collections.Generic;
using Game.Shared.Rules;

namespace Game.Shared.Content.Items
{
    /// <summary>A socket ingredient (04, 7.9: cores, souls, minerals): fits sockets of SocketType (or universal ones); its stats join the item's.</summary>
    public sealed class SocketIngredientComponent : ItemComponent
    {
        public override string Type => ItemComponentTypes.SocketIngredient;

        public string SocketType;
        public readonly List<StatModifier> Modifiers = new List<StatModifier>();
    }
}
