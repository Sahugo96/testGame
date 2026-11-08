using System;
using Game.Server.Items;
using Game.Shared.Content;
using Game.Shared.Rules;
using Game.Shared.Core;

namespace Game.Server.Persistence
{
    /// <summary>Item instance ↔ saved record (characters, shop lots, mail, escrow): the same GUID, sockets and affixes.</summary>
    public static class ItemRecords
    {
        public static ItemRecord Export(ItemInstance item, int bagIndex, string slotId) => new ItemRecord
        {
            BagIndex = bagIndex,
            SlotId = slotId,
            InstanceId = item.SourceId,
            DefinitionId = item.DefinitionId,
            Count = item.Count,
            UpgradeLevel = item.UpgradeLevel,
            BoundTo = item.BoundTo,
            Sockets = item.Sockets.ConvertAll(s => new SocketRecord { Type = s.Type, IsOvercap = s.IsOvercap, IngredientId = s.IngredientId }).ToArray(),
            Affixes = item.Affixes.ConvertAll(a => new AffixRecord { Stat = a.Stat.ToString(), Op = a.Op.ToString(), Value = a.Value }).ToArray(),
        };

        /// <summary>Null (and a warning) if the definition left the content or the id is broken.</summary>
        public static ItemInstance Restore(ContentDatabase content, ItemRecord record, float time)
        {
            if (record == null || !content.Items.ContainsKey(record.DefinitionId ?? string.Empty) || !Guid.TryParse(record.InstanceId, out Guid instanceId))
            {
                Log.Warning(LogTags.Items, $"Saved item '{record?.DefinitionId}' ({record?.InstanceId}) is not in the content: skipped");
                return null;
            }
            var item = new ItemInstance
            {
                InstanceId = instanceId,
                DefinitionId = record.DefinitionId,
                Count = Math.Max(1, record.Count),
                UpgradeLevel = record.UpgradeLevel,
                BoundTo = record.BoundTo,
                CreatedAt = time,
            };
            foreach (SocketRecord socket in record.Sockets ?? new SocketRecord[0])
            {
                item.Sockets.Add(new ItemSocket { Type = socket.Type, IsOvercap = socket.IsOvercap, IngredientId = string.IsNullOrEmpty(socket.IngredientId) ? null : socket.IngredientId });
            }
            foreach (AffixRecord affix in record.Affixes ?? new AffixRecord[0])
            {
                if (Enum.TryParse(affix.Stat, out StatId stat) && Enum.TryParse(affix.Op, out ModifierOp op))
                {
                    item.Affixes.Add(new StatModifier(stat, op, affix.Value, null));
                }
            }
            return item;
        }
    }
}
