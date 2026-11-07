using System.Collections.Generic;
using Game.Server.Items;
using Game.Server.Players;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Bag and equipment operations, all validated on the server and audited (principle 10).
    /// In combat only weapons may be swapped, with the timers of question 10; armor and jewelry are locked.
    /// </summary>
    public sealed class InventorySystem
    {
        private readonly ServerContext _ctx;
        private readonly StatSystem _stats;
        private readonly ResourceSystem _resources;
        private readonly List<string> _cooldownKeys = new List<string>();

        private readonly StatusSystem _statuses;

        public InventorySystem(ServerContext ctx, StatSystem stats, ResourceSystem resources, StatusSystem statuses)
        {
            _statuses = statuses;
            _ctx = ctx;
            _stats = stats;
            _resources = resources;
        }

        public void GiveStartingItems(PlayerCharacter player)
        {
            foreach (StartingItemDto start in player.Class.StartingItems)
            {
                ItemInstance item = GiveItem(player, start.ItemId, start.Count);
                if (item != null && start.Equip)
                {
                    Equip(player, new EquipItemCommand { BagIndex = player.Bag.IndexOf(item) });
                }
            }
        }

        /// <summary>
        /// Puts items into the bag section of their category: tops up existing stacks first, then creates new instances
        /// through the item factory (only the server creates items). Returns the first new instance, if any.
        /// Items that do not fit are not picked up and the player is notified (question 68; ground loot with LootEntry arrives in module 10).
        /// </summary>
        public ItemInstance GiveItem(PlayerCharacter player, string definitionId, int count)
        {
            if (!_ctx.Content.Items.TryGetValue(definitionId, out ItemDefinition definition) || count <= 0)
            {
                return null;
            }
            BagSection section = SectionOf(definition);
            int remaining = count;
            if (definition.MaxStack > 1)
            {
                for (int i = section.Start; i < section.Start + section.Size && remaining > 0; i++)
                {
                    ItemInstance stack = player.Bag.Get(i);
                    if (stack == null || stack.DefinitionId != definitionId || stack.Count >= definition.MaxStack)
                    {
                        continue;
                    }
                    int added = System.Math.Min(remaining, definition.MaxStack - stack.Count);
                    stack.Count += added;
                    remaining -= added;
                    Audit(AuditActions.StackAdd, player, stack, added);
                }
            }
            ItemInstance first = null;
            while (remaining > 0)
            {
                int index = player.Bag.FindFreeSlot(section.Start, section.Size);
                if (index < 0)
                {
                    player.Outbox.Notifications.Add(new NotificationMessage
                    {
                        TextKey = NotificationKeys.BagFullItemLost,
                        Number = remaining,
                        ItemNameKey = definition.NameKey,
                    });
                    break;
                }
                ItemInstance created = _ctx.Items.Create(definitionId, remaining, player.CharacterId, _ctx.Time);
                if (created == null)
                {
                    break; // a unique item that already exists elsewhere (04, 7.11)
                }
                player.Bag.Set(index, created);
                remaining -= created.Count;
                if (first == null)
                {
                    first = created;
                }
            }
            return first;
        }

        /// <summary>Whether count units fit into the item's bag section (free stack room plus free slots).</summary>
        public bool CanFit(PlayerCharacter player, string definitionId, int count)
        {
            if (!_ctx.Content.Items.TryGetValue(definitionId, out ItemDefinition definition))
            {
                return false;
            }
            BagSection section = SectionOf(definition);
            int room = 0;
            for (int i = section.Start; i < section.Start + section.Size; i++)
            {
                ItemInstance item = player.Bag.Get(i);
                if (item == null)
                {
                    room += definition.MaxStack;
                }
                else if (item.DefinitionId == definitionId)
                {
                    room += definition.MaxStack - item.Count;
                }
                if (room >= count)
                {
                    return true;
                }
            }
            return false;
        }

        public int CountItems(PlayerCharacter player, string definitionId)
        {
            int total = 0;
            for (int i = 0; i < player.Bag.Size; i++)
            {
                ItemInstance item = player.Bag.Get(i);
                if (item != null && item.DefinitionId == definitionId)
                {
                    total += item.Count;
                }
            }
            return total;
        }

        /// <summary>Removes count units of an item from the bag (all or nothing) and audits it.</summary>
        public bool ConsumeItems(PlayerCharacter player, string definitionId, int count)
        {
            if (CountItems(player, definitionId) < count)
            {
                return false;
            }
            int remaining = count;
            for (int i = 0; i < player.Bag.Size && remaining > 0; i++)
            {
                ItemInstance item = player.Bag.Get(i);
                if (item == null || item.DefinitionId != definitionId)
                {
                    continue;
                }
                int taken = System.Math.Min(remaining, item.Count);
                item.Count -= taken;
                remaining -= taken;
                Audit(AuditActions.MaterialSpent, player, item, taken);
                if (item.Count <= 0)
                {
                    player.Bag.Set(i, null);
                }
            }
            return true;
        }

        /// <summary>Finds an owned item (bag or equipment) by instance id.</summary>
        public ItemInstance FindOwned(PlayerCharacter player, string instanceId, out string equippedSlotId)
        {
            equippedSlotId = null;
            for (int i = 0; i < player.Bag.Size; i++)
            {
                ItemInstance item = player.Bag.Get(i);
                if (item != null && item.SourceId == instanceId)
                {
                    return item;
                }
            }
            foreach (var pair in player.Equipment)
            {
                if (pair.Value.SourceId == instanceId)
                {
                    equippedSlotId = pair.Key;
                    return pair.Value;
                }
            }
            return null;
        }

        /// <summary>Destroys an owned item (bag or equipment) and audits it.</summary>
        public void DestroyOwned(PlayerCharacter player, ItemInstance item, string equippedSlotId)
        {
            if (equippedSlotId != null)
            {
                player.Equipment.Remove(equippedSlotId);
                _stats.Recalculate(player);
            }
            else
            {
                player.Bag.Set(player.Bag.IndexOf(item), null);
            }
            Audit(AuditActions.Destroy, player, item, item.Count);
        }

        public void Equip(PlayerCharacter player, EquipItemCommand command)
        {
            ItemInstance item = player.Bag.Get(command.BagIndex);
            if (item == null)
            {
                CombatEvents.Reject(player, RejectReasons.ItemNotFound);
                return;
            }
            ItemDefinition definition = _ctx.Content.Items[item.DefinitionId];
            EquipSlotComponent slotComponent = definition.Get<EquipSlotComponent>();
            if (slotComponent == null || !definition.Has<EquipmentComponent>())
            {
                CombatEvents.Reject(player, RejectReasons.CannotEquip);
                return;
            }
            if (!definition.IsUsableBy(player.Class.Id) || !IsAllowedType(player, definition))
            {
                CombatEvents.Reject(player, RejectReasons.WrongClass);
                return;
            }
            if (definition.RequiredLevel > player.Level)
            {
                CombatEvents.Reject(player, RejectReasons.LevelTooLow);
                return;
            }
            EquipmentSlotDto slot = ChooseSlot(player, slotComponent.SlotType);
            if (slot == null)
            {
                CombatEvents.Reject(player, RejectReasons.CannotEquip);
                return;
            }
            if (!CheckCombatSwap(player, slot))
            {
                return;
            }

            // Items that must leave equipment: the one in the slot, plus the other hand for two-handed weapons.
            var displaced = new List<string>();
            if (player.Equipment.ContainsKey(slot.Id))
            {
                displaced.Add(slot.Id);
            }
            if (slot.IsMainHand && slotComponent.Hands > 1)
            {
                string offHand = FindSlotId(player, s => s.IsOffHand);
                if (offHand != null && player.Equipment.ContainsKey(offHand))
                {
                    displaced.Add(offHand);
                }
            }
            if (slot.IsOffHand)
            {
                string mainHand = FindSlotId(player, s => s.IsMainHand);
                if (mainHand != null && player.Equipment.TryGetValue(mainHand, out ItemInstance main)
                    && _ctx.Content.Items[main.DefinitionId].Get<EquipSlotComponent>().Hands > 1)
                {
                    displaced.Add(mainHand);
                }
            }
            // The freed bag slot of the new item (same section: equipment) can take one displaced item.
            BagSection section = SectionOf(definition);
            if (displaced.Count > 1 + player.Bag.FreeSlotCount(section.Start, section.Size))
            {
                CombatEvents.Reject(player, RejectReasons.BagFull);
                return;
            }

            player.Bag.Set(command.BagIndex, null);
            foreach (string slotId in displaced)
            {
                ItemInstance removed = player.Equipment[slotId];
                player.Equipment.Remove(slotId);
                PlaceInBag(player, removed);
                Audit(AuditActions.Unequip, player, removed);
            }
            player.Equipment[slot.Id] = item;
            if (definition.Binding == BindingType.OnEquip && item.BoundTo == null)
            {
                item.BoundTo = player.CharacterId;
            }
            Audit(AuditActions.Equip, player, item);
            RegisterCombatSwap(player, slot);
            _stats.Recalculate(player);
        }

        public void Unequip(PlayerCharacter player, UnequipItemCommand command)
        {
            if (command.SlotId == null || !player.Equipment.TryGetValue(command.SlotId, out ItemInstance item))
            {
                CombatEvents.Reject(player, RejectReasons.ItemNotFound);
                return;
            }
            EquipmentSlotDto slot = FindSlot(player, command.SlotId);
            if (slot == null)
            {
                CombatEvents.Reject(player, RejectReasons.ItemNotFound);
                return;
            }
            if (!CheckCombatSwap(player, slot))
            {
                return;
            }
            BagSection section = SectionOf(_ctx.Content.Items[item.DefinitionId]);
            if (player.Bag.FindFreeSlot(section.Start, section.Size) < 0)
            {
                CombatEvents.Reject(player, RejectReasons.BagFull);
                return;
            }
            player.Equipment.Remove(command.SlotId);
            PlaceInBag(player, item);
            Audit(AuditActions.Unequip, player, item);
            RegisterCombatSwap(player, slot);
            _stats.Recalculate(player);
        }

        /// <summary>Consumables (02, 7.7 Б; 03, 7.12): mana restores work at ManaConsumableInCombatFactor in combat (question 1).</summary>
        public void UseConsumable(PlayerCharacter player, UseConsumableCommand command)
        {
            ItemInstance item = player.Bag.Get(command.BagIndex);
            if (item == null)
            {
                CombatEvents.Reject(player, RejectReasons.ItemNotFound);
                return;
            }
            if (!_ctx.World.IsAlive(player.EntityId))
            {
                CombatEvents.Reject(player, RejectReasons.Dead);
                return;
            }
            ConsumableComponent consumable = _ctx.Content.Items[item.DefinitionId].Get<ConsumableComponent>();
            if (consumable == null)
            {
                CombatEvents.Reject(player, RejectReasons.NotConsumable);
                return;
            }
            if (!string.IsNullOrEmpty(consumable.CooldownGroup)
                && player.ConsumableCooldowns.TryGetValue(consumable.CooldownGroup, out float remaining) && remaining > 0f)
            {
                CombatEvents.Reject(player, RejectReasons.OnCooldown);
                return;
            }
            if (consumable.EffectKey == EffectKeys.ApplyStatus)
            {
                // Buff potions use the same statuses (03, 7.12); one exclusive group replaces the other.
                if (!_statuses.Apply(player.EntityId, player.EntityId, consumable.StatusId, 1f))
                {
                    CombatEvents.Reject(player, RejectReasons.CannotUse);
                    return;
                }
            }
            else
            {
                if (_resources.Missing(player.EntityId, consumable.Resource) <= 0f)
                {
                    CombatEvents.Reject(player, RejectReasons.ResourceFull);
                    return;
                }
                float amount = consumable.Amount;
                bool isMana = consumable.Resource != ResourceKind.Health;
                if (isMana && _resources.IsInCombat(player.EntityId))
                {
                    amount *= _ctx.Content.Config.ManaConsumableInCombatFactor;
                }
                _resources.Restore(player.EntityId, consumable.Resource, amount, player.EntityId, null);
            }
            if (!string.IsNullOrEmpty(consumable.CooldownGroup))
            {
                player.ConsumableCooldowns[consumable.CooldownGroup] = consumable.Cooldown;
            }
            item.Count--;
            Audit(AuditActions.Consume, player, item);
            if (item.Count <= 0)
            {
                player.Bag.Set(command.BagIndex, null);
            }
        }

        public void Update(float dt)
        {
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                player.WeaponSwap.Tick(dt);
                _cooldownKeys.Clear();
                _cooldownKeys.AddRange(player.ConsumableCooldowns.Keys);
                foreach (string key in _cooldownKeys)
                {
                    float value = player.ConsumableCooldowns[key] - dt;
                    player.ConsumableCooldowns[key] = value > 0f ? value : 0f;
                }
            }
        }

        private bool CheckCombatSwap(PlayerCharacter player, EquipmentSlotDto slot)
        {
            if (!_resources.IsInCombat(player.EntityId))
            {
                return true;
            }
            if (!slot.IsMainHand && !slot.IsOffHand)
            {
                CombatEvents.Reject(player, RejectReasons.ArmorInCombat);
                return false;
            }
            bool allowed = slot.IsMainHand ? player.WeaponSwap.CanSwapMainHand : player.WeaponSwap.CanSwapOffHand;
            if (!allowed)
            {
                CombatEvents.Reject(player, RejectReasons.WeaponSwapLocked);
            }
            return allowed;
        }

        private void RegisterCombatSwap(PlayerCharacter player, EquipmentSlotDto slot)
        {
            if (!_resources.IsInCombat(player.EntityId))
            {
                return;
            }
            GameConfig cfg = _ctx.Content.Config;
            if (slot.IsMainHand)
            {
                player.WeaponSwap.OnMainHandSwapped(cfg.MainHandSwapCooldownSeconds, cfg.MainHandSwapRiskWindowSeconds, cfg.MainHandSwapPenaltySeconds);
            }
            else if (slot.IsOffHand)
            {
                player.WeaponSwap.OnOffHandSwapped(cfg.OffHandSwapCooldownSeconds);
            }
        }

        private bool IsAllowedType(PlayerCharacter player, ItemDefinition definition)
        {
            ArmorComponent armor = definition.Get<ArmorComponent>();
            if (armor != null && !player.Class.AllowedArmorTypes.Contains(armor.ArmorType))
            {
                return false;
            }
            WeaponComponent weapon = definition.Get<WeaponComponent>();
            return weapon == null || player.Class.AllowedWeaponTypes.Contains(weapon.WeaponType);
        }

        /// <summary>First free slot of the type, otherwise the first slot of the type (its item gets swapped out).</summary>
        private EquipmentSlotDto ChooseSlot(PlayerCharacter player, string slotType)
        {
            EquipmentSlotDto fallback = null;
            foreach (EquipmentSlotDto slot in _ctx.Content.SlotsFor(player.Class))
            {
                if (slot.SlotType != slotType)
                {
                    continue;
                }
                if (!player.Equipment.ContainsKey(slot.Id))
                {
                    return slot;
                }
                if (fallback == null)
                {
                    fallback = slot;
                }
            }
            return fallback;
        }

        private EquipmentSlotDto FindSlot(PlayerCharacter player, string slotId)
        {
            foreach (EquipmentSlotDto slot in _ctx.Content.SlotsFor(player.Class))
            {
                if (slot.Id == slotId)
                {
                    return slot;
                }
            }
            return null;
        }

        private string FindSlotId(PlayerCharacter player, System.Func<EquipmentSlotDto, bool> predicate)
        {
            foreach (EquipmentSlotDto slot in _ctx.Content.SlotsFor(player.Class))
            {
                if (predicate(slot))
                {
                    return slot.Id;
                }
            }
            return null;
        }

        /// <summary>The bag section an item goes to (by its category).</summary>
        public BagSection SectionOf(ItemDefinition definition) =>
            _ctx.Content.BagSections[_ctx.Content.SectionIndexFor(definition.Category)];

        private void PlaceInBag(PlayerCharacter player, ItemInstance item)
        {
            BagSection section = SectionOf(_ctx.Content.Items[item.DefinitionId]);
            player.Bag.Set(player.Bag.FindFreeSlot(section.Start, section.Size), item);
        }

        private void Audit(string action, PlayerCharacter player, ItemInstance item) => Audit(action, player, item, item.Count);

        private void Audit(string action, PlayerCharacter player, ItemInstance item, int count) =>
            _ctx.Audit.Write(new AuditEntry
            {
                Time = _ctx.Time,
                Action = action,
                ActorId = player.CharacterId,
                InstanceId = item.SourceId,
                DefinitionId = item.DefinitionId,
                Count = count,
            });
    }
}
