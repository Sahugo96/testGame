using System;

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
        public float CreatedAt;

        /// <summary>SourceId for StatModifiers of this item (03, 7.12).</summary>
        public string SourceId => InstanceId.ToString("N");
    }
}
