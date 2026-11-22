using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// choice-card (Phase 2): structured scenario / clue / sequence card with
    /// optional object sprite, plus 2..4 shuffled answer buttons.
    /// Used across Math, Word Forest, Learning Island, Science Lab, Sky Kingdom,
    /// Ocean Reef, Puzzle Park, Space Station, Kindness Corner, and Dino Valley.
    /// </summary>
    public class ChoiceCardModule : ModuleBase
    {
        bool _solved;

        public override string Type { get { return "choice-card"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _solved = false;
            var m = C != null ? C.media : null;

            var card = UIKit.Card(activityArea, "ChoiceCard", UIKit.Parchment, Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 500f));

            bool hasSub = m != null && !string.IsNullOrEmpty(m.subtitle);
            if (hasSub)
            {
                float pillW = Mathf.Clamp(m.subtitle.Length * 18f + 64f, 240f, 620f);
                var subPill = UIKit.Card(card.gameObject, "SubtitlePill", UIKit.WarmWood, Ui("ui_btn"));
                UIKit.Place(subPill.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(pillW, 52f));
                var subLbl = UIKit.Label(subPill.gameObject, "Subtitle", m.subtitle, 26, UIKit.SoftGold);
                UIKit.Stretch(subLbl.rectTransform, 16, 4);
            }

            var body = UIKit.Node(card.gameObject, "Body");
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(44f, 24f);
            bodyRt.offsetMax = new Vector2(-44f, hasSub ? -82f : -24f);

            bool hasObj = m != null && !string.IsNullOrEmpty(m.obj);
            bool hasVisual = hasObj;

            if (hasVisual)
            {
                var visCol = UIKit.Node(body, "VisualCol");
                var visRt = visCol.GetComponent<RectTransform>();
                visRt.anchorMin = new Vector2(0f, 0f);
                visRt.anchorMax = new Vector2(0f, 1f);
                visRt.pivot = new Vector2(0f, 0.5f);
                visRt.anchoredPosition = Vector2.zero;
                visRt.sizeDelta = new Vector2(300f, 0f);

                var visLayout = UIKit.VLayout(visCol, "VisualStack", 14f, TextAnchor.MiddleCenter, false);
                UIKit.Stretch(visLayout.GetComponent<RectTransform>());

                var objFrame = UIKit.Card(visLayout.gameObject, "ObjCard", UIKit.SoftGold, Ui("ui_card"));
                UIKit.Flex(objFrame.gameObject, 220f, 220f);
                var objWell = UIKit.Panel(objFrame.gameObject, "ObjWell", UIKit.Paper, Ui("ui_card"));
                objWell.raycastTarget = false;
                UIKit.Stretch(objWell.rectTransform, 14f, 14f);
                var objImg = UIKit.Img(objWell.gameObject, "ObjImg", Obj(m.obj), Tint(m.tint));
                objImg.preserveAspect = true;
                UIKit.Stretch(objImg.rectTransform, 10f, 10f);
            }

            var contentCol = UIKit.Node(body, "ContentCol");
            var contentRt = contentCol.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(hasVisual ? 336f : 0f, 0f);
            contentRt.offsetMax = Vector2.zero;

            var contentLayout = contentCol.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 14f;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            bool hasClues = m != null && m.clues != null && m.clues.Length > 0;
            bool hasSeq = m != null && m.sequence != null && m.sequence.Length > 0;
            string scenarioText = m != null && !string.IsNullOrEmpty(m.scenario) ? m.scenario : (C != null ? C.prompt : "");

            if (!string.IsNullOrEmpty(scenarioText))
            {
                int scenarioSize = (hasClues && m.clues.Length >= 3) ? 30 : 34;
                var align = (hasVisual || hasClues) ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
                var scenLbl = UIKit.Label(contentCol, "Scenario", scenarioText, scenarioSize, UIKit.Ink, align);
                scenLbl.fontStyle = FontStyles.Bold;
            }

            if (hasClues)
            {
                var cluesCard = UIKit.Card(contentCol, "CluesBox", UIKit.SoftGold, Ui("ui_card"));
                // Cream inner well behind the clues; ignoreLayout keeps it out of the
                // VerticalLayoutGroup so the Clue_* labels stay direct children of
                // CluesBox and lay out exactly as before.
                var cluesWell = UIKit.Panel(cluesCard.gameObject, "CluesWell", UIKit.Cream, Ui("ui_card"));
                cluesWell.raycastTarget = false;
                UIKit.Stretch(cluesWell.rectTransform, 10f, 10f);
                var cluesWellLE = cluesWell.gameObject.AddComponent<LayoutElement>();
                cluesWellLE.ignoreLayout = true;
                var cluesLayout = cluesCard.gameObject.AddComponent<VerticalLayoutGroup>();
                cluesLayout.padding = new RectOffset(22, 22, 12, 12);
                cluesLayout.spacing = 6f;
                cluesLayout.childAlignment = TextAnchor.UpperLeft;
                cluesLayout.childControlWidth = true;
                cluesLayout.childControlHeight = true;
                cluesLayout.childForceExpandWidth = true;
                cluesLayout.childForceExpandHeight = false;

                int clueFont = m.clues.Length >= 4 ? 25 : 28;
                for (int i = 0; i < m.clues.Length; i++)
                {
                    if (string.IsNullOrEmpty(m.clues[i])) continue;
                    UIKit.Label(cluesCard.gameObject, "Clue_" + i, "- " + m.clues[i], clueFont,
                        UIKit.Ink, TextAlignmentOptions.Left, null, FontStyles.Normal);
                }
            }

            if (hasSeq)
            {
                float availW = hasVisual ? 1080f : 1420f;
                int count = m.sequence.Length;
                float pillW = Mathf.Clamp((availW - (count - 1) * 14f) / Mathf.Max(1, count), 76f, 220f);
                int maxSeqLen = 1;
                for (int i = 0; i < count; i++)
                    if (m.sequence[i] != null && m.sequence[i].Length > maxSeqLen) maxSeqLen = m.sequence[i].Length;
                int seqFont = maxSeqLen > 12 ? 20 : maxSeqLen > 7 ? 24 : 30;

                var strip = UIKit.HLayout(contentCol, "SequenceStrip", 14f, TextAnchor.MiddleCenter, false);
                UIKit.Flex(strip.gameObject, -1f, 88f);
                for (int i = 0; i < count; i++)
                {
                    string item = m.sequence[i] ?? "";
                    bool isMissing = item == "?";
                    Color pillCol = isMissing ? UIKit.Gold : (i % 2 == 0 ? UIKit.Seafoam : UIKit.Cream);
                    var pill = UIKit.Card(strip.gameObject, "Seq_" + i, pillCol, Ui("ui_card"));
                    UIKit.Flex(pill.gameObject, pillW, 78f);
                    var lbl = UIKit.Label(pill.gameObject, "Text", item, seqFont, UIKit.Ink);
                    UIKit.Stretch(lbl.rectTransform, 10f, 6f);
                }
            }

            BuildAnswerButtons(answerArea);
            AudioManager.Instance?.PlayVoice(C != null ? C.vo : null);
        }

        void BuildAnswerButtons(GameObject answerArea)
        {
            if (C == null || C.options == null || C.options.Length == 0) return;

            var opts = (string[])C.options.Clone();
            for (int i = opts.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                string tmp = opts[i];
                opts[i] = opts[j];
                opts[j] = tmp;
            }

            int longest = 0;
            for (int i = 0; i < opts.Length; i++)
                if (opts[i] != null && opts[i].Length > longest) longest = opts[i].Length;

            int fontSize = longest > 28 ? 20 : longest > 22 ? 22 : longest > 12 ? 28 : 34;
            float btnW = Mathf.Min(420f, 1700f / Mathf.Max(1, opts.Length) - 24f);

            var row = UIKit.HLayout(answerArea, "Options", 24f, TextAnchor.MiddleCenter, false);
            UIKit.Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1780f, 136f));

            for (int i = 0; i < opts.Length; i++)
            {
                string opt = opts[i];
                Button btn = null;
                btn = UIKit.TextButton(row.gameObject, "Opt_" + i, opt, new Vector2(btnW, 120f),
                    UIKit.Teal, () => OnTapOption(btn, opt), Ui("ui_btn"), fontSize);
                var btnLbl = btn.GetComponentInChildren<TMP_Text>(true);
                if (btnLbl != null)
                {
                    btnLbl.enableAutoSizing = true;
                    btnLbl.fontSizeMin = 18;
                    btnLbl.fontSizeMax = fontSize;
                }
                UIKit.Flex(btn.gameObject, btnW, 120f);
            }
        }

        void OnTapOption(Button btn, string opt)
        {
            if (_solved) return;
            if (opt == C.correctAnswer)
            {
                _solved = true;
                var img = btn != null ? btn.GetComponent<Image>() : null;
                if (img != null) img.color = UIKit.Green;
                var lbl = btn != null ? btn.GetComponentInChildren<TMP_Text>(true) : null;
                if (lbl != null) lbl.color = Color.white;   // white stays readable on Green
                AudioManager.Instance?.PlayCollect();
                Answer(true);
            }
            else
            {
                var img = btn != null ? btn.GetComponent<Image>() : null;
                if (img != null) img.color = UIKit.SoftGold;   // warm amber retry cue
                var lbl = btn != null ? btn.GetComponentInChildren<TMP_Text>(true) : null;
                if (lbl != null) lbl.color = UIKit.Ink;        // keep the label readable on SoftGold
                Answer(false);
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _solved = false;
        }
    }
}
