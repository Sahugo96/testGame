using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Network
{
    /// <summary>
    /// Binary wire format of all messages (temporary own transport, question 69). One message per frame:
    /// a <see cref="MessageType"/> byte followed by the fields in a fixed order. Strings are nullable.
    /// </summary>
    public static class ProtocolCodec
    {
        public static byte[] Encode(object message)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            {
                Write(w, message);
                w.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// World snapshot for one peer (module 3 traffic): static fields (definition, name key, display name, faction) only
        /// for entities the peer does not know yet; `known` is updated (entities that left the view are forgotten, so they
        /// come back in full). The client side fills the missing fields from its cache (RemoteServerConnection).
        /// </summary>
        public static byte[] EncodeWorld(WorldSnapshot world, HashSet<EntityId> known)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            {
                w.Write((byte)MessageType.World);
                WriteWorld(w, world, known);
                w.Flush();
                return stream.ToArray();
            }
        }

        public static object Decode(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var r = new BinaryReader(stream, Encoding.UTF8))
            {
                return Read(r);
            }
        }

        private static void Write(BinaryWriter w, object message)
        {
            switch (message)
            {
                case HelloMessage m:
                    w.Write((byte)MessageType.Hello);
                    w.Write(m.ProtocolVersion);
                    Str(w, m.PlayerName);
                    Str(w, m.ClassId);
                    Str(w, m.Login);
                    Str(w, m.Password);
                    Str(w, m.Token);
                    break;
                case WelcomeMessage m:
                    w.Write((byte)MessageType.Welcome);
                    Id(w, m.PlayerId);
                    w.Write(m.TickRate);
                    w.Write(m.WorldEveryTicks);
                    Str(w, m.Error);
                    Str(w, m.Token);
                    break;
                case MoveCommand m:
                    w.Write((byte)MessageType.Move);
                    w.Write(m.DirX);
                    w.Write(m.DirZ);
                    break;
                case UseSkillCommand m:
                    w.Write((byte)MessageType.UseSkill);
                    Str(w, m.SkillId);
                    Id(w, m.TargetId);
                    w.Write((byte)m.Modifier);
                    w.Write(m.ClientSeq);
                    break;
                case SelectTargetCommand m:
                    w.Write((byte)MessageType.SelectTarget);
                    Id(w, m.TargetId);
                    break;
                case CycleTargetCommand _:
                    w.Write((byte)MessageType.CycleTarget);
                    break;
                case EquipItemCommand m:
                    w.Write((byte)MessageType.Equip);
                    w.Write(m.BagIndex);
                    break;
                case UnequipItemCommand m:
                    w.Write((byte)MessageType.Unequip);
                    Str(w, m.SlotId);
                    break;
                case UseConsumableCommand m:
                    w.Write((byte)MessageType.UseConsumable);
                    w.Write(m.BagIndex);
                    break;
                case UpgradeItemCommand m:
                    w.Write((byte)MessageType.UpgradeItem);
                    Str(w, m.ItemInstanceId);
                    Id(w, m.StationId);
                    break;
                case SpendAttributePointCommand m:
                    w.Write((byte)MessageType.SpendAttributePoint);
                    w.Write((int)m.Attribute);
                    break;
                case DuelRequestCommand m:
                    w.Write((byte)MessageType.DuelRequest);
                    Id(w, m.TargetId);
                    break;
                case DuelResponseCommand m:
                    w.Write((byte)MessageType.DuelResponse);
                    w.Write(m.Accept);
                    break;
                case DuelSurrenderCommand _:
                    w.Write((byte)MessageType.DuelSurrender);
                    break;
                case GroupInviteCommand m:
                    w.Write((byte)MessageType.GroupInvite);
                    Id(w, m.TargetId);
                    break;
                case GroupInviteResponseCommand m:
                    w.Write((byte)MessageType.GroupInviteResponse);
                    w.Write(m.Accept);
                    break;
                case GroupLeaveCommand _:
                    w.Write((byte)MessageType.GroupLeave);
                    break;
                case GroupKickCommand m:
                    w.Write((byte)MessageType.GroupKick);
                    Id(w, m.MemberId);
                    break;
                case GroupPromoteCommand m:
                    w.Write((byte)MessageType.GroupPromote);
                    Id(w, m.MemberId);
                    break;
                case LootModeProposeCommand m:
                    w.Write((byte)MessageType.LootModePropose);
                    w.Write((byte)m.Mode);
                    break;
                case LootModeVoteCommand m:
                    w.Write((byte)MessageType.LootModeVote);
                    w.Write(m.ProposalId);
                    w.Write(m.Accept);
                    break;
                case CraftItemCommand m:
                    w.Write((byte)MessageType.CraftItem);
                    Str(w, m.RecipeId);
                    Id(w, m.StationId);
                    break;
                case CancelCraftCommand _:
                    w.Write((byte)MessageType.CancelCraft);
                    break;
                case PickUpLootCommand m:
                    w.Write((byte)MessageType.PickUpLoot);
                    Id(w, m.LootId);
                    break;
                case WorldSnapshot m:
                    w.Write((byte)MessageType.World);
                    WriteWorld(w, m, null);
                    break;
                case PlayerStateSnapshot m:
                    w.Write((byte)MessageType.PlayerState);
                    WriteState(w, m);
                    break;
                case CombatEventMessage m:
                    w.Write((byte)MessageType.CombatEvent);
                    Id(w, m.SourceId);
                    Id(w, m.TargetId);
                    Str(w, m.SkillId);
                    w.Write((byte)m.Kind);
                    w.Write(m.Amount);
                    w.Write(m.CritTier);
                    w.Write(m.Absorbed);
                    break;
                case CommandRejectedMessage m:
                    w.Write((byte)MessageType.Rejected);
                    Str(w, m.ReasonKey);
                    break;
                case NotificationMessage m:
                    w.Write((byte)MessageType.Notification);
                    Str(w, m.TextKey);
                    w.Write(m.Number);
                    Str(w, m.ItemNameKey);
                    w.Write(m.IsPositive);
                    break;
                default:
                    throw new ArgumentException($"Cannot encode {message?.GetType().Name ?? "null"}");
            }
        }

        private static object Read(BinaryReader r)
        {
            var type = (MessageType)r.ReadByte();
            switch (type)
            {
                case MessageType.Hello:
                    return new HelloMessage { ProtocolVersion = r.ReadInt32(), PlayerName = Str(r), ClassId = Str(r), Login = Str(r), Password = Str(r), Token = Str(r) };
                case MessageType.Welcome:
                    return new WelcomeMessage { PlayerId = Id(r), TickRate = r.ReadInt32(), WorldEveryTicks = r.ReadInt32(), Error = Str(r), Token = Str(r) };
                case MessageType.Move:
                    return new MoveCommand { DirX = r.ReadSingle(), DirZ = r.ReadSingle() };
                case MessageType.UseSkill:
                    return new UseSkillCommand { SkillId = Str(r), TargetId = Id(r), Modifier = (SkillModifier)r.ReadByte(), ClientSeq = r.ReadInt32() };
                case MessageType.SelectTarget:
                    return new SelectTargetCommand { TargetId = Id(r) };
                case MessageType.CycleTarget:
                    return new CycleTargetCommand();
                case MessageType.Equip:
                    return new EquipItemCommand { BagIndex = r.ReadInt32() };
                case MessageType.Unequip:
                    return new UnequipItemCommand { SlotId = Str(r) };
                case MessageType.UseConsumable:
                    return new UseConsumableCommand { BagIndex = r.ReadInt32() };
                case MessageType.UpgradeItem:
                    return new UpgradeItemCommand { ItemInstanceId = Str(r), StationId = Id(r) };
                case MessageType.SpendAttributePoint:
                    return new SpendAttributePointCommand { Attribute = (StatId)r.ReadInt32() };
                case MessageType.DuelRequest:
                    return new DuelRequestCommand { TargetId = Id(r) };
                case MessageType.DuelResponse:
                    return new DuelResponseCommand { Accept = r.ReadBoolean() };
                case MessageType.DuelSurrender:
                    return new DuelSurrenderCommand();
                case MessageType.GroupInvite:
                    return new GroupInviteCommand { TargetId = Id(r) };
                case MessageType.GroupInviteResponse:
                    return new GroupInviteResponseCommand { Accept = r.ReadBoolean() };
                case MessageType.GroupLeave:
                    return new GroupLeaveCommand();
                case MessageType.GroupKick:
                    return new GroupKickCommand { MemberId = Id(r) };
                case MessageType.GroupPromote:
                    return new GroupPromoteCommand { MemberId = Id(r) };
                case MessageType.LootModePropose:
                    return new LootModeProposeCommand { Mode = (LootMode)r.ReadByte() };
                case MessageType.LootModeVote:
                    return new LootModeVoteCommand { ProposalId = r.ReadInt32(), Accept = r.ReadBoolean() };
                case MessageType.CraftItem:
                    return new CraftItemCommand { RecipeId = Str(r), StationId = Id(r) };
                case MessageType.CancelCraft:
                    return new CancelCraftCommand();
                case MessageType.PickUpLoot:
                    return new PickUpLootCommand { LootId = Id(r) };
                case MessageType.World:
                    return ReadWorld(r);
                case MessageType.PlayerState:
                    return ReadState(r);
                case MessageType.CombatEvent:
                    return new CombatEventMessage
                    {
                        SourceId = Id(r), TargetId = Id(r), SkillId = Str(r), Kind = (CombatEventKind)r.ReadByte(), Amount = r.ReadInt32(), CritTier = r.ReadInt32(),
                        Absorbed = r.ReadInt32(),
                    };
                case MessageType.Rejected:
                    return new CommandRejectedMessage { ReasonKey = Str(r) };
                case MessageType.Notification:
                    return new NotificationMessage { TextKey = Str(r), Number = r.ReadInt32(), ItemNameKey = Str(r), IsPositive = r.ReadBoolean() };
                default:
                    throw new InvalidDataException($"Unknown message type {type}");
            }
        }

        private static void WriteWorld(BinaryWriter w, WorldSnapshot m, HashSet<EntityId> known)
        {
            w.Write(m.Tick);
            w.Write(m.ServerTime);
            w.Write(m.Entities.Length);
            HashSet<EntityId> seen = known == null ? null : new HashSet<EntityId>();
            foreach (EntitySnapshot e in m.Entities)
            {
                Id(w, e.Id);
                w.Write((byte)e.Kind);
                bool full = known == null || !known.Contains(e.Id);
                w.Write(full);
                if (full)
                {
                    Str(w, e.DefinitionId);
                    Str(w, e.NameKey);
                    Str(w, e.DisplayName);
                    Str(w, e.Faction);
                }
                seen?.Add(e.Id);
                w.Write(e.Position.X);
                w.Write(e.Position.Y);
                w.Write(e.Position.Z);
                w.Write(e.Yaw);
                w.Write(e.Health);
                w.Write(e.MaxHealth);
                w.Write(e.IsDead);
                w.Write(e.IsCasting);
                StatusSnapshot[] statuses = e.Statuses ?? new StatusSnapshot[0];
                w.Write(statuses.Length);
                foreach (StatusSnapshot s in statuses)
                {
                    Str(w, s.StatusId);
                    w.Write(s.Stacks);
                    w.Write(s.Remaining);
                }
                Id(w, e.TargetOfId);
            }
            if (known != null)
            {
                known.IntersectWith(seen);
                known.UnionWith(seen);
            }
        }

        private static WorldSnapshot ReadWorld(BinaryReader r)
        {
            var m = new WorldSnapshot { Tick = r.ReadInt32(), ServerTime = r.ReadSingle() };
            m.Entities = new EntitySnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Entities.Length; i++)
            {
                var e = new EntitySnapshot { Id = Id(r), Kind = (EntityKind)r.ReadByte() };
                if (r.ReadBoolean())
                {
                    e.DefinitionId = Str(r);
                    e.NameKey = Str(r);
                    e.DisplayName = Str(r);
                    e.Faction = Str(r);
                }
                m.Entities[i] = new EntitySnapshot
                {
                    Id = e.Id,
                    Kind = e.Kind,
                    DefinitionId = e.DefinitionId,
                    NameKey = e.NameKey,
                    DisplayName = e.DisplayName,
                    Faction = e.Faction,
                    Position = new Vec3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                    Yaw = r.ReadSingle(),
                    Health = r.ReadSingle(),
                    MaxHealth = r.ReadSingle(),
                    IsDead = r.ReadBoolean(),
                    IsCasting = r.ReadBoolean(),
                };
                var statuses = new StatusSnapshot[r.ReadInt32()];
                for (int k = 0; k < statuses.Length; k++)
                {
                    statuses[k] = new StatusSnapshot { StatusId = Str(r), Stacks = r.ReadInt32(), Remaining = r.ReadSingle() };
                }
                m.Entities[i].Statuses = statuses;
                m.Entities[i].TargetOfId = Id(r);
            }
            return m;
        }

        private static void WriteState(BinaryWriter w, PlayerStateSnapshot m)
        {
            Id(w, m.EntityId);
            Str(w, m.ClassId);
            w.Write(m.Level);
            w.Write(m.Experience);
            w.Write(m.ExperienceToNext);
            w.Write(m.AttributePoints);
            w.Write(m.ClassPoints);
            w.Write(m.Health);
            w.Write(m.MaxHealth);
            w.Write(m.MainMana);
            w.Write(m.MaxMainMana);
            w.Write(m.ReserveMana);
            w.Write(m.MaxReserveMana);
            w.Write(m.MainManaLocked);
            w.Write(m.InCombat);
            w.Write(m.IsDead);
            Id(w, m.TargetId);
            Str(w, m.CastSkillId);
            w.Write(m.CastProgress);
            w.Write(m.GlobalCooldownRemaining);
            w.Write(m.GlobalCooldownDuration);
            w.Write(m.MainHandSwapLockRemaining);
            w.Write(m.MainHandSwapRiskRemaining);
            w.Write(m.OffHandSwapLockRemaining);

            w.Write(m.Cooldowns.Length);
            foreach (SkillCooldownSnapshot c in m.Cooldowns)
            {
                Str(w, c.SkillId);
                w.Write(c.Remaining);
                w.Write(c.Duration);
            }
            w.Write(m.Skills.Length);
            foreach (SkillStateSnapshot s in m.Skills)
            {
                Str(w, s.SkillId);
                w.Write(s.Unlocked);
                w.Write(s.Mastery);
                w.Write(s.Efficiency);
                w.Write(s.EmpoweredAvailable);
                w.Write(s.WeakenedAvailable);
            }
            // Sparse bag (module 3 traffic): the size, then only the occupied slots.
            w.Write(m.Bag.Length);
            int occupied = 0;
            foreach (ItemSlotSnapshot b in m.Bag)
            {
                occupied += b.DefinitionId != null ? 1 : 0;
            }
            w.Write(occupied);
            foreach (ItemSlotSnapshot b in m.Bag)
            {
                if (b.DefinitionId == null)
                {
                    continue;
                }
                w.Write(b.Index);
                Str(w, b.InstanceId);
                Str(w, b.DefinitionId);
                w.Write(b.Count);
                w.Write(b.UpgradeLevel);
            }
            w.Write(m.Equipment.Length);
            foreach (EquipmentSlotSnapshot e in m.Equipment)
            {
                Str(w, e.SlotId);
                Str(w, e.InstanceId);
                Str(w, e.DefinitionId);
                w.Write(e.UpgradeLevel);
            }
            w.Write(m.Stats.Length);
            foreach (StatSnapshot s in m.Stats)
            {
                w.Write((int)s.Stat);
                w.Write(s.Value);
            }
            w.Write(m.ConsumableCooldowns.Length);
            foreach (CooldownGroupSnapshot g in m.ConsumableCooldowns)
            {
                Str(w, g.Group);
                w.Write(g.Remaining);
            }
            Str(w, m.CraftRecipeId);
            w.Write(m.CraftProgress);
            w.Write((byte)m.Duel.Phase);
            Id(w, m.Duel.OpponentId);
            w.Write(m.Duel.Remaining);
            w.Write(m.Duel.CenterX);
            w.Write(m.Duel.CenterZ);
            w.Write(m.Duel.Radius);
            WriteGroup(w, m.Group ?? new GroupSnapshot());
        }

        private static void WriteGroup(BinaryWriter w, GroupSnapshot g)
        {
            w.Write(g.InGroup);
            Id(w, g.LeaderId);
            w.Write((byte)g.LootMode);
            Ids(w, g.Members);
            Id(w, g.InviteFromId);
            w.Write(g.InviteRemaining);
            w.Write(g.VoteActive);
            w.Write(g.VoteProposalId);
            Id(w, g.VoteProposerId);
            w.Write((byte)g.VoteMode);
            w.Write(g.VoteRemaining);
            Ids(w, g.VotesFor);
            w.Write(g.ProposeCooldownRemaining);
            w.Write(g.Log.Length);
            foreach (GroupLogEntry e in g.Log)
            {
                Str(w, e.RecipientName);
                Str(w, e.ItemDefinitionId);
                w.Write(e.Count);
                w.Write((byte)e.Mode);
            }
        }

        private static GroupSnapshot ReadGroup(BinaryReader r)
        {
            var g = new GroupSnapshot
            {
                InGroup = r.ReadBoolean(),
                LeaderId = Id(r),
                LootMode = (LootMode)r.ReadByte(),
                Members = Ids(r),
                InviteFromId = Id(r),
                InviteRemaining = r.ReadSingle(),
                VoteActive = r.ReadBoolean(),
                VoteProposalId = r.ReadInt32(),
                VoteProposerId = Id(r),
                VoteMode = (LootMode)r.ReadByte(),
                VoteRemaining = r.ReadSingle(),
                VotesFor = Ids(r),
                ProposeCooldownRemaining = r.ReadSingle(),
            };
            g.Log = new GroupLogEntry[r.ReadInt32()];
            for (int i = 0; i < g.Log.Length; i++)
            {
                g.Log[i] = new GroupLogEntry { RecipientName = Str(r), ItemDefinitionId = Str(r), Count = r.ReadInt32(), Mode = (LootMode)r.ReadByte() };
            }
            return g;
        }

        private static void Ids(BinaryWriter w, EntityId[] ids)
        {
            ids = ids ?? new EntityId[0];
            w.Write(ids.Length);
            foreach (EntityId id in ids)
            {
                Id(w, id);
            }
        }

        private static EntityId[] Ids(BinaryReader r)
        {
            var ids = new EntityId[r.ReadInt32()];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Id(r);
            }
            return ids;
        }

        private static PlayerStateSnapshot ReadState(BinaryReader r)
        {
            var m = new PlayerStateSnapshot
            {
                EntityId = Id(r),
                ClassId = Str(r),
                Level = r.ReadInt32(),
                Experience = r.ReadInt32(),
                ExperienceToNext = r.ReadInt32(),
                AttributePoints = r.ReadInt32(),
                ClassPoints = r.ReadInt32(),
                Health = r.ReadSingle(),
                MaxHealth = r.ReadSingle(),
                MainMana = r.ReadSingle(),
                MaxMainMana = r.ReadSingle(),
                ReserveMana = r.ReadSingle(),
                MaxReserveMana = r.ReadSingle(),
                MainManaLocked = r.ReadBoolean(),
                InCombat = r.ReadBoolean(),
                IsDead = r.ReadBoolean(),
                TargetId = Id(r),
                CastSkillId = Str(r),
                CastProgress = r.ReadSingle(),
                GlobalCooldownRemaining = r.ReadSingle(),
                GlobalCooldownDuration = r.ReadSingle(),
                MainHandSwapLockRemaining = r.ReadSingle(),
                MainHandSwapRiskRemaining = r.ReadSingle(),
                OffHandSwapLockRemaining = r.ReadSingle(),
            };
            m.Cooldowns = new SkillCooldownSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Cooldowns.Length; i++)
            {
                m.Cooldowns[i] = new SkillCooldownSnapshot { SkillId = Str(r), Remaining = r.ReadSingle(), Duration = r.ReadSingle() };
            }
            m.Skills = new SkillStateSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Skills.Length; i++)
            {
                m.Skills[i] = new SkillStateSnapshot
                {
                    SkillId = Str(r), Unlocked = r.ReadBoolean(), Mastery = r.ReadSingle(), Efficiency = r.ReadSingle(),
                    EmpoweredAvailable = r.ReadBoolean(), WeakenedAvailable = r.ReadBoolean(),
                };
            }
            m.Bag = new ItemSlotSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Bag.Length; i++)
            {
                m.Bag[i] = new ItemSlotSnapshot { Index = i };
            }
            int occupiedSlots = r.ReadInt32();
            for (int i = 0; i < occupiedSlots; i++)
            {
                var slot = new ItemSlotSnapshot { Index = r.ReadInt32(), InstanceId = Str(r), DefinitionId = Str(r), Count = r.ReadInt32(), UpgradeLevel = r.ReadInt32() };
                if (slot.Index >= 0 && slot.Index < m.Bag.Length)
                {
                    m.Bag[slot.Index] = slot;
                }
            }
            m.Equipment = new EquipmentSlotSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Equipment.Length; i++)
            {
                m.Equipment[i] = new EquipmentSlotSnapshot { SlotId = Str(r), InstanceId = Str(r), DefinitionId = Str(r), UpgradeLevel = r.ReadInt32() };
            }
            m.Stats = new StatSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Stats.Length; i++)
            {
                m.Stats[i] = new StatSnapshot { Stat = (StatId)r.ReadInt32(), Value = r.ReadSingle() };
            }
            m.ConsumableCooldowns = new CooldownGroupSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.ConsumableCooldowns.Length; i++)
            {
                m.ConsumableCooldowns[i] = new CooldownGroupSnapshot { Group = Str(r), Remaining = r.ReadSingle() };
            }
            m.CraftRecipeId = Str(r);
            m.CraftProgress = r.ReadSingle();
            m.Duel = new DuelSnapshot
            {
                Phase = (DuelPhase)r.ReadByte(), OpponentId = Id(r), Remaining = r.ReadSingle(), CenterX = r.ReadSingle(), CenterZ = r.ReadSingle(), Radius = r.ReadSingle(),
            };
            m.Group = ReadGroup(r);
            return m;
        }

        private static void Id(BinaryWriter w, EntityId id) => w.Write(id.Value);

        private static EntityId Id(BinaryReader r) => new EntityId(r.ReadInt32());

        private static void Str(BinaryWriter w, string value)
        {
            w.Write(value != null);
            if (value != null)
            {
                w.Write(value);
            }
        }

        private static string Str(BinaryReader r) => r.ReadBoolean() ? r.ReadString() : null;
    }
}
