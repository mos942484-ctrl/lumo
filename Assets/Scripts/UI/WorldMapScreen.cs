using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>
    /// Wonder Islands Archipelago map: 13 themed islands arranged in 3 staggered rows.
    /// Supports Free Sail (all 13 islands open) and Story Path (sequential unlock).
    /// </summary>
    public class WorldMapScreen : ScreenBase
    {
        public override string Name { get { return "WorldMap"; } }

        readonly IslandNode[] _nodes = new IslandNode[13];
        TMP_Text _stats, _modeLabel, _helperText;
        Image _modeImg, _heroImg;
        Coroutine _sailRoutine, _bobRoutine;
        bool _sailing;

        class IslandNode
        {
            public RectTransform SlotRt;
            public Button Btn;
            public Image Img;
            public Image IconImg;
            public Image LockIcon;
            public TMP_Text Num;
            public TMP_Text Stars;
            public GameObject NextBadge;
            public Color AccentColor;
            public int LevelId;
        }

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_kingdom"));
            if (bg.sprite == null) bg.color = UIKit.Teal;
            UIKit.Stretch(bg.rectTransform);

            // Calm ocean scrim over the background so all 13 islands pop cleanly.
            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.05f, 0.14f, 0.24f, 0.42f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            // ---- Floating top archipelago header dock (y = 0.94f) ----
            var topDock = UIKit.Card(Panel, "TopDock", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            topDock.raycastTarget = false;
            UIKit.Place(topDock.rectTransform, new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(1860, 84));

            // LXD Phase D1: leaving the map for the menu after finishing a level this
            // session detours through the out-game session recap first (never mid-level).
            var back = UIKit.TextButton(Panel, "Back", "Home", new Vector2(210, 68), UIKit.Coral, () =>
            {
                var tracker = Lumo.Core.SessionTracker.Instance;
                if (tracker != null && tracker.HasCompletedLevel) M.Show("SessionRecap");
                else M.Show("MainMenu");
            }, GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.025f, 0.94f), Vector2.zero, new Vector2(210, 68), new Vector2(0f, 0.5f));
            UIKit.AttachButtonIcon(back, GameAssets.Ui("ui_icon_home"), 32f);

            var modeBtn = UIKit.TextButton(Panel, "ModeToggle", "Free Sail", new Vector2(260, 66), UIKit.Teal, () =>
            {
                SaveManager.Instance.Data.freeExplore = !SaveManager.Instance.Data.freeExplore;
                SaveManager.Instance.Save();
                Refresh();
            }, GameAssets.Ui("ui_btn"), 26);
            UIKit.Place(modeBtn.GetComponent<RectTransform>(), new Vector2(0.22f, 0.94f), Vector2.zero, new Vector2(260, 66));
            UIKit.AttachButtonIcon(modeBtn, GameAssets.Ui("ui_icon_map"), 32f);
            _modeImg = modeBtn.GetComponent<Image>();
            _modeLabel = modeBtn.GetComponentInChildren<TMP_Text>(true);

            var title = UIKit.Label(Panel, "Title", "Wonder Islands", 50, UIKit.Yellow);
            UIKit.Place(title.rectTransform, new Vector2(0.485f, 0.94f), Vector2.zero, new Vector2(430, 76));
            UIKit.StyleTitle(title);

            var statsPill = UIKit.Card(Panel, "StatsPill", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(statsPill.rectTransform, new Vector2(0.975f, 0.94f), Vector2.zero, new Vector2(630, 60), new Vector2(1f, 0.5f));
            _stats = UIKit.Label(statsPill.gameObject, "Stats", "", 24, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Stretch(_stats.rectTransform, 18, 6);

            // ---- Subtle sea-route connector lines behind the 3 rows ----
            MakeTrail("Trail0", new Vector2(0.50f, 0.743f), 1100f);
            MakeTrail("Trail1", new Vector2(0.50f, 0.523f), 880f);
            MakeTrail("Trail2", new Vector2(0.54f, 0.303f), 880f);

            // ---- 3 staggered rows of islands ----
            BuildRow(0, 1, 5, new Vector2(0.50f, 0.72f), 34f, 1344f);
            BuildRow(1, 6, 9, new Vector2(0.50f, 0.50f), 52f, 1120f);
            BuildRow(2, 10, 13, new Vector2(0.54f, 0.28f), 52f, 1120f);

            // ---- Hero marker + Bottom bar (y = 0.08f) ----
            var heroSprite = SaveManager.Instance.Data.avatar == 2 ? GameAssets.Char("char_hero_b") : GameAssets.Char("char_hero_a");
            _heroImg = UIKit.Img(Panel, "Hero", heroSprite, Color.white);
            UIKit.Place(_heroImg.rectTransform, new Vector2(0.085f, 0.19f), Vector2.zero, new Vector2(136, 168));

            var helperCard = UIKit.Card(Panel, "HelperBanner", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(helperCard.rectTransform, new Vector2(0.42f, 0.08f), Vector2.zero, new Vector2(840, 64));
            var compass = UIKit.Img(helperCard.gameObject, "Compass", GameAssets.Ui("ui_icon_map"), Color.white);
            UIKit.Place(compass.rectTransform, new Vector2(0.036f, 0.5f), Vector2.zero, new Vector2(36, 36));
            _helperText = UIKit.Label(helperCard.gameObject, "Text", "", 25, UIKit.Ink, TextAlignmentOptions.Center);
            _helperText.rectTransform.anchorMin = Vector2.zero;
            _helperText.rectTransform.anchorMax = Vector2.one;
            _helperText.rectTransform.offsetMin = new Vector2(62, 6);
            _helperText.rectTransform.offsetMax = new Vector2(-18, -6);

            var parents = UIKit.TextButton(Panel, "Parents", "Parents", new Vector2(236, 72), UIKit.WarmWood,
                () => M.OpenParentsArea(), GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(parents.GetComponent<RectTransform>(), new Vector2(0.755f, 0.08f), Vector2.zero, new Vector2(236, 72));
            UIKit.AttachButtonIcon(parents, GameAssets.Ui("ui_icon_report"), 34f);

            var settings = UIKit.TextButton(Panel, "Settings", "Settings", new Vector2(236, 72), UIKit.Purple,
                () => M.ShowOverlay("Settings"), GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(settings.GetComponent<RectTransform>(), new Vector2(0.91f, 0.08f), Vector2.zero, new Vector2(236, 72));
            UIKit.AttachButtonIcon(settings, GameAssets.Ui("ui_icon_settings"), 34f);
        }

        void MakeTrail(string name, Vector2 anchor, float width)
        {
            var trail = UIKit.Panel(Panel, name, new Color(1f, 0.96f, 0.84f, 0.24f), GameAssets.Ui("ui_bar_fill"));
            trail.raycastTarget = false;
            UIKit.Place(trail.rectTransform, anchor, Vector2.zero, new Vector2(width, 10f));
        }

        void BuildRow(int rowIdx, int startId, int endId, Vector2 anchor, float spacing, float width)
        {
            var row = UIKit.HLayout(Panel, "Row" + rowIdx, spacing, TextAnchor.MiddleCenter, false);
            UIKit.Place(row.GetComponent<RectTransform>(), anchor, Vector2.zero, new Vector2(width, 168));
            for (int id = startId; id <= endId; id++)
                MakeIslandSlot(row.gameObject, id);
        }

        void MakeIslandSlot(GameObject parent, int levelId)
        {
            var info = IslandCatalog.Get(levelId);
            Color accent = UIKit.Hex(info.AccentHex);

            var slot = UIKit.Node(parent, "Island" + levelId);
            UIKit.Flex(slot, 240, 168);

            var btn = UIKit.TextButton(slot, "Level" + levelId, "", new Vector2(116, 116), accent,
                () => Tap(levelId), GameAssets.Ui("ui_node"), 44);
            UIKit.Place(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(116, 116));

            float lum = 0.299f * accent.r + 0.587f * accent.g + 0.114f * accent.b;
            Color nodeText = lum < 0.52f ? Color.white : UIKit.Ink;

            var iconImg = UIKit.Img(btn.gameObject, "Icon", GameAssets.Obj(info.IconSprite), Color.white);
            UIKit.Place(iconImg.rectTransform, new Vector2(0.52f, 0.58f), Vector2.zero, new Vector2(56, 56));

            var num = UIKit.Label(btn.gameObject, "Num", levelId.ToString(), 22, nodeText);
            UIKit.Place(num.rectTransform, new Vector2(0.28f, 0.74f), Vector2.zero, new Vector2(40, 30));

            var stars = UIKit.Label(btn.gameObject, "Stars", "", 20, nodeText);
            UIKit.Place(stars.rectTransform, new Vector2(0.5f, 0.19f), Vector2.zero, new Vector2(104, 26));

            var lockIcon = UIKit.Img(btn.gameObject, "Lock", GameAssets.Ui("ui_lock"), new Color(1, 1, 1, 0.92f));
            UIKit.Place(lockIcon.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(46, 46));

            var trigger = btn.gameObject.AddComponent<EventTrigger>();
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => PreviewIsland(levelId));
            trigger.triggers.Add(enterEntry);
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => RestoreDefaultHelper());
            trigger.triggers.Add(exitEntry);

            var nextPill = UIKit.Card(slot, "NextBadge", UIKit.Gold, GameAssets.Ui("ui_btn"));
            nextPill.raycastTarget = false;
            UIKit.Place(nextPill.rectTransform, new Vector2(0.82f, 0.88f), Vector2.zero, new Vector2(68, 28));
            var nextTxt = UIKit.Label(nextPill.gameObject, "Text", "Next", 18, UIKit.Ink);
            UIKit.Stretch(nextTxt.rectTransform, 4, 2);
            nextPill.gameObject.SetActive(false);

            var labelBtn = UIKit.TextButton(slot, "LabelPill", info.IslandName, new Vector2(230, 44), UIKit.Paper,
                () => Tap(levelId), GameAssets.Ui("ui_btn"), 21, UIKit.Ink);
            UIKit.Place(labelBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(230, 44));

            _nodes[levelId - 1] = new IslandNode
            {
                SlotRt = slot.GetComponent<RectTransform>(),
                Btn = btn,
                Img = btn.GetComponent<Image>(),
                IconImg = iconImg,
                LockIcon = lockIcon,
                Num = num,
                Stars = stars,
                NextBadge = nextPill.gameObject,
                AccentColor = accent,
                LevelId = levelId
            };
        }

        void Tap(int levelId)
        {
            if (_sailing) return;
            if (!LevelManager.Instance.IsUnlocked(levelId))
            {
                M.Toast(levelId == 13 ? "Finish all 12 islands to open the finale!" : "Finish the previous island first!");
                return;
            }
            StopIdleBob();
            _sailing = true;
            _sailRoutine = M.StartCoroutine(SailAndOpen(levelId));
        }

        void PreviewIsland(int levelId)
        {
            if (_helperText == null) return;
            var info = IslandCatalog.Get(levelId);
            bool unlocked = LevelManager.Instance == null || LevelManager.Instance.IsUnlocked(levelId);
            if (!unlocked)
            {
                _helperText.text = levelId == 13
                    ? info.IslandName + " - Finish all 12 islands to open!"
                    : info.IslandName + " - Finish island " + (levelId - 1) + " first!";
                return;
            }
            var rec = SaveManager.Instance != null ? SaveManager.Instance.Record(levelId) : null;
            int earned = SaveManager.SumStars(rec);
            int max = SaveManager.RoundsForLevel(levelId) * GameRules.MaxStarsPerChallenge;
            _helperText.text = info.IslandName + " - " + info.Subject + " (" + earned + "/" + max + " stars)";
        }

        void RestoreDefaultHelper()
        {
            if (_helperText != null) _helperText.text = DefaultHelperText();
        }

        static string DefaultHelperText()
        {
            bool freeExplore = SaveManager.Instance == null || SaveManager.Instance.Data == null || SaveManager.Instance.Data.freeExplore;
            return freeExplore ? "Tap any island to sail there!" : "Follow the crystal path!";
        }

        void StopIdleBob()
        {
            if (_bobRoutine != null && M != null)
            {
                M.StopCoroutine(_bobRoutine);
                _bobRoutine = null;
            }
            if (_heroImg != null)
                _heroImg.rectTransform.anchoredPosition = Vector2.zero;
        }

        IEnumerator HeroIdleBob()
        {
            float t = 0f;
            while (Panel != null && Panel.activeInHierarchy && _heroImg != null && !_sailing)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsCalmMode)
                {
                    _heroImg.rectTransform.anchoredPosition = Vector2.zero;
                    _bobRoutine = null;
                    yield break;
                }
                t += Time.deltaTime;
                _heroImg.rectTransform.anchoredPosition = new Vector2(0f, 4f * Mathf.Sin(t * 2.2f));
                yield return null;
            }
            _bobRoutine = null;
        }

        IEnumerator SailAndOpen(int levelId)
        {
            var node = _nodes[levelId - 1];
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            if (!calm && _heroImg != null && node != null && node.SlotRt != null)
            {
                Vector3 startPos = _heroImg.rectTransform.position;
                Vector3 targetPos = node.SlotRt.position;
                const float dur = 0.28f;
                float t = 0f;
                while (t < dur)
                {
                    if (Panel == null || !Panel.activeInHierarchy || _heroImg == null)
                    {
                        _sailing = false;
                        _sailRoutine = null;
                        yield break;
                    }
                    t += Time.deltaTime;
                    float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                    _heroImg.rectTransform.position = Vector3.Lerp(startPos, targetPos, s);
                    yield return null;
                }
            }
            _sailing = false;
            _sailRoutine = null;
            GameManager.Instance.SelectedLevelId = levelId;
            M.Show("LevelIntro");
        }

        public override void OnShow()
        {
            base.OnShow();
            AudioManager.Instance.PlayMusic("music_menu");
            Refresh();
        }

        public override void OnHide()
        {
            StopIdleBob();
            if (_sailRoutine != null && M != null)
            {
                M.StopCoroutine(_sailRoutine);
                _sailRoutine = null;
            }
            _sailing = false;
            base.OnHide();
        }

        /// <summary>Updates all 13 island medallions, stats pill, mode toggle, and hero sprite.</summary>
        public void Refresh()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            var d = SaveManager.Instance.Data;

            StopIdleBob();
            if (_sailRoutine != null && M != null)
            {
                M.StopCoroutine(_sailRoutine);
                _sailRoutine = null;
            }
            _sailing = false;

            if (_heroImg != null)
            {
                _heroImg.sprite = d.avatar == 2 ? GameAssets.Char("char_hero_b") : GameAssets.Char("char_hero_a");
                UIKit.Place(_heroImg.rectTransform, new Vector2(0.085f, 0.19f), Vector2.zero, new Vector2(136, 168));
            }

            int nextLevel = LevelManager.Instance != null ? LevelManager.Instance.ContinueLevel() : 1;

            for (int i = 0; i < 13; i++)
            {
                var node = _nodes[i];
                if (node == null) continue;
                int id = i + 1;
                bool unlocked = LevelManager.Instance.IsUnlocked(id);
                var rec = SaveManager.Instance.Record(id);
                int earned = SaveManager.SumStars(rec);
                int max = SaveManager.RoundsForLevel(id) * GameRules.MaxStarsPerChallenge;

                node.Img.color = unlocked ? node.AccentColor : new Color(0.58f, 0.51f, 0.44f, 1f);
                if (node.IconImg != null) node.IconImg.gameObject.SetActive(unlocked);
                node.Num.gameObject.SetActive(unlocked);
                node.LockIcon.gameObject.SetActive(!unlocked);
                node.Num.text = id.ToString();
                node.Stars.text = (rec.completed || earned > 0) ? earned + "/" + max : "";
                if (node.NextBadge != null)
                    node.NextBadge.SetActive(d.freeExplore && id == nextLevel && !rec.completed);
                node.Btn.interactable = true;
            }

            if (_modeLabel != null) _modeLabel.text = d.freeExplore ? "Free Sail" : "Story Path";
            if (_modeImg != null) _modeImg.color = d.freeExplore ? UIKit.Teal : UIKit.Orange;
            RestoreDefaultHelper();

            int shards = ScoreManager.Instance != null ? ScoreManager.Instance.ShardsCollected() : 0;
            if (_stats != null)
                _stats.text = "Stars: " + d.totalStars + "/" + GameRules.DynamicTotalStars() + "  |  Shards: " + shards + "/12  |  Ages " + GameRules.AgeBandShort(d.ageBand);

            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            if (!calm && !_sailing && Panel != null && Panel.activeInHierarchy && M != null)
                _bobRoutine = M.StartCoroutine(HeroIdleBob());
        }
    }
}
