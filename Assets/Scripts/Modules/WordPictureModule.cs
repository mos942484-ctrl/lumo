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
    /// word-picture (Day 3): 4 pictures on screen; child hears "Find the Cat!" and taps
    /// the right one. After a correct answer the word appears beside the picture and its
    /// pronunciation plays (levels 2/8 + variants).
    /// Two-property rounds (§5 L8: red/big/small + ball/tree/house) work via tint+size.
    /// </summary>
    public class WordPictureModule : ModuleBase
    {
        readonly Dictionary<Transform, Vector3> _basePositions = new Dictionary<Transform, Vector3>();
        PicData[] _pics;
        bool _solved;

        public override string Type { get { return "word-picture"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _basePositions.Clear();
            _pics = (PicData[])C.media.pictures.Clone();
            for (int i = _pics.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = _pics[i]; _pics[i] = _pics[j]; _pics[j] = tmp;
            }
            _solved = false;

            var grid = UIKit.Grid(activityArea, "Pics", new Vector2(310, 320), new Vector2(50, 30), 4);
            UIKit.Place(grid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(1500, 400));

            for (int i = 0; i < _pics.Length; i++)
            {
                PicData p = _pics[i];
                var cellImg = UIKit.Card(grid.gameObject, "Pic_" + p.id, UIKit.Paper, Ui("ui_card"));
                GameObject cell = cellImg.gameObject;

                var frame = UIKit.Panel(cell, "PicFrame", UIKit.Cream, Ui("ui_panel"));
                frame.raycastTarget = false;
                UIKit.Place(frame.rectTransform, new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(256, 206));

                Sprite sprite = Obj(p.sprite);
                float imgSize = Mathf.Clamp(150f * (p.size > 0f ? p.size : 1f), 96f, 194f);
                var img = UIKit.Img(cell, "Img", sprite, Tint(p.tint));
                UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(imgSize, imgSize));
                if (sprite == null)
                {
                    img.color = Color.clear;
                    var fallback = UIKit.Label(frame.gameObject, "Fallback", p.id.ToUpper(), 32, UIKit.Ink);
                    UIKit.Stretch(fallback.rectTransform, 12, 8);
                }

                var word = UIKit.Label(cell, "Word", "", 38, UIKit.Green);
                word.enableAutoSizing = true;
                word.fontSizeMin = 22;
                word.fontSizeMax = 38;
                UIKit.Place(word.rectTransform, new Vector2(0.5f, 0.11f), Vector2.zero, new Vector2(280, 56));

                var id = p.id;
                UIKit.OnClick(cell.AddComponent<Button>(), () => Tap(cell, p, id));
            }
        }

        void Tap(GameObject cell, PicData p, string id)
        {
            if (_solved) return;
            if (id == C.correctAnswer)
            {
                _solved = true;
                AudioManager.Instance.PlayCollect();

                var cellImg = cell.GetComponent<Image>();
                if (cellImg != null) cellImg.color = UIKit.Hex("#D8F5E0");

                var word = cell.transform.Find("Word")?.GetComponent<TMP_Text>();
                if (word != null) word.text = C.media.word;
                AudioManager.Instance.PlayVoice(C.media.voWord);

                var img = cell.transform.Find("Img")?.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    Color t = Tint(p.tint);
                    img.color = new Color(t.r, t.g, t.b, 1f);
                }

                S.StartRoutine(PulseCard(cell.transform));
                Answer(true);
            }
            else
            {
                var cellImg = cell.GetComponent<Image>();
                if (cellImg != null) cellImg.color = new Color(0.88f, 0.86f, 0.84f, 0.92f);
                bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
                if (!calm)
                    S.StartRoutine(NudgeCard(cell.transform));
                Answer(false);
            }
        }

        IEnumerator PulseCard(Transform tr)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.10f : 0.26f;
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

        IEnumerator NudgeCard(Transform tr)
        {
            if (tr == null) yield break;
            if (!_basePositions.TryGetValue(tr, out Vector3 basePos))
            {
                basePos = tr.localPosition;
                _basePositions[tr] = basePos;
            }
            const float dur = 0.20f;
            float t = 0f;
            while (t < dur)
            {
                if (tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float offset = Mathf.Sin(u * Mathf.PI * 3f) * 8f * (1f - u);
                tr.localPosition = basePos + new Vector3(offset, 0f, 0f);
                yield return null;
            }
            if (tr != null) tr.localPosition = basePos;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _basePositions.Clear();
            _solved = false;
        }
    }
}
