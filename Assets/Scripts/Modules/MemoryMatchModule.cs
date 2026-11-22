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
    /// memory-match (Phase 2): flip-card matching for identical concrete pairs (5-7)
    /// and conceptual/deductive pairs across Memory Garden, Science Lab, Ocean Reef,
    /// and Space Station.
    /// </summary>
    public class MemoryMatchModule : ModuleBase
    {
        static readonly Color CardBackColor = UIKit.Blue;

        class CardData
        {
            public string PairId;
            public string Label;
            public string SpriteName;
            public string TintHex;
            public GameObject Go;
            public Image Bg;
            public GameObject BackGroup;
            public GameObject FrontGroup;
            public bool FaceUp;
            public bool Matched;
            public int FlipToken;
        }

        readonly List<CardData> _cards = new List<CardData>();
        CardData _first;
        CardData _second;
        TMP_Text _progress;
        int _totalPairs;
        int _matchedPairs;
        bool _locked;
        bool _solved;
        bool _active;

        public override string Type { get { return "memory-match"; } }
        public bool IsLocked { get { return _locked; } }

        /// <summary>Returns the card indices [a, b] of an unmatched pair for automated testing, or null.</summary>
        public int[] GetUnmatchedPairIndices()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Matched) continue;
                for (int j = i + 1; j < _cards.Count; j++)
                {
                    if (!_cards[j].Matched && _cards[i].PairId == _cards[j].PairId)
                        return new[] { i, j };
                }
            }
            return null;
        }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _cards.Clear();
            _first = null;
            _second = null;
            _matchedPairs = 0;
            _locked = false;
            _solved = false;
            _active = true;

            var pairs = C != null && C.media != null ? C.media.pairs : null;
            _totalPairs = pairs != null ? pairs.Length : 0;

            if (_totalPairs > 0)
            {
                for (int i = 0; i < pairs.Length; i++)
                {
                    var p = pairs[i];
                    _cards.Add(new CardData
                    {
                        PairId = p.id,
                        Label = p.aLabel,
                        SpriteName = p.aSprite,
                        TintHex = p.aTint
                    });

                    bool cloneA = string.IsNullOrEmpty(p.bLabel) && string.IsNullOrEmpty(p.bSprite);
                    _cards.Add(new CardData
                    {
                        PairId = p.id,
                        Label = cloneA ? p.aLabel : p.bLabel,
                        SpriteName = cloneA ? p.aSprite : p.bSprite,
                        TintHex = cloneA ? p.aTint : p.bTint
                    });
                }

                // Fisher-Yates shuffle
                for (int i = _cards.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    var tmp = _cards[i];
                    _cards[i] = _cards[j];
                    _cards[j] = tmp;
                }

                BuildGrid(activityArea);
            }

            BuildProgress(answerArea);
            AudioManager.Instance?.PlayVoice(C != null ? C.vo : null);
        }

        void BuildGrid(GameObject activityArea)
        {
            int total = _cards.Count;
            int cols = total <= 4 ? 2 : total <= 6 ? 3 : 4;
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)total / cols));

            const float spacingX = 24f;
            const float spacingY = 20f;
            float cellW = Mathf.Min(cols <= 3 ? 350f : 310f, (1760f - (cols - 1) * spacingX) / cols);
            float cellH = Mathf.Min(220f, (500f - (rows - 1) * spacingY) / rows);

            var grid = UIKit.Grid(activityArea, "MemoryGrid", new Vector2(cellW, cellH),
                new Vector2(spacingX, spacingY), cols, TextAnchor.MiddleCenter);
            float gridW = cols * cellW + (cols - 1) * spacingX;
            float gridH = rows * cellH + (rows - 1) * spacingY;
            UIKit.Place(grid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(gridW, gridH));

            for (int i = 0; i < total; i++)
            {
                var card = _cards[i];
                var bg = UIKit.Card(grid.gameObject, "Card_" + i, CardBackColor, Ui("ui_card"));
                UIKit.Flex(bg.gameObject, cellW, cellH);
                card.Go = bg.gameObject;
                card.Bg = bg;

                // Face-down visuals: star watermark + "?"
                var back = UIKit.Node(bg.gameObject, "Back");
                UIKit.Stretch(back.GetComponent<RectTransform>());
                var star = UIKit.Img(back, "Watermark", Ui("ui_star"), new Color(1f, 1f, 1f, 0.25f));
                UIKit.Place(star.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(cellH * 0.52f, cellH * 0.52f));
                var q = UIKit.Label(back, "QuestionMark", "?", Mathf.RoundToInt(cellH * 0.34f), Color.white);
                UIKit.Stretch(q.rectTransform);
                card.BackGroup = back;

                // Face-up visuals: sprite and/or text label
                var front = UIKit.Node(bg.gameObject, "Front");
                UIKit.Stretch(front.GetComponent<RectTransform>(), 14f, 12f);
                bool hasSprite = !string.IsNullOrEmpty(card.SpriteName);
                bool hasLabel = !string.IsNullOrEmpty(card.Label);

                if (hasSprite && hasLabel)
                {
                    float imgSz = cellH * 0.45f;
                    var img = UIKit.Img(front, "Sprite", Obj(card.SpriteName), Tint(card.TintHex));
                    UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(imgSz, imgSz));
                    int lblFont = card.Label.Length > 14 ? 22 : 26;
                    var lbl = UIKit.Label(front, "Label", card.Label, lblFont, UIKit.Ink);
                    lbl.enableAutoSizing = true;
                    lbl.fontSizeMin = 16;
                    lbl.fontSizeMax = lblFont;
                    UIKit.Place(lbl.rectTransform, new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(cellW - 28f, cellH * 0.32f));
                }
                else if (hasSprite)
                {
                    float imgSz = cellH * 0.64f;
                    var img = UIKit.Img(front, "Sprite", Obj(card.SpriteName), Tint(card.TintHex));
                    UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(imgSz, imgSz));
                }
                else
                {
                    string text = card.Label ?? "";
                    int lblFont = text.Length > 20 ? 22 : text.Length > 11 ? 26 : 32;
                    var lbl = UIKit.Label(front, "Label", text, lblFont, UIKit.Ink);
                    lbl.enableAutoSizing = true;
                    lbl.fontSizeMin = 16;
                    lbl.fontSizeMax = lblFont;
                    UIKit.Stretch(lbl.rectTransform, 6f, 6f);
                }

                front.SetActive(false);
                card.FrontGroup = front;

                var btn = bg.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                var captured = card;
                btn.onClick.AddListener(() => TapCard(captured));
            }
        }

        void BuildProgress(GameObject answerArea)
        {
            var pill = UIKit.Card(answerArea, "ProgressPill", UIKit.CreamAlpha(0.95f), Ui("ui_panel"));
            UIKit.Place(pill.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 88f));
            _progress = UIKit.Label(pill.gameObject, "Progress", "Pairs found: 0 / " + _totalPairs, 34, UIKit.Ink);
            UIKit.Stretch(_progress.rectTransform, 16f, 8f);
        }

        void TapCard(CardData card)
        {
            if (!_active || _solved || _locked || card.Matched || card.FaceUp) return;

            card.FaceUp = true;
            AudioManager.Instance?.PlaySfx("sfx_drop");

            if (_first == null)
            {
                _first = card;
                StartFlipVisual(card, true);
                return;
            }

            _second = card;
            if (_first.PairId == _second.PairId)
            {
                var firstCard = _first;
                var secondCard = _second;
                firstCard.Matched = true;
                secondCard.Matched = true;
                _first = null;
                _second = null;

                _matchedPairs++;
                if (_progress != null)
                    _progress.text = "Pairs found: " + _matchedPairs + " / " + _totalPairs;
                bool solvedNow = _matchedPairs >= _totalPairs;
                if (solvedNow) _solved = true;

                bool calm = IsCalm();
                if (!calm && S != null)
                {
                    _locked = true;
                    S.StartRoutine(MatchFlipRoutine(firstCard, secondCard, solvedNow));
                }
                else
                {
                    SetCardVisual(secondCard, true);
                    firstCard.Bg.color = UIKit.Teal;
                    secondCard.Bg.color = UIKit.Teal;
                    AudioManager.Instance?.PlayCollect();
                    if (solvedNow) Answer(true);
                }
            }
            else
            {
                _locked = true;
                if (S != null)
                    S.StartRoutine(MismatchFlipBack(_first, _second));
                else
                    ResetMismatch(_first, _second);
            }
        }

        static bool IsCalm()
        {
            return GameManager.Instance != null && GameManager.Instance.IsCalmMode;
        }

        void StartFlipVisual(CardData card, bool up)
        {
            if (!IsCalm() && S != null)
                S.StartRoutine(FlipCardRoutine(card, up, 0.18f));
            else
                SetCardVisual(card, up);
        }

        IEnumerator FlipCardRoutine(CardData card, bool up, float totalDur)
        {
            if (card == null || card.Go == null) yield break;
            int token = ++card.FlipToken;
            var tr = card.Go.transform;
            float half = totalDur * 0.5f;
            float t = 0f;
            while (t < half)
            {
                if (!_active || card.Go == null || card.FlipToken != token) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / half);
                tr.localScale = new Vector3(Mathf.Lerp(1f, 0f, u), 1f, 1f);
                yield return null;
            }
            if (!_active || card.Go == null || card.FlipToken != token) yield break;
            SetCardVisual(card, up);
            t = 0f;
            while (t < half)
            {
                if (!_active || card.Go == null || card.FlipToken != token) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / half);
                tr.localScale = new Vector3(Mathf.Lerp(0f, 1f, u), 1f, 1f);
                yield return null;
            }
            if (card.Go != null && card.FlipToken == token)
                tr.localScale = Vector3.one;
        }

        IEnumerator MatchFlipRoutine(CardData a, CardData b, bool solvedNow)
        {
            yield return FlipCardRoutine(b, true, 0.18f);
            if (!_active || a == null || b == null || a.Go == null || b.Go == null)
            {
                _locked = false;
                yield break;
            }
            if (a.Go != null) a.Go.transform.localScale = Vector3.one;
            if (b.Go != null) b.Go.transform.localScale = Vector3.one;
            SetCardVisual(a, true);
            SetCardVisual(b, true);
            if (a.Bg != null) a.Bg.color = UIKit.Teal;
            if (b.Bg != null) b.Bg.color = UIKit.Teal;
            _locked = false;
            AudioManager.Instance?.PlayCollect();
            if (solvedNow) Answer(true);
        }

        IEnumerator MismatchFlipBack(CardData a, CardData b)
        {
            bool calm = IsCalm();
            if (!calm)
                yield return FlipCardRoutine(b, true, 0.18f);
            else
                SetCardVisual(b, true);

            yield return new WaitForSeconds(calm ? 1.15f : 0.85f);
            if (!_active || a == null || b == null || a.Go == null || b.Go == null)
            {
                _locked = false;
                yield break;
            }

            if (!calm)
            {
                S.StartRoutine(FlipCardRoutine(a, false, 0.18f));
                yield return FlipCardRoutine(b, false, 0.18f);
                if (!_active || a == null || b == null || a.Go == null || b.Go == null)
                {
                    _locked = false;
                    yield break;
                }
            }
            ResetMismatch(a, b);
        }

        void ResetMismatch(CardData a, CardData b)
        {
            a.FlipToken++;
            b.FlipToken++;
            a.FaceUp = false;
            b.FaceUp = false;
            SetCardVisual(a, false);
            SetCardVisual(b, false);
            if (a.Go != null) a.Go.transform.localScale = Vector3.one;
            if (b.Go != null) b.Go.transform.localScale = Vector3.one;
            _first = null;
            _second = null;
            _locked = false;
            Answer(false);
        }

        static void SetCardVisual(CardData card, bool up)
        {
            if (card == null) return;
            if (card.Bg != null) card.Bg.color = up ? (card.Matched ? UIKit.Teal : UIKit.Paper) : CardBackColor;
            if (card.BackGroup != null) card.BackGroup.SetActive(!up);
            if (card.FrontGroup != null) card.FrontGroup.SetActive(up);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _active = false;
            _locked = false;
            _solved = false;
            _first = null;
            _second = null;
            _cards.Clear();
        }
    }
}
