using System.Collections.Generic;
using Game.Shared.Content;

namespace Game.Shared.Rules
{
    /// <summary>
    /// Skill cost rules (03, 7.1), shared by the server (payment) and the client (skill bar hints).
    /// PercentOfMax amounts are percents; for the smart Mana cost the base is the maximum of pool A (question 45, ДЕФОЛТ).
    /// </summary>
    public static class CostMath
    {
        private const float PercentDivisor = 100f;

        public static CostTotals Sum(IReadOnlyList<CostEntry> costs, float maxMain, float maxReserve, float maxHealth)
        {
            var totals = new CostTotals();
            foreach (CostEntry cost in costs)
            {
                switch (cost.Resource)
                {
                    case ResourceKind.CombatMana:
                        totals.Main += Amount(cost, maxMain);
                        break;
                    case ResourceKind.ReserveMana:
                        totals.Reserve += Amount(cost, maxReserve);
                        break;
                    case ResourceKind.Mana:
                        totals.Smart += Amount(cost, maxMain);
                        break;
                    case ResourceKind.Health:
                        totals.Health += Amount(cost, maxHealth);
                        break;
                }
            }
            return totals;
        }

        /// <summary>All mana parts are payable. Health parts are always payable: they may kill the caster (question 3).</summary>
        public static bool CanPay(CostTotals totals, float main, float reserve)
        {
            if (main < totals.Main || reserve < totals.Reserve)
            {
                return false;
            }
            return (main - totals.Main) + (reserve - totals.Reserve) >= totals.Smart;
        }

        private static float Amount(CostEntry cost, float max) =>
            cost.Mode == CostMode.PercentOfMax ? max * cost.Amount / PercentDivisor : cost.Amount;
    }
}
