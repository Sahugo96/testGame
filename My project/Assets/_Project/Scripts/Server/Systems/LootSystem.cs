using System.Collections.Generic;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Who receives a mob's drop (06, 7.8): by the group's loot mode among eligible members — those who took part in the
    /// fight (damage, or healing someone who did) and stand within LootRadius of the death spot. Drops can lie on the
    /// ground as LootEntry entities (Server/World/LootData) and are picked up with PickUpLootCommand; see Give.
    /// </summary>
    public sealed class LootSystem
    {
        private readonly ServerContext _ctx;
        private readonly GroupSystem _groups;
        private readonly InventorySystem _inventory;
        private readonly Dictionary<EntityId, HashSet<EntityId>> _participants = new Dictionary<EntityId, HashSet<EntityId>>();
        private readonly List<EntityId> _expired = new List<EntityId>();

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

        /// <summary>
        /// One drop of a dead mob (06, 7.8). Solo: straight into the killer's bag (question 68), on the ground for them if
        /// the section is full. Group: "PickedUp" — on the ground for the eligible members, the first to pick it up gets it;
        /// "Leader" — into the leader's bag, on the ground for the leader if full (or by "PickedUp" if the leader did not
        /// fight); "RoundRobin" — by the queue, on the ground for everyone eligible if nobody has room.
        /// Returns who got it now, or null (on the ground).
        /// </summary>
        public PlayerCharacter Give(PlayerCharacter killer, EntityId mob, MobDropDto drop)
        {
            if (!_groups.TryGetGroup(killer.EntityId, out GroupData group))
            {
                return GiveOrDrop(killer, mob, drop, 0, LootMode.PickedUp);
            }
            switch (group.LootMode)
            {
                case LootMode.Leader:
                    return GiveToLeader(mob, drop, group);
                case LootMode.RoundRobin:
                    return GiveRoundRobin(mob, drop, group);
                default:
                    Drop(mob, drop, EligibleMembers(group, mob), group.Id, LootMode.PickedUp);
                    return null;
            }
        }

        /// <summary>Picking up a drop from the ground: eligible, close enough, room in the bag; the first one wins (06, 7.8).</summary>
        public void PickUp(PlayerCharacter player, EntityId lootId)
        {
            ServerWorld world = _ctx.World;
            if (!world.Loot.Has(lootId))
            {
                CombatEvents.Reject(player, RejectReasons.LootGone);
                return;
            }
            LootData loot = world.Loot.Get(lootId);
            if (System.Array.IndexOf(loot.Eligible, player.EntityId) < 0)
            {
                CombatEvents.Reject(player, RejectReasons.LootNotYours);
                return;
            }
            if (!world.IsAlive(player.EntityId)
                || Vec3.DistanceXZ(world.Transforms.Get(player.EntityId).Position, world.Transforms.Get(lootId).Position) > _ctx.Content.Config.InteractRange)
            {
                CombatEvents.Reject(player, RejectReasons.LootTooFar);
                return;
            }
            if (!_inventory.CanFit(player, loot.ItemId, loot.Count))
            {
                CombatEvents.Reject(player, RejectReasons.BagFull);
                return;
            }
            world.DestroyEntity(lootId);
            GiveTo(player, loot.ItemId, loot.Count, loot.GroupId, loot.Mode);
        }

        /// <summary>Drops on the ground disappear after LootSeconds (06, 7.8: 5 minutes).</summary>
        public void Update()
        {
            ServerWorld world = _ctx.World;
            _expired.Clear();
            foreach (EntityId id in world.Entities)
            {
                if (world.Loot.Has(id) && _ctx.Time >= world.Loot.Get(id).ExpiresAt)
                {
                    _expired.Add(id);
                }
            }
            foreach (EntityId id in _expired)
            {
                world.DestroyEntity(id);
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

        private PlayerCharacter GiveToLeader(EntityId mob, MobDropDto drop, GroupData group)
        {
            PlayerCharacter leader = _ctx.GetPlayer(group.LeaderId);
            if (leader == null || !IsEligible(leader.EntityId, mob))
            {
                // The leader did not take part: the item goes by the default mode, "PickedUp" (question 71).
                Drop(mob, drop, EligibleMembers(group, mob), group.Id, LootMode.PickedUp);
                return null;
            }
            if (!_inventory.CanFit(leader, drop.ItemId, drop.Count))
            {
                leader.Outbox.Notifications.Add(new NotificationMessage
                {
                    TextKey = NotificationKeys.LootLeaderBagFull,
                    Number = drop.Count,
                    ItemNameKey = _ctx.Content.Items[drop.ItemId].NameKey,
                });
                Drop(mob, drop, new[] { leader.EntityId }, group.Id, LootMode.Leader);
                return null;
            }
            return GiveTo(leader, drop.ItemId, drop.Count, group.Id, LootMode.Leader);
        }

        private PlayerCharacter GiveRoundRobin(EntityId mob, MobDropDto drop, GroupData group)
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
                return GiveTo(candidate, drop.ItemId, drop.Count, group.Id, LootMode.RoundRobin);
            }
            Drop(mob, drop, EligibleMembers(group, mob), group.Id, LootMode.RoundRobin);
            return null;
        }

        /// <summary>Solo: into the bag, or onto the ground for the killer when the section is full.</summary>
        private PlayerCharacter GiveOrDrop(PlayerCharacter killer, EntityId mob, MobDropDto drop, int groupId, LootMode mode)
        {
            if (_inventory.CanFit(killer, drop.ItemId, drop.Count))
            {
                return GiveTo(killer, drop.ItemId, drop.Count, groupId, mode);
            }
            killer.Outbox.Notifications.Add(new NotificationMessage
            {
                TextKey = NotificationKeys.LootOnGround,
                Number = drop.Count,
                ItemNameKey = _ctx.Content.Items[drop.ItemId].NameKey,
            });
            Drop(mob, drop, new[] { killer.EntityId }, groupId, mode);
            return null;
        }

        private EntityId[] EligibleMembers(GroupData group, EntityId mob)
        {
            var eligible = new List<EntityId>();
            foreach (EntityId member in group.Members)
            {
                if (IsEligible(member, mob))
                {
                    eligible.Add(member);
                }
            }
            return eligible.ToArray();
        }

        /// <summary>Puts a LootEntry on the ground near the death spot (scattered a little so drops do not overlap).</summary>
        private void Drop(EntityId mob, MobDropDto drop, EntityId[] eligible, int groupId, LootMode mode)
        {
            if (eligible.Length == 0)
            {
                return;
            }
            ServerWorld world = _ctx.World;
            Vec3 spot = world.Transforms.Get(mob).Position;
            float scatter = _ctx.Content.Config.LootScatterRadius;
            double angle = _ctx.Random.NextFloat() * System.Math.PI * 2d;
            var position = new Vec3(spot.X + (float)System.Math.Cos(angle) * scatter, 0f, spot.Z + (float)System.Math.Sin(angle) * scatter);
            EntityId id = world.CreateEntity();
            world.Identities.Add(id, new IdentityData { Kind = EntityKind.Loot, DefinitionId = drop.ItemId, NameKey = _ctx.Content.Items[drop.ItemId].NameKey });
            world.Transforms.Add(id, new TransformData { Position = position });
            world.Loot.Add(id, new LootData
            {
                ItemId = drop.ItemId,
                Count = drop.Count,
                Mode = mode,
                Eligible = eligible,
                GroupId = groupId,
                ExpiresAt = _ctx.Time + _ctx.Content.Config.LootSeconds,
            });
        }

        /// <summary>The item instance is created now, at delivery (06, 7.8); the group log records it.</summary>
        private PlayerCharacter GiveTo(PlayerCharacter receiver, string itemId, int count, int groupId, LootMode mode)
        {
            int before = _inventory.CountItems(receiver, itemId);
            _inventory.GiveItem(receiver, itemId, count);
            int given = _inventory.CountItems(receiver, itemId) - before;
            if (given <= 0)
            {
                return null;
            }
            receiver.Outbox.Notifications.Add(new NotificationMessage
            {
                TextKey = NotificationKeys.Loot,
                Number = given,
                ItemNameKey = _ctx.Content.Items[itemId].NameKey,
                IsPositive = true,
            });
            if (groupId != 0)
            {
                IdentityData identity = _ctx.World.Identities.Get(receiver.EntityId);
                _groups.AddLog(groupId, new GroupLogEntry
                {
                    RecipientName = identity.DisplayName ?? receiver.CharacterId,
                    ItemDefinitionId = itemId,
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
