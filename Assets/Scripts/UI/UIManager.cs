using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>Base class for full-screen panels built from code.</summary>
    public abstract class ScreenBase
    {
        public abstract string Name { get; }
        public GameObject Panel;
        protected UIManager M;

        public virtual void Build(UIManager m)
        {
            M = m;
            Panel = UIKit.Node(m.RootGo, "Screen_" + Name);
            UIKit.Stretch(Panel.GetComponent<RectTransform>());
        }

        public virtual void OnShow()
        {
            if (Panel != null) Panel.SetActive(true);
        }

        public virtual void OnHide()
        {
            if (Panel != null) Panel.SetActive(false);
        }

        /// <summary>Per-frame update while this screen is active.</summary>
        public virtual void Tick(float dt) { }
    }

    /// <summary>Panel shown on top of the current screen (pause, settings, hints...).</summary>
    public abstract class OverlayPanel
    {
        public abstract string Name { get; }
        public GameObject Panel;
        protected UIManager M;

        public virtual void Build(UIManager m)
        {
            M = m;
            Panel = UIKit.Node(m.OverlayRootGo, "Overlay_" + Name);
            UIKit.Stretch(Panel.GetComponent<RectTransform>());
        }

        public virtual void Show()
        {
            if (Panel != null)
            {
                Panel.SetActive(true);
                Panel.transform.SetAsLastSibling();
            }
            OnShow();
        }

        public virtual void Hide()
        {
            if (Panel != null) Panel.SetActive(false);
            OnHide();
        }

        public virtual void OnShow() { }
        public virtual void OnHide() { }
        public virtual void Tick(float dt) { }
    }

    /// <summary>
    /// Owns the canvas and all screens/overlays. Screens are built once, shown/hidden
    /// by name — instant transitions (performance §11).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public Camera Cam;
        public Canvas Canvas;
        public GameObject RootGo { get; private set; }
        public GameObject OverlayRootGo { get; private set; }

        readonly Dictionary<string, ScreenBase> _screens = new Dictionary<string, ScreenBase>();
        readonly Dictionary<string, OverlayPanel> _overlays = new Dictionary<string, OverlayPanel>();
        readonly List<OverlayPanel> _overlayList = new List<OverlayPanel>();
        // Batch 4 fix: lazy screen construction — factories for every screen;
        // only Splash/MainMenu (the startup path) are built eagerly in Awake.
        // Addressables migration is longer-term and explicitly out of scope.
        readonly Dictionary<string, System.Func<ScreenBase>> _screenFactories = new Dictionary<string, System.Func<ScreenBase>>();
        ScreenBase _current;
        TextMeshProUGUI _toast;
        float _toastTimer;
        Coroutine _fadeRoutine;
        CanvasGroup _fadeTargetCg;

        public string CurrentScreenName { get { return _current != null ? _current.Name : ""; } }
        public ScreenBase CurrentScreen { get { return _current; } }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Cam = Camera.main;
            Canvas = UIKit.CreateUICanvas(Cam, "UICanvas");

            RootGo = UIKit.Node(Canvas.gameObject, "Screens");
            UIKit.Stretch(RootGo.GetComponent<RectTransform>());

            OverlayRootGo = UIKit.Node(Canvas.gameObject, "Overlays");
            UIKit.Stretch(OverlayRootGo.GetComponent<RectTransform>());

            // Batch 4 fix: keep notched / gesture-bar devices clear — inset the
            // UI roots to the OS safe area (no-op when the safe area is full-screen).
            UIKit.ApplySafeArea(RootGo.GetComponent<RectTransform>());
            UIKit.ApplySafeArea(OverlayRootGo.GetComponent<RectTransform>());

            // ---- screens: registered as factories, built lazily on first Show/Get ----
            RegisterFactory("Splash", () => new SplashScreen());
            RegisterFactory("MainMenu", () => new MainMenuScreen());
            RegisterFactory("CharacterSelect", () => new CharacterSelectScreen());
            RegisterFactory("Tutorial", () => new TutorialScreen());
            RegisterFactory("WorldMap", () => new WorldMapScreen());
            RegisterFactory("LevelIntro", () => new LevelIntroScreen());
            RegisterFactory("Gameplay", () => new GameplayScreen());
            RegisterFactory("LevelResult", () => new LevelResultScreen());
            RegisterFactory("ProgressReport", () => new ProgressReportScreen());
            RegisterFactory("FinalResult", () => new FinalResultScreen());
            RegisterFactory("SessionRecap", () => new SessionRecapScreen()); // LXD Phase D1

            // Startup path stays eager so the first frame is identical to before.
            EnsureScreen("Splash");
            EnsureScreen("MainMenu");

            // ---- overlays (small, always needed — stay eager) ----
            RegisterOverlay(new SettingsPanel());
            RegisterOverlay(new PausePanel());
            RegisterOverlay(new HintPanel());
            RegisterOverlay(new CollectionPanel());
            RegisterOverlay(new HowToPlayPanel());
            RegisterOverlay(new TimeLimitPanel());
            RegisterOverlay(new ParentalGatePanel());
            RegisterOverlay(new ParentsPanel());
            RegisterOverlay(new ParentTipsPanel()); // LXD Phase D2
            RegisterOverlay(new MyWordsPanel());    // LXD Phase D5

            BuildToast();
            Show("Splash");
        }

        void Start()
        {
            if (SessionTimeManager.Instance != null)
            {
                SessionTimeManager.Instance.OnDailyLimitReached += HandleDailyLimitReached;
                if (SessionTimeManager.Instance.IsDailyLimitReached)
                    HandleDailyLimitReached();
            }
        }

        void OnDestroy()
        {
            if (SessionTimeManager.Instance != null)
                SessionTimeManager.Instance.OnDailyLimitReached -= HandleDailyLimitReached;
        }

        void HandleDailyLimitReached()
        {
            if (TimerManager.Instance != null) TimerManager.Instance.Pause();
            ShowOverlay("TimeLimit");
        }

        /// <summary>Opens the parental gate before showing the Parents Area.</summary>
        public void OpenParentsArea()
        {
            var gate = GetOverlay<ParentalGatePanel>();
            if (gate != null) gate.OpenFor(() => ShowOverlay("Parents"));
        }

        static void StretchRect(GameObject go)
        {
            UIKit.Stretch(go.GetComponent<RectTransform>());
        }

        void RegisterFactory(string name, System.Func<ScreenBase> factory)
        {
            _screenFactories[name] = factory;
        }

        /// <summary>Builds the named screen on first use (no-op if already built). Returns false if there is no such screen.</summary>
        bool EnsureScreen(string name)
        {
            if (_screens.ContainsKey(name)) return true;
            System.Func<ScreenBase> factory;
            if (!_screenFactories.TryGetValue(name, out factory)) return false;
            ScreenBase s;
            try { s = factory(); }
            catch (System.Exception e)
            {
                Debug.LogError("[UI] failed to create screen '" + name + "': " + e.Message + "\n" + e.StackTrace);
                return false;
            }
            return Register(s);
        }

        bool Register(ScreenBase s)
        {
            try
            {
                s.Build(this);
                _screens[s.Name] = s;
                s.Panel.SetActive(false);
                return true;
            }
            catch (System.Exception e)
            {
                // one broken screen must never kill the whole UI chain
                Debug.LogError("[UI] failed to build screen '" + s.Name + "': " + e.Message + "\n" + e.StackTrace);
                return false;
            }
        }

        void RegisterOverlay(OverlayPanel o)
        {
            try
            {
                o.Build(this);
                _overlays[o.Name] = o;
                _overlayList.Add(o);
                o.Panel.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UI] failed to build overlay '" + o.Name + "': " + e.Message + "\n" + e.StackTrace);
            }
        }

        public void Show(string name)
        {
            // Batch 4 fix: look the target up (building it lazily) FIRST and
            // only hide the current screen once the lookup succeeded — the old
            // code hid first, so a bad name left a blank screen. On failure the
            // current screen stays visible and the child gets a toast.
            ScreenBase s;
            if (!EnsureScreen(name) || !_screens.TryGetValue(name, out s) || s == null)
            {
                Debug.LogWarning("[UI] screen not registered: " + name);
                Toast("Something went wrong — please try again");
                return;
            }
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
            if (_fadeTargetCg != null)
            {
                _fadeTargetCg.alpha = 1f;
                _fadeTargetCg = null;
            }
            if (_current != null) _current.OnHide();
            _current = s;
            Debug.Log("[UI] show screen: " + name);
            s.OnShow();
            if (s.Panel != null)
            {
                var cg = s.Panel.GetComponent<CanvasGroup>();
                if (cg == null) cg = s.Panel.AddComponent<CanvasGroup>();
                cg.interactable = true;
                cg.blocksRaycasts = true;
                _fadeTargetCg = cg;
                _fadeRoutine = StartCoroutine(FadeInScreen(cg));
            }
            // Batch 4 fix: coming back to the World Map releases assets from
            // finished levels (screens built lazily are otherwise held forever).
            if (name == "WorldMap") Resources.UnloadUnusedAssets();
        }

        System.Collections.IEnumerator FadeInScreen(CanvasGroup cg)
        {
            if (cg == null) yield break;
            const float dur = 0.22f;
            float t = 0f;
            cg.alpha = 0.22f;
            while (t < dur && cg != null)
            {
                t += Mathf.Min(0.05f, Time.unscaledDeltaTime);
                cg.alpha = Mathf.Lerp(0.22f, 1f, Mathf.Clamp01(t / dur));
                yield return null;
            }
            if (cg != null) cg.alpha = 1f;
            _fadeTargetCg = null;
            _fadeRoutine = null;
        }

        public T Get<T>() where T : ScreenBase
        {
            foreach (var kv in _screens)
                if (kv.Value is T) return (T)kv.Value;
            // Batch 4 fix: Get builds the requested screen on demand instead
            // of assuming Awake built everything.
            foreach (var kv in _screenFactories)
            {
                if (_screens.ContainsKey(kv.Key)) continue;
                ScreenBase s;
                try { s = kv.Value(); }
                catch (System.Exception) { continue; }
                if (!Register(s)) continue;
                if (s is T) return (T)s;
            }
            return null;
        }

        public T GetOverlay<T>() where T : OverlayPanel
        {
            foreach (var kv in _overlays)
                if (kv.Value is T) return (T)kv.Value;
            return null;
        }

        public void ShowOverlay(string name)
        {
            OverlayPanel o;
            if (_overlays.TryGetValue(name, out o)) o.Show();
            else Debug.LogWarning("[UI] overlay not registered: " + name);
        }

        public void HideOverlay(string name)
        {
            OverlayPanel o;
            if (_overlays.TryGetValue(name, out o)) o.Hide();
        }

        public bool IsOverlayOpen(string name)
        {
            OverlayPanel o;
            return _overlays.TryGetValue(name, out o) && o.Panel != null && o.Panel.activeSelf;
        }

        // ---- toast ----
        void BuildToast()
        {
            var panel = UIKit.Panel(Canvas.gameObject, "Toast", new Color(0.14f, 0.17f, 0.24f, 0.92f), GameAssets.Ui("ui_btn"));
            panel.raycastTarget = false;
            var rt = panel.rectTransform;
            UIKit.Place(rt, new Vector2(0.5f, 0.805f), Vector2.zero, new Vector2(1080, 66));
            _toast = UIKit.Label(panel.gameObject, "Text", "", 34, Color.white);
            _toast.enableAutoSizing = true;
            _toast.fontSizeMin = 22;
            _toast.fontSizeMax = 34;
            UIKit.Stretch(_toast.rectTransform, 28, 8);
            _toast.alignment = TextAlignmentOptions.Center;
            panel.gameObject.SetActive(false);
        }

        public void Toast(string message, float seconds = 2f)
        {
            // Batch 4 fix: Show() can toast on a lookup failure before the
            // toast UI exists (e.g. a Show call during Awake) — stay silent.
            if (_toast == null) return;
            _toast.transform.parent.gameObject.SetActive(true);
            _toast.text = message;
            _toastTimer = seconds;
        }

        void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f) _toast.transform.parent.gameObject.SetActive(false);
            }
            if (_current != null) _current.Tick(Time.deltaTime);
            for (int i = 0; i < _overlayList.Count; i++)
            {
                var o = _overlayList[i];
                if (o != null && o.Panel != null && o.Panel.activeSelf)
                    o.Tick(Time.unscaledDeltaTime);
            }
        }
    }
}
