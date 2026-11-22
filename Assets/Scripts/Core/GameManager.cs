using System;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Overall game state coordinator: owns the question bank, the current
    /// level/round, practice mode, and routes answers through timer → score →
    /// adaptive → feedback. Screens subscribe to the events; interaction modules
    /// call ReportAnswer.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public LevelBank Bank { get; private set; }
        public int CurrentLevelId { get; private set; }
        public int CurrentRoundIdx { get; private set; }
        public ChallengeData CurrentChallenge { get; private set; }
        public LevelData CurrentLevel { get; private set; }
        public bool PracticeMode { get; private set; }

        /// <summary>Level chosen on the map / Continue — shown by the intro screen, then started.</summary>
        public int SelectedLevelId { get; set; }

        /// <summary>Outcome of the most recently completed level (drives the result screen).</summary>
        [NonSerialized]
        public ScoreManager.LevelOutcome LastOutcome;

        public event Action<ChallengeData> RoundStarted;              // challenge begins (timer starts here)
        public event Action<bool> Answered;                           // correct?
        public event Action<int> WrongAnswered;                       // hint level 0/1/2
        public event Action RoundCompleted;                           // challenge solved (before advancing)
        public event Action<ScoreManager.LevelOutcome> LevelCompleted;
        public event Action<string> StruggleOffered;                  // LXD Phase F: easier-variant offer (variant id)

        bool[] _roundHinted = new bool[3];
        bool _awaitingAdvance;
        bool _inReview;
        int _savedRoundIdx;
        int _runStars;
        bool _struggleOffered; // LXD Phase F: learner-agency offer fires at most once per level

        public bool InReview { get { return _inReview; } }
        public bool IsTimedMode { get { return SaveManager.Instance != null && SaveManager.Instance.Data != null && SaveManager.Instance.Data.timedMode && !PracticeMode; } }
        public bool IsCalmMode { get { return SaveManager.Instance != null && SaveManager.Instance.Data != null && SaveManager.Instance.Data.calmMode; } }

        /// <summary>Words that can be rendered as review pictures (content words with sprites; colors use a tinted ball).</summary>
        public static readonly System.Collections.Generic.HashSet<string> ReviewableWords = new System.Collections.Generic.HashSet<string> {
            "cat", "dog", "bird", "fish", "ball", "tree", "house", "star", "sun", "key",
            "apple", "banana", "gem", "chest", "coin", "crown", "basket", "crystal", "shard",
            "red", "blue", "green", "yellow"
        };
        static readonly System.Collections.Generic.Dictionary<string, string> ColorTints = new System.Collections.Generic.Dictionary<string, string> {
            { "red", "#E53935" }, { "blue", "#1E88E5" }, { "green", "#43A047" }, { "yellow", "#FDD835" }
        };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only — destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
            // LXD Phase C2: streak milestone banner (review/practice filtered here —
            // only real rounds may celebrate; calm mode shows the same plain banner
            // without sound).
            if (AdaptiveManager.Instance != null)
                AdaptiveManager.Instance.OnStreakMilestone += HandleStreakMilestone;
        }

        /// <summary>LXD Phase D5: can this word be rendered as a review/focus round picture?</summary>
        public static bool IsReviewableWord(string word)
        {
            return !string.IsNullOrEmpty(word) && ReviewableWords.Contains(word.ToLower());
        }

        void OnDestroy()
        {
            if (AdaptiveManager.Instance != null)
                AdaptiveManager.Instance.OnStreakMilestone -= HandleStreakMilestone;
        }

        void HandleStreakMilestone(int streak)
        {
            if (_inReview || PracticeMode) return;
            if (Lumo.UI.UIManager.Instance != null)
                Lumo.UI.UIManager.Instance.Toast(streak + " in a row! Lumo is proud!", 2.5f);
            if (!IsCalmMode && AudioManager.Instance != null) AudioManager.Instance.PlayStar();
        }

        /// <summary>Loads and validates the question bank (fail loudly on malformed data).</summary>
        public void LoadContent()
        {
            Bank = QuestionManager.LoadAndValidate();
            if (AdaptiveManager.Instance != null) AdaptiveManager.Instance.HydrateFromSave();
            Debug.Log("[GameManager] content loaded: " + Bank.levels.Length + " levels, " +
                      (Bank.variants != null ? Bank.variants.Length : 0) + " adaptive variants");
        }

        // ---- Level flow ----
        // Batch 2 fix: refuse to start a locked level (e.g. level 13 before 1–12
        // are complete) even if the UI is bypassed; also guards the level index
        // so a bad id can never index Bank.levels out of range.
        public void StartLevel(int levelId)
        {
            if (Bank == null || Bank.levels == null || levelId < 1 || levelId > Bank.levels.Length)
            {
                Debug.LogWarning("[Game] StartLevel refused: invalid level id " + levelId);
                return;
            }
            // Batch 5 fix: without a save there is nowhere to record progress.
            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("[Game] StartLevel refused: SaveManager missing");
                return;
            }
            if (LevelManager.Instance != null && !LevelManager.Instance.IsUnlocked(levelId))
            {
                Debug.LogWarning("[Game] StartLevel refused: level " + levelId + " is locked");
                return;
            }
            CurrentLevel = Bank.levels[levelId - 1];
            CurrentLevelId = levelId;
            PracticeMode = SaveManager.Instance.Data.practiceMode;
            _roundHinted = new bool[CurrentLevel.challenges.Length];
            _inReview = false;
            _runStars = 0;
            _struggleOffered = false; // LXD Phase F: once per level
            // LXD Phase D1: the session recap remembers which islands were visited today.
            if (SessionTracker.Instance != null) SessionTracker.Instance.RecordLevelStarted(levelId);
            var data = SaveManager.Instance.Data;
            data.lastLevel = levelId;
            SaveManager.Instance.Save();
            // Batch 3 fix (§6.5): a due review word opens the next English level
            // as a review round; otherwise start at round 1 as before.
            if (TryStartWithReview()) return;
            BeginRound(0);
        }

        /// <summary>Batch 3 fix (§6.5): serves a due review word as the first round of
        /// an English level. Filed under _savedRoundIdx = -1 so Advance() resumes
        /// at round 0 afterwards; the mid-level injection in Advance() is untouched.</summary>
        bool TryStartWithReview()
        {
            if (CurrentLevel.world != "english" || AdaptiveManager.Instance == null) return false;
            CurrentRoundIdx = 0;
            string word = AdaptiveManager.Instance.TakeReviewWordAtLevelStart(w => ReviewableWords.Contains(w));
            if (word == null) return false;
            _inReview = true;
            _savedRoundIdx = -1;
            CurrentChallenge = BuildReviewChallenge(word);
            _awaitingAdvance = false;
            HintManager.Instance.BeginChallenge();
            TimerManager.Instance.Reset();
            if (IsTimedMode) TimerManager.Instance.StartChallenge(CurrentChallenge.timer);
            var data = SaveManager.Instance.Data;
            data.lastRound = 1;
            SaveManager.Instance.Save();
            Debug.Log("[Review] injected review round for word '" + word + "' at level start");
            if (RoundStarted != null) RoundStarted(CurrentChallenge);
            return true;
        }

        public void BeginRound(int roundIdx)
        {
            _awaitingAdvance = false;
            CurrentRoundIdx = roundIdx;
            var baseChallenge = CurrentLevel.challenges[roundIdx];
            CurrentChallenge = AdaptiveManager.Instance.SelectChallenge(Bank, baseChallenge);

            HintManager.Instance.BeginChallenge();
            TimerManager.Instance.Reset();

            var data = SaveManager.Instance.Data;
            data.lastRound = roundIdx + 1;
            SaveManager.Instance.Save();

            // Tutorial is not a level; real levels run their timer when the challenge starts.
            if (IsTimedMode && CurrentLevelId >= 1)
            {
                TimerManager.Instance.Stop();
                // timer starts when the screen reports the challenge actually visible:
                // RoundStarted handlers call GameManager.ActivateTimer().
            }

            if (RoundStarted != null) RoundStarted(CurrentChallenge);
        }

        /// <summary>Called by the gameplay screen once the challenge is actually on screen (§6.1: timer counts from the moment the challenge starts).</summary>
        public void ActivateTimer()
        {
            if (!IsTimedMode) return;
            TimerManager.Instance.StartChallenge(CurrentChallenge.timer);
        }

        public void RepeatInstruction()
        {
            AudioManager.Instance.PlayVoice(CurrentChallenge.vo);
        }

        // ---- Answer flow ----
        public void ReportAnswer(bool correct)
        {
            if (_awaitingAdvance) return;
            if (!correct)
            {
                int hintLevel = HintManager.Instance.RegisterMistake();
                FeedbackManager.Instance.Retry(CurrentChallenge);
                if (hintLevel == 1) FeedbackManager.Instance.Hint(CurrentChallenge);
                if (hintLevel == 2) FeedbackManager.Instance.Solution(CurrentChallenge);
                // Batch 2 fix: practice answers must not train the adaptive model.
                // LXD Phase F: capture the skill struggle streak BEFORE RecordAnswer
                // consumes it (reaching 2 starts remediation and resets the counter).
                int preStreak = -1;
                if (!PracticeMode)
                {
                    int sIdx = AdaptiveManager.SkillIndex(Bank, CurrentChallenge.skill);
                    var st = sIdx >= 0 ? SaveManager.Instance.Skill(sIdx) : null;
                    if (st != null) preStreak = st.struggleStreak;
                    AdaptiveManager.Instance.RecordAnswer(Bank, CurrentChallenge, false, HintManager.Instance.UsedHelp);
                }
                if (Answered != null) Answered(false);
                if (WrongAnswered != null) WrongAnswered(hintLevel);
                // LXD Phase F: learner agency - after the retry line shows, offer
                // "Easier / Try again" on the 2nd consecutive struggle (once per level).
                if (ShouldOfferStruggleChoice(preStreak, _inReview, PracticeMode, _struggleOffered,
                        CurrentChallenge != null && !string.IsNullOrEmpty(CurrentChallenge.easier)))
                {
                    _struggleOffered = true;
                    Debug.Log("[LXD] struggle-choice offered for " + CurrentChallenge.id + " -> easier " + CurrentChallenge.easier);
                    if (StruggleOffered != null) StruggleOffered(CurrentChallenge.easier);
                }
                return;
            }

            // correct
            TimerManager.Instance.Stop();
            _awaitingAdvance = true;
            bool firstTry = HintManager.Instance.Mistakes == 0 && !HintManager.Instance.UsedHelp;
            bool hinted = HintManager.Instance.UsedHelp;
            // Batch 4 fix: a review round must not overwrite the real round's
            // hinted state (during review CurrentRoundIdx is the saved round).
            if (!_inReview) _roundHinted[CurrentRoundIdx] = hinted;

            float elapsed = TimerManager.Instance.Elapsed;
            int stars = PracticeMode
                ? 0
                : (IsTimedMode
                    ? GameRules.StarsForTime(elapsed, firstTry)
                    : GameRules.StarsForCalmAnswer(HintManager.Instance.Mistakes, hinted));

            // Batch 2 fix: practice answers must not train the adaptive model
            // (the XP path below was already practice-guarded).
            if (!PracticeMode) AdaptiveManager.Instance.RecordAnswer(Bank, CurrentChallenge, firstTry, hinted);
            if (!PracticeMode && !_inReview)
            {
                _runStars += stars;
                ScoreManager.Instance.RecordChallenge(CurrentLevelId, CurrentRoundIdx, stars, firstTry, hinted);
            }

            FeedbackManager.Instance.Success(CurrentChallenge);
            // LXD Phase D1: a clean review-word answer counts as "remembered it today"
            // for the session recap.
            if (firstTry && CurrentChallenge.id != null && CurrentChallenge.id.StartsWith("RV_") && SessionTracker.Instance != null)
                SessionTracker.Instance.RecordWordClean(CurrentChallenge.correctAnswer);
            // LXD Phase E1: session-only play log row (practice rounds excluded).
            if (!PracticeMode)
                PlayLog.Record(CurrentLevelId, CurrentChallenge.id, CurrentChallenge.skill,
                    HintManager.Instance.Mistakes + 1,
                    (HintManager.Instance.HintShown ? 1 : 0) + (HintManager.Instance.SolutionShown ? 1 : 0),
                    elapsed, firstTry);
            if (Answered != null) Answered(true);
            if (RoundCompleted != null) RoundCompleted();
            Debug.Log("[Game] challenge " + CurrentChallenge.id + " solved in " + elapsed.ToString("0.0") + "s -> " + stars + " stars");
        }

        /// <summary>LXD Phase F: pure offer rule — the 2nd consecutive struggle, once per
        /// level, never in review/practice, only when an easier variant exists.</summary>
        public static bool ShouldOfferStruggleChoice(int preStreak, bool inReview, bool practice, bool alreadyOffered, bool hasEasier)
        {
            return !alreadyOffered && !inReview && !practice && preStreak + 1 == 2 && hasEasier;
        }

        /// <summary>
        /// LXD Phase F: learner agency — swaps the current challenge for its easier
        /// variant in place, without advancing the round counter. Mirrors normal
        /// round-start setup: fresh hint ladder, reset timer, module re-init via the
        /// RoundStarted event. Prior attempts do not count against the new variant;
        /// the round keeps scoring normally (calm rules apply).
        /// </summary>
        public void ReplaceCurrentChallenge(string variantId)
        {
            if (_awaitingAdvance) return; // never swap after the round was solved
            var variant = QuestionManager.GetVariant(Bank, variantId);
            if (variant == null)
            {
                Debug.LogWarning("[LXD] struggle swap refused: variant '" + variantId + "' not found");
                return;
            }
            CurrentChallenge = variant;
            HintManager.Instance.BeginChallenge(); // fresh hint ladder + UsedHelp for the new variant
            TimerManager.Instance.Reset();
            if (IsTimedMode) TimerManager.Instance.Stop(); // restarts via ActivateTimer when visible
            // LXD Phase E1: log the learner-chosen difficulty swap
            PlayLog.Record(CurrentLevelId, "swap:" + variant.id, variant.skill, 0, 0, 0f, false);
            Debug.Log("[LXD] struggle swap -> " + variant.id + " (round index unchanged: " + CurrentRoundIdx + ")");
            if (RoundStarted != null) RoundStarted(CurrentChallenge);
        }

        /// <summary>Advances to the next round or completes the level (child-controlled pacing, §8.6).
        /// In English levels a due review word (§6.5) is served as an extra challenge first.</summary>
        public void Advance()
        {
            if (!_awaitingAdvance) return;
            if (AudioManager.Instance != null) AudioManager.Instance.StopVoice();

            if (!_inReview && CurrentLevel.world == "english")
            {
                string word = AdaptiveManager.Instance.DueReviewWord(w => ReviewableWords.Contains(w));
                if (word != null)
                {
                    _inReview = true;
                    _savedRoundIdx = CurrentRoundIdx;
                    CurrentChallenge = BuildReviewChallenge(word);
                    _awaitingAdvance = false;   // the review answer must be accepted
                    HintManager.Instance.BeginChallenge();
                    TimerManager.Instance.Reset();
                    // Batch 4 fix: route the review timer through ActivateTimer
                    // (practice-guarded, starts from challenge-visible) instead
                    // of starting it directly here.
                    ActivateTimer();
                    Debug.Log("[Review] injected review round for word '" + word + "'");
                    if (RoundStarted != null) RoundStarted(CurrentChallenge);
                    return;
                }
            }

            if (_inReview)
            {
                _inReview = false;
                CurrentRoundIdx = _savedRoundIdx;
            }

            int next = CurrentRoundIdx + 1;
            if (next < CurrentLevel.challenges.Length) { BeginRound(next); return; }
            CompleteLevel();
        }

        /// <summary>Builds a small word-picture review challenge for the re-drill word (§6.5).</summary>
        ChallengeData BuildReviewChallenge(string word)
        {
            string sprite = "obj_" + word;
            string tint = null;
            if (ColorTints.ContainsKey(word)) { sprite = "obj_ball"; tint = ColorTints[word]; }
            var pics = new System.Collections.Generic.List<PicData> {
                new PicData { id = word, sprite = sprite, tint = tint, size = 1.0f }
            };
            string[] pool = { "cat", "dog", "bird", "fish", "ball", "tree", "house", "star", "sun", "key", "apple", "banana", "gem", "coin" };
            foreach (var p in pool)
            {
                if (pics.Count >= 4) break;
                if (p == word || (ColorTints.ContainsKey(word) && p == "ball")) continue;
                pics.Add(new PicData { id = p, sprite = "obj_" + p, size = 1.0f });
            }
            // Shuffle so the correct picture is not always first: a child must
            // recognise the word, not learn "tap the first card" (Batch 1 fix).
            for (int i = pics.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = pics[i]; pics[i] = pics[j]; pics[j] = tmp;
            }
            var W = word.ToUpper();
            return new ChallengeData
            {
                id = "RV_" + word,
                level = CurrentLevelId,
                round = CurrentRoundIdx,
                part = 0,
                type = "word-picture",
                skill = "vocabulary",
                difficulty = "base",
                timer = GameRules.ChallengeTimerSeconds,
                prompt = "Review time! Find the " + W + "!",
                vo = "vo_word_" + word,
                correctAnswer = word,
                hints = new[] { "Listen to the word and find the right picture!", "The word is " + W + ". Tap its picture!" },
                feedback = new FeedbackData { success = "Amazing! You remembered " + W + "!", retry = "Listen again and find the " + word + "!" },
                words = new[] { word },
                media = new MediaData { word = W, voWord = "vo_word_" + word, pictures = pics.ToArray() }
            };
        }

        void CompleteLevel()
        {
            _inReview = false;
            CurrentChallenge = null;
            // LXD Phase D1: completed levels feed the session recap (practice included —
            // it is out-game reflection, not a reward).
            if (SessionTracker.Instance != null) SessionTracker.Instance.RecordLevelCompleted(CurrentLevelId);
            // Batch 2 fix: finishing a level in Practice Mode must not record the
            // completion, unlock the next level, or award XP/badges — practice is
            // free play. Still fire the outcome event (with zero XP) so the result
            // screen shows.
            if (PracticeMode)
            {
                bool perfect = true;
                for (int i = 0; i < _roundHinted.Length; i++) if (_roundHinted[i]) perfect = false;
                var practiceOutcome = new ScoreManager.LevelOutcome
                {
                    StarsThisRun = _runStars,
                    XpEarned = 0,
                    NewBadges = new string[0],
                    PerfectNoHint = perfect
                };
                Debug.Log("[Game] level " + CurrentLevelId + " complete (practice — no XP/badges recorded)");
                if (LevelCompleted != null) LevelCompleted(practiceOutcome);
                return;
            }
            var outcome = ScoreManager.Instance.CompleteLevel(CurrentLevelId, _roundHinted, _runStars);
            Debug.Log("[Game] level " + CurrentLevelId + " complete: +" + outcome.XpEarned + " xp, badges=" + outcome.NewBadges.Length);
            if (LevelCompleted != null) LevelCompleted(outcome);
        }

        // ---- helpers ----
        public bool IsFinalLevel { get { return CurrentLevelId == 13; } }
        public string HudLine
        {
            get
            {
                string island = IslandCatalog.Get(CurrentLevelId).IslandName;
                int totalRounds = CurrentLevel != null && CurrentLevel.challenges != null ? CurrentLevel.challenges.Length : 0;
                return _inReview
                    ? island + "  |  Review Round"
                    : island + "  |  Round " + (CurrentRoundIdx + 1) + "/" + totalRounds;
            }
        }

        /// <summary>Replays the current level from round 1 (no previous attempt time carries over, §11).</summary>
        public void ForceRestartLevel() { StartLevel(CurrentLevelId); }

        /// <summary>Aborts the active level cleanly when returning to the map or restarting.</summary>
        public void AbortLevel()
        {
            CurrentChallenge = null;
            _inReview = false;
            _awaitingAdvance = false;
            if (TimerManager.Instance != null) TimerManager.Instance.Stop();
            if (AudioManager.Instance != null) AudioManager.Instance.StopVoice();
        }
    }
}
