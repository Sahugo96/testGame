using System;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;

namespace Game.Server.Systems
{
    /// <summary>
    /// Server-side movement on the XZ plane (module 2 basics, no network yet): the client sends a direction, the server
    /// moves the character by its MoveSpeed stat and keeps it inside the world bounds. Moving interrupts interruptible casts.
    /// </summary>
    public sealed class MovementSystem
    {
        private const float RadiansToDegrees = 180f / (float)Math.PI;

        private readonly ServerContext _ctx;
        private readonly SkillSystem _skills;

        public MovementSystem(ServerContext ctx, SkillSystem skills)
        {
            _ctx = ctx;
            _skills = skills;
        }

        public void Update(float dt)
        {
            float half = _ctx.Content.Config.WorldHalfSize;
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                EntityId id = player.EntityId;
                if (!_ctx.World.IsAlive(id))
                {
                    continue;
                }
                var direction = new Vec3(player.LastMove.DirX, 0f, player.LastMove.DirZ);
                if (direction.LengthXZ > 1f)
                {
                    direction = direction.Normalized;
                }
                ref TransformData transform = ref _ctx.World.Transforms.Get(id);
                transform.MoveDirection = direction;
                if (direction.LengthXZ <= 0f)
                {
                    continue;
                }
                float speed = player.Stats.Get(StatId.MoveSpeed);
                Vec3 position = transform.Position + direction * (speed * dt);
                position.X = Math.Max(-half, Math.Min(half, position.X));
                position.Z = Math.Max(-half, Math.Min(half, position.Z));
                transform.Position = position;
                transform.Yaw = (float)Math.Atan2(direction.X, direction.Z) * RadiansToDegrees;
                _skills.OnMoved(id);
            }
        }
    }
}
