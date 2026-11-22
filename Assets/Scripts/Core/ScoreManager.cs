using System.Collections.Generic;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Stars, XP and badges (§6.3). XP: +10 per challenge only when that round's
    /// stars improve on the previous best, +20 per level (+10 no-hint bonus) only
    /// on first completion (Batch 2 fix — replays earn no XP/badges). Stars come
    /// from GameRules and never exceed DynamicTotalStars by construction
    /// (6 rounds × 12 levels + 15 final tasks × 3 = 261).
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public struct LevelOutcome
        {
            public int StarsThisRun;
            public int XpEarned;
            public string[] NewBadges;
            public bool PerfectNoHint;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only — destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
        }

        /// <summary>Records one solved challenge: stores best stars per round.
        /// Batch 2 fix: challenge XP (+10) is awarded only when the stars for this
        /// round improve on the previous best — replaying a level can no longer
        /// farm XP round after round.</summary>
        public void RecordChallenge(int levelId, int roundIdx, int stars, bool firstTry, bool hinted)
        {
            // Batch 5 fix: refuse an out-of-range level id (SaveManager.Record
            // also guards, but it returns null there — never reach it).
            if (levelId < 1 || levelId > 13)
            {
                Debug.LogWarning("[Score] RecordChallenge ignored: level " + levelId + " out of range 1..13");
                return;
            }
            var rec = SaveManager.Instance.Record(levelId);
            if (roundIdx < 0 || roundIdx >= rec.stars.Length)
            {
                Debug.LogWarning("[Score] RecordChallenge ignored: round " + roundIdx + " out of range for level " + levelId);
                return;
            }
            int prevBest = rec.stars[roundIdx];
            int newBest = Mathf.Max(prevBest, stars);
            rec.stars[roundIdx] = newBest;
            var data = SaveManager.Instance.Data;
            if (newBest > prevBest) data.totalXp += GameRules.XpPerChallenge;
            RecomputeTotalStars();
            SaveManager.Instance.Save();
            Debug.Log("[Score] L" + levelId + " R" + (roundIdx + 1) + " stars=" + stars + " firstTry=" + firstTry + " hinted=" + hinted + " totalStars=" + data.totalStars);
        }

        /// <summary>Called when all rounds of a level are complete: level XP, no-hint bonus, badges.
        /// Batch 2 fix: level XP, the no-hint bonus and badges are awarded only on
        /// first completion — replaying a finished level earns nothing (bestScore
        /// and best stars are still updated).</summary>
        public LevelOutcome CompleteLevel(int levelId, bool[] roundHinted, int runStars = -1)
        {
            // Batch 5 fix: refuse an out-of-range level id before touching the save.
            if (levelId < 1 || levelId > 13)
            {
                Debug.LogWarning("[Score] CompleteLevel ignored: level " + levelId + " out of range 1..13");
                return new LevelOutcome { StarsThisRun = 0, XpEarned = 0, NewBadges = new string[0], PerfectNoHint = false };
            }
            var data = SaveManager.Instance.Data;
            var rec = SaveManager.Instance.Record(levelId);
            bool firstCompletion = !rec.completed;
            rec.completed = true;
            int xp = 0;
            bool perfect = true;
            for (int i = 0; i < roundHinted.Length; i++) if (roundHinted[i]) perfect = false;
            if (firstCompletion)
            {
                xp = GameRules.XpPerLevel;
                if (perfect) xp += GameRules.XpNoHintBonus;
                data.totalXp += xp;
            }

            int levelTotal = SaveManager.SumStars(rec);
            rec.bestScore = Mathf.Max(rec.bestScore, levelTotal);

            var newBadges = new List<string>();
            if (firstCompletion)
            {
                void Award(string badge)
                {
                    var list = new List<string>(data.badges);
                    if (!list.Contains(badge)) { list.Add(badge); data.badges = list.ToArray(); newBadges.Add(badge); }
                }
                if (AllCompleted(new[] { 1, 4, 7, 10 })) Award("Math Master");
                if (AllCompleted(new[] { 2, 5, 8, 11 })) Award("Word Explorer");
                if (AllCompleted(new[] { 3, 6, 9, 12 })) Award("Puzzle Genius");
                if (LevelCompleted(13)) Award("Kingdom Hero");
            }

            RecomputeTotalStars();
            LevelManager.Instance.MarkCompleted(levelId);
            SaveManager.Instance.Save();

            return new LevelOutcome
            {
                StarsThisRun = runStars >= 0 ? runStars : levelTotal,
                XpEarned = xp,
                NewBadges = newBadges.ToArray(),
                PerfectNoHint = perfect
            };
        }

        public bool LevelCompleted(int id)
        {
            if (id < 1 || id > 13) return false;
            return SaveManager.Instance.Record(id).completed;
        }

        public bool AllCompleted(int[] ids)
        {
            foreach (var id in ids) if (!LevelCompleted(id)) return false;
            return true;
        }

        /// <summary>Total stars = sum of best stars across all levels (capped at DynamicTotalStars).</summary>
        public void RecomputeTotalStars()
        {
            int total = 0;
            for (int i = 0; i < 13; i++)
            {
                var r = SaveManager.Instance.Data.levelRecords[i];
                if (r != null && r.stars != null)
                    for (int j = 0; j < r.stars.Length; j++) total += r.stars[j];
            }
            SaveManager.Instance.Data.totalStars = Mathf.Min(total, GameRules.DynamicTotalStars());
        }

        public int ShardsCollected()
        {
            int n = 0;
            for (int i = 1; i <= 12; i++) if (LevelCompleted(i)) n++;
            return n;
        }
    }
}
