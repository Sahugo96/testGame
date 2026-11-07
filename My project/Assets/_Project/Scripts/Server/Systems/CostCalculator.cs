using System;
using System.Collections.Generic;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Rules;

namespace Game.Server.Systems
{
    /// <summary>Applies <see cref="CostMath"/> to simulation data: everything is checked first, then paid in one operation (03, 7.1).</summary>
    public static class CostCalculator
    {
        public static CostTotals Sum(IReadOnlyList<CostEntry> costs, in ManaData mana, in VitalsData vitals) =>
            CostMath.Sum(costs, mana.MaxMain, mana.MaxReserve, vitals.MaxHealth);

        public static bool CanPay(CostTotals totals, in ManaData mana) => CostMath.CanPay(totals, mana.Main, mana.Reserve);

        /// <summary>Pays all parts; returns true when the health part killed the caster.</summary>
        public static bool Pay(CostTotals totals, ref ManaData mana, ref VitalsData vitals)
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
    }
}
