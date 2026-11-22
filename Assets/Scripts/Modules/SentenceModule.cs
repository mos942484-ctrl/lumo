using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// sentence (Day 4): related picture shown; word cards tapped or dragged into order,
    /// Submit, full sentence pronounced with word-by-word highlighting (§5 L11).
    /// </summary>
    public class SentenceModule : ModuleBase
    {
        static readonly Color SlotIdleColor = new Color(0.52f, 0.40f, 0.28f, 0.95f);
        static readonly Color SlotNextColor = new Color(0.64f, 0.48f, 0.26f, 0.98f);
        static readonly Color SlotFilledColor = new Color(0.28f, 0.56f, 0.36f, 0.96f);

        string _sentence;
        string[] _cards;
        int _filled;
        bool[] _slotFilled;
        Transform _slotsRow;
        Transform _cardRow;
        bool _checking;

        public override string Type { get { return "sentence"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _sentence = C.media.sentence;
            _cards = C.media.words;
            _filled = 0;
            var slotWords = _sentence.Replace(".", "").Split(' ');
            _slotFilled = new bool[slotWords.Length];
            _checking = false;

            var picCard = UIKit.Card(activityArea, "PicCard", new Color(1f, 0.99f, 0.95f, 0.97f), Ui("ui_panel"));
            UIKit.Place(picCard.rectTransform, new Vector2(0.16f, 0.52f), Vector2.zero, new Vector2(300, 300));
            Sprite picSprite = Obj(C.media.obj);
            var pic = UIKit.Img(picCard.gameObject, "Pic", picSprite, Tint(C.media.tint));
            UIKit.Stretch(pic.rectTransform, 24, 24);
            if (picSprite == null) pic.color = Color.clear;

            _slotsRow = UIKit.HLayout(activityArea, "Slots", 14, TextAnchor.MiddleCenter, false).transform;
            var slotsRt = (RectTransform)_slotsRow;
            UIKit.Place(slotsRt, new Vector2(0.58f, 0.64f), Vector2.zero, new Vector2(1140, 140));
            for (int i = 0; i < slotWords.Length; i++)
            {
                var w = slotWords[i];
                var slot = UIKit.Panel(_slotsRow.gameObject, "Slot_" + i, SlotIdleColor, Ui("ui_slot"));
                UIKit.Flex(slot.gameObject, Mathf.Max(140, w.Length * 30 + 50), 120);
                var outline = slot.gameObject.AddComponent<Outline>();
                outline.effectColor = UIKit.Gold;
                outline.effectDistance = new Vector2(4f, -4f);
                outline.enabled = false;
                int slotIdx = i;
                DragDropManager.Register(slot.rectTransform, word => OnWordDropped(slotIdx, word), () => _slotFilled[slotIdx]);
            }
            HighlightNextOpenSlot();

            var hintTag = UIKit.Label(activityArea, "HintTag", "Tap or drag words into order", 22, UIKit.Brown);
            UIKit.Place(hintTag.rectTransform, new Vector2(0.58f, 0.45f), Vector2.zero, new Vector2(640, 36));

            _cardRow = UIKit.HLayout(answerArea, "Cards", 20, TextAnchor.MiddleCenter, false).transform;
            UIKit.Stretch((RectTransform)_cardRow);
            foreach (var w in _cards)
            {
                float cardW = Mathf.Max(150, w.Length * 32 + 64);
                var chipGo = UIKit.Node(_cardRow.gameObject, "Chip_" + w);
                UIKit.Flex(chipGo, cardW, 110);
                var card = UIKit.Card(chipGo, "Card", UIKit.Teal, Ui("ui_btn"));
                UIKit.Stretch(card.rectTransform);
                var wordLbl = UIKit.Label(chipGo, "W", w, 40, UIKit.Ink);
                UIKit.Stretch(wordLbl.rectTransform, 12, 6);
                DragChip.Make(chipGo, w);
            }

            var submit = UIKit.TextButton(activityArea, "Submit", "Check!", new Vector2(300, 90), UIKit.Orange,
                () => Check(), Ui("ui_btn"), 40);
            UIKit.Place(submit.GetComponent<RectTransform>(), new Vector2(0.58f, 0.25f), Vector2.zero, new Vector2(300, 90));

            AudioManager.Instance.PlayVoice(C.vo);
        }

        void HighlightNextOpenSlot()
        {
            if (_slotsRow == null) return;
            int nextIdx = -1;
            for (int i = 0; i < _slotFilled.Length; i++)
            {
                if (!_slotFilled[i]) { nextIdx = i; break; }
            }
            for (int i = 0; i < _slotFilled.Length; i++)
            {
                var slot = _slotsRow.Find("Slot_" + i);
                if (slot == null) continue;
                var img = slot.GetComponent<Image>();
                var outline = slot.GetComponent<Outline>();
                if (_slotFilled[i])
                {
                    if (img != null) img.color = SlotFilledColor;
                    if (outline != null) { outline.effectColor = UIKit.Green; outline.enabled = true; }
                }
                else if (i == nextIdx)
                {
                    if (img != null) img.color = SlotNextColor;
                    if (outline != null) { outline.effectColor = UIKit.Gold; outline.enabled = true; }
                }
                else
                {
                    if (img != null) img.color = SlotIdleColor;
                    if (outline != null) outline.enabled = false;
                }
            }
        }

        bool OnWordDropped(int slotIdx, string word)
        {
            if (_checking) return false;
            var slotWords = _sentence.Replace(".", "").Split(' ');
            if (slotIdx < 0 || slotIdx >= slotWords.Length) return false;
            if (_slotFilled[slotIdx]) return false;
            if (word.ToLower() != slotWords[slotIdx].ToLower())
            {
                Answer(false);
                return false;
            }
            AudioManager.Instance.PlayDrop();
            _slotFilled[slotIdx] = true;
            var slot = _slotsRow.Find("Slot_" + slotIdx);
            if (slot != null)
            {
                var t = UIKit.Label(slot.gameObject, "Word", slotWords[slotIdx], 40, Color.white);
                UIKit.Stretch(t.rectTransform, 10, 6);
                t.transform.SetAsLastSibling();
                S.StartRoutine(PopWord(t.transform));
            }
            HighlightNextOpenSlot();
            _filled++;
            return true;
        }

        IEnumerator PopWord(Transform tr)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.07f : 0.18f;
            float peak = calm ? 1.04f : 1.10f;
            float t = 0f;
            while (t < dur)
            {
                if (tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float s = u < 0.5f
                    ? Mathf.Lerp(0.75f, peak, u * 2f)
                    : Mathf.Lerp(peak, 1f, (u - 0.5f) * 2f);
                tr.localScale = Vector3.one * s;
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        void Check()
        {
            if (_checking) return;
            var slotWords = _sentence.Replace(".", "").Split(' ');
            if (_filled < slotWords.Length)
            {
                S.ShowToast("Place all the words first!");
                return;
            }
            _checking = true;
            AudioManager.Instance.PlayVoice(C.media.voSentence);
            S.StartRoutine(HighlightSequence(slotWords));
            Answer(true);
        }

        /// <summary>Word-by-word highlight while the full sentence plays (§5 L11 mechanic).</summary>
        IEnumerator HighlightSequence(string[] words)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            Color highlight = calm ? UIKit.Hex("#FFF3C4") : UIKit.Yellow;
            float stepWait = calm ? 0.4f : 0.55f;
            for (int i = 0; i < words.Length; i++)
            {
                if (_slotsRow == null) yield break;
                var slot = _slotsRow.Find("Slot_" + i);
                if (slot != null)
                {
                    var img = slot.GetComponent<Image>();
                    var wordLbl = slot.Find("Word")?.GetComponent<TMP_Text>();
                    if (img != null) img.color = highlight;
                    if (wordLbl != null) wordLbl.color = UIKit.Ink;
                    yield return new WaitForSeconds(stepWait);
                    if (_slotsRow == null || slot == null) yield break;
                    if (img != null) img.color = SlotFilledColor;
                    if (wordLbl != null) wordLbl.color = Color.white;
                }
            }
            _checking = false;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _checking = false;
        }
    }
}
