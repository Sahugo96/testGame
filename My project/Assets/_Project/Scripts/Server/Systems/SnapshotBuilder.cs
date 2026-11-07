using System.Collections.Generic;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>Builds snapshots for players. No interest management yet (module 3): every player sees the whole test zone.</summary>
    public sealed class SnapshotBuilder
    {
        private static readonly StatId[] ShownStats =
        {
            StatId.MaxHealth, StatId.MaxMainMana, StatId.MaxReserveMana, StatId.HealthRegen, StatId.MainManaRegen,
            StatId.Defense, StatId.CritChance, StatId.CritSize, StatId.DamageBonus, StatId.MoveSpeed,
            StatId.Strength, StatId.Intelligence, StatId.Stamina,
        };

        private readonly ServerContext _ctx;
        private readonly SkillSystem _skills;

        public SnapshotBuilder(ServerContext ctx, SkillSystem skills)
        {
            _ctx = ctx;
            _skills = skills;
        }

        public WorldSnapshot BuildWorld(int tick)
        {
            ServerWorld world = _ctx.World;
            var entities = new List<EntitySnapshot>(world.Entities.Count);
            foreach (EntityId id in world.Entities)
            {
                IdentityData identity = world.Identities.Get(id);
                TransformData transform = world.Transforms.Get(id);
                VitalsData vitals = world.Vitals.Has(id) ? world.Vitals.Get(id) : default;
                entities.Add(new EntitySnapshot
                {
                    Id = id,
                    Kind = identity.Kind,
                    DefinitionId = identity.DefinitionId,
                    NameKey = identity.NameKey,
                    DisplayName = identity.DisplayName,
                    Faction = identity.Faction,
                    Position = transform.Position,
                    Yaw = transform.Yaw,
                    Health = vitals.Health,
                    MaxHealth = vitals.MaxHealth,
                    IsDead = vitals.IsDead,
                    IsCasting = world.Casts.Has(id) && world.Casts.Get(id).IsCasting,
                });
            }
            return new WorldSnapshot { Tick = tick, ServerTime = _ctx.Time, Entities = entities.ToArray() };
        }

        public PlayerStateSnapshot BuildPlayer(PlayerCharacter player)
        {
            ServerWorld world = _ctx.World;
            EntityId id = player.EntityId;
            VitalsData vitals = world.Vitals.Get(id);
            ManaData mana = world.Mana.Get(id);
            CastData cast = world.Casts.Get(id);
            var state = new PlayerStateSnapshot
            {
                EntityId = id,
                ClassId = player.Class.Id,
                Level = player.Level,
                Experience = player.Experience,
                ExperienceToNext = _ctx.Content.ExperienceFor(player.Level),
                AttributePoints = player.AttributePoints,
                ClassPoints = player.ClassPoints,
                Health = vitals.Health,
                MaxHealth = vitals.MaxHealth,
                MainMana = mana.Main,
                MaxMainMana = mana.MaxMain,
                ReserveMana = mana.Reserve,
                MaxReserveMana = mana.MaxReserve,
                MainManaLocked = mana.MainLocked,
                InCombat = world.CombatStates.Get(id).InCombat,
                IsDead = vitals.IsDead,
                TargetId = world.IsAlive(player.TargetId) ? player.TargetId : EntityId.None,
                CastSkillId = cast.IsCasting ? cast.SkillId : null,
                CastProgress = cast.IsCasting && cast.Duration > 0f ? cast.Elapsed / cast.Duration : 0f,
                GlobalCooldownRemaining = cast.GlobalCooldownRemaining,
                GlobalCooldownDuration = cast.GlobalCooldownDuration,
                MainHandSwapLockRemaining = player.WeaponSwap.MainHandLockRemaining,
                MainHandSwapRiskRemaining = player.WeaponSwap.MainHandRiskRemaining,
                OffHandSwapLockRemaining = player.WeaponSwap.OffHandLockRemaining,
            };

            var cooldowns = new List<SkillCooldownSnapshot>();
            foreach (var pair in player.SkillCooldowns)
            {
                cooldowns.Add(new SkillCooldownSnapshot { SkillId = pair.Key, Remaining = pair.Value.Remaining, Duration = pair.Value.Duration });
            }
            state.Cooldowns = cooldowns.ToArray();

            var skills = new List<SkillStateSnapshot>();
            foreach (ClassSkill classSkill in player.Class.Skills)
            {
                SkillDefinition skill = _ctx.Content.Skills[classSkill.SkillId];
                skills.Add(new SkillStateSnapshot
                {
                    SkillId = skill.Id,
                    Unlocked = classSkill.LevelUnlock <= player.Level,
                    Mastery = player.GetMastery(skill.Id),
                    Efficiency = _skills.Efficiency(player, skill.Id),
                    EmpoweredAvailable = _skills.ResolveVariant(player, skill, SkillModifier.Shift) == SkillVariantKind.Empowered,
                    WeakenedAvailable = _skills.ResolveVariant(player, skill, SkillModifier.Ctrl) == SkillVariantKind.Weakened,
                });
            }
            state.Skills = skills.ToArray();

            var bag = new ItemSlotSnapshot[player.Bag.Size];
            for (int i = 0; i < bag.Length; i++)
            {
                ItemInstance item = player.Bag.Get(i);
                bag[i] = new ItemSlotSnapshot
                {
                    Index = i,
                    InstanceId = item?.SourceId,
                    DefinitionId = item?.DefinitionId,
                    Count = item?.Count ?? 0,
                    UpgradeLevel = item?.UpgradeLevel ?? 0,
                };
            }
            state.Bag = bag;

            var equipment = new List<EquipmentSlotSnapshot>();
            foreach (EquipmentSlotDto slot in _ctx.Content.SlotsFor(player.Class))
            {
                player.Equipment.TryGetValue(slot.Id, out ItemInstance item);
                equipment.Add(new EquipmentSlotSnapshot { SlotId = slot.Id, InstanceId = item?.SourceId, DefinitionId = item?.DefinitionId, UpgradeLevel = item?.UpgradeLevel ?? 0 });
            }
            state.Equipment = equipment.ToArray();

            var stats = new StatSnapshot[ShownStats.Length];
            for (int i = 0; i < ShownStats.Length; i++)
            {
                stats[i] = new StatSnapshot { Stat = ShownStats[i], Value = player.Stats.Get(ShownStats[i]) };
            }
            state.Stats = stats;

            var groups = new List<CooldownGroupSnapshot>();
            foreach (var pair in player.ConsumableCooldowns)
            {
                groups.Add(new CooldownGroupSnapshot { Group = pair.Key, Remaining = pair.Value });
            }
            state.ConsumableCooldowns = groups.ToArray();
            return state;
        }
    }
}
