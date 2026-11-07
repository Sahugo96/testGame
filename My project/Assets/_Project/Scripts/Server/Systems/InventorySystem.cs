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

        public InventorySystem(ServerContext ctx, StatSystem stats, ResourceSystem resources)
        {
            _ctx = ctx;
            _stats = stats;
            _resources = resources;
        }

        public void GiveStartingItems(PlayerCharacter player)
        {
            foreach (StartingItemDto start in player.Class.StartingItems)
            {
                ItemInstance item = _ctx.Items.Create(start.ItemId, start.Count, player.CharacterId, _ctx.Time);
                if (item == null || player.Bag.Add(item) < 0)
                {
                    continue;
                }
                if (start.Equip)
                {
                    Equip(player, new EquipItemCommand { BagIndex = FindInBag(player, item) });
                }
            }
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
            // The freed bag slot of the new item can take one displaced item.
            if (displaced.Count > 1 + player.Bag.FreeSlotCount())
            {
                CombatEvents.Reject(player, RejectReasons.BagFull);
                return;
            }

            player.Bag.Set(command.BagIndex, null);
            foreach (string slotId in displaced)
            {
                ItemInstance removed = player.Equipment[slotId];
                player.Equipment.Remove(slotId);
                player.Bag.Add(removed);
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
            if (player.Bag.FindFreeSlot() < 0)
            {
                CombatEvents.Reject(player, RejectReasons.BagFull);
                return;
            }
            player.Equipment.Remove(command.SlotId);
            player.Bag.Add(item);
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
            if (consumable.EffectKey == EffectKeys.RestoreResource && _resources.Missing(player.EntityId, consumable.Resource) <= 0f)
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

        private static int FindInBag(PlayerCharacter player, ItemInstance item)
        {
            for (int i = 0; i < player.Bag.Size; i++)
            {
                if (player.Bag.Get(i) == item)
                {
                    return i;
                }
            }
            return -1;
        }

        private void Audit(string action, PlayerCharacter player, ItemInstance item) =>
            _ctx.Audit.Write(new AuditEntry
            {
                Time = _ctx.Time,
                Action = action,
                ActorId = player.CharacterId,
                InstanceId = item.SourceId,
                DefinitionId = item.DefinitionId,
                Count = item.Count,
            });
    }
}
