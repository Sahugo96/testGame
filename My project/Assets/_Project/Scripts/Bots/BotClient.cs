using System;
using Game.Network;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Bots
{
    /// <summary>
    /// Headless bot (module 3): a real network client without graphics. It wanders (a new random direction every few
    /// seconds, back towards the centre near the edge), picks the nearest hostile with Tab and uses its first ready
    /// unlocked skill. Everything goes through the same commands as a player, so the server cannot tell the difference.
    /// </summary>
    public sealed class BotClient : IDisposable
    {
        private const float TurnSeconds = 3f;
        private const float EdgeFraction = 0.8f;
        private const float ActSeconds = 0.5f;

        private readonly NetworkClientSession _session;
        private readonly Random _random;
        private readonly float _worldHalfSize;
        private float _turnTimer;
        private float _actTimer;
        private MoveCommand _move;

        public BotClient(NetworkClientSession session, int seed, float worldHalfSize)
        {
            _session = session;
            _random = new Random(seed);
            _worldHalfSize = worldHalfSize;
            _turnTimer = (float)_random.NextDouble() * TurnSeconds;
        }

        public bool IsClosed => _session.IsClosed;
        public PlayerStateSnapshot State { get; private set; }
        public WorldSnapshot World { get; private set; }
        public int SkillsUsed { get; private set; }
        public int CombatEventsSeen { get; private set; }

        public void Update(float dt)
        {
            while (_session.TryReceive(out object message))
            {
                switch (message)
                {
                    case PlayerStateSnapshot state:
                        State = state;
                        break;
                    case WorldSnapshot world:
                        World = world;
                        break;
                    case CombatEventMessage _:
                        CombatEventsSeen++;
                        break;
                }
            }
            if (!_session.IsWelcomed || State == null || State.IsDead)
            {
                return;
            }
            _turnTimer -= dt;
            if (_turnTimer <= 0f)
            {
                _turnTimer = TurnSeconds;
                Steer();
            }
            _actTimer -= dt;
            if (_actTimer <= 0f)
            {
                _actTimer = ActSeconds;
                Act();
            }
        }

        public void Dispose() => _session.Dispose();

        private void Steer()
        {
            Vec3 position = SelfPosition();
            float limit = _worldHalfSize * EdgeFraction;
            double angle = _random.NextDouble() * Math.PI * 2d;
            float x = (float)Math.Sin(angle);
            float z = (float)Math.Cos(angle);
            if (Math.Abs(position.X) > limit || Math.Abs(position.Z) > limit)
            {
                Vec3 home = new Vec3(-position.X, 0f, -position.Z);
                float length = Math.Max(home.LengthXZ, 0.001f);
                x = home.X / length;
                z = home.Z / length;
            }
            _move = new MoveCommand { DirX = (float)Math.Round(x, 2), DirZ = (float)Math.Round(z, 2) };
            _session.Send(_move);
        }

        private void Act()
        {
            if (!State.TargetId.IsValid)
            {
                _session.Send(new CycleTargetCommand());
                return;
            }
            foreach (SkillStateSnapshot skill in State.Skills ?? new SkillStateSnapshot[0])
            {
                if (skill.Unlocked && !OnCooldown(skill.SkillId))
                {
                    _session.Send(new UseSkillCommand { SkillId = skill.SkillId, TargetId = State.TargetId });
                    SkillsUsed++;
                    return;
                }
            }
        }

        private bool OnCooldown(string skillId)
        {
            foreach (SkillCooldownSnapshot cooldown in State.Cooldowns ?? new SkillCooldownSnapshot[0])
            {
                if (cooldown.SkillId == skillId && cooldown.Remaining > 0f)
                {
                    return true;
                }
            }
            return false;
        }

        private Vec3 SelfPosition()
        {
            if (World != null)
            {
                foreach (EntitySnapshot entity in World.Entities)
                {
                    if (entity.Id == _session.PlayerId)
                    {
                        return entity.Position;
                    }
                }
            }
            return Vec3.Zero;
        }
    }
}
