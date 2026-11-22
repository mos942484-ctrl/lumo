using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// sorting (Day 5): pile of colored items; two boxes (Red Items / Blue Items); child
    /// taps or drags every item into the right box, then answers the arithmetic question (§5 L12).
    /// </summary>
    public class SortingModule : ModuleBase
    {
        Transform _redBox, _blueBox;
        int _redIn, _blueIn;
        int _redTotal, _blueTotal;
        int _totalItems;
        bool _sorted;

        public override string Type { get { return "sorting"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _redIn = 0;
            _blueIn = 0;
            _redTotal = 0;
            _blueTotal = 0;
            _sorted = false;
            _totalItems = 0;
            foreach (var it in C.media.items)
            {
                _totalItems += it.count;
                if (it.color == "red") _redTotal += it.count;
                else if (it.color == "blue") _blueTotal += it.count;
            }

            var veil = UIKit.Panel(activityArea, "Veil", new Color(1f, 1f, 1f, 0.42f));
            veil.raycastTarget = false;
            UIKit.Stretch(veil.rectTransform);
            veil.transform.SetAsFirstSibling();

            _redBox = MakeBox(activityArea, "RedBox", "Red Items", UIKit.Red, _redTotal, new Vector2(0.72f, 0.63f));
            _blueBox = MakeBox(activityArea, "BlueBox", "Blue Items", UIKit.Blue, _blueTotal, new Vector2(0.72f, 0.24f));

            var pile = UIKit.Grid(activityArea, "Pile", new Vector2(106, 106), new Vector2(14, 14), 3, TextAnchor.MiddleCenter).transform;
            UIKit.Place((RectTransform)pile, new Vector2(0.24f, 0.48f), Vector2.zero, new Vector2(400, 420));
            foreach (var item in C.media.items)
            {
                Color col = ColorOf(item.color);
                string spriteName = item.kind == "star" ? "obj_star" : "obj_gem";
                Sprite itemSprite = Obj(spriteName);
                for (int i = 0; i < item.count; i++)
                {
                    var chipGo = UIKit.Node(pile.gameObject, "Chip_" + item.color + "_" + item.kind + "_" + i);
                    UIKit.Flex(chipGo, 106, 106);
                    var cardBg = UIKit.Card(chipGo, "Bg", UIKit.Paper, Ui("ui_card"));
                    UIKit.Stretch(cardBg.rectTransform);
                    var img = UIKit.Img(chipGo, "Img", itemSprite, col);
                    UIKit.Stretch(img.rectTransform, 14, 14);
                    if (itemSprite == null)
                    {
                        img.color = Color.clear;
                        var fb = UIKit.Label(chipGo, "Fb", item.color.ToUpper(), 20, col);
                        UIKit.Stretch(fb.rectTransform, 6, 6);
                    }
                    DragChip.Make(chipGo, item.color);
                }
            }

            var hintTag = UIKit.Label(activityArea, "HintTag", "Tap or drag each item into its color box", 22, UIKit.Brown);
            UIKit.Place(hintTag.rectTransform, new Vector2(0.24f, 0.08f), Vector2.zero, new Vector2(480, 36));

            var instPill = UIKit.Card(activityArea, "InstBg", UIKit.CreamAlpha(0.97f), Ui("ui_panel"));
            UIKit.Place(instPill.rectTransform, new Vector2(0.5f, 0.925f), Vector2.zero, new Vector2(900, 62));
            var inst = UIKit.Label(instPill.gameObject, "Inst", "Tap or drag every item into the right box!", 34, UIKit.Ink);
            UIKit.Stretch(inst.rectTransform, 20, 6);

            BuildNumberOptions(ParseAnswer(C.correctAnswer, C.id), startDisabled: true);
            AudioManager.Instance.PlayVoice(C.vo);
        }

        Transform MakeBox(GameObject area, string name, string label, Color col, int targetTotal, Vector2 pos)
        {
            bool isRed = name == "RedBox";
            var box = UIKit.Card(area, name, new Color(col.r, col.g, col.b, 0.92f), Ui("ui_card"));
            UIKit.Place(box.rectTransform, pos, Vector2.zero, new Vector2(490, 190));

            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = UIKit.Gold;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = targetTotal > 0;

            Sprite badgeSprite = Obj(isRed ? "obj_star" : "obj_gem");
            var badge = UIKit.Img(box.gameObject, "Icon", badgeSprite, Color.white);
            UIKit.Place(badge.rectTransform, new Vector2(0.13f, 0.74f), Vector2.zero, new Vector2(44, 44));
            if (badgeSprite == null) badge.gameObject.SetActive(false);

            var title = UIKit.Label(box.gameObject, "Title", label, 38, Color.white);
            UIKit.Place(title.rectTransform, new Vector2(0.54f, 0.74f), Vector2.zero, new Vector2(360, 52));

            var count = UIKit.Label(box.gameObject, "Count", "0 / " + targetTotal, 46, Color.white);
            UIKit.Place(count.rectTransform, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(300, 62));

            DragDropManager.Register(box.rectTransform, payload =>
            {
                if (_sorted) return false;
                if (payload != (isRed ? "red" : "blue"))
                {
                    Answer(false);
                    return false;
                }
                AudioManager.Instance.PlayDrop();
                if (isRed) _redIn++; else _blueIn++;
                int now = isRed ? _redIn : _blueIn;
                var countTxt = box.transform.Find("Count");
                if (countTxt != null)
                    countTxt.GetComponent<TMP_Text>().text = now + " / " + targetTotal;
                if (now >= targetTotal)
                    outline.effectColor = UIKit.Green;
                S.StartRoutine(PopBox(box.transform));
                if (_redIn + _blueIn >= _totalItems) OnSorted();
                return true;
            }, () => _sorted, isRed ? "red" : "blue");

            return box.transform;
        }

        IEnumerator PopBox(Transform tr)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.06f : 0.16f;
            float peak = calm ? 1.02f : 1.06f;
            float t = 0f;
            while (t < dur)
            {
                if (tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                tr.localScale = Vector3.one * (1f + (peak - 1f) * Mathf.Sin(u * Mathf.PI));
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        void OnSorted()
        {
            _sorted = true;
            AudioManager.Instance.PlayCollect();
            string question;
            switch (C.media.question)
            {
                case "collect-red": question = "How many RED items are in the box?"; break;
                case "collect-blue": question = "How many BLUE items are in the box?"; break;
                default: question = "Count all items, then take 2 away!"; break;
            }
            var inst = S != null && S.ActivityArea != null ? S.ActivityArea.transform.Find("InstBg/Inst")?.GetComponent<TMP_Text>() : null;
            if (inst != null) inst.text = question;
            S.ShowToast(question);
            SetOptionsInteractable(true);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _sorted = false;
        }
    }
}
