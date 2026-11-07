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
        private readonly DuelSystem _duels;
        private readonly GroupSystem _groups;
        private readonly LootSystem _loot;
        private readonly CraftingSystem _crafting;
        private readonly StatusSystem _statuses;
        private readonly ThreatSystem _threat;
        private readonly MobAiSystem _mobAi;
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
            _duels = new DuelSystem(_ctx, _resources);
            _statuses = new StatusSystem(_ctx);
            _combat = new CombatSystem(_ctx, _resources, _duels, _statuses);
            _statuses.BindCombat(_combat);
            var effects = new EffectHandlerRegistry();
            effects.Register(new DamageEffectHandler(_combat));
            effects.Register(new HealEffectHandler(_combat));
            effects.Register(new RestoreResourceEffectHandler(_resources));
            effects.Register(new ApplyStatusEffectHandler(_statuses));
            effects.Register(new DispelEffectHandler(_statuses));
            _skills = new SkillSystem(_ctx, _resources, _combat, effects, _statuses);
            _movement = new MovementSystem(_ctx, _skills, _statuses);
            _targeting = new TargetingSystem(_ctx, _combat);
            _stats = new StatSystem(_ctx) { ExtraModifiers = _statuses.ModifiersOf };
            _inventory = new InventorySystem(_ctx, _stats, _resources, _statuses);
            _threat = new ThreatSystem(_ctx);
            _mobAi = new MobAiSystem(_ctx, _combat, _statuses, _threat, effects);
            _respawn = new RespawnSystem(_ctx);
            _snapshots = new SnapshotBuilder(_ctx, _skills);
            _progression = new ProgressionSystem(_ctx, _stats);
            _upgrades = new UpgradeSystem(_ctx, _inventory, _stats);
            _groups = new GroupSystem(_ctx);
            _loot = new LootSystem(_ctx, _groups, _inventory);
            _crafting = new CraftingSystem(_ctx, _inventory, _resources);
            _combat.Damaged += _loot.OnDamaged;
            _combat.Healed += _loot.OnHealed;
            _combat.Damaged += _threat.OnDamaged;
            _combat.Healed += _threat.OnHealed;
            _combat.Died += (victim, killer) => _threat.OnDied(victim);
            _statuses.Changed += OnStatusesChanged;
            _statuses.Controlled += _skills.CancelCast;
            _combat.Died += OnKilled;
            _duels.Ended += OnDuelEnded;
            SpawnMobs();
            SpawnStations();
        }

        public ServerContext Context => _ctx;

        public float TickInterval => 1f / _ctx.Content.Config.TickRate;

        public int CurrentTick => _tick;

        /// <summary>Raised after every tick when snapshots are ready (transports send them).</summary>
        public event Action Ticked;

        public bool HasClass(string classId) => classId != null && _ctx.Content.Classes.ContainsKey(classId);

        public EntityId AddPlayer(string classId) => AddPlayer(classId, null);

        public EntityId AddPlayer(string classId, string displayName)
        {
            if (!_ctx.Content.Classes.TryGetValue(classId, out ClassDefinition cls))
            {
                throw new ArgumentException($"Unknown class '{classId}'");
            }
            ServerWorld world = _ctx.World;
            EntityId id = world.CreateEntity();
            GameConfig cfg = _ctx.Content.Config;
            world.Identities.Add(id, new IdentityData { Kind = EntityKind.Player, DefinitionId = cls.Id, NameKey = cls.NameKey, DisplayName = displayName, Faction = cfg.PlayerFaction });
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
            if (!_ctx.Players.ContainsKey(id))
            {
                return;
            }
            _duels.OnPlayerRemoved(id);
            _statuses.OnEntityRemoved(id);
            _threat.OnDied(id);
            _groups.OnPlayerRemoved(id);
            _ctx.Players.Remove(id);
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
            _mobAi.Update(dt);
            _skills.Update(dt);
            _statuses.Update(dt);
            _inventory.Update(dt);
            _resources.Update(dt);
            _respawn.Update(dt);
            _duels.Update();
            _crafting.Update(dt);
            _groups.Update();

            WorldSnapshot worldSnapshot = _snapshots.BuildWorld(_tick);
            for (int i = 0; i < worldSnapshot.Entities.Length; i++)
            {
                EntityId id = worldSnapshot.Entities[i].Id;
                worldSnapshot.Entities[i].Statuses = _statuses.Snapshot(id);
                PlayerCharacter owner = _ctx.GetPlayer(id);
                worldSnapshot.Entities[i].TargetOfId = owner != null ? owner.TargetId : _threat.TopThreat(id);
            }
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                player.Outbox.World = worldSnapshot;
                player.Outbox.State = _snapshots.BuildPlayer(player);
                player.Outbox.State.Duel = _duels.Snapshot(player.EntityId);
                player.Outbox.State.CraftRecipeId = player.Craft.Active ? player.Craft.RecipeId : null;
                player.Outbox.State.CraftProgress = _crafting.Progress(player);
                player.Outbox.State.Group = _groups.Snapshot(player.EntityId);
            }
            Ticked?.Invoke();
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
                case CraftItemCommand craft:
                    _crafting.Start(player, craft);
                    break;
                case CancelCraftCommand _:
                    _crafting.Cancel(player, true);
                    break;
                case SpendAttributePointCommand spend:
                    _progression.SpendAttributePoint(player, spend);
                    break;
                case DuelRequestCommand duelRequest:
                    _duels.Request(player, duelRequest.TargetId);
                    break;
                case DuelResponseCommand duelResponse:
                    _duels.Respond(player, duelResponse.Accept);
                    break;
                case DuelSurrenderCommand _:
                    _duels.Surrender(player);
                    break;
                case GroupInviteCommand invite:
                    _groups.Invite(player, invite.TargetId);
                    break;
                case GroupInviteResponseCommand inviteResponse:
                    _groups.RespondInvite(player, inviteResponse.Accept);
                    break;
                case GroupLeaveCommand _:
                    _groups.Leave(player);
                    break;
                case GroupKickCommand kick:
                    _groups.Kick(player, kick.MemberId);
                    break;
                case GroupPromoteCommand promote:
                    _groups.Promote(player, promote.MemberId);
                    break;
                case LootModeProposeCommand propose:
                    _groups.Propose(player, propose.Mode);
                    break;
                case LootModeVoteCommand vote:
                    _groups.Vote(player, vote.ProposalId, vote.Accept);
                    break;
                default:
                    Log.Warning(LogTags.Server, $"Unknown command {command.GetType().Name}");
                    break;
            }
        }

        /// <summary>Statuses changed what the entity's stats are (03, 7.12).</summary>
        private void OnStatusesChanged(EntityId id)
        {
            PlayerCharacter player = _ctx.GetPlayer(id);
            if (player != null)
            {
                _stats.Recalculate(player);
            }
            else
            {
                _stats.RecalculateMob(id);
            }
        }

        /// <summary>Threat on mobs (module 8) for tests and the coming mob AI (module 10).</summary>
        public ThreatSystem Threat => _threat;

        /// <summary>Damage and healing (03, 7.12) for tests and tools.</summary>
        public CombatSystem Combat => _combat;

        /// <summary>Statuses (03, 7.12) for tests and tools.</summary>
        public StatusSystem Statuses => _statuses;

        /// <summary>After a duel nobody keeps hitting: casts aimed at the former opponent are cancelled.</summary>
        private void OnDuelEnded(EntityId a, EntityId b)
        {
            _skills.CancelCastAt(a, b);
            _skills.CancelCastAt(b, a);
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
                _ctx.Content.AiProfiles.TryGetValue(mob.AiProfile ?? string.Empty, out AiProfile profile);
                world.MobAi.Add(id, MobAiSystem.Create(profile));
            }
        }

        /// <summary>
        /// Kill rewards: experience to the killer (question 72); drops by the loot rules — solo to the killer, in a group
        /// by the group's loot mode among eligible members (06, 7.8; LootSystem).
        /// </summary>
        private void OnKilled(EntityId victim, EntityId killer)
        {
            if (!_ctx.World.Mobs.Has(victim))
            {
                return;
            }
            PlayerCharacter player = _ctx.GetPlayer(killer);
            if (player != null && victim != killer)
            {
                MobDto mob = _ctx.Content.Mobs[_ctx.World.Identities.Get(victim).DefinitionId];
                _progression.AwardExperience(player, mob.ExperienceReward);
                foreach (MobDropDto drop in mob.Drops ?? new MobDropDto[0])
                {
                    if (_ctx.Random.NextFloat() < drop.Chance)
                    {
                        _loot.Give(player, victim, drop);
                    }
                }
            }
            _loot.OnMobLooted(victim);
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
