using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Lumo.UI;
using Lumo.Modules;

namespace Lumo.Core
{
    /// <summary>
    /// QA driver (dev-only, _tools/autotest.txt = "qa"): verifies §11 checks through the real UI —
    /// hint timing (2 → hint, 3 → solution), practice mode (no timer, no stars), pause/resume with a
    /// running timer, and save persistence. Logs [QA] PASS/FAIL lines.
    /// </summary>
    public class QaTestDriver : MonoBehaviour
    {
        int _phase;
        float _timer, _phaseTimer;
        int _mistakes;
        float _elapsedAtAnswer;
        bool _hintAfter2, _solutionAfter3, _practiceTimerNeverRan;
        bool _practiceWasOn, _timedWasOn;
        int _ageBandWas, _lastLevelWas;
        bool _storyLockOk, _ageBandOk, _calmOk, _practiceOk, _pauseOk, _realUiNavOk, _saveOk;

        void Update()
        {
            _timer += Time.deltaTime;
            _phaseTimer += Time.deltaTime;
            if (_timer < 0.45f) return;
            _timer = 0f;

            var m = UIManager.Instance;
            var gm = GameManager.Instance;
            if (m == null || gm == null) return;

            switch (_phase)
            {
                case 0: // ---- Phase 1 checks (Story Lock, Age Band routing, Calm Untimed Default) ----
                    var d = SaveManager.Instance.Data;
                    _practiceWasOn = d.practiceMode;
                    _timedWasOn = d.timedMode;
                    _ageBandWas = d.ageBand;
                    _lastLevelWas = d.lastLevel;

                    // Story path lock: the highest unlocked island is open, the next is not
                    int hu = LevelManager.Instance != null ? LevelManager.Instance.HighestUnlocked : 1;
                    _storyLockOk = LevelManager.Instance != null && LevelManager.Instance.IsUnlocked(hu)
                                   && (hu >= 13 || !LevelManager.Instance.IsUnlocked(hu + 1));
                    Debug.Log("[QA] story lock => " + (_storyLockOk ? "PASS" : "FAIL"));

                    // Age Band routing on L1R1 (save/restore skill 0 flags around check)
                    var skill0 = SaveManager.Instance.Skill(0);
                    bool prevRem = skill0.remediation;
                    bool prevHard = skill0.pendingHarder;
                    skill0.remediation = false;
                    skill0.pendingHarder = false;
                    var l1r1 = gm.Bank.levels[0].challenges[0];
                    d.ageBand = GameRules.AgeBand5To7;
                    var cEasy = AdaptiveManager.Instance.SelectChallenge(gm.Bank, l1r1);
                    d.ageBand = GameRules.AgeBand11To12;
                    var cHard = AdaptiveManager.Instance.SelectChallenge(gm.Bank, l1r1);
                    skill0.remediation = prevRem;
                    skill0.pendingHarder = prevHard;

                    bool p2BandOk = true;
                    ChallengeData p2Sample = null;
                    foreach (var lv in gm.Bank.levels)
                    {
                        if (lv.challenges == null) continue;
                        foreach (var ch in lv.challenges)
                            if (ch.id == "P2_L3R4") { p2Sample = ch; break; }
                        if (p2Sample != null) break;
                    }
                    if (p2Sample != null)
                    {
                        int memIdx = AdaptiveManager.SkillIndex(gm.Bank, p2Sample.skill);
                        var memStat = memIdx >= 0 ? SaveManager.Instance.Skill(memIdx) : null;
                        bool pRem = memStat != null && memStat.remediation;
                        bool pHard = memStat != null && memStat.pendingHarder;
                        if (memStat != null) { memStat.remediation = false; memStat.pendingHarder = false; }
                        d.ageBand = GameRules.AgeBand5To7;
                        var p57 = AdaptiveManager.Instance.SelectChallenge(gm.Bank, p2Sample);
                        d.ageBand = GameRules.AgeBand11To12;
                        var p1112 = AdaptiveManager.Instance.SelectChallenge(gm.Bank, p2Sample);
                        if (memStat != null) { memStat.remediation = pRem; memStat.pendingHarder = pHard; }
                        p2BandOk = p57 != null && p57.id == p2Sample.band5to7 && p1112 != null && p1112.id == p2Sample.band11to12;
                    }
                    _ageBandOk = cEasy != null && cEasy.id == "V_ADD_E1" && cHard != null && cHard.id == "V_ADD_H1" && p2BandOk;
                    Debug.Log("[QA] age band routing => " + (_ageBandOk ? "PASS" : "FAIL") +
                              " (5-7=" + (cEasy != null ? cEasy.id : "null") + ", 11-12=" + (cHard != null ? cHard.id : "null") + ")");

                    // Calm Untimed Default
                    d.practiceMode = false;
                    d.timedMode = false;
                    d.ageBand = GameRules.AgeBand8To10;
                    SaveManager.Instance.Save();
                    gm.SelectedLevelId = 1;
                    m.Show("Gameplay");
                    _phase = 10;
                    _phaseTimer = 0f;
                    break;

                case 10: // verify Calm Untimed Default did not start the timer, then start Practice Mode test
                    if (gm.CurrentChallenge == null) return;
                    _calmOk = !gm.IsTimedMode && !TimerManager.Instance.Running;
                    Debug.Log("[QA] calm untimed => " + (_calmOk ? "PASS" : "FAIL") +
                              " (isTimed=" + gm.IsTimedMode + " timerRunning=" + TimerManager.Instance.Running + ")");

                    SaveManager.Instance.Data.practiceMode = true;
                    SaveManager.Instance.Save();
                    _mistakes = 0;
                    gm.ForceRestartLevel();
                    _phase = 1;
                    _phaseTimer = 0f;
                    Debug.Log("[QA] practice mode test started");
                    break;

                case 1: // 3 wrong clicks (hint after 2, solution after 3), then the correct answer
                    if (gm.CurrentChallenge == null || gm.CurrentChallenge.type != "math-choice") return;
                    if (_mistakes < 3)
                    {
                        ClickWrongOption(gp(m), gm.CurrentChallenge.correctAnswer);
                        _mistakes++;
                        if (_mistakes == 2) _hintAfter2 = HintManager.Instance.HintShown;
                        if (_mistakes == 3) _solutionAfter3 = HintManager.Instance.SolutionShown;
                        return;
                    }
                    _elapsedAtAnswer = TimerManager.Instance.Elapsed;
                    _practiceTimerNeverRan = _elapsedAtAnswer < 0.05f;
                    ClickCorrect(m, gm, gp(m));
                    _phase = 2;
                    _phaseTimer = 0f;
                    break;

                case 2: // evaluate practice, then move to pause test with a RUNNING timer
                    var nextGo = NextButton(m);
                    if (nextGo == null || !nextGo.activeInHierarchy) return;
                    _practiceOk = _practiceTimerNeverRan && _hintAfter2 && _solutionAfter3;
                    Debug.Log("[QA] practice mode => " + (_practiceOk ? "PASS" : "FAIL") +
                              " (timerNeverRan=" + _practiceTimerNeverRan + " hintAfter2=" + _hintAfter2 +
                              " solutionAfter3=" + _solutionAfter3 + ")");

                    SaveManager.Instance.Data.practiceMode = false;   // force timed challenge mode for the pause test
                    SaveManager.Instance.Data.timedMode = true;
                    SaveManager.Instance.Save();
                    gm.ForceRestartLevel();   // fresh level 1 in timed mode — timer starts
                    _phase = 3;
                    _phaseTimer = 0f;
                    break;

                case 3: // wait until the timer is actually running, then pause via the real Pause button
                    if (!TimerManager.Instance.Running)
                    {
                        if (_phaseTimer > 8f) Debug.Log("[QA] diag: timer still not running (challenge=" +
                            (gm.CurrentChallenge != null ? gm.CurrentChallenge.id : "null") + ")");
                        return;
                    }
                    var pauseBtn = gp(m).Panel.transform.Find("ControlArea/Pause");
                    if (pauseBtn == null) { Debug.Log("[QA] diag: Pause button not found"); return; }
                    pauseBtn.GetComponent<Button>().onClick.Invoke();
                    _phase = 4;
                    _phaseTimer = 0f;
                    break;

                case 4: // check paused, then resume
                    bool pausedNow = TimerManager.Instance.Paused;
                    float e1 = TimerManager.Instance.Elapsed;
                    m.HideOverlay("Pause");
                    bool resumed = !TimerManager.Instance.Paused;
                    _pauseOk = pausedNow && resumed && TimerManager.Instance.Elapsed >= e1 - 0.01f;
                    Debug.Log("[QA] pause/resume => " + (_pauseOk ? "PASS" : "FAIL") +
                              " (paused=" + pausedNow + " resumed=" + resumed + ")");

                    TimerManager.Instance.Stop();
                    gm.AbortLevel();
                    m.Show("MainMenu");
                    _phase = 20;
                    _phaseTimer = 0f;
                    break;

                case 20: // Real UI Nav: MainMenu -> WorldMap
                    if (m.CurrentScreenName != "MainMenu") return;
                    m.Show("WorldMap");
                    _phase = 21;
                    _phaseTimer = 0f;
                    break;

                case 21: // Real UI Nav: click Level2 on WorldMap
                    if (m.CurrentScreenName != "WorldMap") return;
                    var wm = m.Get<WorldMapScreen>();
                    var lvl2Btn = wm != null && wm.Panel != null ? wm.Panel.transform.Find("Row0/Island2/Level2")?.GetComponent<Button>() : null;
                    if (lvl2Btn == null)
                    {
                        Debug.Log("[QA] real UI nav => FAIL (WorldMap Level2 button not found)");
                        _realUiNavOk = false;
                        _phase = 5;
                        break;
                    }
                    lvl2Btn.onClick.Invoke();
                    _phase = 22;
                    _phaseTimer = 0f;
                    break;

                case 22: // Real UI Nav: wait for LevelIntro, click Start
                    if (m.CurrentScreenName != "LevelIntro")
                    {
                        if (_phaseTimer > 8f)
                        {
                            Debug.Log("[QA] real UI nav => FAIL (LevelIntro timeout, current=" + m.CurrentScreenName + ")");
                            _realUiNavOk = false;
                            _phase = 5;
                        }
                        return;
                    }
                    var li = m.Get<LevelIntroScreen>();
                    var startBtn = li != null && li.Panel != null ? li.Panel.transform.Find("Start")?.GetComponent<Button>() : null;
                    if (startBtn == null)
                    {
                        Debug.Log("[QA] real UI nav => FAIL (LevelIntro Start button not found)");
                        _realUiNavOk = false;
                        _phase = 5;
                        break;
                    }
                    startBtn.onClick.Invoke();
                    _phase = 23;
                    _phaseTimer = 0f;
                    break;

                case 23: // Real UI Nav: wait for Gameplay, verify non-blank UI, click ControlArea/Home, verify AbortLevel
                    if (m.CurrentScreenName != "Gameplay")
                    {
                        if (_phaseTimer > 8f)
                        {
                            Debug.Log("[QA] real UI nav => FAIL (Gameplay timeout, current=" + m.CurrentScreenName + ")");
                            _realUiNavOk = false;
                            _phase = 5;
                        }
                        return;
                    }
                    var gscreen = gp(m);
                    bool hasChildren = gscreen != null && gscreen.ActivityArea != null && gscreen.ActivityArea.transform.childCount > 0;
                    var bgImg = gscreen != null && gscreen.Panel != null ? gscreen.Panel.transform.Find("BG")?.GetComponent<Image>() : null;
                    bool hasBg = bgImg != null && bgImg.sprite != null;
                    var promptTxt = gscreen != null && gscreen.Panel != null ? gscreen.Panel.transform.Find("InstructionArea/Prompt")?.GetComponent<TMP_Text>() : null;
                    bool hasPrompt = promptTxt != null && !string.IsNullOrEmpty(promptTxt.text);
                    var homeBtn = gscreen != null && gscreen.Panel != null ? gscreen.Panel.transform.Find("ControlArea/Home")?.GetComponent<Button>() : null;
                    bool aborted = false;
                    if (homeBtn != null)
                    {
                        homeBtn.onClick.Invoke();
                        aborted = gm.CurrentChallenge == null;
                    }
                    _realUiNavOk = hasChildren && hasBg && hasPrompt && aborted;
                    Debug.Log("[QA] real UI nav => " + (_realUiNavOk ? "PASS" : "FAIL") +
                              " (children=" + hasChildren + " bg=" + hasBg + " prompt=" + hasPrompt + " aborted=" + aborted + ")");
                    _phase = 5;
                    _phaseTimer = 0f;
                    break;

                case 5: // restore original settings & verify save persistence
                    var data = SaveManager.Instance.Data;
                    data.practiceMode = _practiceWasOn;
                    data.timedMode = _timedWasOn;
                    data.ageBand = _ageBandWas;
                    data.lastLevel = _lastLevelWas;
                    SaveManager.Instance.Save();

                    var reloaded = SaveManager.Instance.Load();
                    _saveOk = reloaded.totalStars == SaveManager.Instance.Data.totalStars &&
                              reloaded.highestUnlocked == SaveManager.Instance.Data.highestUnlocked &&
                              reloaded.ageBand == _ageBandWas &&
                              reloaded.timedMode == _timedWasOn;
                    Debug.Log("[QA] save persistence => " + (_saveOk ? "PASS" : "FAIL") +
                              " (stars=" + SaveManager.Instance.Data.totalStars + " unlocked=" +
                              SaveManager.Instance.Data.highestUnlocked + ")");
                    bool allOk = _storyLockOk && _ageBandOk && _calmOk && _practiceOk && _pauseOk && _realUiNavOk && _saveOk;
                    Debug.Log("[QA] ALL CHECKS " + (allOk ? "PASS" : "SEE LINES ABOVE"));
                    TimerManager.Instance.Stop();
                    _phase = 6;
                    Destroy(gameObject);
                    break;
            }
        }

        GameplayScreen gp(UIManager m) { return m.Get<GameplayScreen>(); }

        void ClickWrongOption(GameplayScreen gp, string correct)
        {
            if (gp == null || gp.AnswerArea == null) return;
            var buttons = gp.AnswerArea.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                var t = b.GetComponentInChildren<TMP_Text>(true);
                if (t != null && t.text.Trim() != correct)
                {
                    if (b.interactable) b.onClick.Invoke();
                    return;
                }
            }
        }

        void ClickCorrect(UIManager m, GameManager gm, GameplayScreen gp)
        {
            if (gp == null || gp.AnswerArea == null) return;
            if (gm.CurrentChallenge.type == "math-choice" && gp.CurrentModule is MathChoiceModule)
            {
                string expected = ((MathChoiceModule)gp.CurrentModule).CurrentExpected.ToString();
                ClickLabeled(gp.AnswerArea, expected);
            }
            else
            {
                ClickLabeled(gp.AnswerArea, gm.CurrentChallenge.correctAnswer);
            }
        }

        void ClickLabeled(GameObject area, string label)
        {
            var buttons = area.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                var t = b.GetComponentInChildren<TMP_Text>(true);
                if (t != null && t.text.Trim() == label)
                {
                    if (b.interactable) b.onClick.Invoke();
                    return;
                }
            }
        }

        static GameObject NextButton(UIManager m)
        {
            var gp = m.Get<GameplayScreen>();
            if (gp == null || gp.Panel == null) return null;
            var t = gp.Panel.transform.Find("Next");
            return t != null ? t.gameObject : null;
        }
    }
}
