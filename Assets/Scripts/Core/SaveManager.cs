using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lumo.Core
{
    [Serializable]
    public class LevelRecord
    {
        public bool completed;
        public int[] stars = new int[3];   // per round (0..3)
        public int bestScore;
    }

    [Serializable]
    public class SkillStats
    {
        public int firstTryStreak;   // consecutive first-try correct answers
        public int struggleStreak;   // consecutive wrong-or-hinted answers
        public int mastery;          // 0 Learning, 1 Practicing, 2 Good, 3 Mastered
        public int firstTryCorrect;
        public int attempts;
        public bool pendingHarder;   // mastery just leveled up -> next challenge uses harder variant
        public bool remediation;     // struggling -> easier variants until clean
        public int cleanStreak;      // consecutive clean answers during remediation
    }

    [Serializable]
    public class SaveData
    {
        // Batch 2 fix: save format version. Old saves (no field) load as 0 and are
        // stamped CurrentVersion by Normalize; bump when the shape changes.
        // Batch 3: v2 adds reviewSolved (per-word LevelsSolved, "word|level").
        // Phase 2: v4 expands skills (8 -> 13) and dynamic per-level round stars (6 per L1..12, 15 for L13).
        public const int CurrentVersion = 4;
        public int version = CurrentVersion;
        public int avatar = 1;                 // 1 = Hero A, 2 = Hero B
        public bool tutorialDone;
        public int highestUnlocked = 1;        // 1..13
        public LevelRecord[] levelRecords;     // index 0..12 -> level 1..13
        public SkillStats[] skills;            // index maps to bank.skills order
        public int totalStars;
        public int totalXp;
        public string[] badges = new string[0];
        // LXD Phase C3: Mastery Medals - skill ids that reached Mastered (mastery == 3)
        // for the first time. Unlocked by learning, not play count; cleared with progress.
        public string[] medals = new string[0];
        public string[] reviewWords = new string[0];
        // Batch 3 fix: per-word LevelsSolved for word re-drill retirement,
        // stored as "word|level" entries. Old (v1) saves lack the field and
        // load it as null - Normalize dedupes it to an empty array, so no
        // existing save breaks.
        public string[] reviewSolved = new string[0];
        public bool musicOn = true;
        public bool soundOn = true;
        // Batch 4 fix: separate voice-over switch; Sound stays the master.
        // Old saves lack the field, so JsonUtility leaves the default true.
        public bool voiceOn = true;
        public bool practiceMode;
        public int lastLevel = 1;
        public int lastRound = 1;

        // Wonder Isles Phase 1 fields (flat primitives/arrays for JsonUtility)
        public bool calmMode = false;
        public bool timedMode = false;
        public bool freeExplore = true;
        public int ageBand = 1;
        public string parentPin = "";
        public int dailyLimitMinutes = 0;
        public int extraMinutesToday = 0;
        public int totalSecondsPlayed = 0;
        public int todaySecondsPlayed = 0;
        public string lastPlayDate = "";
        public int[] islandSeconds = new int[13];
        // LXD Phase D3: child reflection on the result screen ("What did you like most?").
        // Parallel arrays (JsonUtility-friendly): level ids that have a recorded feel and
        // its value (0 = "So fun!", 1 = "Tricky but I did it!"). Latest wins.
        public int[] levelFeelIds = new int[0];
        public int[] levelFeelVals = new int[0];
        // LXD Phase D5: parent-picked focus words (max 5) served before random review words.
        public string[] focusWords = new string[0];
    }

    /// <summary>Local, offline save at persistentDataPath/lumo_save.json (Sec 6.6).</summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        public SaveData Data { get; private set; }

        // Stage 1.1: exposed publicly so the AutoTestDriver can snapshot/restore the
        // raw save files around a full 13-level run (and Reload() re-reads them).
        public string SavePath { get { return Application.persistentDataPath + "/lumo_save.json"; } }
        public string TempPath { get { return SavePath + ".tmp"; } }
        public string BackupPath { get { return SavePath + ".bak"; } }

        /// <summary>Stage 1.1: re-reads the save from disk (e.g. after the test driver restored the original files).</summary>
        public void Reload() { Data = Load(); }

        /// <summary>Batch 2 fix: set when the last Load had to fall back (backup or fresh save).</summary>
        public bool LastLoadFailed { get; private set; }
        public string LastLoadError { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only - destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (Data == null) Data = Load();
            Debug.Log("[SaveManager] ready at " + SavePath + " (highestUnlocked=" + Data.highestUnlocked + ", stars=" + Data.totalStars + ")");
        }

        // Batch 2 fix: on load failure try the .bak backup before starting fresh,
        // and surface the failure (log + LastLoadFailed/LastLoadError) instead of
        // silently discarding the child's progress.
        public SaveData Load()
        {
            LastLoadFailed = false;
            LastLoadError = null;
            SaveData d = TryLoad(SavePath);
            if (d != null) return Normalize(d);
            if (File.Exists(SavePath))
            {
                Debug.LogWarning("[SaveManager] main save unreadable, trying backup");
                d = TryLoad(BackupPath);
                if (d != null)
                {
                    LastLoadFailed = true;
                    LastLoadError = "Main save unreadable; restored from backup.";
                    Debug.LogWarning("[SaveManager] " + LastLoadError);
                    return Normalize(d);
                }
            }
            if (File.Exists(SavePath) || File.Exists(BackupPath))
            {
                LastLoadFailed = true;
                LastLoadError = "Save and backup both unreadable; starting fresh.";
                Debug.LogError("[SaveManager] " + LastLoadError);
            }
            return Normalize(null);
        }

        static SaveData TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveManager] load failed for " + path + ": " + e.Message);
                return null;
            }
        }

        public static int SkillCount
        {
            get
            {
                if (GameManager.Instance != null && GameManager.Instance.Bank != null &&
                    GameManager.Instance.Bank.skills != null && GameManager.Instance.Bank.skills.Length > 0)
                    return GameManager.Instance.Bank.skills.Length;
                return QuestionManager.ValidSkills.Length;
            }
        }

        public static int RoundsForLevel(int levelId)
        {
            if (GameManager.Instance != null && GameManager.Instance.Bank != null &&
                GameManager.Instance.Bank.levels != null &&
                levelId >= 1 && levelId <= GameManager.Instance.Bank.levels.Length)
            {
                var ch = GameManager.Instance.Bank.levels[levelId - 1].challenges;
                if (ch != null && ch.Length > 0) return ch.Length;
            }
            return levelId == 13 ? 15 : 6;
        }

        public static int SumStars(LevelRecord rec)
        {
            if (rec == null || rec.stars == null) return 0;
            int sum = 0;
            for (int i = 0; i < rec.stars.Length; i++) sum += rec.stars[i];
            return sum;
        }

        // Batch 2 fix: normalise a loaded (or about-to-be-saved) save - clamp every
        // scalar to a sane range, adjust stars arrays to the rounds for
        // that level, dedupe badges/reviewWords, clamp per-round stars, and
        // recompute totalStars from the stars arrays so it can never drift.
        // SkillStats stay positional by bank.skills order (expanded v3 -> v4 from 8 to 13).
        public static SaveData Normalize(SaveData d)
        {
            if (d == null) return NewSave();
            if (d.version < 3)
            {
                d.freeExplore = true;
                d.ageBand = GameRules.AgeBand8To10;
                d.timedMode = false;
                d.calmMode = false;
            }
            if (d.version < SaveData.CurrentVersion)
                d.version = SaveData.CurrentVersion;
            if (d.version > SaveData.CurrentVersion)
                Debug.LogWarning("[SaveManager] save version " + d.version + " is newer than supported " + SaveData.CurrentVersion + "; loading as-is");
            d.avatar = Mathf.Clamp(d.avatar, 1, 2);
            d.highestUnlocked = Mathf.Clamp(d.highestUnlocked, 1, 13);
            d.lastLevel = Mathf.Clamp(d.lastLevel, 1, 13);
            d.lastRound = Mathf.Clamp(d.lastRound, 1, RoundsForLevel(d.lastLevel));
            d.totalXp = Mathf.Max(0, d.totalXp);
            d.ageBand = Mathf.Clamp(d.ageBand, GameRules.AgeBand5To7, GameRules.AgeBand11To12);
            d.dailyLimitMinutes = Mathf.Clamp(d.dailyLimitMinutes, 0, 240);
            d.extraMinutesToday = Mathf.Clamp(d.extraMinutesToday, 0, 240);
            d.totalSecondsPlayed = Mathf.Max(0, d.totalSecondsPlayed);
            d.todaySecondsPlayed = Mathf.Max(0, d.todaySecondsPlayed);
            d.parentPin = IsValidPin(d.parentPin) ? d.parentPin : "";
            if (d.islandSeconds == null || d.islandSeconds.Length != 13)
            {
                var secs = new int[13];
                if (d.islandSeconds != null)
                    Array.Copy(d.islandSeconds, secs, Mathf.Min(13, d.islandSeconds.Length));
                d.islandSeconds = secs;
            }
            for (int i = 0; i < 13; i++)
                d.islandSeconds[i] = Mathf.Max(0, d.islandSeconds[i]);
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (d.lastPlayDate != today)
            {
                d.lastPlayDate = today;
                d.todaySecondsPlayed = 0;
                d.extraMinutesToday = 0;
            }
            if (d.levelRecords == null || d.levelRecords.Length != 13)
            {
                var resized = new LevelRecord[13];
                if (d.levelRecords != null)
                    for (int i = 0; i < Mathf.Min(13, d.levelRecords.Length); i++) resized[i] = d.levelRecords[i];
                d.levelRecords = resized;
            }
            for (int i = 0; i < 13; i++)
            {
                int needed = RoundsForLevel(i + 1);
                if (d.levelRecords[i] == null) d.levelRecords[i] = new LevelRecord { stars = new int[needed] };
                else if (d.levelRecords[i].stars == null || d.levelRecords[i].stars.Length != needed)
                {
                    var old = d.levelRecords[i].stars;
                    var arr = new int[needed];
                    if (old != null) Array.Copy(old, arr, Mathf.Min(needed, old.Length));
                    d.levelRecords[i].stars = arr;
                }
                // Clamp per-round stars (0..3) and bestScore to the level maximum.
                for (int r = 0; r < needed; r++)
                    d.levelRecords[i].stars[r] = Mathf.Clamp(d.levelRecords[i].stars[r], 0, GameRules.MaxStarsPerChallenge);
                d.levelRecords[i].bestScore = Mathf.Clamp(d.levelRecords[i].bestScore, 0, needed * GameRules.MaxStarsPerChallenge);
            }
            int skillCount = SkillCount;
            if (d.skills == null || d.skills.Length != skillCount)
            {
                var skills = new SkillStats[skillCount];
                if (d.skills != null)
                    for (int i = 0; i < Mathf.Min(skillCount, d.skills.Length); i++) skills[i] = d.skills[i];
                d.skills = skills;
            }
            for (int i = 0; i < skillCount; i++)
            {
                if (d.skills[i] == null) d.skills[i] = new SkillStats();
                var s = d.skills[i];
                s.mastery = Mathf.Clamp(s.mastery, 0, 3);
                s.firstTryStreak = Mathf.Max(0, s.firstTryStreak);
                s.struggleStreak = Mathf.Max(0, s.struggleStreak);
                s.cleanStreak = Mathf.Max(0, s.cleanStreak);
                s.attempts = Mathf.Max(0, s.attempts);
                s.firstTryCorrect = Mathf.Clamp(s.firstTryCorrect, 0, s.attempts);
            }
            d.badges = Dedupe(d.badges);
            d.medals = Dedupe(d.medals); // LXD Phase C3
            d.reviewWords = Dedupe(d.reviewWords);
            d.reviewSolved = Dedupe(d.reviewSolved);
            d.focusWords = Dedupe(d.focusWords); // LXD Phase D5 (case/dedupe; focus logic applies its own cap)
            if (d.focusWords != null && d.focusWords.Length > 5)
            {
                var capped = new string[5];
                Array.Copy(d.focusWords, capped, 5);
                d.focusWords = capped;
            }
            NormalizeLevelFeels(d); // LXD Phase D3
            // Recompute totalStars from the (now clamped/truncated) stars arrays.
            int total = 0;
            for (int i = 0; i < 13; i++) total += SumStars(d.levelRecords[i]);
            d.totalStars = Mathf.Min(total, GameRules.DynamicTotalStars());
            return d;
        }

        static string[] Dedupe(string[] values)
        {
            if (values == null) return new string[0];
            var seen = new System.Collections.Generic.HashSet<string>();
            var outList = new System.Collections.Generic.List<string>();
            foreach (var v in values)
            {
                if (string.IsNullOrEmpty(v)) continue;
                if (seen.Add(v)) outList.Add(v);
            }
            return outList.ToArray();
        }

        /// <summary>LXD Phase D3: keeps the parallel reflection arrays paired, clamped and
        /// deduped by level id (latest entry wins).</summary>
        static void NormalizeLevelFeels(SaveData d)
        {
            int n = Mathf.Min(d.levelFeelIds != null ? d.levelFeelIds.Length : 0,
                              d.levelFeelVals != null ? d.levelFeelVals.Length : 0);
            var ids = new List<int>();
            var vals = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int id = d.levelFeelIds[i];
                int val = d.levelFeelVals[i];
                if (id < 1 || id > 13) continue;
                val = Mathf.Clamp(val, 0, 1);
                int at = ids.IndexOf(id);
                if (at >= 0) { ids.RemoveAt(at); vals.RemoveAt(at); } // latest wins
                ids.Add(id); vals.Add(val);
            }
            d.levelFeelIds = ids.ToArray();
            d.levelFeelVals = vals.ToArray();
        }

        /// <summary>LXD Phase D3: records the child's reflection for a level (latest wins).</summary>
        public static void ApplyLevelFeel(SaveData d, int levelId, int feel)
        {
            if (d == null || levelId < 1 || levelId > 13) return;
            var ids = new List<int>(d.levelFeelIds != null ? d.levelFeelIds : new int[0]);
            var vals = new List<int>(d.levelFeelVals != null ? d.levelFeelVals : new int[0]);
            int at = ids.IndexOf(levelId);
            if (at >= 0) { ids.RemoveAt(at); vals.RemoveAt(at); }
            ids.Add(levelId);
            vals.Add(Mathf.Clamp(feel, 0, 1));
            d.levelFeelIds = ids.ToArray();
            d.levelFeelVals = vals.ToArray();
        }

        /// <summary>LXD Phase D3: the most recent reflection as "So fun!" / "Tricky but I did it!" or null.</summary>
        public static string LastLevelFeel(SaveData d, out int levelId)
        {
            levelId = -1;
            if (d == null || d.levelFeelIds == null || d.levelFeelIds.Length == 0) return null;
            int last = d.levelFeelIds.Length - 1;
            levelId = d.levelFeelIds[last];
            return d.levelFeelVals[last] == 1 ? "Tricky but I did it!" : "So fun!";
        }

        // Batch 2 fix: atomic save - write to .tmp, then File.Replace swaps it over
        // the real file and keeps the previous copy as .bak, so a crash mid-write
        // can never leave a half-written save. A failed write logs an error and
        // leaves the previous good save in place.
        public void Save()
        {
            if (Data == null) { Debug.LogError("[SaveManager] save aborted: no data"); return; }
            try
            {
                Normalize(Data);
                string json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(TempPath, json);
                if (File.Exists(SavePath))
                    File.Replace(TempPath, SavePath, BackupPath);
                else
                    File.Move(TempPath, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveManager] write failed: " + e.Message);
                try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch (Exception) { }
            }
        }

        public static bool IsValidPin(string pin)
        {
            if (string.IsNullOrEmpty(pin) || pin.Length != 4) return false;
            for (int i = 0; i < 4; i++)
                if (pin[i] < '0' || pin[i] > '9') return false;
            return true;
        }

        public static SaveData NewSave()
        {
            var d = new SaveData();
            d.levelRecords = new LevelRecord[13];
            for (int i = 0; i < 13; i++)
                d.levelRecords[i] = new LevelRecord { stars = new int[RoundsForLevel(i + 1)] };
            int skillCount = SkillCount;
            d.skills = new SkillStats[skillCount];
            for (int i = 0; i < skillCount; i++) d.skills[i] = new SkillStats();
            d.islandSeconds = new int[13];
            d.lastPlayDate = DateTime.Now.ToString("yyyy-MM-dd");
            return d;
        }

        /// <summary>Resets all learning progress while keeping parent settings, audio toggles, age band, and time stats.</summary>
        public void ResetProgressKeepSettings()
        {
            if (Data == null) Data = NewSave();
            Data.tutorialDone = false;
            Data.highestUnlocked = 1;
            Data.levelRecords = new LevelRecord[13];
            for (int i = 0; i < 13; i++)
                Data.levelRecords[i] = new LevelRecord { stars = new int[RoundsForLevel(i + 1)] };
            int skillCount = SkillCount;
            Data.skills = new SkillStats[skillCount];
            for (int i = 0; i < skillCount; i++) Data.skills[i] = new SkillStats();
            Data.totalStars = 0;
            Data.totalXp = 0;
            Data.badges = new string[0];
            Data.medals = new string[0]; // LXD Phase C3: medals are learning progress - reset with it
            Data.reviewWords = new string[0];
            Data.reviewSolved = new string[0];
            Data.lastLevel = 1;
            Data.lastRound = 1;
            Data.islandSeconds = new int[13];
            Data.levelFeelIds = new int[0]; // LXD Phase D3: reflections are child progress
            Data.levelFeelVals = new int[0];
            if (AdaptiveManager.Instance != null) AdaptiveManager.Instance.ClearSession();
            Save();
            Debug.Log("[SaveManager] progress reset (settings kept)");
        }

        public void ResetSave()
        {
            Data = NewSave();
            try { if (File.Exists(BackupPath)) File.Delete(BackupPath); } catch (Exception) { }
            Save();
            if (AdaptiveManager.Instance != null) AdaptiveManager.Instance.ClearSession();
            Debug.Log("[SaveManager] save reset");
        }

        // Batch 5 fix: guard the Record/Skill paths - an out-of-range id/index
        // used to index straight into the arrays (IndexOutOfRange). Callers
        // that take a level id from outside the bank guard it first and never
        // rely on this returning a record.
        public LevelRecord Record(int levelId)
        {
            if (levelId < 1 || levelId > 13)
            {
                Debug.LogError("[SaveManager] Record refused: level id " + levelId + " out of range 1..13");
                return null;
            }
            return Data.levelRecords[levelId - 1];
        }
        public SkillStats Skill(int index)
        {
            int count = SkillCount;
            if (index < 0 || index >= count || Data == null || Data.skills == null || index >= Data.skills.Length)
            {
                Debug.LogError("[SaveManager] Skill refused: index " + index + " out of range 0.." + (count - 1));
                return null;
            }
            return Data.skills[index];
        }
    }
}
