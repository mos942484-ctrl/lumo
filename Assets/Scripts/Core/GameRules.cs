using System;

namespace Lumo.Core
{
    /// <summary>
    /// Fixed game rules from BUILD_PROMPT §6.1/§6.3 — one implementation, tested at boundaries.
    /// </summary>
    public static class GameRules
    {
        public const int ChallengeTimerSeconds = 30;
        public const int MaxStarsPerChallenge = 3;
        public const int TotalStars = 261;          // 12 levels * 6 * 3 + 15 final tasks * 3
        public const int XpPerChallenge = 10;
        public const int XpPerLevel = 20;
        public const int XpNoHintBonus = 10;

        public const int AgeBand5To7 = 0;
        public const int AgeBand8To10 = 1;
        public const int AgeBand11To12 = 2;

        /// <summary>Computes max total stars from the loaded bank (falling back to 261 when unloaded).</summary>
        public static int DynamicTotalStars(LevelBank bank = null)
        {
            if (bank == null && GameManager.Instance != null) bank = GameManager.Instance.Bank;
            if (bank != null && bank.levels != null && bank.levels.Length > 0)
            {
                int total = 0;
                foreach (var lv in bank.levels)
                    if (lv != null && lv.challenges != null)
                        total += lv.challenges.Length * MaxStarsPerChallenge;
                if (total > 0) return total;
            }
            return TotalStars;
        }

        /// <summary>
        /// Time-based star reward. Elapsed <= 10 -> 3; <= 20 -> 2; <= 30 -> 1; else 0.
        /// Boundary table (§11): 8s->3, 10s->3, 15s->2, 20s->2, 24s->1, 30s->1, 31s->0.
        /// LXD Phase C1 (Abbott Table 1 row 6 — rewards must not undermine learning):
        /// speed stars are gated by first-try accuracy. A non-first-try answer caps at
        /// 1 star (finishing after retries still earns <= 1); the boundary table holds
        /// only for first-try answers.
        /// </summary>
        public static int StarsForTime(float elapsedSeconds, bool firstTryCorrect)
        {
            int speedStars;
            if (elapsedSeconds <= 10f) speedStars = 3;
            else if (elapsedSeconds <= 20f) speedStars = 2;
            else if (elapsedSeconds <= 30f) speedStars = 1;
            else speedStars = 0;
            return firstTryCorrect ? speedStars : Math.Min(1, speedStars);
        }

        /// <summary>
        /// Untimed calm-play star reward based on accuracy and help used (always >= 1).
        /// </summary>
        public static int StarsForCalmAnswer(int mistakes, bool usedHelp)
        {
            if (mistakes <= 0 && !usedHelp) return 3;
            if (mistakes <= 1) return 2;
            return 1;
        }

        /// <summary>Short ASCII label for the age band.</summary>
        public static string AgeBandShort(int band)
        {
            switch (band)
            {
                case AgeBand5To7: return "5-7";
                case AgeBand11To12: return "11-12";
                default: return "8-10";
            }
        }

        /// <summary>Descriptive ASCII label for the age band.</summary>
        public static string AgeBandDescription(int band)
        {
            switch (band)
            {
                case AgeBand5To7: return "5-7: Concrete & Visual";
                case AgeBand11To12: return "11-12: Deductive & Multi-step";
                default: return "8-10: Conceptual & Guided";
            }
        }
    }
}
