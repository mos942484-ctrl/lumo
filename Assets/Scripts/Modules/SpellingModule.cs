using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// spelling (Day 4): word picture is shown and pronounced, scrambled letters appear,
    /// child taps or drags them into slots. Wrong order -> gentle retry and letter snaps back.
    /// </summary>
    public class SpellingModule : ModuleBase
    {
        static readonly Color SlotIdleColor = new Color(0.52f, 0.40f, 0.28f, 0.95f);
        static readonly Color SlotNextColor = new Color(0.64f, 0.48f, 0.26f, 0.98f);
        static readonly Color SlotFilledColor = new Color(0.28f, 0.56f, 0.36f, 0.96f);

        string _word;
        string[] _scrambled;
        int _filled;
        bool[] _slotFilled;
        Transform _slotsRow;
        bool _solved;

        public override string Type { get { return "spelling"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _word = C.media.word;
            _scrambled = ShuffleLetters(_word);
            _filled = 0;
            _slotFilled = new bool[_word.Length];
            _solved = false;

            var picCard = UIKit.Card(activityArea, "PicCard", new Color(1f, 0.99f, 0.95f, 0.97f), Ui("ui_panel"));
            UIKit.Place(picCard.rectTransform, new Vector2(0.22f, 0.50f), Vector2.zero, new Vector2(360, 420));

            Sprite picSprite = Obj(C.media.obj);
            var pic = UIKit.Img(picCard.gameObject, "Pic", picSprite, Color.white);
            UIKit.Place(pic.rectTransform, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(260, 260));
            if (picSprite == null)
            {
                pic.color = Color.clear;
                var fb = UIKit.Label(picCard.gameObject, "Fallback", _word, 54, UIKit.Ink);
                UIKit.Place(fb.rectTransform, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(300, 120));
            }

            var word = UIKit.Label(picCard.gameObject, "WordLabel", _word, 58, UIKit.Green);
            UIKit.Place(word.rectTransform, new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(320, 80));
            AudioManager.Instance.PlayVoice(C.media.voWord);

            _slotsRow = UIKit.HLayout(activityArea, "Slots", 16, TextAnchor.MiddleCenter, false).transform;
            UIKit.Place((RectTransform)_slotsRow, new Vector2(0.62f, 0.66f), Vector2.zero, new Vector2(900, 170));
            for (int i = 0; i < _word.Length; i++)
            {
                var slot = UIKit.Panel(_slotsRow.gameObject, "Slot" + i, SlotIdleColor, Ui("ui_slot"));
                UIKit.Flex(slot.gameObject, 130, 150);
                var outline = slot.gameObject.AddComponent<Outline>();
                outline.effectColor = UIKit.Gold;
                outline.effectDistance = new Vector2(4f, -4f);
                outline.enabled = false;
                int slotIdx = i;
                DragDropManager.Register(slot.rectTransform, letter => OnLetterDropped(slotIdx, letter), () => _slotFilled[slotIdx]);
            }
            HighlightNextOpenSlot();

            var letters = UIKit.HLayout(activityArea, "Letters", 20, TextAnchor.MiddleCenter, false);
            UIKit.Place(letters.GetComponent<RectTransform>(), new Vector2(0.62f, 0.30f), Vector2.zero, new Vector2(900, 160));
            foreach (var ch in _scrambled)
            {
                var chipGo = UIKit.Node(letters.gameObject, "Chip_" + ch + "_" + Random.Range(0, 9999));
                UIKit.Flex(chipGo, 110, 130);
                var card = UIKit.Card(chipGo, "Card", UIKit.Orange, Ui("ui_btn"));
                UIKit.Stretch(card.rectTransform);
                var letterLbl = UIKit.Label(chipGo, "L", ch, 64, UIKit.Ink);
                UIKit.Stretch(letterLbl.rectTransform);
                DragChip.Make(chipGo, ch);
            }

            var hintTag = UIKit.Label(activityArea, "HintTag", "Tap or drag letters into the boxes", 22, UIKit.Brown);
            UIKit.Place(hintTag.rectTransform, new Vector2(0.62f, 0.07f), Vector2.zero, new Vector2(640, 36));
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
                var slot = _slotsRow.Find("Slot" + i);
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

        string[] ShuffleLetters(string word)
        {
            string[] arr;
            if (C.media.preset != null && C.media.preset.Length == word.Length)
                arr = (string[])C.media.preset.Clone();
            else
            {
                arr = new string[word.Length];
                for (int i = 0; i < word.Length; i++) arr[i] = word[i].ToString();
            }
            for (int attempt = 0; attempt < 10; attempt++)
            {
                for (int i = arr.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    var tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
                }
                if (string.Join("", arr) != word) return arr;
            }
            return arr;
        }

        bool OnLetterDropped(int slotIdx, string letter)
        {
            if (_solved) return false;
            if (slotIdx < 0 || slotIdx >= _word.Length) return false;
            if (_slotFilled[slotIdx]) return false;

            var slot = _slotsRow.Find("Slot" + slotIdx);
            if (slot == null) return false;
            if (letter != _word[slotIdx].ToString())
            {
                Answer(false);
                return false;
            }

            AudioManager.Instance.PlayDrop();
            _slotFilled[slotIdx] = true;
            var chipLabel = UIKit.Label(slot.gameObject, "Letter", letter, 72, Color.white);
            UIKit.Stretch(chipLabel.rectTransform);
            chipLabel.transform.SetAsLastSibling();
            S.StartRoutine(PopSlot(chipLabel.transform));
            HighlightNextOpenSlot();
            _filled++;

            if (_filled >= _word.Length)
            {
                _solved = true;
                AudioManager.Instance.PlayCollect();
                Answer(true);
            }
            return true;
        }

        IEnumerator PopSlot(Transform tr)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.07f : 0.18f;
            float peak = calm ? 1.04f : 1.12f;
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

        /// <summary>Resets letters when the child wants to retry (Hint path or manual reset).</summary>
        public void ResetSlots()
        {
            if (_slotsRow == null) return;
            for (int i = 0; i < _word.Length; i++)
            {
                var slot = _slotsRow.Find("Slot" + i);
                if (slot == null) continue;
                var letter = slot.Find("Letter");
                if (letter != null) Object.Destroy(letter.gameObject);
            }
            var lettersRow = S != null && S.ActivityArea != null ? S.ActivityArea.transform.Find("Letters") : null;
            if (lettersRow != null)
            {
                foreach (Transform chip in lettersRow)
                {
                    var cg = chip.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }
                }
            }
            _filled = 0;
            _slotFilled = new bool[_word.Length];
            _solved = false;
            HighlightNextOpenSlot();
        }
    }
}
