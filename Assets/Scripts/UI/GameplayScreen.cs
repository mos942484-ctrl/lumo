using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.Modules;

namespace Lumo.UI
{
    /// <summary>
    /// Gameplay screen with 5 fixed zones identical across all level types (§7):
    /// 1 top bar (level, round, timer, stars + progress) · 2 main activity area ·
    /// 3 instruction area · 4 answer area · 5 control area (Hint, Repeat, Pause, Home).
    /// </summary>
    public class GameplayScreen : ScreenBase
    {
        public override string Name { get { return "Gameplay"; } }

        public GameObject ActivityArea { get; private set; }
        public GameObject AnswerArea { get; private set; }
        public ModuleBase CurrentModule { get { return _module; } }

        TMP_Text _hud, _timer, _prompt, _modeBadge;
        Image _progressFill;
        Image[] _starImgs;
        // Batch 4 fix: star sprites cached once in Build — Tick used to call
        // GameAssets.Ui("ui_star"/"ui_star_empty") 3x per frame.
        Sprite _starFull, _starEmpty;
        int _shownStars = -1;
        GameObject _nextBtn, _banner, _barBgGo;
        TMP_Text _bannerText;
        ModuleBase _module;
        float _bannerTimer;
        GameObject _struggleRow;      // LXD Phase F
        string _struggleVariantId;    // LXD Phase F: variant id offered
        readonly System.Collections.Generic.List<Coroutine> _routines = new System.Collections.Generic.List<Coroutine>();

        public override void Build(UIManager m)
        {
            base.Build(m);

            var bg = UIKit.Bg(Panel, "BG", null);
            bg.color = UIKit.Cream;
            UIKit.Stretch(bg.rectTransform);

            var backdropTint = UIKit.Panel(Panel, "BackdropTint", new Color(0.08f, 0.13f, 0.20f, 0.14f));
            backdropTint.raycastTarget = false;
            UIKit.Stretch(backdropTint.rectTransform);

            // ---- zone 1: floating top capsule HUD ----
            var top = UIKit.Card(Panel, "TopBar", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            var topRt = top.rectTransform;
            UIKit.Place(topRt, new Vector2(0.5f, 0.946f), Vector2.zero, new Vector2(1820, 84));

            var hudPill = UIKit.Panel(top.gameObject, "HudPill", UIKit.SoftGold, GameAssets.Ui("ui_btn"));
            hudPill.raycastTarget = false;
            UIKit.Place(hudPill.rectTransform, new Vector2(0.015f, 0.5f), Vector2.zero, new Vector2(720, 58), new Vector2(0f, 0.5f));

            _hud = UIKit.Label(top.gameObject, "Hud", "", 36, UIKit.Ink, TextAlignmentOptions.Left);
            var hudRt = _hud.GetComponent<RectTransform>();
            UIKit.Place(hudRt, new Vector2(0.028f, 0.5f), Vector2.zero, new Vector2(680, 54), new Vector2(0f, 0.5f));

            _modeBadge = UIKit.Label(top.gameObject, "Practice", "", 30, UIKit.Teal, TextAlignmentOptions.Center);
            UIKit.Place(_modeBadge.GetComponent<RectTransform>(), new Vector2(0.50f, 0.5f), Vector2.zero, new Vector2(260, 56));

            _timer = UIKit.Label(top.gameObject, "Timer", "00:30", 42, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Place(_timer.GetComponent<RectTransform>(), new Vector2(0.53f, 0.5f), Vector2.zero, new Vector2(180, 60));

            // stars
            _starFull = GameAssets.Ui("ui_star");
            _starEmpty = GameAssets.Ui("ui_star_empty");
            _starImgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var s = UIKit.Img(top.gameObject, "Star" + i, _starEmpty, new Color(1f, 1f, 1f, 0.88f));
                UIKit.Place(s.rectTransform, new Vector2(0.625f + i * 0.042f, 0.5f), Vector2.zero, new Vector2(52, 52));
                _starImgs[i] = s;
            }

            // progress bar
            var barBg = UIKit.Panel(top.gameObject, "BarBg", new Color(1, 1, 1, 0.55f), GameAssets.Ui("ui_bar_bg"));
            _barBgGo = barBg.gameObject;
            UIKit.Place(barBg.rectTransform, new Vector2(0.76f, 0.5f), Vector2.zero, new Vector2(410, 30), new Vector2(0f, 0.5f));
            _progressFill = UIKit.Panel(barBg.gameObject, "Fill", UIKit.Teal, GameAssets.Ui("ui_bar_fill"));
            var fillRt = _progressFill.rectTransform;
            fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = new Vector2(4, 4); fillRt.offsetMax = new Vector2(4, -4);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.sizeDelta = new Vector2(0, 0);

            // ---- zone 2: main activity area ----
            ActivityArea = UIKit.Node(Panel, "ActivityArea");
            var aaRt = ActivityArea.GetComponent<RectTransform>();
            UIKit.Place(aaRt, new Vector2(0.5f, 0.5f), new Vector2(0, 78), new Vector2(1860, 560));

            // ---- zone 3: storybook speech card ----
            var instr = UIKit.Card(Panel, "InstructionArea", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(instr.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -246), new Vector2(1580, 102));
            var lumoRing = UIKit.Panel(instr.gameObject, "LumoRing", UIKit.SoftGold, GameAssets.Ui("ui_node"));
            lumoRing.raycastTarget = false;
            UIKit.Place(lumoRing.rectTransform, new Vector2(0.048f, 0.5f), Vector2.zero, new Vector2(90, 90));
            var lumo = UIKit.Img(instr.gameObject, "Lumo", GameAssets.Char("char_lumo"), Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.048f, 0.5f), Vector2.zero, new Vector2(78, 78));
            _prompt = UIKit.Label(instr.gameObject, "Prompt", "", 38, UIKit.Ink, TextAlignmentOptions.Left);
            _prompt.enableAutoSizing = true;
            _prompt.fontSizeMin = 24;
            _prompt.fontSizeMax = 38;
            var prRt = _prompt.GetComponent<RectTransform>();
            UIKit.Place(prRt, new Vector2(0.095f, 0.5f), Vector2.zero, new Vector2(1380, 78), new Vector2(0f, 0.5f));

            // ---- zone 4: answer area (module fills) ----
            AnswerArea = UIKit.Node(Panel, "AnswerArea");
            var ansRt = AnswerArea.GetComponent<RectTransform>();
            UIKit.Place(ansRt, new Vector2(0.5f, 0.5f), new Vector2(0, -378), new Vector2(1860, 146));

            // ---- zone 5: floating bottom control dock ----
            var ctrl = UIKit.Card(Panel, "ControlArea", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            UIKit.Place(ctrl.rectTransform, new Vector2(0.5f, 0f), new Vector2(-160, 48), new Vector2(1380, 88));
            MakeCtrl(ctrl.gameObject, "Hint", "Hint", UIKit.Yellow, new Vector2(-480, 0), GameAssets.Ui("ui_icon_hint"), () =>
            {
                HintManager.Instance.MarkHintUsed();
                RefreshCalmStars();
                var c = GameManager.Instance.CurrentChallenge;
                if (c != null) M.GetOverlay<HintPanel>().ShowHint(c.hints[0]);
            });
            MakeCtrl(ctrl.gameObject, "Repeat", "Repeat", UIKit.SkyBlue, new Vector2(-160, 0), GameAssets.Ui("ui_icon_repeat"), () => GameManager.Instance.RepeatInstruction());
            MakeCtrl(ctrl.gameObject, "Pause", "Pause", UIKit.Purple, new Vector2(160, 0), GameAssets.Ui("ui_icon_pause"), () => M.ShowOverlay("Pause"));
            MakeCtrl(ctrl.gameObject, "Home", "Home", UIKit.Coral, new Vector2(480, 0), GameAssets.Ui("ui_icon_home"), () =>
            {
                GameManager.Instance.AbortLevel();
                M.Show("WorldMap");
            });

            // next button (child-paced advancing, §8.6)
            var nextBtn = UIKit.TextButton(Panel, "Next", "Next", new Vector2(300, 86), UIKit.Green,
                () => GameManager.Instance.Advance(), GameAssets.Ui("ui_btn_gold"), 42);
            UIKit.AttachButtonIcon(nextBtn, GameAssets.Ui("ui_icon_next"), 36f);
            _nextBtn = nextBtn.gameObject;
            UIKit.Place(_nextBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(690, 48), new Vector2(300, 86));
            _nextBtn.SetActive(false);

            // feedback banner (top of activity area — never overlaps prompt, cards, Next, or controls)
            var bannerImg = UIKit.Card(Panel, "Banner", UIKit.Green, GameAssets.Ui("ui_btn"));
            bannerImg.raycastTarget = false;
            _banner = bannerImg.gameObject;
            UIKit.Place(bannerImg.rectTransform, new Vector2(0.5f, 0.875f), Vector2.zero, new Vector2(1420, 82));
            _bannerText = UIKit.Label(_banner, "Text", "", 36, Color.white);
            _bannerText.enableAutoSizing = true;
            _bannerText.fontSizeMin = 22;
            _bannerText.fontSizeMax = 36;
            UIKit.Stretch(_bannerText.rectTransform, 32, 10);
            _banner.SetActive(false);

            // LXD Phase F: learner-agency struggle choice - small, bottom-right (clear of
            // the control dock and the Next button), two buttons. Same overlay in calm
            // mode (no sound either way - it is a silent choice, never a punisher).
            var struggle = UIKit.Card(Panel, "StruggleChoice", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            _struggleRow = struggle.gameObject;
            UIKit.Place(struggle.rectTransform, new Vector2(0.885f, 0.045f), Vector2.zero, new Vector2(396, 86));
            var ask = UIKit.Label(struggle.gameObject, "Ask", "Tricky?", 24, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Place(ask.rectTransform, new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(360, 32));
            var easierBtn = UIKit.TextButton(struggle.gameObject, "Easier", "Easier", new Vector2(172, 48), UIKit.Seafoam, () =>
            {
                AcceptStruggleSwap();
            }, GameAssets.Ui("ui_btn"), 23);
            UIKit.Place(easierBtn.GetComponent<RectTransform>(), new Vector2(0.27f, 0.30f), Vector2.zero, new Vector2(172, 48));
            var againBtn = UIKit.TextButton(struggle.gameObject, "Again", "Try again", new Vector2(172, 48), UIKit.SoftGold, () =>
            {
                DismissStruggleChoice();
            }, GameAssets.Ui("ui_btn"), 23);
            UIKit.Place(againBtn.GetComponent<RectTransform>(), new Vector2(0.73f, 0.30f), Vector2.zero, new Vector2(172, 48));
            _struggleRow.SetActive(false);
        }

        void AcceptStruggleSwap()
        {
            string variantId = _struggleVariantId;
            DismissStruggleChoice();
            if (string.IsNullOrEmpty(variantId)) return;
            ShowToast("Here is an easier game!");
            GameManager.Instance.ReplaceCurrentChallenge(variantId);
        }

        void DismissStruggleChoice()
        {
            _struggleVariantId = null;
            if (_struggleRow != null) _struggleRow.SetActive(false);
        }

        void MakeCtrl(GameObject parent, string name, string label, Color c, Vector2 x, Sprite icon, System.Action onClick)
        {
            var b = UIKit.TextButton(parent, name, label, new Vector2(280, 68), c, onClick, GameAssets.Ui("ui_btn"), 32);
            UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), x, new Vector2(280, 68));
            UIKit.AttachButtonIcon(b, icon, 36f);
        }

        public void StartRoutine(IEnumerator e)
        {
            if (M != null) _routines.Add(M.StartCoroutine(e));
        }

        public Coroutine StartTrackedRoutine(IEnumerator e)
        {
            if (M == null) return null;
            var c = M.StartCoroutine(e);
            _routines.Add(c);
            return c;
        }

        public void StopTrackedRoutine(Coroutine c)
        {
            if (c == null) return;
            if (M != null) M.StopCoroutine(c);
            _routines.Remove(c);
        }

        void StopModuleRoutines()
        {
            if (M != null)
            {
                for (int i = 0; i < _routines.Count; i++)
                    if (_routines[i] != null) M.StopCoroutine(_routines[i]);
            }
            _routines.Clear();
        }

        public void ShowToast(string msg) { M.Toast(msg, 1.6f); }

        // ---- flow ----
        public void ReportAnswer(bool correct)
        {
            GameManager.Instance.ReportAnswer(correct);
        }

        public override void OnShow()
        {
            base.OnShow();
            AudioManager.Instance.PlayMusic("music_game");
            var gm = GameManager.Instance;
            gm.RoundStarted += OnRoundStarted;
            gm.Answered += OnAnswered;
            gm.RoundCompleted += OnRoundCompleted;
            gm.LevelCompleted += OnLevelCompleted;
            gm.StruggleOffered += OnStruggleOffered; // LXD Phase F
            FeedbackManager.Instance.OnSuccess += OnFeedbackSuccess;
            FeedbackManager.Instance.OnRetry += OnFeedbackRetry;
            FeedbackManager.Instance.OnHint += OnHintRequested;
            FeedbackManager.Instance.OnSolution += OnSolutionRequested;

            UpdateModeBadge(gm);
            SetHudVisible(gm.IsTimedMode, !gm.PracticeMode);

            if (gm.CurrentChallenge == null)
            {
                // defensive: never start with an unset level id (0 would index out of range)
                if (gm.SelectedLevelId < 1)
                    gm.SelectedLevelId = Mathf.Max(1, SaveManager.Instance.Data.lastLevel);
                GameManager.Instance.StartLevel(gm.SelectedLevelId);
            }
            else
            {
                // StartLevel() may be called before M.Show("Gameplay") (e.g. LevelIntro / Play Again),
                // firing RoundStarted before OnShow() subscribed — render the active challenge now.
                OnRoundStarted(gm.CurrentChallenge);
            }
        }

        void UpdateModeBadge(GameManager gm)
        {
            if (_modeBadge == null || gm == null) return;
            _modeBadge.gameObject.SetActive(!gm.IsTimedMode);
            _modeBadge.text = gm.PracticeMode ? "Practice Mode" : (gm.IsTimedMode ? "Timed Challenge" : "Calm Play");
        }

        void SetHudVisible(bool showTimer, bool showStars)
        {
            if (_timer != null) _timer.gameObject.SetActive(showTimer);
            if (_barBgGo != null) _barBgGo.SetActive(showTimer);
            if (_modeBadge != null) _modeBadge.gameObject.SetActive(!showTimer);
            if (_starImgs != null)
                for (int i = 0; i < _starImgs.Length; i++)
                    if (_starImgs[i] != null) _starImgs[i].gameObject.SetActive(showStars);
        }

        void RefreshCalmStars()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.PracticeMode || gm.IsTimedMode) return;
            RefreshStarDisplay(GameRules.StarsForCalmAnswer(HintManager.Instance.Mistakes, HintManager.Instance.UsedHelp));
        }

        void RefreshStarDisplay(int stars)
        {
            if (_starImgs == null || stars == _shownStars) return;
            _shownStars = stars;
            for (int i = 0; i < 3; i++)
            {
                var sprite = i < stars ? _starFull : _starEmpty;
                if (sprite != null) _starImgs[i].sprite = sprite;
                _starImgs[i].color = i < stars ? Color.white : new Color(1f, 1f, 1f, 0.88f);
            }
        }

        void DestroyConfetti()
        {
            if (Panel == null) return;
            var burst = Panel.transform.Find("ConfettiBurst");
            if (burst != null) Object.Destroy(burst.gameObject);
        }

        public override void OnHide()
        {
            StopModuleRoutines();
            DestroyConfetti();
            var gm = GameManager.Instance;
            gm.RoundStarted -= OnRoundStarted;
            gm.Answered -= OnAnswered;
            gm.RoundCompleted -= OnRoundCompleted;
            gm.LevelCompleted -= OnLevelCompleted;
            gm.StruggleOffered -= OnStruggleOffered; // LXD Phase F
            FeedbackManager.Instance.OnSuccess -= OnFeedbackSuccess;
            FeedbackManager.Instance.OnRetry -= OnFeedbackRetry;
            FeedbackManager.Instance.OnHint -= OnHintRequested;
            FeedbackManager.Instance.OnSolution -= OnSolutionRequested;
            if (_module != null) { _module.Cleanup(); _module = null; }
            ClearAreas();
            TimerManager.Instance.Stop();
            base.OnHide();
        }

        void ClearAreas()
        {
            StopModuleRoutines();
            DestroyConfetti();
            ClearChildren(ActivityArea);
            ClearChildren(AnswerArea);
            if (_nextBtn != null) _nextBtn.SetActive(false);
            if (_banner != null) _banner.SetActive(false);
        }

        static void ClearChildren(GameObject go)
        {
            if (go == null) return;
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(go.transform.GetChild(i).gameObject);
        }

        void OnRoundStarted(ChallengeData c)
        {
            if (M != null && M.IsOverlayOpen("Hint")) M.HideOverlay("Hint");
            DismissStruggleChoice(); // LXD Phase F: a new round clears any open offer
            ClearAreas();
            DragDropManager.Clear();
            HideBanner();
            _shownStars = -1;   // Batch 4 fix: force the star display to refresh for the new round

            var gm = GameManager.Instance;
            _hud.text = gm.HudLine;
            UpdateModeBadge(gm);
            SetHudVisible(gm.IsTimedMode, !gm.PracticeMode);
            RefreshCalmStars();

            var bg = Panel.transform.Find("BG").GetComponent<Image>();
            var sprite = GameAssets.Bg(gm.CurrentLevel.bg);
            bg.sprite = sprite;
            bg.color = sprite != null ? Color.white : UIKit.Cream;

            _prompt.text = c.prompt;
            AudioManager.Instance.PlayVoice(c.vo);
            GameManager.Instance.ActivateTimer();   // timer starts now (challenge visible)

            // module factory — one reusable system, 12 module types
            switch (c.type)
            {
                case "math-choice": _module = new MathChoiceModule(); break;
                case "word-picture": _module = new WordPictureModule(); break;
                case "spelling": _module = new SpellingModule(); break;
                case "sentence": _module = new SentenceModule(); break;
                case "matching": _module = new MatchingModule(); break;
                case "potion": _module = new PotionModule(); break;
                case "directions": _module = new DirectionsModule(); break;
                case "sorting": _module = new SortingModule(); break;
                case "choice-card": _module = new ChoiceCardModule(); break;
                case "memory-match": _module = new MemoryMatchModule(); break;
                case "pixel-art": _module = new PixelArtModule(); break;
                case "music-play": _module = new MusicPlayModule(); break;
                default:
                    _module = null;
                    var soon = UIKit.Label(ActivityArea, "ComingSoon", "New games are coming!", 56, UIKit.Ink);
                    UIKit.Place(soon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 100));
                    break;
            }
            if (_module != null)
            {
                _module.Init(this, c);
                _module.Build(ActivityArea, AnswerArea);
            }
        }

        void OnAnswered(bool correct)
        {
            if (!correct) RefreshCalmStars();
        }

        void OnRoundCompleted()
        {
            if (M != null && M.IsOverlayOpen("Hint")) M.HideOverlay("Hint");
            _nextBtn.SetActive(true);
        }

        void OnLevelCompleted(ScoreManager.LevelOutcome outcome)
        {
            GameManager.Instance.LastOutcome = outcome;
            M.Show(GameManager.Instance.IsFinalLevel ? "FinalResult" : "LevelResult");
        }

        void OnFeedbackSuccess(string line, string id)
        {
            ShowBanner(line, UIKit.Green, Color.white);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayStar();
            if (GameManager.Instance != null)
                StartRoutine(ConfettiEffect.Play(Panel, GameManager.Instance.IsCalmMode));
        }

        void OnFeedbackRetry(string line, string id)
        {
            ShowBanner(line, UIKit.Hex("#E8A838"), UIKit.Ink);
        }

        // LXD Phase F: the struggle-choice offer arrives after the retry banner shows
        void OnStruggleOffered(string variantId)
        {
            _struggleVariantId = variantId;
            if (_struggleRow != null) _struggleRow.SetActive(true);
        }

        void OnHintRequested(string text)
        {
            M.GetOverlay<HintPanel>().ShowHint(text);
        }

        void OnSolutionRequested(string text)
        {
            M.GetOverlay<HintPanel>().ShowSolution(text);
        }

        void ShowBanner(string text, Color c, Color textColor)
        {
            _banner.SetActive(true);
            _banner.GetComponent<Image>().color = new Color(c.r, c.g, c.b, 0.94f);
            _bannerText.color = textColor;
            _bannerText.text = text;
            _bannerTimer = 2.2f;
        }

        void HideBanner()
        {
            _banner.SetActive(false);
        }

        public override void Tick(float dt)
        {
            if (_bannerTimer > 0f)
            {
                _bannerTimer -= dt;
                if (_bannerTimer <= 0f) HideBanner();
            }
            var t = TimerManager.Instance;
            if (GameManager.Instance != null && GameManager.Instance.IsTimedMode && t.Duration > 0 && (t.Running || t.Elapsed > 0))
            {
                _timer.text = t.Format();
                _timer.color = t.SecondsLeft <= 5f && t.Running ? UIKit.Coral : UIKit.Ink;
                if (t.SecondsLeft <= 5f && t.Running && Mathf.CeilToInt(t.SecondsLeft) != Mathf.CeilToInt(t.SecondsLeft - dt))
                    AudioManager.Instance.PlayTimerTick();
                float frac = Mathf.Clamp01(t.SecondsLeft / Mathf.Max(1, t.Duration));
                var rt = _progressFill.rectTransform;
                var parent = rt.parent.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2((parent.rect.width - 8) * frac, rt.sizeDelta.y);
                // LXD Phase C1: the live preview honours the same accuracy gate as
                // the scored result — after a mistake/help, speed can earn at most 1 star.
                bool previewFirstTry = HintManager.Instance != null &&
                                       HintManager.Instance.Mistakes == 0 && !HintManager.Instance.UsedHelp;
                RefreshStarDisplay(GameRules.StarsForTime(t.Elapsed, previewFirstTry));
            }
            if (_module != null) _module.Tick(dt);
        }
    }
}
