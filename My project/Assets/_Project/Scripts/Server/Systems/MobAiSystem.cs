using System;
using Game.Server.Effects;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;

namespace Game.Server.Systems
{
    /// <summary>
    /// Mob AI (module 10): Passive mobs never fight; Aggressive mobs notice hostile players within AggroRadius; Defensive
    /// mobs answer only to threat (being hurt). A fighting mob chases the player with the most threat (module 8) and uses
    /// the first ready ability in range (abilities are skills: Default variant effects, cooldown and range, no costs).
    /// It returns home (leash) when it gets farther than LeashRadius from its spawn or has nobody left to fight: threat and
    /// statuses are cleared and health is restored on arrival. Herd mobs call idle kin nearby into the fight (question 75). The ground is flat, so movement is a straight line
    /// (NavMesh — when zones have obstacles, question 75). Stun and root stop moving, stun and silence stop abilities.
    /// </summary>
    public sealed class MobAiSystem
    {
        private readonly ServerContext _ctx;
        private readonly CombatSystem _combat;
        private readonly StatusSystem _statuses;
        private readonly ThreatSystem _threat;
        private readonly EffectHandlerRegistry _effects;

        public MobAiSystem(ServerContext ctx, CombatSystem combat, StatusSystem statuses, ThreatSystem threat, EffectHandlerRegistry effects)
        {
            _ctx = ctx;
            _combat = combat;
            _statuses = statuses;
            _threat = threat;
            _effects = effects;
        }

        public static MobAiData Create(AiProfile profile) =>
            new MobAiData { AbilityCooldowns = new float[profile != null ? profile.Abilities.Count : 0] };

        public void Update(float dt)
        {
            ServerWorld world = _ctx.World;
            foreach (EntityId id in world.Entities)
            {
                if (!world.MobAi.Has(id))
                {
                    continue;
                }
                ref MobAiData ai = ref world.MobAi.Get(id);
                if (!world.IsAlive(id))
                {
                    Reset(ref ai);
                    continue;
                }
                AiProfile profile = ProfileOf(id);
                if (profile == null || profile.Behavior == AiBehavior.Passive)
                {
                    continue;
                }
                ai.GlobalCooldown = Math.Max(0f, ai.GlobalCooldown - dt);
                for (int i = 0; i < ai.AbilityCooldowns.Length; i++)
                {
                    ai.AbilityCooldowns[i] = Math.Max(0f, ai.AbilityCooldowns[i] - dt);
                }
                switch (ai.State)
                {
                    case MobAiState.Idle:
                        UpdateIdle(id, profile, ref ai);
                        break;
                    case MobAiState.Chase:
                        UpdateChase(id, profile, ref ai, dt);
                        break;
                    default:
                        UpdateReturn(id, profile, ref ai, dt);
                        break;
                }
            }
        }

        private void UpdateIdle(EntityId id, AiProfile profile, ref MobAiData ai)
        {
            EntityId target = _threat.TopThreat(id);
            if (!target.IsValid && profile.Behavior == AiBehavior.Aggressive)
            {
                target = FindNearbyEnemy(id, profile.AggroRadius);
                if (target.IsValid)
                {
                    _threat.Add(id, target, _ctx.Content.Config.MobAggroThreat);
                }
            }
            if (target.IsValid)
            {
                ai.State = MobAiState.Chase;
                ai.TargetId = target;
                if (profile.Herd)
                {
                    CallKin(id, target, profile.AssistRadius);
                }
            }
        }

        /// <summary>Herd mobs (question 75): idle living kin (same definition) within the radius take the same enemy.</summary>
        private void CallKin(EntityId caller, EntityId enemy, float radius)
        {
            ServerWorld world = _ctx.World;
            string kind = world.Identities.Get(caller).DefinitionId;
            Vec3 origin = world.Transforms.Get(caller).Position;
            foreach (EntityId other in world.Entities)
            {
                if (other == caller || !world.MobAi.Has(other) || !world.IsAlive(other) || world.MobAi.Get(other).State != MobAiState.Idle
                    || world.Identities.Get(other).DefinitionId != kind
                    || Vec3.DistanceXZ(world.Transforms.Get(other).Position, origin) > radius)
                {
                    continue;
                }
                if (!_threat.TopThreat(other).IsValid)
                {
                    _threat.Add(other, enemy, _ctx.Content.Config.MobAggroThreat);
                }
            }
        }

        private void UpdateChase(EntityId id, AiProfile profile, ref MobAiData ai, float dt)
        {
            ServerWorld world = _ctx.World;
            EntityId target = _threat.TopThreat(id);
            Vec3 home = world.Mobs.Get(id).SpawnPosition;
            Vec3 position = world.Transforms.Get(id).Position;
            if (!target.IsValid || !world.IsAlive(target) || Vec3.DistanceXZ(position, home) > profile.LeashRadius)
            {
                StartReturn(id, ref ai);
                return;
            }
            ai.TargetId = target;
            Vec3 targetPosition = world.Transforms.Get(target).Position;
            float distance = Vec3.DistanceXZ(position, targetPosition);
            ControlType control = _statuses.Controls(id);
            bool canAct = (control & (ControlType.Stun | ControlType.Silence)) == 0;
            bool canMove = (control & (ControlType.Stun | ControlType.Root)) == 0;

            float reach = float.MaxValue;
            for (int i = 0; i < profile.Abilities.Count; i++)
            {
                SkillDefinition skill = _ctx.Content.Skills[profile.Abilities[i]];
                reach = Math.Min(reach, skill.Range);
                if (canAct && ai.GlobalCooldown <= 0f && ai.AbilityCooldowns[i] <= 0f && distance <= skill.Range)
                {
                    Use(id, target, skill);
                    ai.AbilityCooldowns[i] = skill.Default.Cooldown;
                    ai.GlobalCooldown = _ctx.Content.Config.MobGlobalCooldownSeconds;
                    break;
                }
            }
            if (canMove && distance > reach)
            {
                MoveTowards(id, targetPosition, profile.MoveSpeed * dt, reach);
            }
            else
            {
                world.Transforms.Get(id).MoveDirection = Vec3.Zero;
            }
        }

        private void UpdateReturn(EntityId id, AiProfile profile, ref MobAiData ai, float dt)
        {
            ServerWorld world = _ctx.World;
            _threat.ClearTable(id);
            Vec3 home = world.Mobs.Get(id).SpawnPosition;
            if (Vec3.DistanceXZ(world.Transforms.Get(id).Position, home) <= _ctx.Content.Config.MobReturnArriveDistance)
            {
                ref VitalsData vitals = ref world.Vitals.Get(id);
                vitals.Health = vitals.MaxHealth;
                world.Transforms.Get(id).MoveDirection = Vec3.Zero;
                Reset(ref ai);
                return;
            }
            if ((_statuses.Controls(id) & (ControlType.Stun | ControlType.Root)) == 0)
            {
                MoveTowards(id, home, profile.MoveSpeed * dt, 0f);
            }
        }

        private void StartReturn(EntityId id, ref MobAiData ai)
        {
            ai.State = MobAiState.Return;
            ai.TargetId = EntityId.None;
            _threat.ClearTable(id);
            _statuses.OnDied(id);
        }

        private void Use(EntityId mob, EntityId target, SkillDefinition skill)
        {
            foreach (EffectDefinition effect in skill.Default.Effects)
            {
                _effects.Apply(new EffectContext
                {
                    Server = _ctx,
                    SourceId = mob,
                    TargetId = target,
                    SkillId = skill.Id,
                    Effect = effect,
                    Efficiency = 1f,
                });
            }
        }

        /// <summary>Straight move on the flat ground, stopping `stopAt` short of the point.</summary>
        private void MoveTowards(EntityId id, Vec3 point, float step, float stopAt)
        {
            ref TransformData transform = ref _ctx.World.Transforms.Get(id);
            Vec3 delta = new Vec3(point.X - transform.Position.X, 0f, point.Z - transform.Position.Z);
            float distance = delta.LengthXZ;
            if (distance <= stopAt || distance <= 0f)
            {
                transform.MoveDirection = Vec3.Zero;
                return;
            }
            Vec3 direction = delta / distance;
            float move = Math.Min(step, distance - stopAt);
            transform.Position = transform.Position + direction * move;
            transform.MoveDirection = direction;
            transform.Yaw = (float)(Math.Atan2(direction.X, direction.Z) * 180.0 / Math.PI);
        }

        private EntityId FindNearbyEnemy(EntityId mob, float radius)
        {
            Vec3 origin = _ctx.World.Transforms.Get(mob).Position;
            EntityId best = EntityId.None;
            float bestDistance = radius;
            foreach (EntityId player in _ctx.Players.Keys)
            {
                if (!_ctx.World.IsAlive(player) || !_combat.AreHostile(mob, player))
                {
                    continue;
                }
                float distance = Vec3.DistanceXZ(origin, _ctx.World.Transforms.Get(player).Position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = player;
                }
            }
            return best;
        }

        private AiProfile ProfileOf(EntityId id)
        {
            string mobId = _ctx.World.Identities.Get(id).DefinitionId;
            return _ctx.Content.Mobs.TryGetValue(mobId, out MobDto mob) && !string.IsNullOrEmpty(mob.AiProfile)
                && _ctx.Content.AiProfiles.TryGetValue(mob.AiProfile, out AiProfile profile) ? profile : null;
        }

        private static void Reset(ref MobAiData ai)
        {
            ai.State = MobAiState.Idle;
            ai.TargetId = EntityId.None;
            ai.GlobalCooldown = 0f;
            for (int i = 0; i < ai.AbilityCooldowns.Length; i++)
            {
                ai.AbilityCooldowns[i] = 0f;
            }
        }
    }
}
