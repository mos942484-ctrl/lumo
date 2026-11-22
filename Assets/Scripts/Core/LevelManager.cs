using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Unlocking rules (§7): completing a level unlocks the next regardless of stars;
    /// Level 13 opens only after all 12 are complete (no star requirement).
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only — destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
        }

        /// <summary>Sequential story-path unlock rule (level 13 after 1..12 complete).</summary>
        public bool IsStoryUnlocked(int levelId)
        {
            if (levelId < 1 || levelId > 13) return false;
            if (levelId == 13)
            {
                for (int i = 1; i <= 12; i++)
                    if (!SaveManager.Instance.Record(i).completed) return false;
                return true;
            }
            return levelId <= SaveManager.Instance.Data.highestUnlocked;
        }

        public bool IsUnlocked(int levelId)
        {
            if (levelId < 1 || levelId > 13) return false;
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null && SaveManager.Instance.Data.freeExplore)
                return true;
            return IsStoryUnlocked(levelId);
        }

        public void MarkCompleted(int levelId)
        {
            // Batch 5 fix: refuse an out-of-range level id.
            if (levelId < 1 || levelId > 13)
            {
                Debug.LogWarning("[LevelManager] MarkCompleted ignored: level " + levelId + " out of range 1..13");
                return;
            }
            var data = SaveManager.Instance.Data;
            if (levelId < 13 && levelId + 1 > data.highestUnlocked)
            {
                data.highestUnlocked = levelId + 1;
                Debug.Log("[LevelManager] unlocked level " + data.highestUnlocked);
            }
            if (levelId == 13)
                Debug.Log("[LevelManager] final level completed — Kingdom saved!");
            SaveManager.Instance.Save();
        }

        public int HighestUnlocked { get { return SaveManager.Instance.Data.highestUnlocked; } }

        /// <summary>First uncompleted level (for Continue).
        /// Batch 2 fix: never return a locked level — with a corrupt/inconsistent
        /// save the first uncompleted level could sit past highestUnlocked, and
        /// StartLevel would (correctly) refuse it. Fall back to the highest
        /// unlocked level in that case.</summary>
        public int ContinueLevel()
        {
            for (int i = 1; i <= 13; i++)
                if (!ScoreManager.Instance.LevelCompleted(i) && IsUnlocked(i)) return i;
            return Mathf.Clamp(SaveManager.Instance.Data.highestUnlocked, 1, 13);
        }
    }
}
