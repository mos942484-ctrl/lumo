using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Lumo.UI;
using Lumo.Modules;

namespace Lumo.Core
{
    /// <summary>
    /// Dev-only storyboard capture driver (companion to AutoTestDriver): walks the REAL UI
    /// through every screen, captures 1920x1080 PNGs for the visual storyboard PDF, then
    /// plays one full level per island (solving every module type with the real buttons)
    /// to capture chapter intros, gameplay rounds, results and the finale. The child's
    /// save file is snapshotted before the run and restored byte-for-byte afterwards.
    /// Spawned from an editor command via StoryboardShotRunner.Begin(outDir).
    /// </summary>
    public class StoryboardShotRunner : MonoBehaviour
    {
        string _dir;

        // ---- solver state (mirrors AutoTestDriver) ----
        int _dragIdx, _matchStep, _tapIdx, _keyIdx, _keyUseIdx, _totalSortCount, _artColor;
        int[] _artCurrent;
        string _lastChallengeId;
        readonly HashSet<GameObject> _usedChips = new HashSet<GameObject>();
        readonly List<string> _tapSequence = new List<string>();

        // ---- save snapshot ----
        bool _saveExisted;
        byte[] _saveBytes;
        string _savePath;

        public static void Begin(string dir)
        {
            var go = new GameObject("StoryboardShotRunner");
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<StoryboardShotRunner>();
            runner._dir = dir;
            runner.StartCoroutine(runner.Tour());
            Debug.Log("[Shots] runner spawned -> " + dir);
        }

        IEnumerator Tour()
        {
            Directory.CreateDirectory(_dir);
            SnapshotSave();
            OverrideSave();

            var ui = UIManager.Instance;
            var gm = GameManager.Instance;
            if (ui == null || gm == null)
            {
                Debug.LogError("[Shots] UIManager/GameManager missing — aborting");
                yield break;
            }

            // ================= Phase 1: every static screen =================
            ui.Show("Splash"); yield return Wait(1.5f); yield return Shot("01_splash");
            ui.Show("MainMenu"); yield return Wait(0.8f); yield return Shot("02_main_menu");

            ui.ShowOverlay("HowToPlay"); yield return Wait(0.45f); yield return Shot("03_how_to_play"); ui.HideOverlay("HowToPlay");
            ui.ShowOverlay("Collection"); yield return Wait(0.45f); yield return Shot("04_collection"); ui.HideOverlay("Collection");
            ui.ShowOverlay("Settings"); yield return Wait(0.45f); yield return Shot("05_settings"); ui.HideOverlay("Settings");
            ui.OpenParentsArea(); yield return Wait(0.6f); yield return Shot("06_parents_area");
            ui.HideOverlay("Parents"); ui.HideOverlay("ParentalGate");

            ui.Show("CharacterSelect"); yield return Wait(0.7f); yield return Shot("07_character_select");

            ui.Show("Tutorial");
            var tut = ui.Get<TutorialScreen>();
            var showStep = typeof(TutorialScreen).GetMethod("ShowStep", BindingFlags.NonPublic | BindingFlags.Instance);
            if (tut != null && showStep != null)
                for (int s = 0; s < 4; s++)
                {
                    showStep.Invoke(tut, new object[] { s });
                    yield return Wait(0.5f);
                    yield return Shot("08_tutorial_step" + (s + 1));
                }

            ui.Show("WorldMap"); yield return Wait(1.0f); yield return Shot("12_world_map");
            ui.Show("SessionRecap"); yield return Wait(0.6f); yield return Shot("13_session_recap");
            ui.Show("ProgressReport"); yield return Wait(0.6f); yield return Shot("14_progress_report");

            // ================= Phase 2: level tour (all 13 islands) =================
            for (int id = 1; id <= 13; id++)
            {
                gm.SelectedLevelId = id;
                ui.Show("LevelIntro"); yield return Wait(0.8f);
                yield return Shot("20_intro_L" + id.ToString("00"));

                var li = ui.Get<LevelIntroScreen>();
                var startT = li != null && li.Panel != null ? li.Panel.transform.Find("Start") : null;
                var start = startT != null ? startT.GetComponent<Button>() : null;
                if (start == null)
                {
                    Debug.LogError("[Shots] L" + id + ": Start button not found — skipping level");
                    continue;
                }
                start.onClick.Invoke();
                yield return Wait(1.1f);
                yield return Shot("21_gameplay_L" + id.ToString("00") + "_R1");

                // sample overlays once, on early levels
                if (id == 2)
                {
                    ui.ShowOverlay("Pause"); yield return Wait(0.45f); yield return Shot("30_pause_panel"); ui.HideOverlay("Pause");
                }
                if (id == 4)
                {
                    ui.ShowOverlay("Hint"); yield return Wait(0.45f); yield return Shot("31_hint_panel"); ui.HideOverlay("Hint");
                }

                // solve every round through the real UI until the result screen appears
                float deadline = Time.realtimeSinceStartup + 150f;
                bool resultShot = false;
                while (Time.realtimeSinceStartup < deadline)
                {
                    var m = UIManager.Instance;
                    var g = GameManager.Instance;
                    if (m == null || g == null) yield break;
                    string cur = m.CurrentScreenName;
                    if (cur == "LevelResult" || cur == "FinalResult")
                    {
                        if (!resultShot)
                        {
                            yield return Wait(1.2f);
                            yield return Shot(cur == "FinalResult" ? "51_final_result" : "50_level_result");
                            resultShot = true;
                        }
                        break;
                    }
                    if (cur != "Gameplay") { yield return Wait(0.3f); continue; }

                    var gp = m.Get<GameplayScreen>();
                    var nextGo = NextGo(gp);
                    if (nextGo != null && nextGo.activeInHierarchy)
                    {
                        nextGo.GetComponent<Button>().onClick.Invoke();
                        yield return Wait(0.4f);
                        continue;
                    }
                    if (g.CurrentChallenge == null) { yield return null; continue; }
                    try { SolveTick(gp, g); }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[Shots] solve error L" + id + " " + g.CurrentChallenge.id + ": " + e.Message);
                        break;
                    }
                    yield return Wait(0.15f);
                }
                if (!resultShot) Debug.LogError("[Shots] level " + id + " never reached a result screen");
            }

            RestoreSave();
            File.WriteAllText(Path.Combine(_dir, "_done.txt"), "done\n");
            Debug.Log("[Shots] DONE — all captures written to " + _dir);
        }

        // ================= capture =================
        IEnumerator Wait(float seconds) { yield return new WaitForSecondsRealtime(seconds); }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var cam = UIManager.Instance != null && UIManager.Instance.Cam != null ? UIManager.Instance.Cam : Camera.main;
            if (cam == null) { Debug.LogError("[Shots] no camera for " + name); yield break; }
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            cam.targetTexture = previous;
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log("[Shots] saved " + name);
        }

        // ================= save handling =================
        void SnapshotSave()
        {
            var sm = SaveManager.Instance;
            if (sm == null) return;
            _savePath = sm.SavePath;
            _saveExisted = File.Exists(_savePath);
            if (_saveExisted) _saveBytes = File.ReadAllBytes(_savePath);
            Debug.Log("[Shots] save snapshot taken (existed=" + _saveExisted + ")");
        }

        void OverrideSave()
        {
            var sm = SaveManager.Instance;
            if (sm == null || sm.Data == null) return;
            var d = sm.Data;
            // Story path only: levels 1-12 are marked complete below, which opens the finale.
            d.calmMode = true;         // untimed -> deterministic, no timer HUD
            d.timedMode = false;
            d.practiceMode = false;
            d.dailyLimitMinutes = 0;
            d.highestUnlocked = 13;
            d.tutorialDone = true;
            d.avatar = 1;
            if (d.reviewWords == null || d.reviewWords.Length > 0) d.reviewWords = new string[0];
            if (d.levelRecords == null || d.levelRecords.Length < 13) d.levelRecords = new LevelRecord[13];
            int stars = 0;
            for (int i = 0; i < 12; i++)
            {
                if (d.levelRecords[i] == null) d.levelRecords[i] = new LevelRecord();
                d.levelRecords[i].completed = true;
                d.levelRecords[i].stars = new[] { 3, 3, 3 };
                d.levelRecords[i].bestScore = 90;
                stars += 18;
            }
            if (d.totalStars < stars) d.totalStars = stars;
            sm.Save();
            Debug.Log("[Shots] save overridden (all islands unlocked, calm mode)");
        }

        void RestoreSave()
        {
            try
            {
                if (string.IsNullOrEmpty(_savePath)) return;
                if (_saveExisted && _saveBytes != null) File.WriteAllBytes(_savePath, _saveBytes);
                else if (!_saveExisted && File.Exists(_savePath)) File.Delete(_savePath);
                var tmp = _savePath + ".tmp";
                if (File.Exists(tmp)) File.Delete(tmp);
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.Reload();
                    if (AdaptiveManager.Instance != null) AdaptiveManager.Instance.HydrateFromSave();
                }
                Debug.Log("[Shots] save restored");
            }
            catch (System.Exception e) { Debug.LogError("[Shots] save restore failed: " + e.Message); }
        }

        // ================= solver (mirrors AutoTestDriver, always-correct path) =================
        void ResetRoundState(ChallengeData c)
        {
            _lastChallengeId = c.id;
            _dragIdx = 0; _matchStep = 0; _tapIdx = 0; _keyIdx = 0; _keyUseIdx = 0;
            _usedChips.Clear(); _tapSequence.Clear(); _totalSortCount = 0;
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

        void SolveTick(GameplayScreen gp, GameManager gm)
        {
            if (gp == null || gp.AnswerArea == null) return;
            var c = gm.CurrentChallenge;
            if (c == null) return;
            if (_lastChallengeId != c.id) ResetRoundState(c);

            GameObject go;
            Button b;
            switch (c.type)
            {
                case "word-picture":
                    go = FindPicChild(PicsRoot(gp), c.correctAnswer, true);
                    b = go != null ? go.GetComponent<Button>() : null;
                    if (b != null && b.interactable) b.onClick.Invoke();
                    break;
                case "math-choice":
                    var mcm = gp.CurrentModule as MathChoiceModule;
                    string expected = mcm != null ? mcm.CurrentExpected.ToString() : c.correctAnswer;
                    go = FindLabeledButton(gp.AnswerArea, expected, true);
                    b = go != null ? go.GetComponent<Button>() : null;
                    if (b != null && b.interactable) b.onClick.Invoke();
                    break;
                case "spelling":
                case "sentence":
                    DragTick(gp, c);
                    break;
                case "matching":
                    TryMatching(gp, c);
                    break;
                case "potion":
                case "directions":
                    TryTapSequence(gp, c);
                    break;
                case "sorting":
                    TrySorting(gp, c);
                    break;
                case "memory-match":
                    TryMemoryMatch(gp);
                    break;
                case "pixel-art":
                    TryPixelArt(gp, c);
                    break;
                case "music-play":
                    TryMusicPlay(gp, c);
                    break;
                default:
                    go = FindLabeledButton(gp.AnswerArea, c.correctAnswer, true);
                    b = go != null ? go.GetComponent<Button>() : null;
                    if (b != null && b.interactable) b.onClick.Invoke();
                    break;
            }
        }

        void DragTick(GameplayScreen gp, ChallengeData c)
        {
            var activity = gp.ActivityArea;
            var slotsRow = activity != null ? activity.transform.Find("Slots") : null;
            if (slotsRow == null) return;

            if (c.type == "spelling")
            {
                string word = c.media.word;
                if (_dragIdx >= word.Length) return;
                char target = word[_dragIdx];
                var slotRt = slotsRow.Find("Slot" + _dragIdx) as RectTransform;
                if (slotRt == null) return;
                var lettersRow = activity.transform.Find("Letters");
                if (lettersRow == null) return;
                foreach (Transform chip in lettersRow)
                {
                    var comp = chip.GetComponent<DragChip>();
                    if (comp == null || comp.Payload != target.ToString()) continue;
                    if (_usedChips.Contains(chip.gameObject)) continue;
                    _usedChips.Add(chip.gameObject);
                    SimulateDrop(chip.gameObject, slotRt);
                    _dragIdx++;
                    return;
                }
                return;
            }

            // sentence
            var words = c.media.sentence.Replace(".", "").Split(' ');
            if (_dragIdx >= words.Length)
            {
                var submitT = activity.transform.Find("Submit");
                var submit = submitT != null ? submitT.GetComponent<Button>() : null;
                if (submit != null) { submit.onClick.Invoke(); _dragIdx = 0; }
                return;
            }
            var slot2 = slotsRow.Find("Slot_" + _dragIdx) as RectTransform;
            if (slot2 == null) return;
            var cardsRow = gp.AnswerArea.transform.Find("Cards");
            if (cardsRow == null) return;
            foreach (Transform chip in cardsRow)
            {
                var comp = chip.GetComponent<DragChip>();
                if (comp == null || comp.Payload.ToLower() != words[_dragIdx].ToLower()) continue;
                if (_usedChips.Contains(chip.gameObject)) continue;
                _usedChips.Add(chip.gameObject);
                SimulateDrop(chip.gameObject, slot2);
                _dragIdx++;
                return;
            }
        }

        void TryMatching(GameplayScreen gp, ChallengeData c)
        {
            var seq = new List<string>();
            foreach (var t in c.media.triples)
            {
                seq.Add("Card_num_" + t.num);
                seq.Add("Card_pic_" + t.num);
                seq.Add("Card_pic_" + t.num);
                seq.Add("Card_word_" + t.num);
            }
            if (_matchStep >= seq.Count) return;
            ClickCard(gp.ActivityArea, seq[_matchStep]);
            _matchStep++;
        }

        void ClickCard(GameObject activity, string cardName)
        {
            var t = activity.transform.Find(cardName);
            if (t == null) { Debug.LogError("[Shots] card missing: " + cardName); return; }
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

        void TryTapSequence(GameplayScreen gp, ChallengeData c)
        {
            if (_tapIdx < _tapSequence.Count)
            {
                string tap = _tapSequence[_tapIdx];
                Button b = null;
                if (c.type == "potion")
                {
                    if (tap == "potion-remove")
                    {
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
                if (b == null) return;
                b.onClick.Invoke();
                _tapIdx++;
                if (tap == "key-collect" || tap == "potion-remove") _keyIdx++;
                if (tap == "key-use") _keyUseIdx++;
                return;
            }

            // taps complete
            if (c.correctAnswer == "target") return;   // module advanced on arrival
            var right = FindLabeledButton(gp.AnswerArea, c.correctAnswer, true);
            if (right != null && right.GetComponent<Button>().interactable)
                right.GetComponent<Button>().onClick.Invoke();
        }

        void TryMemoryMatch(GameplayScreen gp)
        {
            var mem = gp.CurrentModule as MemoryMatchModule;
            if (mem == null || mem.IsLocked) return;
            var pair = mem.GetUnmatchedPairIndices();
            if (pair == null) return;
            var b0 = FindChildButton(gp.ActivityArea.transform, "MemoryGrid/Card_" + pair[0]);
            var b1 = FindChildButton(gp.ActivityArea.transform, "MemoryGrid/Card_" + pair[1]);
            if (b0 == null || b1 == null) return;
            b0.onClick.Invoke();
            b1.onClick.Invoke();
        }

        void TryPixelArt(GameplayScreen gp, ChallengeData c)
        {
            if (_artCurrent == null || c.media.targetGrid == null) return;
            for (int i = 0; i < c.media.targetGrid.Length; i++)
            {
                int want = c.media.targetGrid[i];
                if (_artCurrent[i] == want) continue;
                if (_artColor != want)
                {
                    var sw = FindChildButton(gp.ActivityArea.transform, "PaletteCard/Swatches/Swatch_" + want);
                    if (sw == null) return;
                    sw.onClick.Invoke();
                    _artColor = want;
                }
                var cell = FindChildButton(gp.ActivityArea.transform, "CanvasCard/PixelGrid/Cell_" + i);
                if (cell == null) return;
                cell.onClick.Invoke();
                _artCurrent[i] = want;
                return;
            }
            var check = FindChildButton(gp.AnswerArea.transform, "ArtControls/CheckArt");
            if (check == null) return;
            check.onClick.Invoke();
        }

        void TryMusicPlay(GameplayScreen gp, ChallengeData c)
        {
            var mm = gp.CurrentModule as MusicPlayModule;
            if (mm != null && mm.IsPlayingDemo) return;
            if (c.media.notes == null || c.media.notes.Length == 0) return;
            if (c.media.musicMode == "next")
            {
                string last = c.media.notes[c.media.notes.Length - 1];
                var b = FindChildButton(gp.ActivityArea.transform, "XyloRack/Bars/Bar_" + last);
                if (b == null) return;
                b.onClick.Invoke();
                return;
            }
            if (_tapIdx < c.media.notes.Length)
            {
                var b = FindChildButton(gp.ActivityArea.transform, "XyloRack/Bars/Bar_" + c.media.notes[_tapIdx]);
                if (b == null) return;
                b.onClick.Invoke();
                _tapIdx++;
            }
        }

        void TrySorting(GameplayScreen gp, ChallengeData c)
        {
            var pile = gp.ActivityArea.transform.Find("Pile");
            if (pile == null) return;

            if (_dragIdx >= _totalSortCount)
            {
                var right = FindLabeledButton(gp.AnswerArea, c.correctAnswer, true);
                if (right != null && right.GetComponent<Button>().interactable)
                    right.GetComponent<Button>().onClick.Invoke();
                return;
            }

            foreach (Transform chip in pile)
            {
                var comp = chip.GetComponent<DragChip>();
                if (comp == null || _usedChips.Contains(chip.gameObject)) continue;
                _usedChips.Add(chip.gameObject);
                var boxT = gp.ActivityArea.transform.Find(comp.Payload == "red" ? "RedBox" : "BlueBox");
                if (boxT == null) return;
                SimulateDrop(chip.gameObject, (RectTransform)boxT);
                _dragIdx++;
                return;
            }
        }

        static GameObject NextGo(GameplayScreen gp)
        {
            if (gp == null || gp.Panel == null) return null;
            var t = gp.Panel.transform.Find("Next");
            return t != null ? t.gameObject : null;
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
