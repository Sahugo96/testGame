using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Core;

namespace Game.Server.Systems
{
    /// <summary>Brings dead entities back: mobs at their spawn point, players at the world spawn with full resources (ДЕФОЛТ).</summary>
    public sealed class RespawnSystem
    {
        private readonly ServerContext _ctx;

        public RespawnSystem(ServerContext ctx) => _ctx = ctx;

        public void Update(float dt)
        {
            ServerWorld world = _ctx.World;
            for (int i = 0; i < world.Vitals.Count; i++)
            {
                ref VitalsData vitals = ref world.Vitals.At(i);
                if (!vitals.IsDead)
                {
                    continue;
                }
                vitals.RespawnRemaining -= dt;
                if (vitals.RespawnRemaining > 0f)
                {
                    continue;
                }
                EntityId id = world.Vitals.OwnerAt(i);
                vitals.IsDead = false;
                vitals.Health = vitals.MaxHealth;
                ref TransformData transform = ref world.Transforms.Get(id);
                transform.Position = world.Mobs.Has(id) ? world.Mobs.Get(id).SpawnPosition : PlayerSpawn();
                if (world.Mana.Has(id))
                {
                    ref ManaData mana = ref world.Mana.Get(id);
                    mana.Main = mana.MaxMain;
                    mana.Reserve = mana.MaxReserve;
                    mana.MainLocked = false;
                }
                if (world.CombatStates.Has(id))
                {
                    world.CombatStates.Get(id).InCombat = false;
                }
                PlayerCharacter player = _ctx.GetPlayer(id);
                if (player != null)
                {
                    player.LastMove = default;
                }
            }
        }

        public Vec3 PlayerSpawn() => new Vec3(_ctx.Content.World.PlayerSpawnX, 0f, _ctx.Content.World.PlayerSpawnZ);
    }
}
