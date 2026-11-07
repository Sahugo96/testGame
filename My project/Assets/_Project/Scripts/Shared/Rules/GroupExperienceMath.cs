using System;

namespace Game.Shared.Rules
{
    /// <summary>
    /// Group experience split (developer decision, question 72). The highest level sets the scale: members who are
    /// gapShares.Length or more levels below it get nothing and are not counted. The rest start with equal parts; a member
    /// `gap` levels below the top keeps part × gapShares[gap]; what they lose goes to the top-level members in equal parts.
    /// Example (shares 1.0, 0.86, 0.72, 0.69, 0.66, 0.64, ...): levels 20, 20, 18, 15 and six ≤10 → 33%, 33%, 18%, 16%.
    /// </summary>
    public static class GroupExperienceMath
    {
        /// <summary>Shares come from float data (0.64f = 0.6399999…): a tiny tolerance keeps 16.0 from becoming 15.</summary>
        private const double RoundingTolerance = 1e-4;

        public static int[] Split(int total, int[] levels, float[] gapShares)
        {
            var result = new int[levels.Length];
            if (total <= 0 || levels.Length == 0 || gapShares == null || gapShares.Length == 0)
            {
                return result;
            }
            int top = int.MinValue;
            foreach (int level in levels)
            {
                top = Math.Max(top, level);
            }
            int counted = 0;
            int topCount = 0;
            foreach (int level in levels)
            {
                int gap = top - level;
                if (gap < gapShares.Length)
                {
                    counted++;
                    topCount += gap == 0 ? 1 : 0;
                }
            }
            double part = (double)total / counted;
            double lost = 0d;
            var exact = new double[levels.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                int gap = top - levels[i];
                if (gap >= gapShares.Length || gap == 0)
                {
                    continue;
                }
                exact[i] = part * gapShares[gap];
                lost += part - exact[i];
            }
            for (int i = 0; i < levels.Length; i++)
            {
                if (top - levels[i] == 0)
                {
                    exact[i] = part + lost / topCount;
                }
            }
            // Whole points: round down, the leftover goes to the top-level members first.
            int given = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                result[i] = (int)Math.Floor(exact[i] + RoundingTolerance);
                given += result[i];
            }
            for (int i = 0; given < total; i = (i + 1) % levels.Length)
            {
                if (top - levels[i] == 0)
                {
                    result[i]++;
                    given++;
                }
            }
            return result;
        }
    }
}
