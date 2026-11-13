using System;
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
    /// Quick panels of a character (02, 7.7 А–В; module 18). Slots hold references only — a class skill id, a consumable item
    /// definition id; using them still goes through UseSkill / UseConsumable, checked as before. Loadouts remember the
    /// equipment by instance id; applying one equips what is still in the bag through InventorySystem.Equip (so every rule of
    /// equipping, including weapon swaps in combat, applies); in combat only weapons change (question 10). The layout is
    /// saved with the character and sent to the client after login and after each change.
    /// </summary>
    public sealed class PanelSystem
    {
        private readonly ServerContext _ctx;
        private readonly InventorySystem _inventory;

        public PanelSystem(ServerContext ctx, InventorySystem inventory)
        {
            _ctx = ctx;
            _inventory = inventory;
        }

        private int SkillSlots => Math.Max(1, _ctx.Content.Config.SkillBarRows) * Math.Max(1, _ctx.Content.Config.SkillBarSlotsPerRow);

        private int ConsumableSlots => Math.Max(1, _ctx.Content.Config.ConsumableBarSlots);

        private int LoadoutSlots => Math.Max(1, _ctx.Content.Config.LoadoutCount);

        /// <summary>
        /// A character without a saved layout: the class skills in the first row (as before module 18) and the consumables of
        /// the bag in the consumable panel.
        /// </summary>
        public void InitDefaults(PlayerCharacter player)
        {
            PanelData panels = player.Panels;
            panels.SkillBar = new string[SkillSlots];
            int perRow = Math.Max(1, _ctx.Content.Config.SkillBarSlotsPerRow);
            for (int i = 0; i < player.Class.Skills.Count && i < perRow; i++)
            {
                panels.SkillBar[i] = player.Class.Skills[i].SkillId;
            }
            panels.ConsumableBar = new string[ConsumableSlots];
            int slot = 0;
            for (int i = 0; i < player.Bag.Size && slot < panels.ConsumableBar.Length; i++)
            {
                ItemInstance item = player.Bag.Get(i);
                if (item != null && IsConsumable(item.DefinitionId) && Array.IndexOf(panels.ConsumableBar, item.DefinitionId) < 0)
                {
                    panels.ConsumableBar[slot++] = item.DefinitionId;
                }
            }
            panels.Loadouts = NewLoadouts();
            panels.ToSend = true;
        }

        /// <summary>A saved layout (persistence): slots are fitted to the current panel sizes; unknown ids are dropped.</summary>
        public void Load(PlayerCharacter player, string[] skillBar, string[] consumableBar, LoadoutData[] loadouts)
        {
            if (skillBar == null || skillBar.Length == 0)
            {
                InitDefaults(player);
                return;
            }
            PanelData panels = player.Panels;
            panels.SkillBar = new string[SkillSlots];
            for (int i = 0; i < skillBar.Length && i < panels.SkillBar.Length; i++)
            {
                panels.SkillBar[i] = IsClassSkill(player, skillBar[i]) ? skillBar[i] : null;
            }
            panels.ConsumableBar = new string[ConsumableSlots];
            for (int i = 0; consumableBar != null && i < consumableBar.Length && i < panels.ConsumableBar.Length; i++)
            {
                panels.ConsumableBar[i] = IsConsumable(consumableBar[i]) ? consumableBar[i] : null;
            }
            panels.Loadouts = NewLoadouts();
            for (int i = 0; loadouts != null && i < loadouts.Length && i < panels.Loadouts.Length; i++)
            {
                if (loadouts[i] != null)
                {
                    panels.Loadouts[i] = loadouts[i];
                }
            }
            panels.ToSend = true;
        }

        public void SetSkillSlot(PlayerCharacter player, SetSkillSlotCommand command)
        {
            string skillId = string.IsNullOrEmpty(command.SkillId) ? null : command.SkillId;
            if (command.Index < 0 || command.Index >= player.Panels.SkillBar.Length || (skillId != null && !IsClassSkill(player, skillId)))
            {
                CombatEvents.Reject(player, RejectReasons.PanelSlotInvalid);
                return;
            }
            player.Panels.SkillBar[command.Index] = skillId;
            player.Panels.ToSend = true;
        }

        public void SetConsumableSlot(PlayerCharacter player, SetConsumableSlotCommand command)
        {
            string itemId = string.IsNullOrEmpty(command.ItemId) ? null : command.ItemId;
            if (command.Index < 0 || command.Index >= player.Panels.ConsumableBar.Length || (itemId != null && !IsConsumable(itemId)))
            {
                CombatEvents.Reject(player, RejectReasons.PanelSlotInvalid);
                return;
            }
            player.Panels.ConsumableBar[command.Index] = itemId;
            player.Panels.ToSend = true;
        }

        public void SaveLoadout(PlayerCharacter player, SaveLoadoutCommand command)
        {
            if (command.Index < 0 || command.Index >= player.Panels.Loadouts.Length)
            {
                CombatEvents.Reject(player, RejectReasons.PanelSlotInvalid);
                return;
            }
            var loadout = new LoadoutData();
            foreach (EquipmentSlotDto slot in _ctx.Content.EquipmentSlots)
            {
                if (player.Equipment.TryGetValue(slot.Id, out ItemInstance item))
                {
                    loadout.Items.Add(new LoadoutItemData { SlotId = slot.Id, InstanceId = item.SourceId, DefinitionId = item.DefinitionId });
                }
            }
            player.Panels.Loadouts[command.Index] = loadout;
            player.Panels.ToSend = true;
            player.Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.LoadoutSaved, Number = command.Index + 1, IsPositive = true });
        }

        /// <summary>
        /// Puts on the loadout's items that are still in the bag (main hand first, then the file order of slots); what is
        /// already worn stays; nothing else is taken off. In combat armour is skipped. Missing items: partial, with a notice.
        /// </summary>
        public void ApplyLoadout(PlayerCharacter player, ApplyLoadoutCommand command)
        {
            if (command.Index < 0 || command.Index >= player.Panels.Loadouts.Length || player.Panels.Loadouts[command.Index].Items.Count == 0)
            {
                CombatEvents.Reject(player, RejectReasons.LoadoutEmpty);
                return;
            }
            bool inCombat = _ctx.World.CombatStates.Has(player.EntityId) && _ctx.World.CombatStates.Get(player.EntityId).InCombat;
            var ordered = new List<LoadoutItemData>(player.Panels.Loadouts[command.Index].Items);
            ordered.Sort((a, b) => Rank(a.SlotId).CompareTo(Rank(b.SlotId)));
            int missing = 0;
            int skippedArmour = 0;
            foreach (LoadoutItemData entry in ordered)
            {
                EquipmentSlotDto slot = _ctx.Content.EquipmentSlots.Find(s => s.Id == entry.SlotId);
                if (slot == null)
                {
                    continue;
                }
                if (IsWorn(player, entry.InstanceId))
                {
                    continue;
                }
                if (inCombat && !slot.IsMainHand && !slot.IsOffHand)
                {
                    skippedArmour++;
                    continue;
                }
                int bagIndex = player.Bag.IndexOfInstance(entry.InstanceId);
                if (bagIndex < 0)
                {
                    missing++;
                    continue;
                }
                _inventory.Equip(player, new EquipItemCommand { BagIndex = bagIndex });
            }
            if (missing > 0)
            {
                player.Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.LoadoutPartial, Number = missing });
            }
            if (skippedArmour > 0)
            {
                player.Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.LoadoutArmourInCombat, Number = skippedArmour });
            }
        }

        /// <summary>Queues the panels for the client (once per change).</summary>
        public void Flush(PlayerCharacter player)
        {
            if (!player.Panels.ToSend)
            {
                return;
            }
            player.Panels.ToSend = false;
            player.Outbox.Replies.Add(Snapshot(player));
        }

        public PanelsMessage Snapshot(PlayerCharacter player)
        {
            PanelData panels = player.Panels;
            var loadouts = new LoadoutSnapshot[panels.Loadouts.Length];
            for (int i = 0; i < loadouts.Length; i++)
            {
                var items = new LoadoutItemSnapshot[panels.Loadouts[i].Items.Count];
                for (int k = 0; k < items.Length; k++)
                {
                    LoadoutItemData item = panels.Loadouts[i].Items[k];
                    items[k] = new LoadoutItemSnapshot { SlotId = item.SlotId, InstanceId = item.InstanceId, DefinitionId = item.DefinitionId };
                }
                loadouts[i] = new LoadoutSnapshot { Items = items };
            }
            return new PanelsMessage { SkillBar = (string[])panels.SkillBar.Clone(), ConsumableBar = (string[])panels.ConsumableBar.Clone(), Loadouts = loadouts };
        }

        private LoadoutData[] NewLoadouts()
        {
            var loadouts = new LoadoutData[LoadoutSlots];
            for (int i = 0; i < loadouts.Length; i++)
            {
                loadouts[i] = new LoadoutData();
            }
            return loadouts;
        }

        /// <summary>Main hand first (a two-handed weapon then frees the off hand), the rest in the order of equipment_slots.</summary>
        private int Rank(string slotId)
        {
            int index = _ctx.Content.EquipmentSlots.FindIndex(s => s.Id == slotId);
            bool mainHand = index >= 0 && _ctx.Content.EquipmentSlots[index].IsMainHand;
            return mainHand ? -1 : index;
        }

        private static bool IsClassSkill(PlayerCharacter player, string skillId)
        {
            foreach (ClassSkill skill in player.Class.Skills)
            {
                if (skill.SkillId == skillId)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsConsumable(string itemId) =>
            itemId != null && _ctx.Content.Items.TryGetValue(itemId, out ItemDefinition definition) && definition.Has<ConsumableComponent>();

        private static bool IsWorn(PlayerCharacter player, string instanceId)
        {
            foreach (ItemInstance item in player.Equipment.Values)
            {
                if (item.SourceId == instanceId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
