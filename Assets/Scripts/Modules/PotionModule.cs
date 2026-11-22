using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// potion (Day 5): "Add 2 red stars and 3 blue stars"; child taps color buttons to
    /// add stars into the cauldron, then answers the total. Remove-rounds: tap stars inside
    /// the cauldron to take them out (§5 L6).
    /// </summary>
    public class PotionModule : ModuleBase
    {
        readonly Dictionary<string, int> _placed = new Dictionary<string, int>();
        readonly Dictionary<string, TMP_Text> _colorCounts = new Dictionary<string, TMP_Text>();
        readonly Dictionary<string, Image> _countPills = new Dictionary<string, Image>();
        readonly HashSet<GameObject> _removingStars = new HashSet<GameObject>();

        int _addedTotal;
        int _removed;
        Transform _cauldronStars;
        RectTransform _cauldronRt;
        TMP_Text _counter;
        bool _solved;
        int _correct;
        int _totalWanted;

        public override string Type { get { return "potion"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _placed.Clear();
            _colorCounts.Clear();
            _countPills.Clear();
            _removingStars.Clear();
            _addedTotal = 0;
            _removed = 0;
            _solved = false;
            int remove = C.media.remove;
            _totalWanted = TotalWanted();
            _correct = _totalWanted - remove;

            // cauldron inside soft backdrop frame
            var cauldronCard = UIKit.Card(activityArea, "CauldronCard", UIKit.CreamAlpha(0.78f), Ui("ui_card"));
            UIKit.Place(cauldronCard.rectTransform, new Vector2(0.72f, 0.50f), Vector2.zero, new Vector2(440, 440));
            cauldronCard.raycastTarget = false;

            Sprite cauldronSprite = Obj("obj_cauldron");
            var cauldron = UIKit.Img(activityArea, "Cauldron", cauldronSprite, Color.white);
            UIKit.Place(cauldron.rectTransform, new Vector2(0.72f, 0.50f), Vector2.zero, new Vector2(390, 390));
            _cauldronRt = cauldron.rectTransform;
            if (cauldronSprite == null)
            {
                cauldron.color = Color.clear;
                var fb = UIKit.Label(cauldron.gameObject, "Fallback", "Cauldron", 36, UIKit.Ink);
                UIKit.Stretch(fb.rectTransform, 16, 16);
            }
            var starsGo = UIKit.Node(cauldron.gameObject, "Stars");
            UIKit.Place(starsGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(280, 170));
            _cauldronStars = starsGo.transform;

            // recipe goal instruction
            string inst;
            if (remove > 0)
            {
                inst = "Step 1: Add " + _totalWanted + " stars.\nStep 2: Tap " + remove + " star" + (remove > 1 ? "s" : "") + " in the cauldron to remove!";
            }
            else
            {
                inst = "Recipe: Add ";
                bool first = true;
                foreach (var cc in C.media.colors)
                {
                    inst += (first ? "" : " and ") + cc.count + " " + cc.color + " star" + (cc.count > 1 ? "s" : "");
                    first = false;
                }
                inst += "!";
            }
            var instCard = UIKit.Card(activityArea, "InstCard", UIKit.CreamAlpha(0.97f), Ui("ui_panel"));
            UIKit.Place(instCard.rectTransform, new Vector2(0.28f, 0.68f), Vector2.zero, new Vector2(640, 150));
            var label = UIKit.Label(instCard.gameObject, "Inst", inst, 34, UIKit.Ink);
            UIKit.Stretch(label.rectTransform, 24, 14);

            // color tap buttons (keeps exact ActivityArea/ColorButtons/Btn_<color> path)
            var row = UIKit.HLayout(activityArea, "ColorButtons", 16, TextAnchor.MiddleCenter, false);
            UIKit.Place(row.GetComponent<RectTransform>(), new Vector2(0.28f, 0.37f), Vector2.zero, new Vector2(660, 150));
            Sprite starSprite = Obj("obj_star");
            foreach (var cc in C.media.colors)
            {
                Color col = ColorOf(cc.color);
                var btnCard = UIKit.Card(row.gameObject, "Btn_" + cc.color, UIKit.Paper, Ui("ui_card"));
                UIKit.Flex(btnCard.gameObject, 120, 120);
                var starImg = UIKit.Img(btnCard.gameObject, "Star", starSprite, col);
                UIKit.Place(starImg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88, 88));
                if (starSprite == null)
                {
                    starImg.color = Color.clear;
                    var fb = UIKit.Label(btnCard.gameObject, "Lbl", cc.color.ToUpper(), 24, col);
                    UIKit.Stretch(fb.rectTransform, 8, 8);
                }
                string colorName = cc.color;
                UIKit.OnClick(btnCard.gameObject.AddComponent<Button>(), () => AddStar(colorName, col));

                var pill = UIKit.Card(row.gameObject, "CountBg_" + cc.color, UIKit.CreamAlpha(0.97f), Ui("ui_panel"));
                var cnt = UIKit.Label(pill.gameObject, "Count_" + cc.color, "0/" + cc.count, 32, col);
                UIKit.Stretch(cnt.rectTransform, 10, 6);
                UIKit.Flex(pill.gameObject, 118, 60);
                _colorCounts[cc.color] = cnt;
                _countPills[cc.color] = pill;
            }

            var counterCard = UIKit.Card(activityArea, "CounterCard", UIKit.CreamAlpha(0.97f), Ui("ui_panel"));
            UIKit.Place(counterCard.rectTransform, new Vector2(0.28f, 0.13f), Vector2.zero, new Vector2(560, 72));
            _counter = UIKit.Label(counterCard.gameObject, "Counter", "", 30, UIKit.Blue);
            UIKit.Stretch(_counter.rectTransform, 18, 8);

            RefreshCounter();
            BuildNumberOptions(_correct, startDisabled: true);
            AudioManager.Instance.PlayVoice(C.vo);
        }

        void AddStar(string colorName, Color col)
        {
            if (_solved) return;
            int wanted = WantedCount(colorName);
            int placed = _placed.ContainsKey(colorName) ? _placed[colorName] : 0;
            if (placed >= wanted)
            {
                S.ShowToast("That's enough " + colorName + " stars!");
                return;
            }
            AudioManager.Instance.PlayDrop();
            Sprite starSprite = Obj("obj_star");
            var star = UIKit.Img(_cauldronStars.gameObject, "S" + _addedTotal + "_" + colorName, starSprite, col);
            Vector2 targetPos = new Vector2(Random.Range(-88f, 88f), Random.Range(-18f, 56f));
            star.rectTransform.anchoredPosition = targetPos;
            star.rectTransform.sizeDelta = new Vector2(64, 64);
            if (starSprite == null)
            {
                star.color = Color.clear;
                var fb = UIKit.Label(star.gameObject, "Lbl", "*", 48, col);
                UIKit.Stretch(fb.rectTransform);
            }
            if (C.media.remove > 0)
            {
                star.raycastTarget = true;
                UIKit.OnClick(star.gameObject.AddComponent<Button>(), () => RemoveStar(star.gameObject));
            }
            _placed[colorName] = placed + 1;
            _addedTotal++;
            RefreshCounter();
            S.StartRoutine(DropIntoCauldron(star.rectTransform, targetPos));
            S.StartRoutine(PulseCauldron());
        }

        void RemoveStar(GameObject star)
        {
            if (_solved || _removed >= C.media.remove || star == null || _removingStars.Contains(star)) return;
            _removingStars.Add(star);
            var btn = star.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
            _removed++;
            AudioManager.Instance.PlaySfx("sfx_drop");
            RefreshCounter();
            S.StartRoutine(RemoveStarAnim(star));
            S.StartRoutine(PulseCauldron());
        }

        IEnumerator DropIntoCauldron(RectTransform rt, Vector2 targetPos)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.08f : 0.24f;
            Vector2 startPos = targetPos + new Vector2(0f, calm ? 18f : 110f);
            float startScale = calm ? 1.06f : 1.25f;
            float t = 0f;
            while (t < dur)
            {
                if (rt == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, u);
                rt.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, u);
                yield return null;
            }
            if (rt != null)
            {
                rt.anchoredPosition = targetPos;
                rt.localScale = Vector3.one;
            }
        }

        IEnumerator RemoveStarAnim(GameObject star)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.05f : 0.15f;
            float t = 0f;
            while (t < dur)
            {
                if (star == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                star.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, u);
                yield return null;
            }
            if (star != null) Object.Destroy(star);
        }

        IEnumerator PulseCauldron()
        {
            if (_cauldronRt == null) yield break;
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.08f : 0.20f;
            float peak = calm ? 1.02f : 1.06f;
            float t = 0f;
            while (t < dur)
            {
                if (_cauldronRt == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                _cauldronRt.localScale = Vector3.one * (1f + (peak - 1f) * Mathf.Sin(u * Mathf.PI));
                yield return null;
            }
            if (_cauldronRt != null) _cauldronRt.localScale = Vector3.one;
        }

        int WantedCount(string color)
        {
            foreach (var cc in C.media.colors) if (cc.color == color) return cc.count;
            return 0;
        }

        void RefreshCounter()
        {
            int inCauldron = _addedTotal - _removed;
            if (_counter != null)
            {
                if (C.media.remove > 0)
                {
                    if (_addedTotal < _totalWanted)
                        _counter.text = "Added: " + _addedTotal + "/" + _totalWanted;
                    else if (_removed < C.media.remove)
                        _counter.text = "Tap " + (C.media.remove - _removed) + " star(s) to take out! (" + inCauldron + " in)";
                    else
                        _counter.text = "Stars in cauldron: " + inCauldron + " - pick the answer!";
                }
                else
                {
                    _counter.text = "In cauldron: " + inCauldron + " / " + _totalWanted;
                }
            }

            foreach (var cc in C.media.colors)
            {
                int placed = _placed.ContainsKey(cc.color) ? _placed[cc.color] : 0;
                bool full = placed >= cc.count;
                if (_colorCounts.TryGetValue(cc.color, out var lbl) && lbl != null)
                    lbl.text = placed + "/" + cc.count;
                if (_countPills.TryGetValue(cc.color, out var pill) && pill != null)
                    pill.color = full ? UIKit.Hex("#D8F5E0") : UIKit.CreamAlpha(0.97f);
            }
        }

        public override void Tick(float dt)
        {
            if (_solved) return;
            bool ready = C.media.remove > 0
                ? (_addedTotal >= _totalWanted && _removed >= C.media.remove)
                : (_addedTotal >= _totalWanted);
            if (ready)
            {
                _solved = true;
                S.ShowToast("Now count the stars in the cauldron!");
                SetOptionsInteractable(true);
            }
        }

        int TotalWanted()
        {
            int total = 0;
            foreach (var cc in C.media.colors) total += cc.count;
            return total;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _removingStars.Clear();
            _solved = false;
        }
    }
}
