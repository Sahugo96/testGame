using System;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Combat state and regeneration (03, 7.1, question 1):
    /// pool A regenerates passively in and out of combat unless locked; pool B regenerates only out of combat at
    /// ReserveRegenOutOfCombatFactor of the passive rate; lock = A is empty while B is not full, cleared when B is full.
    /// Health regenerates passively in and out of combat (question 63).
    /// </summary>
    public sealed class ResourceSystem
    {
        private readonly ServerContext _ctx;

        public ResourceSystem(ServerContext ctx) => _ctx = ctx;

        public static void UpdateLock(ref ManaData mana)
        {
            if (mana.Reserve >= mana.MaxReserve)
            {
                mana.MainLocked = false;
            }
            else if (mana.Main <= 0f)
            {
                mana.MainLocked = true;
            }
        }

        public bool IsInCombat(EntityId id) => _ctx.World.CombatStates.Has(id) && _ctx.World.CombatStates.Get(id).InCombat;

        public void MarkCombat(EntityId id)
        {
            if (!_ctx.World.CombatStates.Has(id))
            {
                return;
            }
            ref CombatStateData state = ref _ctx.World.CombatStates.Get(id);
            state.InCombat = true;
            state.SecondsSinceCombatAction = 0f;
        }

        public void Update(float dt)
        {
            ServerWorld world = _ctx.World;
            float exitSeconds = _ctx.Content.Config.CombatExitSeconds;
            for (int i = 0; i < world.CombatStates.Count; i++)
            {
                ref CombatStateData state = ref world.CombatStates.At(i);
                if (!state.InCombat)
                {
                    continue;
                }
                state.SecondsSinceCombatAction += dt;
                if (state.SecondsSinceCombatAction >= exitSeconds)
                {
                    state.InCombat = false;
                }
            }

            float reserveFactor = _ctx.Content.Config.ReserveRegenOutOfCombatFactor;
            for (int i = 0; i < world.Mana.Count; i++)
            {
                EntityId id = world.Mana.OwnerAt(i);
                if (!world.IsAlive(id))
                {
                    continue;
                }
                ref ManaData mana = ref world.Mana.At(i);
                UpdateLock(ref mana);
                if (!mana.MainLocked)
                {
                    mana.Main = Math.Min(mana.MaxMain, mana.Main + mana.MainRegen * dt);
                }
                if (!IsInCombat(id))
                {
                    mana.Reserve = Math.Min(mana.MaxReserve, mana.Reserve + mana.MainRegen * reserveFactor * dt);
                }
                UpdateLock(ref mana);
            }

            for (int i = 0; i < world.Vitals.Count; i++)
            {
                EntityId id = world.Vitals.OwnerAt(i);
                ref VitalsData vitals = ref world.Vitals.At(i);
                if (vitals.IsDead)
                {
                    continue;
                }
                vitals.Health = Math.Min(vitals.MaxHealth, vitals.Health + vitals.HealthRegen * dt);
            }
        }

        /// <summary>Adds a resource. Mana goes to pool A first, then B. Passive lock does not block restores (question 1).</summary>
        public float Restore(EntityId target, ResourceKind resource, float amount, EntityId source, string skillId)
        {
            ServerWorld world = _ctx.World;
            if (!world.IsAlive(target) || amount <= 0f)
            {
                return 0f;
            }
            float restored;
            if (resource == ResourceKind.Health)
            {
                ref VitalsData vitals = ref world.Vitals.Get(target);
                restored = Math.Min(amount, vitals.MaxHealth - vitals.Health);
                vitals.Health += restored;
            }
            else
            {
                if (!world.Mana.Has(target))
                {
                    return 0f;
                }
                ref ManaData mana = ref world.Mana.Get(target);
                float toMain = 0f;
                float toReserve = 0f;
                if (resource == ResourceKind.CombatMana || resource == ResourceKind.Mana)
                {
                    toMain = Math.Min(amount, mana.MaxMain - mana.Main);
                }
                if (resource == ResourceKind.ReserveMana)
                {
                    toReserve = Math.Min(amount, mana.MaxReserve - mana.Reserve);
                }
                else if (resource == ResourceKind.Mana)
                {
                    toReserve = Math.Min(amount - toMain, mana.MaxReserve - mana.Reserve);
                }
                mana.Main += toMain;
                mana.Reserve += toReserve;
                UpdateLock(ref mana);
                restored = toMain + toReserve;
            }
            if (restored > 0f)
            {
                CombatEvents.Broadcast(_ctx, new CombatEventMessage
                {
                    SourceId = source,
                    TargetId = target,
                    SkillId = skillId,
                    Kind = CombatEventKind.ResourceRestore,
                    Amount = (int)Math.Round((double)(float)restored), // narrowed first: same result in Mono and .NET
                });
            }
            return restored;
        }

        /// <summary>How much of the resource is missing (used to refuse potions on a full pool).</summary>
        public float Missing(EntityId target, ResourceKind resource)
        {
            ServerWorld world = _ctx.World;
            if (resource == ResourceKind.Health)
            {
                VitalsData vitals = world.Vitals.Get(target);
                return vitals.MaxHealth - vitals.Health;
            }
            ManaData mana = world.Mana.Get(target);
            switch (resource)
            {
                case ResourceKind.CombatMana:
                    return mana.MaxMain - mana.Main;
                case ResourceKind.ReserveMana:
                    return mana.MaxReserve - mana.Reserve;
                default:
                    return (mana.MaxMain - mana.Main) + (mana.MaxReserve - mana.Reserve);
            }
        }
    }
}
