using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Rule-based adaptive difficulty, per skill (§6.4, no ML):
    /// - 3 first-try correct in a row → mastery level up (Learning→Practicing→Good→Mastered)
    ///   and the NEXT challenge on that skill comes as a HARDER variant.
    /// - 2 wrong-or-hinted answers in a row → remediation: EASIER variants until
    ///   2 clean (first-try, no hint) answers in a row, then normal resumes.
    /// Also tracks the word re-drill review list (§6.5): words return ~two challenges
    /// later in the same session and open the next English level as a review round;
    /// a word leaves the list after two first-try correct answers in two different levels.
    /// </summary>
    public class AdaptiveManager : MonoBehaviour
    {
        public static AdaptiveManager Instance { get; private set; }

        // LXD Phase C2: fired when a skill's first-try streak hits a multiple of 3
        // (visible behavioural momentum). Review/practice filtering happens in the
        // subscriber (GameManager knows the round context).
        public event Action<int> OnStreakMilestone;
        // LXD Phase C3: fired the first time a skill reaches Mastered (mastery == 3).
        public event Action<string> OnMedalEarned;

        // session-scoped review tracking (word -> levels where solved first-try)
        class ReviewEntry { public List<int> LevelsSolved = new List<int>(); }
        readonly Dictionary<string, ReviewEntry> _review = new Dictionary<string, ReviewEntry>();
        readonly List<string> _reviewOrder = new List<string>();
        int _challengesSinceLastReview = 99;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only — destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
        }

        public static int SkillIndex(LevelBank bank, string skill)
        {
            for (int i = 0; i < bank.skills.Length; i++)
                if (bank.skills[i] == skill) return i;
            return -1;
        }

        /// <summary>Records one answered challenge against its skill stats.</summary>
        public void RecordAnswer(LevelBank bank, ChallengeData c, bool firstTry, bool usedHint)
        {
            int idx = SkillIndex(bank, c.skill);
            if (idx < 0 || SaveManager.Instance == null) return;
            var s = SaveManager.Instance.Skill(idx);
            s.attempts++;

            if (firstTry && !usedHint)
            {
                s.firstTryCorrect++;
                s.firstTryStreak++;
                s.struggleStreak = 0;
                if (s.remediation) s.cleanStreak++;
                if (s.cleanStreak >= 2) { s.remediation = false; s.cleanStreak = 0; Debug.Log("[Adaptive] " + c.skill + ": remediation complete"); }
                bool leveledUpNow = false;
                bool masteredNow = false;
                if (s.firstTryStreak >= 3 && s.mastery < 3)
                {
                    s.mastery++;
                    s.pendingHarder = true;
                    s.firstTryStreak = 0;
                    leveledUpNow = true;
                    masteredNow = s.mastery == 3;
                    Debug.Log("[Adaptive] " + c.skill + ": mastery up to " + s.mastery + " — next challenge harder");
                }
                // LXD Phase C2: the streak consumed by a mastery-up still counts as the
                // "3 in a row" milestone; afterwards the surviving streak fires at 6, 9, ...
                int shown = leveledUpNow ? 3 : s.firstTryStreak;
                if (shown > 0 && shown % 3 == 0 && OnStreakMilestone != null) OnStreakMilestone(shown);
                // LXD Phase C3: Mastery Medal on the FIRST time a skill reaches Mastered.
                if (masteredNow) AwardMedal(c.skill);
            }
            else
            {
                s.struggleStreak++;
                s.firstTryStreak = 0;
                s.cleanStreak = 0;
                if (s.struggleStreak >= 2)
                {
                    s.struggleStreak = 0;
                    s.remediation = true;
                    Debug.Log("[Adaptive] " + c.skill + ": remediation started");
                }
            }
            SaveManager.Instance.Save();

            // word re-drill bookkeeping (English words only)
            if (c.words != null && c.words.Length > 0)
            {
                if (!firstTry || usedHint)
                    foreach (var w in c.words) AddReviewWord(w);
                else
                    foreach (var w in c.words) MarkWordClean(w, c.level);
            }
        }

        /// <summary>Returns the challenge to present based on age band and adaptive mastery.</summary>
        public ChallengeData SelectChallenge(LevelBank bank, ChallengeData baseChallenge)
        {
            int idx = SkillIndex(bank, baseChallenge.skill);
            if (idx < 0 || SaveManager.Instance == null) return baseChallenge;
            var s = SaveManager.Instance.Skill(idx);
            int band = SaveManager.Instance.Data != null ? SaveManager.Instance.Data.ageBand : GameRules.AgeBand8To10;

            if (band == GameRules.AgeBand5To7)
            {
                if (s.pendingHarder)
                {
                    s.pendingHarder = false;
                    SaveManager.Instance.Save();
                    return baseChallenge;
                }
                var easier = QuestionManager.GetVariant(bank, baseChallenge.band5to7)
                          ?? QuestionManager.GetVariant(bank, baseChallenge.easier);
                return easier ?? baseChallenge;
            }

            if (band == GameRules.AgeBand11To12)
            {
                if (s.pendingHarder)
                {
                    s.pendingHarder = false;
                    SaveManager.Instance.Save();
                }
                if (s.remediation) return baseChallenge;
                var harder = QuestionManager.GetVariant(bank, baseChallenge.band11to12)
                          ?? QuestionManager.GetVariant(bank, baseChallenge.harder);
                return harder ?? baseChallenge;
            }

            // Ages 8-10 (default band): base with remediation -> easier, mastery up -> harder.
            if (s.remediation && (!string.IsNullOrEmpty(baseChallenge.easier) || !string.IsNullOrEmpty(baseChallenge.band5to7)))
            {
                var v = QuestionManager.GetVariant(bank, baseChallenge.easier)
                     ?? QuestionManager.GetVariant(bank, baseChallenge.band5to7);
                if (v != null) { Debug.Log("[Adaptive] using easier variant " + v.id + " for " + baseChallenge.id); return v; }
            }
            if (s.pendingHarder)
            {
                var v = QuestionManager.GetVariant(bank, baseChallenge.harder)
                     ?? QuestionManager.GetVariant(bank, baseChallenge.band11to12);
                if (v != null)
                {
                    s.pendingHarder = false;
                    SaveManager.Instance.Save();
                    Debug.Log("[Adaptive] using harder variant " + v.id + " for " + baseChallenge.id);
                    return v;
                }
                // Batch 3 fix: clear pendingHarder even when no variant is found
                // (unlinked challenge, or a dangling harder id), so a mastery
                // level-up is never silently lost on the next challenge.
                s.pendingHarder = false;
                SaveManager.Instance.Save();
                Debug.Log("[Adaptive] harder variant '" + baseChallenge.harder + "' not found for " + baseChallenge.id + " — pendingHarder cleared");
            }
            return baseChallenge;
        }

        // ---- Word re-drill (§6.5) ----
        /// <summary>L11 function words (i/see/a/the/is/can/run) have no pictures and
        /// are not in GameManager.ReviewableWords, so they could never retire —
        /// never queue them for re-drill.</summary>
        static readonly HashSet<string> FunctionWords = new HashSet<string> {
            "i", "see", "a", "the", "is", "can", "run"
        };

        /// <summary>LXD Phase D5: function words can never be re-drilled or focused.</summary>
        public static bool IsFunctionWord(string word)
        {
            return !string.IsNullOrEmpty(word) && FunctionWords.Contains(word.ToLower());
        }

        // ---- LXD Phase D5: parent focus words ----
        /// <summary>The parent's focus words (max 5, normalized lowercase) or empty.</summary>
        public List<string> FocusWords
        {
            get
            {
                var outList = new List<string>();
                if (SaveManager.Instance == null || SaveManager.Instance.Data == null ||
                    SaveManager.Instance.Data.focusWords == null) return outList;
                foreach (var w in SaveManager.Instance.Data.focusWords)
                    if (!string.IsNullOrEmpty(w)) outList.Add(w.ToLower());
                return outList;
            }
        }

        /// <summary>A word is "mastered" when it has clean first-try solves recorded in 2 different
        /// levels (same evidence the re-drill queue uses to retire words).</summary>
        public static bool IsWordMastered(string[] reviewSolved, string word)
        {
            if (reviewSolved == null || string.IsNullOrEmpty(word)) return false;
            var levels = new HashSet<string>();
            foreach (var raw in reviewSolved)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                var parts = raw.Split('|');
                if (parts.Length == 2 && parts[0] == word) levels.Add(parts[1]);
            }
            return levels.Count >= 2;
        }

        /// <summary>Static core of the focus-first serve (pure, testable): scans the re-drill
        /// order and returns the first eligible word, preferring focus words; null when none.</summary>
        public static string PickReviewWord(List<string> order, List<string> focusWords, System.Func<string, bool> isEligible)
        {
            if (order == null || order.Count == 0) return null;
            string fallback = null;
            for (int i = 0; i < order.Count; i++)
            {
                var w = order[i];
                if (isEligible != null && !isEligible(w)) continue;
                if (focusWords != null && focusWords.Contains(w)) return w;
                if (fallback == null) fallback = w;
            }
            return fallback;
        }

        /// <summary>First unmastered, review-eligible focus word for the level-start injection
        /// (only used when the re-drill queue is empty — D5 "may inject one focus-word round").</summary>
        public string PickUnmasteredFocusWord(System.Func<string, bool> isEligible)
        {
            var focus = FocusWords;
            if (focus.Count == 0) return null;
            var solved = SaveManager.Instance != null && SaveManager.Instance.Data != null
                ? SaveManager.Instance.Data.reviewSolved : null;
            foreach (var w in focus)
            {
                if (isEligible != null && !isEligible(w)) continue;
                if (!IsWordMastered(solved, w)) return w;
            }
            return null;
        }

        public void AddReviewWord(string word)
        {
            word = word.ToLower();
            if (string.IsNullOrEmpty(word)) return;
            // Batch 3 fix: function words can never be re-drilled (no picture).
            if (FunctionWords.Contains(word)) return;
            // Batch 3 fix: reset the "~two challenges later" counter when a word
            // is queued — it started at 99 and was never reset, so the review
            // fired immediately instead of ~2 challenges after the struggle.
            _challengesSinceLastReview = 0;
            if (!_review.ContainsKey(word))
            {
                _review[word] = new ReviewEntry();
                _reviewOrder.Add(word);
                Debug.Log("[Review] word added: " + word);
            }
            var persistent = new List<string>(SaveManager.Instance.Data.reviewWords);
            if (!persistent.Contains(word)) { persistent.Add(word); SaveManager.Instance.Data.reviewWords = persistent.ToArray(); SaveManager.Instance.Save(); }
        }

        void MarkWordClean(string word, int levelId)
        {
            word = word.ToLower();
            ReviewEntry e;
            if (_review.TryGetValue(word, out e))
            {
                if (!e.LevelsSolved.Contains(levelId)) e.LevelsSolved.Add(levelId);
                if (e.LevelsSolved.Count >= 2)
                {
                    _review.Remove(word);
                    _reviewOrder.Remove(word);
                    var persistent = new List<string>(SaveManager.Instance.Data.reviewWords);
                    persistent.Remove(word);
                    SaveManager.Instance.Data.reviewWords = persistent.ToArray();
                    Debug.Log("[Review] word retired: " + word);
                }
                // Batch 3 fix: persist per-word LevelsSolved so a word's progress
                // toward retirement survives a restart (was session-only).
                PersistReviewSolved();
            }
            else if (FocusWords.Contains(word))
            {
                // LXD Phase D5: a clean solve of a parent focus word counts toward
                // its mastering evidence ("word|level" in reviewSolved) even when it
                // was never in the struggle queue.
                var solved = new List<string>(SaveManager.Instance.Data.reviewSolved);
                var entry = word + "|" + levelId;
                if (!solved.Contains(entry)) solved.Add(entry);
                SaveManager.Instance.Data.reviewSolved = solved.ToArray();
                SaveManager.Instance.Save();
            }
        }

        /// <summary>Batch 3 fix: writes "word|level" entries for every queued word's
        /// LevelsSolved into SaveData.reviewSolved (deduped by Normalize on load).</summary>
        void PersistReviewSolved()
        {
            if (SaveManager.Instance == null) return;
            var list = new List<string>();
            foreach (var kv in _review)
                foreach (int lv in kv.Value.LevelsSolved)
                    list.Add(kv.Key + "|" + lv);
            SaveManager.Instance.Data.reviewSolved = list.ToArray();
            SaveManager.Instance.Save();
        }

        public void HydrateFromSave()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            var saved = SaveManager.Instance.Data.reviewWords;
            if (saved != null)
            {
                foreach (var raw in saved)
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    string word = raw.ToLower();
                    // Batch 3 fix: function words queued by older saves can never
                    // retire — skip them on hydrate too.
                    if (FunctionWords.Contains(word)) continue;
                    if (!_review.ContainsKey(word))
                    {
                        _review[word] = new ReviewEntry();
                        _reviewOrder.Add(word);
                    }
                }
            }
            // Batch 3 fix: restore persisted per-word LevelsSolved ("word|level").
            var solved = SaveManager.Instance.Data.reviewSolved;
            if (solved == null) return;
            foreach (var raw in solved)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                var parts = raw.Split('|');
                if (parts.Length != 2) continue;
                string word = parts[0].ToLower();
                int lv;
                if (FunctionWords.Contains(word)) continue;
                if (!int.TryParse(parts[1], out lv)) continue;
                ReviewEntry e;
                if (!_review.TryGetValue(word, out e))
                {
                    e = new ReviewEntry();
                    _review[word] = e;
                    _reviewOrder.Add(word);
                }
                if (!e.LevelsSolved.Contains(lv)) e.LevelsSolved.Add(lv);
            }
        }

        /// <summary>Batch 3 fix (§6.5): a due review word for the START of the next
        /// English level — no "~two challenges" gate here, the word already waited
        /// through the previous level; the counter restarts for the next word.
        /// LXD Phase D5: when the queue is empty, an unmastered parent focus word
        /// may open the level as one focus-word review round instead.</summary>
        public string TakeReviewWordAtLevelStart(System.Func<string, bool> isEligible = null)
        {
            if (_reviewOrder.Count > 0)
            {
                for (int i = 0; i < _reviewOrder.Count; i++)
                {
                    var w = _reviewOrder[i];
                    if (isEligible == null || isEligible(w))
                    {
                        _reviewOrder.RemoveAt(i);
                        _reviewOrder.Add(w); // cycle to the back
                        _challengesSinceLastReview = 0;
                        return w;
                    }
                }
            }
            // D5: no struggled word due — offer one parent focus word instead.
            return PickUnmasteredFocusWord(isEligible);
        }

        public bool HasReviewWord { get { return _reviewOrder.Count > 0; } }

        /// <summary>Word due for re-drill (~two challenges after struggle) or null.
        /// LXD Phase D5: parent focus words in the queue are served before any other.</summary>
        public string DueReviewWord(System.Func<string, bool> isEligible = null)
        {
            _challengesSinceLastReview++;
            if (_reviewOrder.Count == 0 || _challengesSinceLastReview < 2) return null;
            var picked = PickReviewWord(_reviewOrder, FocusWords, isEligible);
            if (picked == null) return null;
            _reviewOrder.Remove(picked);
            _reviewOrder.Add(picked); // cycle to the back
            _challengesSinceLastReview = 0;
            return picked;
        }

        public void ClearSession() { _review.Clear(); _reviewOrder.Clear(); _challengesSinceLastReview = 99; }

        /// <summary>LXD Phase C3: records a Mastery Medal for the skill (deduped; medals
        /// unlock by learning — first time the skill reaches Mastered — never by play count).</summary>
        void AwardMedal(string skill)
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            var list = new List<string>(SaveManager.Instance.Data.medals);
            if (!list.Contains(skill))
            {
                list.Add(skill);
                SaveManager.Instance.Data.medals = list.ToArray();
                Debug.Log("[Medals] Mastery Medal earned: " + skill);
                if (OnMedalEarned != null) OnMedalEarned(skill);
            }
        }
    }
}
