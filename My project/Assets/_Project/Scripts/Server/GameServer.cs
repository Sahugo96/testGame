using System;
using Game.Server.Effects;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.Systems;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server
{
    /// <summary>
    /// Authoritative simulation of one zone (principles 1, 9): fixed tick, commands in, snapshots out.
    /// Pure C# without Unity or Mirror; the transport (local now, Mirror in module 1) only moves messages.
    /// </summary>
    public sealed class GameServer
    {
        private readonly ServerContext _ctx;
        private readonly ResourceSystem _resources;
        private readonly CombatSystem _combat;
        private readonly SkillSystem _skills;
        private readonly MovementSystem _movement;
        private readonly TargetingSystem _targeting;
        private readonly InventorySystem _inventory;
        private readonly StatSystem _stats;
        private readonly RespawnSystem _respawn;
        private readonly SnapshotBuilder _snapshots;
        private readonly ProgressionSystem _progression;
        private readonly UpgradeSystem _upgrades;
        private int _tick;
        private int _nextCharacterNumber = 1;

        public GameServer(ContentDatabase content, IRandom random, IAuditLog audit)
        {
            _ctx = new ServerContext
            {
                Content = content,
                World = new ServerWorld(),
                Random = random,
                Audit = audit,
                Items = new ItemFactory(content, audit),
            };
            _resources = new ResourceSystem(_ctx);
            _combat = new CombatSystem(_ctx, _resources);
            var effects = new EffectHandlerRegistry();
            effects.Register(new DamageEffectHandler(_combat));
            effects.Register(new HealEffectHandler(_combat));
            effects.Register(new RestoreResourceEffectHandler(_resources));
            _skills = new SkillSystem(_ctx, _resources, _combat, effects);
            _movement = new MovementSystem(_ctx, _skills);
            _targeting = new TargetingSystem(_ctx, _combat);
            _stats = new StatSystem(_ctx);
            _inventory = new InventorySystem(_ctx, _stats, _resources);
            _respawn = new RespawnSystem(_ctx);
            _snapshots = new SnapshotBuilder(_ctx, _skills);
            _progression = new ProgressionSystem(_ctx, _stats);
            _upgrades = new UpgradeSystem(_ctx, _inventory, _stats);
            _combat.Died += OnKilled;
            SpawnMobs();
            SpawnStations();
        }

        public ServerContext Context => _ctx;

        public float TickInterval => 1f / _ctx.Content.Config.TickRate;

        public int CurrentTick => _tick;

        public EntityId AddPlayer(string classId)
        {
            if (!_ctx.Content.Classes.TryGetValue(classId, out ClassDefinition cls))
            {
                throw new ArgumentException($"Unknown class '{classId}'");
            }
            ServerWorld world = _ctx.World;
            EntityId id = world.CreateEntity();
            GameConfig cfg = _ctx.Content.Config;
            world.Identities.Add(id, new IdentityData { Kind = EntityKind.Player, DefinitionId = cls.Id, NameKey = cls.NameKey, Faction = cfg.PlayerFaction });
            world.Transforms.Add(id, new TransformData { Position = _respawn.PlayerSpawn() });
            world.Vitals.Add(id, new VitalsData());
            world.Mana.Add(id, new ManaData());
            world.CombatStates.Add(id, new CombatStateData());
            world.Casts.Add(id, new CastData());

            var player = new PlayerCharacter
            {
                EntityId = id,
                CharacterId = $"character-{_nextCharacterNumber++}",
                Class = cls,
                Level = cfg.StartingLevel,
                Bag = new Inventory(_ctx.Content.BagSize),
            };
            foreach (var pair in cls.StartingAttributes)
            {
                player.Attributes[pair.Key] = pair.Value;
            }
            foreach (ClassSkill skill in cls.Skills)
            {
                player.Mastery[skill.SkillId] = skill.StartingMastery;
            }
            _ctx.Players.Add(id, player);
            _stats.Recalculate(player);
            _inventory.GiveStartingItems(player);
            FillResources(id);
            Log.Info(LogTags.Server, $"Player {id} joined as '{classId}'");
            return id;
        }

        /// <summary>Removes a player and its entity (disconnect or character switch).</summary>
        public void RemovePlayer(EntityId id)
        {
            if (!_ctx.Players.Remove(id))
            {
                return;
            }
            foreach (PlayerCharacter other in _ctx.Players.Values)
            {
                if (other.TargetId == id)
                {
                    other.TargetId = EntityId.None;
                }
            }
            _ctx.World.DestroyEntity(id);
            Log.Info(LogTags.Server, $"Player {id} left");
        }

        public PlayerCharacter GetPlayer(EntityId id) => _ctx.GetPlayer(id);

        public void Submit(EntityId playerId, IGameCommand command)
        {
            PlayerCharacter player = _ctx.GetPlayer(playerId);
            player?.PendingCommands.Enqueue(command);
        }

        public void Tick()
        {
            float dt = TickInterval;
            _ctx.Time += dt;
            _tick++;
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                while (player.PendingCommands.Count > 0)
                {
                    Handle(player, player.PendingCommands.Dequeue());
                }
            }
            _movement.Update(dt);
            _skills.Update(dt);
            _inventory.Update(dt);
            _resources.Update(dt);
            _respawn.Update(dt);

            WorldSnapshot worldSnapshot = _snapshots.BuildWorld(_tick);
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                player.Outbox.World = worldSnapshot;
                player.Outbox.State = _snapshots.BuildPlayer(player);
            }
        }

        private void Handle(PlayerCharacter player, IGameCommand command)
        {
            switch (command)
            {
                case MoveCommand move:
                    player.LastMove = move;
                    break;
                case UseSkillCommand useSkill:
                    _skills.TryUse(player, useSkill);
                    break;
                case SelectTargetCommand select:
                    _targeting.Select(player, select.TargetId);
                    break;
                case CycleTargetCommand _:
                    _targeting.Cycle(player);
                    break;
                case EquipItemCommand equip:
                    _inventory.Equip(player, equip);
                    break;
                case UnequipItemCommand unequip:
                    _inventory.Unequip(player, unequip);
                    break;
                case UseConsumableCommand consume:
                    _inventory.UseConsumable(player, consume);
                    break;
                case UpgradeItemCommand upgrade:
                    _upgrades.TryUpgrade(player, upgrade);
                    break;
                case SpendAttributePointCommand spend:
                    _progression.SpendAttributePoint(player, spend);
                    break;
                default:
                    Log.Warning(LogTags.Server, $"Unknown command {command.GetType().Name}");
                    break;
            }
        }

        private void SpawnMobs()
        {
            ServerWorld world = _ctx.World;
            foreach (SpawnDto spawn in _ctx.Content.World.Spawns ?? new SpawnDto[0])
            {
                MobDto mob = _ctx.Content.Mobs[spawn.MobId];
                EntityId id = world.CreateEntity();
                var position = new Vec3(spawn.X, 0f, spawn.Z);
                world.Identities.Add(id, new IdentityData { Kind = EntityKind.Mob, DefinitionId = mob.Id, NameKey = mob.NameKey, Faction = mob.Faction });
                world.Transforms.Add(id, new TransformData { Position = position });
                world.Vitals.Add(id, new VitalsData { Health = mob.MaxHealth, MaxHealth = mob.MaxHealth, HealthRegen = mob.HealthRegen, Defense = mob.Defense });
                world.CombatStates.Add(id, new CombatStateData());
                world.Mobs.Add(id, new MobData { SpawnPosition = position, RespawnSeconds = mob.RespawnSeconds });
            }
        }

        /// <summary>Kill rewards: experience and drops go straight to the killing player (solo; group loot is module 14).</summary>
        private void OnKilled(EntityId victim, EntityId killer)
        {
            PlayerCharacter player = _ctx.GetPlayer(killer);
            if (player == null || victim == killer || !_ctx.World.Mobs.Has(victim))
            {
                return;
            }
            MobDto mob = _ctx.Content.Mobs[_ctx.World.Identities.Get(victim).DefinitionId];
            _progression.AwardExperience(player, mob.ExperienceReward);
            foreach (MobDropDto drop in mob.Drops ?? new MobDropDto[0])
            {
                if (_ctx.Random.NextFloat() >= drop.Chance)
                {
                    continue;
                }
                int before = _inventory.CountItems(player, drop.ItemId);
                _inventory.GiveItem(player, drop.ItemId, drop.Count);
                int given = _inventory.CountItems(player, drop.ItemId) - before;
                if (given > 0)
                {
                    player.Outbox.Notifications.Add(new NotificationMessage
                    {
                        TextKey = NotificationKeys.Loot,
                        Number = given,
                        ItemNameKey = _ctx.Content.Items[drop.ItemId].NameKey,
                        IsPositive = true,
                    });
                }
            }
        }

        private void SpawnStations()
        {
            ServerWorld world = _ctx.World;
            foreach (StationDto station in _ctx.Content.World.Stations ?? new StationDto[0])
            {
                EntityId id = world.CreateEntity();
                world.Identities.Add(id, new IdentityData { Kind = EntityKind.Npc, DefinitionId = station.Id, NameKey = station.NameKey, Faction = null });
                world.Transforms.Add(id, new TransformData { Position = new Vec3(station.X, 0f, station.Z) });
                world.Stations.Add(id, new StationData { StationId = station.Id, Type = station.Type });
            }
        }

        private void FillResources(EntityId id)
        {
            ref VitalsData vitals = ref _ctx.World.Vitals.Get(id);
            vitals.Health = vitals.MaxHealth;
            ref ManaData mana = ref _ctx.World.Mana.Get(id);
            mana.Main = mana.MaxMain;
            mana.Reserve = mana.MaxReserve;
            mana.MainLocked = false;
        }
    }
}
