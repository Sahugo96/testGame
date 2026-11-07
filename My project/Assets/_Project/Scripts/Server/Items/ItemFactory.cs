using System;
using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Rules;
using Game.Shared.Content;
using Game.Shared.Content.Items;

namespace Game.Server.Items
{
    /// <summary>The only place where item instances are created (principle 10). Every creation is audited.</summary>
    public sealed class ItemFactory
    {
        private const int AffixDecimals = 3;
        private readonly ContentDatabase _content;
        private readonly IAuditLog _audit;

        /// <summary>Unique (divine) items: one instance per world (04, 7.11). Null — no registry (tests without storage).</summary>
        public Persistence.IUniqueItemRegistry UniqueRegistry;

        /// <summary>Rolls sockets and affixes of new instances (server random only).</summary>
        public IRandom Random;

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
            RollSockets(definition, instance);
            RollAffixes(definition, instance);
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
    
        /// <summary>SocketRules (04, 7.9): with the item's chance it gets its socket count, never more than the slot's SocketCap.</summary>
        private void RollSockets(ItemDefinition definition, ItemInstance instance)
        {
            SocketsComponent sockets = definition.Get<SocketsComponent>();
            if (sockets == null || Random == null || Random.NextFloat() >= sockets.Chance)
            {
                return;
            }
            int count = Math.Min(sockets.Count, _content.SocketCapFor(definition));
            for (int i = 0; i < count; i++)
            {
                instance.Sockets.Add(new ItemSocket { Type = sockets.Types[i % sockets.Types.Count] });
            }
        }

        /// <summary>AffixPools (04, 7.9): as many distinct stats from the pool as the rarity allows, each rolled in its range.</summary>
        private void RollAffixes(ItemDefinition definition, ItemInstance instance)
        {
            AffixesComponent affixes = definition.Get<AffixesComponent>();
            int[] byRarity = _content.Config.AffixCountByRarity;
            if (affixes == null || Random == null || byRarity == null || !_content.AffixPools.TryGetValue(affixes.PoolId, out AffixPoolDto pool))
            {
                return;
            }
            int wanted = (int)definition.Rarity < byRarity.Length ? byRarity[(int)definition.Rarity] : 0;
            var options = new List<AffixStatDto>(pool.Stats ?? new AffixStatDto[0]);
            for (int i = 0; i < wanted && options.Count > 0; i++)
            {
                int pick = Math.Min(options.Count - 1, (int)(Random.NextFloat() * options.Count));
                AffixStatDto stat = options[pick];
                options.RemoveAt(pick);
                float value = stat.Min + Random.NextFloat() * (stat.Max - stat.Min);
                instance.Affixes.Add(new StatModifier(
                    (StatId)Enum.Parse(typeof(StatId), stat.Stat),
                    (ModifierOp)Enum.Parse(typeof(ModifierOp), stat.Op),
                    (float)Math.Round(value, AffixDecimals),
                    null));
            }
        }
    }
}
