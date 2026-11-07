using System;
using System.Collections.Generic;
using Game.Server.World;
using Game.Shared.Content;

namespace Game.Server.Systems
{
    /// <summary>
    /// Skill cost rules (03, 7.1): all parts must be payable, then everything is paid in one operation.
    /// Mana = pool A first, then pool B; CombatMana = pool A only; ReserveMana = pool B only.
    /// Health cost may kill the caster (question 3). PercentOfMax amounts are percents.
    /// For the smart Mana cost PercentOfMax uses the maximum of pool A (question 45, ДЕФОЛТ).
    /// </summary>
    public static class CostCalculator
    {
        private const float PercentDivisor = 100f;

        public struct Totals
        {
            public float Main;
            public float Reserve;
            public float Smart;
            public float Health;
        }

        public static Totals Sum(IReadOnlyList<CostEntry> costs, in ManaData mana, in VitalsData vitals)
        {
            var totals = new Totals();
            foreach (CostEntry cost in costs)
            {
                switch (cost.Resource)
                {
                    case ResourceKind.CombatMana:
                        totals.Main += Amount(cost, mana.MaxMain);
                        break;
                    case ResourceKind.ReserveMana:
                        totals.Reserve += Amount(cost, mana.MaxReserve);
                        break;
                    case ResourceKind.Mana:
                        totals.Smart += Amount(cost, mana.MaxMain);
                        break;
                    case ResourceKind.Health:
                        totals.Health += Amount(cost, vitals.MaxHealth);
                        break;
                }
            }
            return totals;
        }

        public static bool CanPay(Totals totals, in ManaData mana)
        {
            if (mana.Main < totals.Main || mana.Reserve < totals.Reserve)
            {
                return false;
            }
            float leftover = (mana.Main - totals.Main) + (mana.Reserve - totals.Reserve);
            return leftover >= totals.Smart;
        }

        /// <summary>Pays all parts; returns true when the health part killed the caster.</summary>
        public static bool Pay(Totals totals, ref ManaData mana, ref VitalsData vitals)
        {
            mana.Main -= totals.Main;
            mana.Reserve -= totals.Reserve;
            float fromMain = Math.Min(mana.Main, totals.Smart);
            mana.Main -= fromMain;
            mana.Reserve -= totals.Smart - fromMain;
            ResourceSystem.UpdateLock(ref mana);
            if (totals.Health <= 0f)
            {
                return false;
            }
            vitals.Health -= totals.Health;
            return vitals.Health <= 0f;
        }

        private static float Amount(CostEntry cost, float max) =>
            cost.Mode == CostMode.PercentOfMax ? max * cost.Amount / PercentDivisor : cost.Amount;
    }
}
