using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// matching (Day 4): three card groups — number <-> picture-quantity <-> English word.
    /// Every round shows all triples with each column shuffled independently.
    /// </summary>
    public class MatchingModule : ModuleBase
    {
        static readonly Color SelectedBg = UIKit.Hex("#FFE0A3");
        static readonly Color PartialMatchBg = UIKit.Hex("#E3F7E9");
        static readonly Color FullMatchBg = UIKit.Hex("#C8F0D2");
        static readonly Color RetryTintBg = UIKit.Hex("#FFE4B5");

        TripleData[] _triples;
        Card _selected;
        int _connections;
        Transform _linesParent;
        bool _solved;

        class Card
        {
            public string Column;    // num | pic | word
            public int TripleIndex;
            public GameObject Go;
            public Image Bg;
            public Outline Border;
            public GameObject CheckIcon;
            public Vector3 BasePos;
            public int Uses;
            public int MaxUses { get { return Column == "pic" ? 2 : 1; } }
            public bool Locked { get { return Uses >= MaxUses; } }
        }

        public override string Type { get { return "matching"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _triples = C.media.triples;
            _selected = null;
            _connections = 0;
            _solved = false;

            _linesParent = UIKit.Node(activityArea, "Lines").transform;
            var linesRt = (RectTransform)_linesParent;
            UIKit.Stretch(linesRt);
            linesRt.SetAsFirstSibling();

            string[] columns = { "Num", "Pic", "Word" };
            float[] xs = { 0.18f, 0.5f, 0.82f };
            float[] ys = { 0.71f, 0.45f, 0.19f };
            for (int col = 0; col < 3; col++)
            {
                var headerPill = UIKit.Panel(activityArea, "HeaderBg" + columns[col], new Color(1f, 0.99f, 0.95f, 0.97f), Ui("ui_panel"));
                UIKit.Place(headerPill.rectTransform, new Vector2(xs[col], 0.91f), Vector2.zero, new Vector2(300, 56));
                var header = UIKit.Label(headerPill.gameObject, "Header" + columns[col],
                    columns[col] == "Num" ? "Number" : columns[col] == "Pic" ? "Picture" : "Word",
                    34, UIKit.Ink);
                UIKit.Stretch(header.rectTransform, 16, 6);

                int[] order = ShuffledIndices(_triples.Length);
                for (int row = 0; row < _triples.Length; row++)
                    MakeCard(activityArea, columns[col].ToLower(), xs[col], ys[row], _triples[order[row]], order[row]);
            }

            AudioManager.Instance.PlayVoice(C.vo);
        }

        static int[] ShuffledIndices(int n)
        {
            var arr = new int[n];
            for (int i = 0; i < n; i++) arr[i] = i;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
            }
            return arr;
        }

        void MakeCard(GameObject area, string columnId, float x, float y, TripleData triple, int tripleIdx)
        {
            var card = UIKit.Card(area, "Card_" + columnId + "_" + triple.num, UIKit.Paper, Ui("ui_card"));
            UIKit.Place(card.rectTransform, new Vector2(x, y), Vector2.zero, new Vector2(340, 122));

            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = UIKit.Orange;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            var check = UIKit.Img(card.gameObject, "Check", Ui("ui_check"), UIKit.Green);
            UIKit.Place(check.rectTransform, new Vector2(0.92f, 0.80f), Vector2.zero, new Vector2(32, 32));
            check.gameObject.SetActive(false);

            var c = new Card
            {
                Column = columnId,
                TripleIndex = tripleIdx,
                Go = card.gameObject,
                Bg = card,
                Border = outline,
                CheckIcon = check.gameObject,
                BasePos = card.transform.localPosition
            };

            switch (columnId)
            {
                case "num":
                    var numTxt = UIKit.Label(card.gameObject, "Text", triple.num, 72, UIKit.Blue);
                    UIKit.Stretch(numTxt.rectTransform, 16, 16);
                    break;
                case "pic":
                    Sprite picSprite = Obj(triple.picObj);
                    if (picSprite == null)
                    {
                        var fb = UIKit.Label(card.gameObject, "Fallback", triple.picCount.ToString(), 52, UIKit.Ink);
                        UIKit.Stretch(fb.rectTransform, 16, 16);
                    }
                    else
                    {
                        float iconSz = triple.picCount > 4 ? 36f : 44f;
                        int cols = triple.picCount > 6 ? 4 : 3;
                        var grid = UIKit.Grid(card.gameObject, "Objs", new Vector2(iconSz, iconSz), new Vector2(8, 6), cols, TextAnchor.MiddleCenter);
                        var gridRt = grid.GetComponent<RectTransform>();
                        UIKit.Place(gridRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(276, 98));
                        for (int i = 0; i < triple.picCount; i++)
                        {
                            var o = UIKit.Img(grid.gameObject, "O" + i, picSprite, Color.white);
                            UIKit.Flex(o.gameObject, iconSz, iconSz);
                        }
                    }
                    break;
                case "word":
                    var wordTxt = UIKit.Label(card.gameObject, "Text", triple.word, 40, UIKit.Purple);
                    UIKit.Stretch(wordTxt.rectTransform, 16, 16);
                    break;
            }

            UIKit.OnClick(card.gameObject.AddComponent<Button>(), () => TapCard(c));
        }

        static Color BaseColorFor(Card c)
        {
            if (c.Locked) return FullMatchBg;
            if (c.Uses > 0) return PartialMatchBg;
            return UIKit.Paper;
        }

        static void ApplyIdleVisual(Card c)
        {
            if (c == null || c.Bg == null) return;
            c.Bg.color = BaseColorFor(c);
            if (c.Go != null) c.Go.transform.localScale = Vector3.one;
            if (c.Border != null)
            {
                if (c.Locked)
                {
                    c.Border.effectColor = UIKit.Green;
                    c.Border.enabled = true;
                }
                else if (c.Uses > 0)
                {
                    c.Border.effectColor = UIKit.Teal;
                    c.Border.enabled = true;
                }
                else
                {
                    c.Border.enabled = false;
                }
            }
            if (c.CheckIcon != null)
                c.CheckIcon.SetActive(c.Locked);
        }

        static void ApplySelectedVisual(Card c)
        {
            if (c == null || c.Bg == null) return;
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            c.Bg.color = SelectedBg;
            if (c.Border != null)
            {
                c.Border.effectColor = UIKit.Orange;
                c.Border.enabled = true;
            }
            if (c.Go != null)
                c.Go.transform.localScale = Vector3.one * (calm ? 1.02f : 1.05f);
        }

        void TapCard(Card card)
        {
            if (_solved || card.Locked) return;
            AudioManager.Instance.PlaySfx("sfx_drop");

            if (_selected == null)
            {
                _selected = card;
                ApplySelectedVisual(card);
                return;
            }

            if (_selected == card)
            {
                ApplyIdleVisual(card);
                _selected = null;
                return;
            }

            if (_selected.Column == card.Column || _selected.Locked)
            {
                if (_selected.Column == card.Column)
                {
                    S.ShowToast("Pick a card from another column!");
                    bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
                    if (!calm) S.StartRoutine(NudgeCard(card));
                }
                ApplyIdleVisual(_selected);
                _selected = card;
                ApplySelectedVisual(card);
                return;
            }

            if (_selected.TripleIndex != card.TripleIndex)
            {
                Card prev = _selected;
                _selected = null;
                bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
                if (calm)
                {
                    ApplyIdleVisual(prev);
                    ApplyIdleVisual(card);
                }
                else
                {
                    S.StartRoutine(MismatchNudge(prev, card));
                }
                Answer(false);
                return;
            }

            DrawLine(_selected, card);
            _selected.Uses++;
            card.Uses++;
            Card first = _selected;
            _selected = null;

            ApplyIdleVisual(first);
            ApplyIdleVisual(card);
            S.StartRoutine(PopMatched(first.Go.transform));
            S.StartRoutine(PopMatched(card.Go.transform));

            _connections++;
            if (_connections >= _triples.Length * 2)
            {
                _solved = true;
                AudioManager.Instance.PlayCollect();
                Answer(true);
            }
        }

        IEnumerator MismatchNudge(Card a, Card b)
        {
            if (a != null && a.Bg != null) a.Bg.color = RetryTintBg;
            if (b != null && b.Bg != null) b.Bg.color = RetryTintBg;
            Vector3 posA = a != null ? a.BasePos : Vector3.zero;
            Vector3 posB = b != null ? b.BasePos : Vector3.zero;
            const float dur = 0.20f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float off = Mathf.Sin(u * Mathf.PI * 3f) * 8f * (1f - u);
                if (a != null && a.Go != null) a.Go.transform.localPosition = posA + new Vector3(off, 0f, 0f);
                if (b != null && b.Go != null) b.Go.transform.localPosition = posB + new Vector3(off, 0f, 0f);
                yield return null;
            }
            if (a != null && a.Go != null) { a.Go.transform.localPosition = posA; if (_selected != a) ApplyIdleVisual(a); }
            if (b != null && b.Go != null) { b.Go.transform.localPosition = posB; if (_selected != b) ApplyIdleVisual(b); }
        }

        IEnumerator NudgeCard(Card card)
        {
            if (card == null || card.Go == null) yield break;
            Vector3 basePos = card.BasePos;
            const float dur = 0.16f;
            float t = 0f;
            while (t < dur)
            {
                if (card.Go == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float off = Mathf.Sin(u * Mathf.PI * 3f) * 7f * (1f - u);
                card.Go.transform.localPosition = basePos + new Vector3(off, 0f, 0f);
                yield return null;
            }
            if (card.Go != null) card.Go.transform.localPosition = basePos;
        }

        IEnumerator PopMatched(Transform tr)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.08f : 0.22f;
            float peak = calm ? 1.03f : 1.09f;
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

        void DrawLine(Card a, Card b)
        {
            var rtA = (RectTransform)a.Go.transform;
            var rtB = (RectTransform)b.Go.transform;
            Vector2 aPos = _linesParent.InverseTransformPoint(rtA.position);
            Vector2 bPos = _linesParent.InverseTransformPoint(rtB.position);
            Vector2 diff = bPos - aPos;
            float len = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            var line = UIKit.Panel(_linesParent.gameObject, "Line" + _connections, UIKit.Gold, Ui("ui_bar_fill"));
            line.raycastTarget = false;
            var rt = line.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localPosition = (aPos + bPos) * 0.5f;
            rt.sizeDelta = new Vector2(len, 14f);
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            rt.SetAsFirstSibling();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _selected = null;
            _solved = false;
        }
    }
}
