using System;
using System.Collections.Generic;
using Game.Shared.Rules;

namespace Game.Server.Items
{
    /// <summary>A concrete item (04, 7.9 ItemInstance). Created only by <see cref="ItemFactory"/> on the server; identity is a GUID.</summary>
    public sealed class ItemInstance
    {
        private Guid _instanceId;
        private string _sourceId;

        public Guid InstanceId
        {
            get => _instanceId;
            set
            {
                _instanceId = value;
                _sourceId = null;
            }
        }

        public string DefinitionId;
        public int Count;
        public int UpgradeLevel;
        public string BoundTo;
        /// <summary>Sockets: [{ Type, IsOvercap, InsertedIngredientId? }] (04, 7.9).</summary>
        public readonly List<ItemSocket> Sockets = new List<ItemSocket>();
        /// <summary>Rolled affixes (04, 7.9: RolledStats/Affixes); SourceId is set when the stats are applied.</summary>
        public readonly List<StatModifier> Affixes = new List<StatModifier>();
        public float CreatedAt;

        /// <summary>
        /// The instance id as text: SourceId of the item's StatModifiers (03, 7.12) and its id in snapshots, commands and saves.
        /// Cached — it is read for every bag slot of every player each tick and in every search by id.
        /// </summary>
        public string SourceId => _sourceId ?? (_sourceId = _instanceId.ToString("N"));
    }

    /// <summary>One socket of an item: its type, whether it is over the limit, the inserted ingredient (definition id) or null.</summary>
    public sealed class ItemSocket
    {
        public string Type;
        public bool IsOvercap;
        public string IngredientId;
    }
}
