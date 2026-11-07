using System.Collections.Generic;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Who receives a mob's drop (06, 7.8). Solo: the killer. In a group, by the group's loot mode among eligible
    /// members — those who took part in the fight with the mob (damage, or healing someone who did) and stand within
    /// LootRadius of the death spot. Until ground loot exists (module 10, LootEntry) items go straight into bags
    /// (question 68), so "PickedUp" means the killer; an item that fits nowhere is lost with a notification (question 71).
    /// </summary>
    public sealed class LootSystem
    {
        private readonly ServerContext _ctx;
        private readonly GroupSystem _groups;
        private readonly InventorySystem _inventory;
        private readonly Dictionary<EntityId, HashSet<EntityId>> _participants = new Dictionary<EntityId, HashSet<EntityId>>();

        public LootSystem(ServerContext ctx, GroupSystem groups, InventorySystem inventory)
        {
            _ctx = ctx;
            _groups = groups;
            _inventory = inventory;
        }

        /// <summary>A player hit a mob: they take part in that fight.</summary>
        public void OnDamaged(EntityId source, EntityId target, int amount)
        {
            if (_ctx.World.Mobs.Has(target) && _ctx.Players.ContainsKey(source))
            {
                Participants(target).Add(source);
            }
        }

        /// <summary>Healing a fighter is taking part in the fight (06, 7.8: damage, healing, buffs, control).</summary>
        public void OnHealed(EntityId source, EntityId target, int amount)
        {
            if (source == target || !_ctx.Players.ContainsKey(source))
            {
                return;
            }
            foreach (HashSet<EntityId> fighters in _participants.Values)
            {
                if (fighters.Contains(target))
                {
                    fighters.Add(source);
                }
            }
        }

        public bool IsEligible(EntityId player, EntityId mob)
        {
            if (!_ctx.Players.ContainsKey(player) || !_participants.TryGetValue(mob, out HashSet<EntityId> fighters) || !fighters.Contains(player))
            {
                return false;
            }
            Vec3 deathSpot = _ctx.World.Transforms.Get(mob).Position;
            return Vec3.DistanceXZ(_ctx.World.Transforms.Get(player).Position, deathSpot) <= _ctx.Content.Config.LootRadius;
        }

        /// <summary>Gives one drop of a dead mob to the right player. Returns the receiver or null (lost).</summary>
        public PlayerCharacter Give(PlayerCharacter killer, EntityId mob, MobDropDto drop)
        {
            if (!_groups.TryGetGroup(killer.EntityId, out GroupData group))
            {
                return GiveTo(killer, drop, null, LootMode.PickedUp);
            }
            switch (group.LootMode)
            {
                case LootMode.Leader:
                    return GiveToLeader(killer, mob, drop, group);
                case LootMode.RoundRobin:
                    return GiveRoundRobin(killer, mob, drop, group);
                default:
                    return GiveTo(killer, drop, group, LootMode.PickedUp);
            }
        }

        /// <summary>
        /// Who shares the kill's experience (question 72): solo — the killer; in a group — the killer and living members
        /// within LootRadius of the death spot (temporarily, DEFAULT: being near is enough, taking part is not required).
        /// </summary>
        public List<PlayerCharacter> ExperienceRecipients(PlayerCharacter killer, EntityId mob)
        {
            var recipients = new List<PlayerCharacter> { killer };
            if (!_groups.TryGetGroup(killer.EntityId, out GroupData group))
            {
                return recipients;
            }
            Vec3 deathSpot = _ctx.World.Transforms.Get(mob).Position;
            foreach (EntityId member in group.Members)
            {
                PlayerCharacter player = _ctx.GetPlayer(member);
                if (player == null || player == killer || !_ctx.World.IsAlive(member)
                    || Vec3.DistanceXZ(_ctx.World.Transforms.Get(member).Position, deathSpot) > _ctx.Content.Config.LootRadius)
                {
                    continue;
                }
                recipients.Add(player);
            }
            return recipients;
        }

        /// <summary>The fight is over: forget who took part (after the drops were handed out).</summary>
        public void OnMobLooted(EntityId mob) => _participants.Remove(mob);

        private PlayerCharacter GiveToLeader(PlayerCharacter killer, EntityId mob, MobDropDto drop, GroupData group)
        {
            PlayerCharacter leader = _ctx.GetPlayer(group.LeaderId);
            if (leader == null || !IsEligible(leader.EntityId, mob))
            {
                // The leader did not take part: the item goes by the default rule (question 71).
                return GiveTo(killer, drop, group, LootMode.PickedUp);
            }
            if (!_inventory.CanFit(leader, drop.ItemId, drop.Count))
            {
                leader.Outbox.Notifications.Add(new NotificationMessage
                {
                    TextKey = NotificationKeys.LootLeaderBagFull,
                    Number = drop.Count,
                    ItemNameKey = _ctx.Content.Items[drop.ItemId].NameKey,
                });
                return null;
            }
            return GiveTo(leader, drop, group, LootMode.Leader);
        }

        private PlayerCharacter GiveRoundRobin(PlayerCharacter killer, EntityId mob, MobDropDto drop, GroupData group)
        {
            foreach (EntityId member in group.LootQueue)
            {
                PlayerCharacter candidate = _ctx.GetPlayer(member);
                // Ineligible players are skipped and keep their place; a full bag passes the item to the next one.
                if (candidate == null || !IsEligible(member, mob) || !_inventory.CanFit(candidate, drop.ItemId, drop.Count))
                {
                    continue;
                }
                _groups.MoveToQueueEnd(group.Id, member);
                return GiveTo(candidate, drop, group, LootMode.RoundRobin);
            }
            // Nobody had room: the killer's normal pickup reports the full bag.
            return GiveTo(killer, drop, group, LootMode.RoundRobin);
        }

        private PlayerCharacter GiveTo(PlayerCharacter receiver, MobDropDto drop, GroupData? group, LootMode mode)
        {
            int before = _inventory.CountItems(receiver, drop.ItemId);
            _inventory.GiveItem(receiver, drop.ItemId, drop.Count);
            int given = _inventory.CountItems(receiver, drop.ItemId) - before;
            if (given <= 0)
            {
                return null;
            }
            receiver.Outbox.Notifications.Add(new NotificationMessage
            {
                TextKey = NotificationKeys.Loot,
                Number = given,
                ItemNameKey = _ctx.Content.Items[drop.ItemId].NameKey,
                IsPositive = true,
            });
            if (group.HasValue)
            {
                IdentityData identity = _ctx.World.Identities.Get(receiver.EntityId);
                _groups.AddLog(group.Value.Id, new GroupLogEntry
                {
                    RecipientName = identity.DisplayName ?? receiver.CharacterId,
                    ItemDefinitionId = drop.ItemId,
                    Count = given,
                    Mode = mode,
                });
            }
            return receiver;
        }

        private HashSet<EntityId> Participants(EntityId mob)
        {
            if (!_participants.TryGetValue(mob, out HashSet<EntityId> fighters))
            {
                fighters = new HashSet<EntityId>();
                _participants.Add(mob, fighters);
            }
            return fighters;
        }
    }
}
