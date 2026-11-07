using System;
using Game.Shared.Content;
using Game.Shared.Content.Items;

namespace Game.Server.Items
{
    /// <summary>The only place where item instances are created (principle 10). Every creation is audited.</summary>
    public sealed class ItemFactory
    {
        private readonly ContentDatabase _content;
        private readonly IAuditLog _audit;

        /// <summary>Unique (divine) items: one instance per world (04, 7.11). Null — no registry (tests without storage).</summary>
        public Persistence.IUniqueItemRegistry UniqueRegistry;

        public ItemFactory(ContentDatabase content, IAuditLog audit)
        {
            _content = content;
            _audit = audit;
        }

        public ItemInstance Create(string definitionId, int count, string ownerId, float time)
        {
            if (!_content.Items.TryGetValue(definitionId, out ItemDefinition definition))
            {
                return null;
            }
            Guid newId = Guid.NewGuid();
            if (definition.Rarity == Rarity.Divine && UniqueRegistry != null && !UniqueRegistry.TryReserve(definitionId, newId.ToString("N"), ownerId))
            {
                _audit.Write(new AuditEntry { Time = time, Action = AuditActions.UniqueRefused, ActorId = ownerId, DefinitionId = definitionId, Count = count });
                return null;
            }
            var instance = new ItemInstance
            {
                InstanceId = newId,
                DefinitionId = definitionId,
                Count = Math.Max(1, Math.Min(count, definition.MaxStack)),
                CreatedAt = time,
                BoundTo = definition.Binding == BindingType.OnPickup ? ownerId : null,
            };
            _audit.Write(new AuditEntry
            {
                Time = time,
                Action = AuditActions.Create,
                ActorId = ownerId,
                InstanceId = instance.SourceId,
                DefinitionId = definitionId,
                Count = instance.Count,
            });
            return instance;
        }
    }
}
