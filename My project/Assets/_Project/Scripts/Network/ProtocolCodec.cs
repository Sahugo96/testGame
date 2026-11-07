using System;
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
                    break;
                case WelcomeMessage m:
                    w.Write((byte)MessageType.Welcome);
                    Id(w, m.PlayerId);
                    w.Write(m.TickRate);
                    w.Write(m.WorldEveryTicks);
                    Str(w, m.Error);
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
                case WorldSnapshot m:
                    w.Write((byte)MessageType.World);
                    WriteWorld(w, m);
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
                    return new HelloMessage { ProtocolVersion = r.ReadInt32(), PlayerName = Str(r), ClassId = Str(r) };
                case MessageType.Welcome:
                    return new WelcomeMessage { PlayerId = Id(r), TickRate = r.ReadInt32(), WorldEveryTicks = r.ReadInt32(), Error = Str(r) };
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
                case MessageType.World:
                    return ReadWorld(r);
                case MessageType.PlayerState:
                    return ReadState(r);
                case MessageType.CombatEvent:
                    return new CombatEventMessage
                    {
                        SourceId = Id(r), TargetId = Id(r), SkillId = Str(r), Kind = (CombatEventKind)r.ReadByte(), Amount = r.ReadInt32(), CritTier = r.ReadInt32(),
                    };
                case MessageType.Rejected:
                    return new CommandRejectedMessage { ReasonKey = Str(r) };
                case MessageType.Notification:
                    return new NotificationMessage { TextKey = Str(r), Number = r.ReadInt32(), ItemNameKey = Str(r), IsPositive = r.ReadBoolean() };
                default:
                    throw new InvalidDataException($"Unknown message type {type}");
            }
        }

        private static void WriteWorld(BinaryWriter w, WorldSnapshot m)
        {
            w.Write(m.Tick);
            w.Write(m.ServerTime);
            w.Write(m.Entities.Length);
            foreach (EntitySnapshot e in m.Entities)
            {
                Id(w, e.Id);
                w.Write((byte)e.Kind);
                Str(w, e.DefinitionId);
                Str(w, e.NameKey);
                Str(w, e.DisplayName);
                Str(w, e.Faction);
                w.Write(e.Position.X);
                w.Write(e.Position.Y);
                w.Write(e.Position.Z);
                w.Write(e.Yaw);
                w.Write(e.Health);
                w.Write(e.MaxHealth);
                w.Write(e.IsDead);
                w.Write(e.IsCasting);
            }
        }

        private static WorldSnapshot ReadWorld(BinaryReader r)
        {
            var m = new WorldSnapshot { Tick = r.ReadInt32(), ServerTime = r.ReadSingle() };
            m.Entities = new EntitySnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Entities.Length; i++)
            {
                m.Entities[i] = new EntitySnapshot
                {
                    Id = Id(r),
                    Kind = (EntityKind)r.ReadByte(),
                    DefinitionId = Str(r),
                    NameKey = Str(r),
                    DisplayName = Str(r),
                    Faction = Str(r),
                    Position = new Vec3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                    Yaw = r.ReadSingle(),
                    Health = r.ReadSingle(),
                    MaxHealth = r.ReadSingle(),
                    IsDead = r.ReadBoolean(),
                    IsCasting = r.ReadBoolean(),
                };
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
            w.Write(m.Bag.Length);
            foreach (ItemSlotSnapshot b in m.Bag)
            {
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
                m.Bag[i] = new ItemSlotSnapshot { Index = r.ReadInt32(), InstanceId = Str(r), DefinitionId = Str(r), Count = r.ReadInt32(), UpgradeLevel = r.ReadInt32() };
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
