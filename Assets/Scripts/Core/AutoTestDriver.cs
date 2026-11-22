using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Lumo.UI;
using Lumo.Modules;

namespace Lumo.Core
{
    /// <summary>
    /// Automated smoke-test driver (development only): plays a full level through the REAL
    /// UI buttons and validates the save outcome. Created by GameRoot when _tools/autotest.txt
    /// contains the level id to run. Logs [AutoTest] PASS/FAIL lines to the console.
    /// </summary>
    public class AutoTestDriver : MonoBehaviour
    {
        public int LevelId = 1;
        /// <summary>Stage 1.1: play levels 1..13 back to back (flag file "all"), restoring the original save at the end.</summary>
        public bool RunAllLevels;
        int _phase;
        float _timer, _phaseTimer;
        int _roundsDone;
        int _levelsPassed;
        int _challengesCompleted;
        readonly System.Collections.Generic.List<string> _failedLevels = new System.Collections.Generic.List<string>();
        bool _wrongThenRight;
        bool _tickLogged;
        int _dragIdx;
        int _matchStep;
        int _tapIdx;
        int _keyIdx;
        int _keyUseIdx;
        int _totalSortCount;
        int[] _artCurrent;
        int _artColor;
        string _lastChallengeId;
        readonly System.Collections.Generic.HashSet<GameObject> _usedChips = new System.Collections.Generic.HashSet<GameObject>();
        readonly System.Collections.Generic.List<string> _tapSequence = new System.Collections.Generic.List<string>();

        // Stage 1.1: raw save snapshot so a full 13-level run leaves the child's
        // real save untouched - bytes are cached locally because SaveManager may
        // already be destroyed when the restore runs during shutdown.
        bool _snapshotTaken, _snapshotRestored;
        string _savePath, _backupPath, _tempPath;
        byte[] _saveBytes, _backupBytes;
        bool _saveExisted, _backupExisted;

        void Start()
        {
            Debug.Log("[AutoTest] driver Start: LevelId=" + LevelId + " RunAllLevels=" + RunAllLevels +
                      " active=" + gameObject.activeInHierarchy +
                      " timeScale=" + Time.timeScale);
            if (!RunAllLevels) return;
            TakeSaveSnapshot();
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                var d = SaveManager.Instance.Data;
                d.freeExplore = true;      // every level selectable without unlock gating
                d.calmMode = true;         // untimed -> deterministic calm star rules
                d.timedMode = false;
                // practice mode skips CompleteLevel (no unlocks/badges/stars) and a
                // pre-set daily limit can open the TimeLimit overlay mid-run; since the
                // exact raw save bytes are restored afterwards, overriding in memory is safe.
                d.practiceMode = false;
                d.dailyLimitMinutes = 0;
                Debug.Log("[AutoTest] save flags overridden for the run (original files restored afterwards)");
            }
        }

        void Update()
        {
            if (!_tickLogged)
            {
                _tickLogged = true;
                Debug.Log("[AutoTest] first Update tick");
            }
            _timer += Time.deltaTime;
            _phaseTimer += Time.deltaTime;
            float stepInterval = RunAllLevels ? 0.08f : 0.45f;
            if (_timer < stepInterval) return;
            _timer = 0f;

            var m = UIManager.Instance;
            var gm = GameManager.Instance;
            if (m == null || gm == null) return;

            // Stage 1.1: per-phase watchdog - a round stuck on a missing UI node,
            // a refused StartLevel or a never-appearing result screen fails fast
            // (with the save restored) instead of hanging the whole run.
            if (_phase >= 1 && _phase <= 3 && _phaseTimer > 20f)
            {
                var stuck = gm.CurrentChallenge;
                string stuckId = stuck != null ? stuck.id : (_lastChallengeId ?? "none");
                string stuckType = stuck != null ? stuck.type : "none";
                string detail = "STALL on level=" + LevelId + " challenge=" + stuckId +
                                " type=" + stuckType + " phase=" + _phase;
                Debug.LogError("[AutoTest] FAIL: " + detail);
                _failedLevels.Add("L" + LevelId + " " + detail);
                if (RunAllLevels)
                    Debug.Log("[AutoTest] ALL 13 LEVELS FAIL (passed=" + _levelsPassed + "/13, challenges=" +
                              _challengesCompleted + ", failed=" + string.Join("; ", _failedLevels) + ")");
                RestoreSaveSnapshot();
                _phase = 4;
                Destroy(gameObject);
                return;
            }

            switch (_phase)
            {
                case 0:
                    Debug.Log("[AutoTest] navigating to Gameplay (level " + LevelId + ")");
                    gm.SelectedLevelId = LevelId;
                    m.Show("Gameplay");
                    _phase = 1;
                    _phaseTimer = 0f;
                    break;

                case 1:
                    if (gm.CurrentChallenge == null) return;
                    try { TryAnswer(m, gm); }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[AutoTest] TryAnswer threw: " + e.Message + "\n" + e.StackTrace);
                        _failedLevels.Add("L" + LevelId + " TryAnswer exception: " + e.Message);
                        if (RunAllLevels)
                            Debug.Log("[AutoTest] ALL 13 LEVELS FAIL (passed=" + _levelsPassed + "/13, challenges=" +
                                      _challengesCompleted + ", failed=" + string.Join("; ", _failedLevels) + ")");
                        RestoreSaveSnapshot();
                        _phase = 4;
                        Destroy(gameObject);
                    }
                    break;

                case 2:
                    var nextGo = NextButton(m);
                    if (nextGo == null || !nextGo.activeInHierarchy)
                    {
                        float waitLimit = 6f;
                        // Stage 1.1: two-step math-choice rounds rebuild the answer options
                        // between steps instead of showing Next - retry the answer phase
                        // quickly instead of stalling 6s per step transition.
                        if (RunAllLevels && gm.CurrentChallenge != null && gm.CurrentChallenge.type == "math-choice")
                        {
                            var gpStep = m.Get<GameplayScreen>();
                            if (gpStep != null && gpStep.CurrentModule is MathChoiceModule) waitLimit = 0.5f;
                        }
                        if (_phaseTimer > waitLimit) { _phase = 1; _phaseTimer = 0f; }
                        return;
                    }
                    var nb = nextGo.GetComponent<Button>();
                    if (nb == null) return;
                    bool wasReview = gm.CurrentChallenge != null && gm.CurrentChallenge.id.StartsWith("RV_");
                    nb.onClick.Invoke();
                    if (!wasReview)
                    {
                        _roundsDone++;
                        _challengesCompleted++;
                        Debug.Log("[AutoTest] round " + _roundsDone + " complete");
                    }
                    else
                    {
                        Debug.Log("[AutoTest] review round completed");
                    }
                    int totalRounds = gm.CurrentLevel != null ? gm.CurrentLevel.challenges.Length : 3;
                    bool nextIsReview = gm.InReview;
                    _phase = (_roundsDone >= totalRounds && !nextIsReview) ? 3 : 1;
                    _phaseTimer = 0f;
                    break;

                case 3:
                    ScreenBase resultScreen = LevelId == 13
                        ? (ScreenBase)m.Get<FinalResultScreen>()
                        : (ScreenBase)m.Get<LevelResultScreen>();
                    if (resultScreen == null || resultScreen.Panel == null || !resultScreen.Panel.activeInHierarchy)
                    {
                        if (_phaseTimer > 12f) Debug.LogError("[AutoTest] FAIL: result screen never appeared");
                        return;
                    }
                    int unlocked = SaveManager.Instance.Data.highestUnlocked;
                    int stars = SaveManager.Instance.Data.totalStars;
                    int l13Stars = SaveManager.SumStars(SaveManager.Instance.Record(13));
                    bool kingdomHero = System.Array.Exists(SaveManager.Instance.Data.badges, b => b == "Kingdom Hero");
                    // Stage 1.1: level 13 runs the expanded 15-round bank, so require its
                    // recorded stars to scale with the round count (>= 2 per round) instead
                    // of a hardcoded 24.
                    bool ok = (unlocked >= LevelId + 1 || LevelId == 13) && stars > 0
                              && (LevelId != 13 || (kingdomHero && l13Stars >= SaveManager.RoundsForLevel(13) * 2));
                    Debug.Log("[AutoTest] level " + LevelId + " => " + (ok ? "PASS" : "FAIL") +
                              " (unlocked=" + unlocked + " stars=" + stars + " l13Stars=" + l13Stars +
                              " xp=" + SaveManager.Instance.Data.totalXp +
                              " badges=" + string.Join(",", SaveManager.Instance.Data.badges) + ")");
                    if (RunAllLevels && ok && LevelId < 13)
                    {
                        _levelsPassed++;
                        Debug.Log("[AutoTest] level " + LevelId + " done, continuing to level " + (LevelId + 1) +
                                  " (passed=" + _levelsPassed + "/13 so far)");
                        ResetRoundState();
                        if (GameManager.Instance != null) GameManager.Instance.AbortLevel();
                        LevelId++;
                        _phase = 0;
                        _phaseTimer = 0f;
                        break;
                    }
                    if (RunAllLevels)
                    {
                        if (ok) _levelsPassed++;
                        else _failedLevels.Add("L" + LevelId + " result-check FAIL");
                        RestoreSaveSnapshot();
                        if (ok && _levelsPassed >= 13)
                            Debug.Log("[AutoTest] ALL 13 LEVELS PASS (passed=13/13, challenges=" + _challengesCompleted + ")");
                        else
                            Debug.Log("[AutoTest] ALL 13 LEVELS FAIL (passed=" + _levelsPassed + "/13, challenges=" +
                                      _challengesCompleted + ", failed=" + string.Join("; ", _failedLevels) + ")");
                    }
                    _phase = 4;
                    Destroy(gameObject);
                    break;
            }
        }

        // ---- Stage 1.1: save preservation + per-level state reset ----

        /// <summary>Caches the raw save-file bytes (main + .bak) before the run touches gameplay,
        /// so restoration works even if SaveManager.Instance is already destroyed at shutdown.</summary>
        void TakeSaveSnapshot()
        {
            if (_snapshotTaken) return;
            try
            {
                var sm = SaveManager.Instance;
                if (sm == null) return;
                _savePath = sm.SavePath;
                _backupPath = sm.BackupPath;
                _tempPath = sm.TempPath;
                _saveExisted = System.IO.File.Exists(_savePath);
                _backupExisted = System.IO.File.Exists(_backupPath);
                _saveBytes = _saveExisted ? System.IO.File.ReadAllBytes(_savePath) : null;
                _backupBytes = _backupExisted ? System.IO.File.ReadAllBytes(_backupPath) : null;
                _snapshotTaken = true;
                Debug.Log("[AutoTest] save snapshot taken (main=" + _saveExisted + ", backup=" + _backupExisted + ")");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AutoTest] save snapshot failed: " + e.Message);
            }
        }

        /// <summary>Writes the original save bytes back (or deletes files the run created) exactly
        /// once; never throws - it can execute during shutdown. Then reloads the managers from disk.</summary>
        void RestoreSaveSnapshot()
        {
            if (!_snapshotTaken || _snapshotRestored) return;
            _snapshotRestored = true;
            try
            {
                if (_saveExisted && _saveBytes != null) System.IO.File.WriteAllBytes(_savePath, _saveBytes);
                else if (!_saveExisted && System.IO.File.Exists(_savePath)) System.IO.File.Delete(_savePath);
                if (_backupExisted && _backupBytes != null) System.IO.File.WriteAllBytes(_backupPath, _backupBytes);
                else if (!_backupExisted && System.IO.File.Exists(_backupPath)) System.IO.File.Delete(_backupPath);
                if (!string.IsNullOrEmpty(_tempPath) && System.IO.File.Exists(_tempPath)) System.IO.File.Delete(_tempPath);
                if (SaveManager.Instance != null) SaveManager.Instance.Reload();
                if (AdaptiveManager.Instance != null) AdaptiveManager.Instance.HydrateFromSave();
                Debug.Log("[AutoTest] original save restored");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AutoTest] save restore failed: " + e.Message);
            }
        }

        /// <summary>Clears all per-round/per-level driver state before continuing with the next level.</summary>
        void ResetRoundState()
        {
            _roundsDone = 0;
            _lastChallengeId = null;
            _wrongThenRight = true;
            _dragIdx = 0;
            _matchStep = 0;
            _tapIdx = 0;
            _keyIdx = 0;
            _keyUseIdx = 0;
            _totalSortCount = 0;
            _artCurrent = null;
            _artColor = int.MinValue;
            _usedChips.Clear();
            _tapSequence.Clear();
        }

        void OnDestroy()
        {
            // Stage 1.1: fallback restore if the driver is torn down (editor stop,
            // crash, exception) before reaching its epilogue; no-op after a normal run.
            RestoreSaveSnapshot();
        }

        void TryAnswer(UIManager m, GameManager gm)
        {
            var gp = m.Get<GameplayScreen>();
            if (gp == null || gp.AnswerArea == null) { Debug.Log("[AutoTest] gameplay screen not ready"); return; }
            var c = gm.CurrentChallenge;

            // per-round state reset
            if (_lastChallengeId != c.id)
            {
                _lastChallengeId = c.id;
                _dragIdx = 0;
                _matchStep = 0;
                _tapIdx = 0;
                _keyIdx = 0;
                _keyUseIdx = 0;
                _usedChips.Clear();
                _tapSequence.Clear();
                _totalSortCount = 0;
                if (c.type == "potion")
                {
                    foreach (var cc in c.media.colors)
                        for (int i = 0; i < cc.count; i++) _tapSequence.Add(cc.color);
                    if (c.media.remove > 0)
                        for (int i = 0; i < c.media.remove; i++) _tapSequence.Add("potion-remove");
                }
                else if (c.type == "directions" && c.media.moves != null && c.media.moves.Length > 0)
                    foreach (var mv in c.media.moves)
                        for (int i = 0; i < mv.n; i++) _tapSequence.Add(mv.dir);
                else if (c.type == "directions" && c.media.collect > 0)
                {
                    for (int i = 0; i < c.media.collect; i++) _tapSequence.Add("key-collect");
                    for (int i = 0; i < c.media.use; i++) _tapSequence.Add("key-use");
                }
                else if (c.type == "sorting")
                    foreach (var it in c.media.items) _totalSortCount += it.count;
                else if (c.type == "pixel-art")
                {
                    int n = c.media.gw * c.media.gh;
                    _artCurrent = new int[n];
                    for (int i = 0; i < n; i++)
                        _artCurrent[i] = (c.media.startGrid != null && i < c.media.startGrid.Length) ? c.media.startGrid[i] : -1;
                    _artColor = int.MinValue;
                }
            }

            if (!_wrongThenRight)
            {
                GameObject wrong = null;
                if (c.type == "word-picture")
                    wrong = FindPicChild(PicsRoot(gp), c.correctAnswer, false);
                else if (c.type == "math-choice" || c.type == "choice-card")
                    wrong = FindLabeledButton(gp.AnswerArea, c.correctAnswer, false);
                if (wrong != null)
                {
                    var wb = wrong.GetComponent<Button>();
                    if (wb != null && wb.interactable)
                    {
                        ExecuteEvents.Execute(wb.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                        _wrongThenRight = true;
                        Debug.Log("[AutoTest] exercised wrong-answer path on " + c.id);
                        return;
                    }
                }
                _wrongThenRight = true;
            }

            GameObject right = null;
            if (c.type == "word-picture")
                right = FindPicChild(PicsRoot(gp), c.correctAnswer, true);
            else if (c.type == "math-choice" && gp.CurrentModule is MathChoiceModule)
            {
                // two-step challenges expect the CURRENT step's value
                string expected = ((MathChoiceModule)gp.CurrentModule).CurrentExpected.ToString();
                right = FindLabeledButton(gp.AnswerArea, expected, true);
            }
            else if (c.type == "spelling" || c.type == "sentence")
            {
                bool advanced = TryDragAndDrop(gp, c);
                if (advanced) { _phase = 2; _phaseTimer = 0f; }
                return;
            }
            else if (c.type == "matching")
            {
                bool done = TryMatching(gp, c);
                if (done) { _phase = 2; _phaseTimer = 0f; }
                return;
            }
            else if (c.type == "potion")
            {
                bool done = TryTapSequence(gp, c);
                if (done) { _phase = 2; _phaseTimer = 0f; }
                return;
            }
            else if (c.type == "directions")
            {
                bool done = TryDirections(gp, c);
                if (done) { _phase = 2; _phaseTimer = 0f; }
                return;
            }
            else if (c.type == "sorting")
            {
                bool done = TrySorting(gp, c);
                if (done) { _phase = 2; _phaseTimer = 0f; }
                return;
            }
            else if (c.type == "memory-match")
            {
                bool done = TryMemoryMatch(gp);
                if (done) { _phase = 2; _phaseTimer = 0f; }
                return;
            }
            else if (c.type == "pixel-art")
            {
                bool done = TryPixelArt(gp, c);
                if (done) { _phase = 2; _phaseTimer = 0f; }
                return;
            }
            else if (c.type == "music-play")
            {
                bool done = TryMusicPlay(gp, c);
                if (done) { _phase = 2; _phaseTimer = 0f; }
                return;
            }
            else
                right = FindLabeledButton(gp.AnswerArea, c.correctAnswer, true);
            if (right == null)
            {
                Debug.Log("[AutoTest] correct-answer control not ready yet (" + c.id + ", " + c.type + ")");
                return;
            }
            var rb = right.GetComponent<Button>();
            if (rb == null || !rb.interactable) return;
            Debug.Log("[AutoTest] clicking " + right.name + " for " + c.id);
            rb.onClick.Invoke();   // direct listener invocation (bypasses OnPointerClick path)
            _phase = 2;
            _phaseTimer = 0f;
        }

        /// <summary>Drags one letter/word chip per tick through the real drag handlers; returns true when the round can advance.</summary>
        bool TryDragAndDrop(GameplayScreen gp, ChallengeData c)
        {
            var activity = gp.ActivityArea;
            var slotsRow = activity.transform.Find("Slots");
            if (slotsRow == null) return false;

            if (c.type == "spelling")
            {
                string word = c.media.word;
                if (_dragIdx >= word.Length) return true;
                char target = word[_dragIdx];
                var slotRt = slotsRow.Find("Slot" + _dragIdx) as RectTransform;
                if (slotRt == null) return false;
                var lettersRow = activity.transform.Find("Letters");
                if (lettersRow == null) return false;
                foreach (Transform chip in lettersRow)
                {
                    var comp = chip.GetComponent<DragChip>();
                    if (comp == null || comp.Payload != target.ToString()) continue;
                    if (_usedChips.Contains(chip.gameObject)) continue;
                    _usedChips.Add(chip.gameObject);
                    Debug.Log("[AutoTest] dropping letter '" + target + "' into slot " + _dragIdx);
                    SimulateDrop(chip.gameObject, slotRt);
                    _dragIdx++;
                    return false;
                }
                return false;
            }

            // sentence
            var words = c.media.sentence.Replace(".", "").Split(' ');
            if (_dragIdx >= words.Length)
            {
                var submitT = activity.transform.Find("Submit");
                var submit = submitT != null ? submitT.GetComponent<Button>() : null;
                if (submit != null)
                {
                    Debug.Log("[AutoTest] submitting sentence");
                    submit.onClick.Invoke();
                    _dragIdx = 0;
                    return true;
                }
                return false;
            }
            // sentence slots are named by index (Batch 1 fix), not by word
            var slot = slotsRow.Find("Slot_" + _dragIdx) as RectTransform;
            if (slot == null) return false;
            var cardsRow = gp.AnswerArea.transform.Find("Cards");
            if (cardsRow == null) return false;
            foreach (Transform chip in cardsRow)
            {
                var comp = chip.GetComponent<DragChip>();
                if (comp == null || comp.Payload.ToLower() != words[_dragIdx].ToLower()) continue;
                if (_usedChips.Contains(chip.gameObject)) continue;
                _usedChips.Add(chip.gameObject);
                Debug.Log("[AutoTest] dropping word '" + words[_dragIdx] + "' (" + (_dragIdx + 1) + "/" + words.Length + ")");
                SimulateDrop(chip.gameObject, slot);
                _dragIdx++;
                return false;
            }
            return false;
        }

        /// <summary>Clicks matching cards in sequence for EVERY triple in the
        /// challenge: num->pic (line 1), pic->word (line 2) per triple.
        /// Batch 1: rounds now show 3 triples, so the test connects all of
        /// them instead of assuming a single triple.</summary>
        bool TryMatching(GameplayScreen gp, ChallengeData c)
        {
            var seq = new System.Collections.Generic.List<string>();
            foreach (var t in c.media.triples)
            {
                seq.Add("Card_num_" + t.num);
                seq.Add("Card_pic_" + t.num);
                seq.Add("Card_pic_" + t.num);
                seq.Add("Card_word_" + t.num);
            }
            if (_matchStep >= seq.Count) return true;
            ClickCard(gp.ActivityArea, seq[_matchStep]);
            _matchStep++;
            return _matchStep >= seq.Count;
        }

        void ClickCard(GameObject activity, string cardName)
        {
            var t = activity.transform.Find(cardName);
            if (t == null) { Debug.LogError("[AutoTest] card missing: " + cardName); return; }
            var b = t.GetComponent<Button>();
            if (b != null) b.onClick.Invoke();
        }

        void SimulateDrop(GameObject chip, RectTransform slot)
        {
            var cam = UIManager.Instance.Cam;
            var ped = new PointerEventData(EventSystem.current);
            ped.position = RectTransformUtility.WorldToScreenPoint(cam, slot.position);
            ExecuteEvents.Execute(chip, ped, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(chip, ped, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(chip, ped, ExecuteEvents.endDragHandler);
        }

        /// <summary>Taps color/arrow/key buttons one per tick, then clicks the correct option.</summary>
        bool TryTapSequence(GameplayScreen gp, ChallengeData c)
        {
            if (_tapIdx < _tapSequence.Count)
            {
                string tap = _tapSequence[_tapIdx];
                Button b = null;
                if (c.type == "potion")
                {
                    if (tap == "potion-remove")
                    {
                        // Stage 1.1: stars are named "S<globalAddIdx>_<color>", so with a
                        // multi-color recipe the counter index can belong to another color.
                        // Any placed star is removable, so tap the first star whose button
                        // is still interactable (RemoveStar disables it immediately).
                        var starsRow = gp.ActivityArea.transform.Find("Cauldron/Stars");
                        if (starsRow != null)
                            foreach (Transform star in starsRow)
                            {
                                var sb = star.GetComponent<Button>();
                                if (sb != null && sb.interactable) { b = sb; break; }
                            }
                    }
                    else
                        b = FindChildButton(gp.ActivityArea.transform, "ColorButtons/Btn_" + tap);
                }
                else if (c.type == "directions")
                {
                    if (tap == "key-collect")
                        b = FindChildButton(gp.ActivityArea.transform, "Keys/Key" + _keyIdx);
                    else if (tap == "key-use")
                        b = FindChildButton(gp.ActivityArea.transform, "Keys/Key" + _keyUseIdx);
                    else
                        b = FindChildButton(gp.ActivityArea.transform, "Pad/Arrow" + Capitalize(tap));
                }
                if (b == null) return false;
                b.onClick.Invoke();
                _tapIdx++;
                if (tap == "key-collect" || tap == "potion-remove") _keyIdx++;
                if (tap == "key-use") _keyUseIdx++;
                return false;
            }

            // taps complete
            if (c.correctAnswer == "target") return true;   // moves round: the module answered on arrival
            var right = FindLabeledButton(gp.AnswerArea, c.correctAnswer, true);
            if (right != null && right.GetComponent<Button>().interactable)
            {
                right.GetComponent<Button>().onClick.Invoke();
                return true;
            }
            return false;
        }

        bool TryDirections(GameplayScreen gp, ChallengeData c) { return TryTapSequence(gp, c); }

        bool TryMemoryMatch(GameplayScreen gp)
        {
            var mem = gp.CurrentModule as MemoryMatchModule;
            if (mem == null || mem.IsLocked) return false;
            var pair = mem.GetUnmatchedPairIndices();
            if (pair == null) return true;
            var b0 = FindChildButton(gp.ActivityArea.transform, "MemoryGrid/Card_" + pair[0]);
            var b1 = FindChildButton(gp.ActivityArea.transform, "MemoryGrid/Card_" + pair[1]);
            if (b0 == null || b1 == null) return false;
            b0.onClick.Invoke();
            b1.onClick.Invoke();
            return mem.GetUnmatchedPairIndices() == null;
        }

        bool TryPixelArt(GameplayScreen gp, ChallengeData c)
        {
            if (_artCurrent == null || c.media.targetGrid == null) return false;
            for (int i = 0; i < c.media.targetGrid.Length; i++)
            {
                int want = c.media.targetGrid[i];
                if (_artCurrent[i] == want) continue;
                if (_artColor != want)
                {
                    var sw = FindChildButton(gp.ActivityArea.transform, "PaletteCard/Swatches/Swatch_" + want);
                    if (sw == null) return false;
                    sw.onClick.Invoke();
                    _artColor = want;
                }
                var cell = FindChildButton(gp.ActivityArea.transform, "CanvasCard/PixelGrid/Cell_" + i);
                if (cell == null) return false;
                cell.onClick.Invoke();
                _artCurrent[i] = want;
                return false;
            }
            var check = FindChildButton(gp.AnswerArea.transform, "ArtControls/CheckArt");
            if (check == null) return false;
            check.onClick.Invoke();
            return true;
        }

        bool TryMusicPlay(GameplayScreen gp, ChallengeData c)
        {
            var mm = gp.CurrentModule as MusicPlayModule;
            if (mm != null && mm.IsPlayingDemo) return false;
            if (c.media.notes == null || c.media.notes.Length == 0) return false;
            if (c.media.musicMode == "next")
            {
                string last = c.media.notes[c.media.notes.Length - 1];
                var b = FindChildButton(gp.ActivityArea.transform, "XyloRack/Bars/Bar_" + last);
                if (b == null) return false;
                b.onClick.Invoke();
                return true;
            }
            if (_tapIdx < c.media.notes.Length)
            {
                var b = FindChildButton(gp.ActivityArea.transform, "XyloRack/Bars/Bar_" + c.media.notes[_tapIdx]);
                if (b == null) return false;
                b.onClick.Invoke();
                _tapIdx++;
            }
            return _tapIdx >= c.media.notes.Length;
        }

        /// <summary>Drags sorting chips into their color boxes one per tick, then clicks the correct option.</summary>
        bool TrySorting(GameplayScreen gp, ChallengeData c)
        {
            var pile = gp.ActivityArea.transform.Find("Pile");
            if (pile == null) return false;

            if (_dragIdx >= _totalSortCount)
            {
                var right = FindLabeledButton(gp.AnswerArea, c.correctAnswer, true);
                if (right != null && right.GetComponent<Button>().interactable)
                {
                    right.GetComponent<Button>().onClick.Invoke();
                    return true;
                }
                return false;
            }

            foreach (Transform chip in pile)
            {
                var comp = chip.GetComponent<DragChip>();
                if (comp == null || _usedChips.Contains(chip.gameObject)) continue;
                _usedChips.Add(chip.gameObject);
                var boxT = gp.ActivityArea.transform.Find(comp.Payload == "red" ? "RedBox" : "BlueBox");
                if (boxT == null) return false;
                SimulateDrop(chip.gameObject, (RectTransform)boxT);
                _dragIdx++;
                return false;
            }
            return false;
        }

        static Button FindChildButton(Transform root, string path)
        {
            if (root == null) return null;
            var t = root.Find(path);
            return t != null ? t.GetComponent<Button>() : null;
        }

        static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        static Transform PicsRoot(GameplayScreen gp)
        {
            return gp.ActivityArea != null ? gp.ActivityArea.transform.Find("Pics") : null;
        }

        static GameObject NextButton(UIManager m)
        {
            var gp = m.Get<GameplayScreen>();
            if (gp == null || gp.Panel == null) return null;
            var t = gp.Panel.transform.Find("Next");
            return t != null ? t.gameObject : null;
        }

        static GameObject FindPicChild(Transform picsRoot, string correctId, bool match)
        {
            if (picsRoot == null) return null;
            foreach (Transform pic in picsRoot)
            {
                bool isCorrect = pic.name == "Pic_" + correctId;
                if (isCorrect == match) return pic.gameObject;
            }
            return null;
        }

        static GameObject FindLabeledButton(GameObject area, string correctLabel, bool match)
        {
            if (area == null) return null;
            var buttons = area.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                var t = b.GetComponentInChildren<TMP_Text>(true);
                if (t == null) continue;
                bool isCorrect = t.text.Trim() == correctLabel;
                if (isCorrect == match) return b.gameObject;
            }
            return null;
        }
    }
}
