using System;
using System.Collections.Generic;
using Game.Shared.Rules;

namespace Game.Server.Items
{
    /// <summary>A concrete item (04, 7.9 ItemInstance). Created only by <see cref="ItemFactory"/> on the server; identity is a GUID.</summary>
    public sealed class ItemInstance
    {
        public Guid InstanceId;
        public string DefinitionId;
        public int Count;
        public int UpgradeLevel;
        public string BoundTo;
        /// <summary>Sockets: [{ Type, IsOvercap, InsertedIngredientId? }] (04, 7.9).</summary>
        public readonly List<ItemSocket> Sockets = new List<ItemSocket>();
        /// <summary>Rolled affixes (04, 7.9: RolledStats/Affixes); SourceId is set when the stats are applied.</summary>
        public readonly List<StatModifier> Affixes = new List<StatModifier>();
        public float CreatedAt;

        /// <summary>SourceId for StatModifiers of this item (03, 7.12).</summary>
        public string SourceId => InstanceId.ToString("N");
    }

    /// <summary>One socket of an item: its type, whether it is over the limit, the inserted ingredient (definition id) or null.</summary>
    public sealed class ItemSocket
    {
        public string Type;
        public bool IsOvercap;
        public string IngredientId;
    }
}
