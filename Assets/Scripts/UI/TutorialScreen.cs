using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>
    /// Tutorial (not counted as a level, §7): hands-on practice of choosing an answer,
    /// tap-or-drag placement, and exploring Wonder Islands at your own pace.
    /// </summary>
    public class TutorialScreen : ScreenBase
    {
        public override string Name { get { return "Tutorial"; } }
        GameObject[] _steps;
        TMP_Text _stepTitle, _hintLine, _stepProgress;
        Image[] _stepPips;
        RectTransform _tutorialSlotRt;
        GameObject _tutorialSlotGo, _tutorialChipGo;
        Image _tutorialSlotBg;
        Outline _tutorialSlotOutline;
        bool _tutorialFilled;
        bool _advancing;
        Coroutine _advanceRoutine;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_village"));
            if (bg.sprite == null) bg.color = UIKit.Yellow;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.14f, 0.24f, 0.26f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            var plaque = UIKit.Card(Panel, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.905f), Vector2.zero, new Vector2(1140, 86));

            _stepTitle = UIKit.Label(Panel, "Title", "", 52, UIKit.Gold);
            UIKit.Place(_stepTitle.rectTransform, new Vector2(0.5f, 0.905f), Vector2.zero, new Vector2(1080, 78));
            UIKit.StyleTitle(_stepTitle);

            var skip = UIKit.TextButton(Panel, "Skip", "Skip", new Vector2(210, 72), UIKit.WarmWood, () =>
            {
                SaveManager.Instance.Data.tutorialDone = true;
                SaveManager.Instance.Save();
                M.Show("WorldMap");
            }, GameAssets.Ui("ui_btn"), 32);
            UIKit.Place(skip.GetComponent<RectTransform>(), new Vector2(0.91f, 0.905f), Vector2.zero, new Vector2(210, 72));
            UIKit.AttachButtonIcon(skip, GameAssets.Ui("ui_icon_next"), 30f);

            var ribbon = UIKit.Card(Panel, "StepRibbon", UIKit.Parchment, GameAssets.Ui("ui_card"));
            ribbon.raycastTarget = false;
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 0.815f), Vector2.zero, new Vector2(560, 44));
            _stepPips = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var pip = UIKit.Img(ribbon.gameObject, "StepPip" + i, GameAssets.Ui("ui_node"), UIKit.BorderColor);
                pip.raycastTarget = false;
                UIKit.Place(pip.rectTransform, new Vector2(0.08f + i * 0.065f, 0.5f), Vector2.zero, new Vector2(22, 22));
                _stepPips[i] = pip;
            }
            _stepProgress = UIKit.Label(ribbon.gameObject, "Progress", "Step 1 of 4", 24, UIKit.Ink, TextAlignmentOptions.Right);
            _stepProgress.rectTransform.anchorMin = new Vector2(0.35f, 0f);
            _stepProgress.rectTransform.anchorMax = new Vector2(0.95f, 1f);
            _stepProgress.rectTransform.offsetMin = Vector2.zero;
            _stepProgress.rectTransform.offsetMax = Vector2.zero;

            var hintPill = UIKit.Card(Panel, "GuideBg", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(hintPill.rectTransform, new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(1260, 84));
            _hintLine = UIKit.Label(hintPill.gameObject, "Guide", "", 32, UIKit.Ink);
            UIKit.Stretch(_hintLine.rectTransform, 32, 10);

            Sprite lumoSprite = GameAssets.Char("char_lumo");
            var lumo = UIKit.Img(Panel, "Lumo", lumoSprite, Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.09f, 0.14f), Vector2.zero, new Vector2(220, 220));
            if (lumoSprite == null) lumo.color = Color.clear;

            _steps = new GameObject[4];
            _steps[0] = BuildStep0();
            _steps[1] = BuildStep1();
            _steps[2] = BuildStep2();
            _steps[3] = BuildStep3();
        }

        GameObject StepRoot(string name)
        {
            var go = UIKit.Node(Panel, name);
            UIKit.Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        GameObject BuildStep0()
        {
            var go = StepRoot("Step0");
            Sprite kingSp = GameAssets.Char("char_king");
            var king = UIKit.Img(go, "King", kingSp, Color.white);
            UIKit.Place(king.rectTransform, new Vector2(0.11f, 0.52f), Vector2.zero, new Vector2(260, 310));
            if (kingSp == null) king.color = Color.clear;

            Sprite shadowSp = GameAssets.Char("char_shadow");
            var shadow = UIKit.Img(go, "Shadow", shadowSp, Color.white);
            UIKit.Place(shadow.rectTransform, new Vector2(0.89f, 0.54f), Vector2.zero, new Vector2(240, 280));
            if (shadowSp == null) shadow.color = Color.clear;

            Sprite shardSp = GameAssets.Obj("obj_shard");
            var shard = UIKit.Img(go, "Shard", shardSp, Color.white);
            UIKit.Place(shard.rectTransform, new Vector2(0.84f, 0.38f), Vector2.zero, new Vector2(110, 110));
            if (shardSp == null) shard.color = Color.clear;

            var card = UIKit.Card(go, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(1080, 440));
            var body = UIKit.Label(card.gameObject, "Body",
                "\"Hello, young hero! Our magic crystal has been broken into twelve pieces.\n\n" +
                "We need your help! Solve the challenges, collect the stars, and save our kingdom!\"",
                40, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Stretch(body.rectTransform, 64, 44);

            var start = UIKit.TextButton(go, "Start", "Let's Start!", new Vector2(400, 96), UIKit.Orange,
                () => NextStep(), GameAssets.Ui("ui_btn_gold"), 42);
            UIKit.Place(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(400, 96));
            UIKit.AttachButtonIcon(start, GameAssets.Ui("ui_icon_play"), 36f);
            return go;
        }

        GameObject BuildStep1()
        {
            var go = StepRoot("Step1");
            var stage = UIKit.Card(go, "Stage", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(stage.rectTransform, new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(1300, 400));

            var row = UIKit.HLayout(stage.gameObject, "Apples", 30);
            UIKit.Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1000, 200));
            Sprite appleSp = GameAssets.Obj("obj_apple");
            for (int i = 0; i < 5; i++)
            {
                var a = UIKit.Img(row.gameObject, "Apple" + i, appleSp, Color.white);
                UIKit.Flex(a.gameObject, 120, 120);
                if (appleSp == null) a.color = Color.clear;
            }

            var eq = UIKit.Label(stage.gameObject, "Eq", "2 apples + 3 apples = ?", 56, UIKit.Ink);
            UIKit.Place(eq.rectTransform, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(1000, 90));

            var opts = UIKit.HLayout(go, "Options", 40);
            UIKit.Place(opts.GetComponent<RectTransform>(), new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(800, 110));
            MakeOpt(opts.gameObject, "4", UIKit.SkyBlue, false);
            MakeOpt(opts.gameObject, "5", UIKit.SkyBlue, true);
            MakeOpt(opts.gameObject, "6", UIKit.SkyBlue, false);
            return go;
        }

        void MakeOpt(GameObject parent, string label, Color c, bool correct)
        {
            var b = UIKit.TextButton(parent, "Opt" + label, label, new Vector2(160, 110), c, () =>
            {
                if (correct)
                {
                    AudioManager.Instance.PlayCorrect();
                    M.Toast("Great! 5 is right! Now tap or drag!");
                    NextStep();
                }
                else
                {
                    AudioManager.Instance.PlaySoftRetry();
                    M.Toast("Not that one - count the apples again!");
                }
            }, GameAssets.Ui("ui_btn"), 56);
            UIKit.Flex(b.gameObject, 160, 110);
        }

        GameObject BuildStep2()
        {
            var go = StepRoot("Step2");
            var stage = UIKit.Card(go, "Stage", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(stage.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(1300, 440));

            var eq = UIKit.Label(stage.gameObject, "Eq", "2 apples + 3 apples = ?", 56, UIKit.Ink);
            UIKit.Place(eq.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(1000, 90));

            // highlighted drop/tap slot
            var slot = UIKit.Panel(stage.gameObject, "Slot", new Color(0.58f, 0.45f, 0.30f, 0.96f), GameAssets.Ui("ui_slot"));
            _tutorialSlotBg = slot;
            _tutorialSlotRt = slot.rectTransform;
            _tutorialSlotGo = slot.gameObject;
            UIKit.Place(_tutorialSlotRt, new Vector2(0.45f, 0.46f), Vector2.zero, new Vector2(220, 180));
            _tutorialSlotOutline = slot.gameObject.AddComponent<Outline>();
            _tutorialSlotOutline.effectColor = UIKit.Gold;
            _tutorialSlotOutline.effectDistance = new Vector2(5f, -5f);
            DragDropManager.Register(_tutorialSlotRt, OnTutorialDrop, () => _tutorialFilled);

            // chip (tap OR drag)
            _tutorialChipGo = UIKit.Node(stage.gameObject, "Chip");
            UIKit.Place(_tutorialChipGo.GetComponent<RectTransform>(), new Vector2(0.70f, 0.46f), Vector2.zero, new Vector2(180, 180));
            var chipImg = UIKit.Card(_tutorialChipGo, "ChipImg", UIKit.Orange, GameAssets.Ui("ui_btn"));
            UIKit.Stretch(chipImg.rectTransform);
            var numLbl = UIKit.Label(_tutorialChipGo, "Num", "5", 76, UIKit.Ink);
            UIKit.Stretch(numLbl.rectTransform);
            DragChip.Make(_tutorialChipGo, "5");

            var helper = UIKit.Label(stage.gameObject, "Helper", "Tap or drag 5 into the highlighted box!", 26, UIKit.Brown);
            UIKit.Place(helper.rectTransform, new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(900, 44));
            return go;
        }

        bool OnTutorialDrop(string payload)
        {
            if (_tutorialFilled || _advancing) return false;
            if (payload == "5")
            {
                _tutorialFilled = true;
                _advancing = true;
                AudioManager.Instance.PlayCollect();

                if (_tutorialChipGo != null)
                {
                    var cg = _tutorialChipGo.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.alpha = 0f; cg.blocksRaycasts = false; }
                }
                if (_tutorialSlotBg != null) _tutorialSlotBg.color = new Color(0.28f, 0.56f, 0.36f, 0.96f);
                if (_tutorialSlotOutline != null) _tutorialSlotOutline.effectColor = UIKit.Green;

                var filled = UIKit.Label(_tutorialSlotGo, "Filled", "5", 76, Color.white);
                UIKit.Stretch(filled.rectTransform);
                M.Toast("Perfect! You are ready for the islands!");
                _advanceRoutine = M.StartCoroutine(PopAndAdvance(filled.transform));
                return true;
            }
            AudioManager.Instance.PlaySoftRetry();
            M.Toast("Count the apples first - then tap or drag the right number!");
            return false;
        }

        IEnumerator PopAndAdvance(Transform tr)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float popDur = calm ? 0.08f : 0.18f;
            float peak = calm ? 1.04f : 1.12f;
            float t = 0f;
            while (t < popDur)
            {
                if (tr == null) break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / popDur);
                float s = u < 0.5f
                    ? Mathf.Lerp(0.75f, peak, u * 2f)
                    : Mathf.Lerp(peak, 1f, (u - 0.5f) * 2f);
                tr.localScale = Vector3.one * s;
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
            yield return new WaitForSeconds(calm ? 0.10f : 0.25f);
            _advancing = false;
            _advanceRoutine = null;
            NextStep();
        }

        GameObject BuildStep3()
        {
            var go = StepRoot("Step3");
            var card = UIKit.Card(go, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(1140, 420));
            var body = UIKit.Label(card.gameObject, "Body",
                "3 things to remember:\n\n" +
                "1) Take your time - sail to any island and earn stars as you learn!\n" +
                "2) Stuck? Tap the Hint button and Lumo will help you.\n" +
                "3) Don't worry about mistakes - you can always try again!",
                36, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Stretch(body.rectTransform, 48, 36);

            var ready = UIKit.TextButton(go, "Ready", "I'm ready!", new Vector2(420, 96), UIKit.Green, () =>
            {
                SaveManager.Instance.Data.tutorialDone = true;
                SaveManager.Instance.Save();
                M.Show("WorldMap");
            }, GameAssets.Ui("ui_btn"), 44);
            UIKit.Place(ready.GetComponent<RectTransform>(), new Vector2(0.5f, 0.23f), Vector2.zero, new Vector2(420, 96));
            UIKit.AttachButtonIcon(ready, GameAssets.Ui("ui_check"), 34f);
            return go;
        }

        void NextStep()
        {
            int idx = CurrentIndex() + 1;
            ShowStep(idx);
        }

        int CurrentIndex()
        {
            for (int i = 0; i < _steps.Length; i++)
                if (_steps[i].activeSelf) return i;
            return -1;
        }

        void ShowStep(int idx)
        {
            for (int i = 0; i < _steps.Length; i++) _steps[i].SetActive(i == idx);
            int stepIdx = Mathf.Clamp(idx, 0, _steps.Length - 1);
            if (_stepProgress != null)
                _stepProgress.text = "Step " + (stepIdx + 1) + " of " + _steps.Length;
            if (_stepPips != null)
            {
                for (int i = 0; i < _stepPips.Length; i++)
                {
                    if (_stepPips[i] == null) continue;
                    _stepPips[i].color = i == stepIdx
                        ? UIKit.Orange
                        : (i < stepIdx ? UIKit.Green : UIKit.BorderColor);
                }
            }
            switch (idx)
            {
                case 0:
                    _stepTitle.text = "Welcome to the Kingdom!";
                    _hintLine.text = "King Arqam & Lumo need your help to restore the Crystal of Knowledge!";
                    AudioManager.Instance.PlayVoice("vo_story_intro");
                    break;
                case 1:
                    _stepTitle.text = "Tutorial 1: Tap the answer!";
                    _hintLine.text = "Lumo: Count the apples with me - tap the right number!";
                    break;
                case 2:
                    _stepTitle.text = "Tutorial 2: Tap or drag!";
                    _hintLine.text = "Lumo: Tap or drag the number 5 into the highlighted box!";
                    break;
                default:
                    _stepTitle.text = "Tutorial 3: Explore at your own pace!";
                    _hintLine.text = "Lumo: You are ready for the islands!";
                    break;
            }
        }

        public override void OnShow()
        {
            base.OnShow();
            if (_advanceRoutine != null && M != null)
            {
                M.StopCoroutine(_advanceRoutine);
                _advanceRoutine = null;
            }
            _advancing = false;
            _tutorialFilled = false;
            DragDropManager.Clear();
            if (_tutorialSlotRt != null)
                DragDropManager.Register(_tutorialSlotRt, OnTutorialDrop, () => _tutorialFilled);
            if (_tutorialSlotBg != null) _tutorialSlotBg.color = new Color(0.58f, 0.45f, 0.30f, 0.96f);
            if (_tutorialSlotOutline != null) _tutorialSlotOutline.effectColor = UIKit.Gold;
            if (_tutorialSlotGo != null)
            {
                var oldFilled = _tutorialSlotGo.transform.Find("Filled");
                if (oldFilled != null) Object.Destroy(oldFilled.gameObject);
            }
            if (_tutorialChipGo != null)
            {
                var cg = _tutorialChipGo.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }
            }
            ShowStep(0);
        }

        public override void OnHide()
        {
            if (_advanceRoutine != null && M != null)
            {
                M.StopCoroutine(_advanceRoutine);
                _advanceRoutine = null;
            }
            _advancing = false;
            DragDropManager.Clear();
            base.OnHide();
        }
    }
}
