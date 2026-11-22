using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// math-choice (Day 3): multiple-choice numbers with optional object groups,
    /// join/disappear demos, an interactive number line, and one-at-a-time steps
    /// for two-step problems (Section 5 levels 1/4/7/10 + variants + final tasks).
    /// </summary>
    public class MathChoiceModule : ModuleBase
    {
        GameObject _answers, _stepHolder;
        TMP_Text _equation;
        Transform _groupB;
        int _stepIdx = -1;             // -1 = single-step challenge
        bool _busy;

        public override string Type { get { return "math-choice"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _answers = answerArea;
            _stepIdx = (C.media.steps != null && C.media.steps.Length >= 2) ? 0 : -1;

            if (C.media.numberline)
            {
                BuildNumberLine(activityArea);
                ShowEquation(activityArea, C.media.a + " " + C.media.op + " " + C.media.b + " = ?");
                BuildOptions(AnswerOf(C));
                return;
            }

            if (C.media.countOnly)
            {
                var holder = UIKit.Card(activityArea, "CountCard", UIKit.Parchment, Ui("ui_panel"));
                UIKit.Place(holder.rectTransform, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(1200, 260));
                var grp = BuildGroup(holder.gameObject, "Group", Obj(C.media.obj), C.media.a, Color.white);
                UIKit.Place(grp.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 220));
                ShowEquation(activityArea, "How many? = ?");
                BuildOptions(AnswerOf(C));
                return;
            }

            if (_stepIdx >= 0)
            {
                // two-step: show one step at a time (Section 8.6 segmenting)
                BuildStepView(activityArea);
                return;
            }

            if (string.IsNullOrEmpty(C.media.obj))
            {
                ShowEquationCard(activityArea, C.media.a + " " + C.media.op + " " + C.media.b + " = ?");
                BuildOptions(AnswerOf(C));
                return;
            }

            // classic groups view
            BuildGroups(activityArea, C.media.a, C.media.b, C.media.op);
            ShowEquation(activityArea, C.media.a + " " + C.media.op + " " + C.media.b + " = ?");
            BuildOptions(AnswerOf(C));
            if (C.media.demo == "disappear")
                S.StartRoutine(DisappearDemo(activityArea));   // options stay disabled until the demo ends
        }

        // Batch 5 fix: TryParse + logged fallback (was int.Parse - a
        // non-numeric correctAnswer crashed the round at load).
        int AnswerOf(ChallengeData c) { return ParseAnswer(c.correctAnswer, c.id); }

        void BuildStepView(GameObject area)
        {
            _stepHolder = UIKit.Node(area, "StepView");
            UIKit.Stretch(_stepHolder.GetComponent<RectTransform>());
            var step = C.media.steps[0];
            RenderStepVisuals(_stepHolder, step);
            BuildOptions(step.r);   // options for the CURRENT step, not the final answer
        }

        void RenderStepVisuals(GameObject holder, StepData step)
        {
            for (int i = holder.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(holder.transform.GetChild(i).gameObject);

            if (string.IsNullOrEmpty(C.media.obj))
            {
                ShowEquationCard(holder, step.a + " " + step.op + " " + step.b + " = ?");
            }
            else
            {
                BuildGroups(holder, step.a, step.b, step.op, false);
                ShowEquation(holder, step.a + " " + step.op + " " + step.b + " = ?");
            }
        }

        /// <summary>The value the child must answer right now (current step or final result).</summary>
        public int CurrentExpected
        {
            // Batch 5 fix: bound-check _stepIdx - after the last step it equals
            // steps.Length, and indexing steps[_stepIdx] used to throw.
            get
            {
                if (_stepIdx >= 0 && C.media.steps != null && _stepIdx < C.media.steps.Length)
                    return C.media.steps[_stepIdx].r;
                return AnswerOf(C);
            }
        }

        void BuildGroups(GameObject area, int a, int b, string op, bool interactive = true)
        {
            // soft cream backdrop so the object groups read as one equation strip over any background
            var backdrop = UIKit.Card(area, "GroupsBackdrop", UIKit.Parchment, Ui("ui_panel"));
            UIKit.Place(backdrop.rectTransform, new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(1560, 310));
            backdrop.raycastTarget = false;

            var row = UIKit.HLayout(area, "Groups", 40, TextAnchor.MiddleCenter, false);
            UIKit.Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(1500, 260));

            var ga = BuildGroup(row.gameObject, "GroupA", Obj(C.media.obj), a, Color.white);
            StyleGroupCard(ga);
            var opCard = UIKit.Card(row.gameObject, "OpCard", UIKit.SoftGold, Ui("ui_card"));
            UIKit.Flex(opCard.gameObject, 100, 120);
            var opLabel = UIKit.Label(opCard.gameObject, "Op", op, 84, UIKit.Ink);
            UIKit.Stretch(opLabel.rectTransform);
            var gb = BuildGroup(row.gameObject, "GroupB", Obj(C.media.obj), b, Color.white);
            StyleGroupCard(gb);
            _groupB = gb.transform;
        }

        GameObject BuildGroup(GameObject parent, string name, Sprite sprite, int count, Color tint)
        {
            bool countOnly = C != null && C.media != null && C.media.countOnly;
            float sz;
            int cols;
            if (count > 10)
            {
                sz = countOnly ? 74f : 58f;
                cols = countOnly ? Mathf.CeilToInt(count / 2f) : 6;
            }
            else
            {
                sz = count > 8 ? 72f : count > 5 ? 86f : 100f;
                cols = Mathf.Max(1, Mathf.Min(count, 5));
            }
            var grp = UIKit.Grid(parent, name, new Vector2(sz, sz), new Vector2(8, 8), cols, TextAnchor.MiddleCenter);
            UIKit.Flex(grp.gameObject, 560, 240);
            for (int i = 0; i < count; i++)
                UIKit.Img(grp.gameObject, name + "_" + i, sprite, tint);
            return grp.gameObject;
        }

        // Cozy Island Storybook: each object group sits on a seafoam card. Styling
        // goes directly on the grid GameObject (no wrapper nodes) so the
        // "Groups/GroupA", "Groups/GroupB" and "GroupB_i" lookups used by
        // DisappearDemo and JoinThenAnswer keep working.
        void StyleGroupCard(GameObject grp)
        {
            var grid = grp.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.padding = new RectOffset(14, 14, 14, 14);   // keep items off the card edge
            var img = grp.GetComponent<Image>();
            if (img == null)
            {
                img = grp.AddComponent<Image>();
                img.sprite = Ui("ui_card");
                img.type = img.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            }
            img.color = UIKit.Seafoam;
            img.raycastTarget = false;
            var outline = grp.GetComponent<Outline>();
            if (outline == null)
            {
                outline = grp.AddComponent<Outline>();
                outline.effectColor = UIKit.BorderColor;
                outline.effectDistance = new Vector2(2f, -2f);
                outline.useGraphicAlpha = true;
            }
            var shadow = grp.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = grp.AddComponent<Shadow>();
                shadow.effectColor = UIKit.ShadowColor;
                shadow.effectDistance = new Vector2(0f, -6f);
                shadow.useGraphicAlpha = true;
            }
        }

        void ShowEquation(GameObject area, string text)
        {
            var pill = UIKit.Card(area, "EquationBg", UIKit.Parchment, Ui("ui_panel"));
            UIKit.Place(pill.rectTransform, new Vector2(0.5f, 0.19f), Vector2.zero, new Vector2(680, 104));
            var eq = UIKit.Label(pill.gameObject, "Equation", text, 72, UIKit.Ink);
            UIKit.Stretch(eq.rectTransform, 24, 8);
            _equation = eq;
        }

        void ShowEquationCard(GameObject area, string text)
        {
            var card = UIKit.Card(area, "EqCard", UIKit.Parchment, Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(760, 220));
            var eq = UIKit.Label(card.gameObject, "Equation", text, 110, UIKit.Ink);
            UIKit.Stretch(eq.rectTransform, 24, 16);
            _equation = eq;
        }

        void BuildNumberLine(GameObject area)
        {
            var panel = UIKit.Card(area, "LineCard", UIKit.Parchment, Ui("ui_panel"));
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(1780, 210));

            var rail = UIKit.Panel(panel.gameObject, "Rail", UIKit.Blue, Ui("ui_bar_fill"));
            UIKit.Place(rail.rectTransform, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(1660, 10));

            var line = UIKit.HLayout(panel.gameObject, "NumberLine", 4, TextAnchor.MiddleCenter, false);
            UIKit.Place(line.GetComponent<RectTransform>(), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1740, 150));
            int from = Mathf.Max(0, Mathf.Min(C.media.start, C.media.start + C.media.move) - 2);
            int to = Mathf.Min(30, from + 20);
            for (int n = from; n <= to; n++)
            {
                var cell = UIKit.Node(line.gameObject, "N" + n);
                UIKit.Flex(cell, 76, 120);
                var t = UIKit.Label(cell, "T", n.ToString(), 40, UIKit.Ink);
                UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(72, 56));
                if (n == C.media.start)
                {
                    t.color = UIKit.Blue;
                    t.fontStyle = FontStyles.Bold;
                    var token = UIKit.Img(cell, "Token", Ui("ui_star"), UIKit.Gold);
                    UIKit.Place(token.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(46, 46));
                }
            }
        }

        IEnumerator DisappearDemo(GameObject area)
        {
            _busy = true;
            SetOptionsActive(false);
            if (GameManager.Instance != null && GameManager.Instance.IsCalmMode)
            {
                yield return new WaitForSeconds(0.45f);
                if (area == null) { _busy = false; yield break; }
                var gbCalm = area.transform.Find("Groups/GroupB");
                if (gbCalm != null)
                {
                    for (int i = 0; i < C.media.b; i++)
                    {
                        var child = gbCalm.Find("GroupB_" + i);
                        if (child != null) child.gameObject.SetActive(false);
                    }
                    AudioManager.Instance.PlaySfx("sfx_drop");
                }
                if (_equation != null)
                    _equation.text = C.media.a + " - " + C.media.b + " = ?";
                _busy = false;
                SetOptionsActive(true);
                yield break;
            }
            yield return new WaitForSeconds(0.6f);
            if (area == null) { _busy = false; yield break; }
            var groupB = area.transform.Find("Groups/GroupB");
            if (groupB != null)
            {
                for (int i = C.media.b; i > 0; i--)
                {
                    if (area == null || groupB == null) { _busy = false; yield break; }
                    var child = groupB.Find("GroupB_" + (i - 1));
                    if (child != null) child.gameObject.SetActive(false);
                    AudioManager.Instance.PlaySfx("sfx_drop");
                    yield return new WaitForSeconds(0.4f);
                }
            }
            if (area == null) { _busy = false; yield break; }
            if (_equation != null)
                _equation.text = C.media.a + " - " + C.media.b + " = ?";
            _busy = false;
            SetOptionsActive(true);
        }

        void SetOptionsActive(bool on)
        {
            if (_answers == null) return;
            var buttons = _answers.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons) b.interactable = on;
        }

        void BuildOptions(int correct)
        {
            // data options belong to the FINAL answer - intermediate steps get generated options
            bool useDataOptions = C.options != null && C.options.Length > 0 && correct == AnswerOf(C);
            var opts = useDataOptions ? C.options : ToStringArray(NumericOptionsAround(correct, 4));
            var row = UIKit.HLayout(_answers, "Options", 36, TextAnchor.MiddleCenter, false);
            UIKit.Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 136));
            foreach (var o in opts)
            {
                // Batch 5 fix: TryParse + skip (was int.Parse - a non-numeric
                // option crashed the round; the validator now rejects those).
                int val;
                if (!int.TryParse(o, out val))
                {
                    Debug.LogWarning("[MathChoice] skipping non-numeric option '" + o + "' for " + C.id);
                    continue;
                }
                bool isCorrect = val == correct;
                var b = UIKit.TextButton(row.gameObject, "Opt" + o, o, new Vector2(150, 122),
                    UIKit.Teal, () => Tap(val, isCorrect), Ui("ui_btn"), 64);
                UIKit.Flex(b.gameObject, 150, 122);
            }
        }

        // Batch 5 fix: the duplicate ToStringArray lived here - removed, the
        // shared copy in ModuleBase is used instead.
        void Tap(int val, bool isCorrect)
        {
            if (_busy) return;
            if (_stepIdx >= 0)
            {
                // Batch 5 fix: a stale tap after the last step leaves _stepIdx
                // == steps.Length - never index steps out of range.
                if (C.media.steps == null || _stepIdx >= C.media.steps.Length) return;
                var step = C.media.steps[_stepIdx];
                if (val != step.r)
                {
                    Answer(false);   // counts a mistake; hint after 2 (Section 6.2)
                    return;
                }
                _stepIdx++;
                AudioManager.Instance.PlayCollect();
                if (_stepIdx < C.media.steps.Length)
                {
                    var next = C.media.steps[_stepIdx];
                    if (_stepHolder != null) RenderStepVisuals(_stepHolder, next);
                    RebuildOptions(next.r);
                    S.ShowToast("Good! Now the next step!");
                }
                else
                {
                    FinishCorrect();
                }
                return;
            }

            if (isCorrect)
            {
                if (C.media.demo == "join")
                    S.StartRoutine(JoinThenAnswer());
                else
                    FinishCorrect();
            }
            else
            {
                Answer(false);       // timer keeps running; both groups stay visible (Section 5 L1)
            }
        }

        void RebuildOptions(int correct)
        {
            var old = _answers.transform.Find("Options");
            if (old != null) Object.Destroy(old.gameObject);
            BuildOptions(correct);
        }

        IEnumerator JoinThenAnswer()
        {
            _busy = true;
            SetOptionsActive(false);
            AudioManager.Instance.PlayCollect();
            if (GameManager.Instance != null && GameManager.Instance.IsCalmMode)
            {
                yield return new WaitForSeconds(0.2f);
                _busy = false;
                FinishCorrect();
                yield break;
            }

            Transform groupsParent = _groupB != null ? _groupB.parent : null;
            if (groupsParent != null)
            {
                var hlg = groupsParent.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) hlg.enabled = false;
            }

            var groupA = groupsParent != null ? groupsParent.Find("GroupA") as RectTransform : null;
            var groupB = _groupB as RectTransform;
            var opCard = groupsParent != null ? groupsParent.Find("OpCard") : null;
            var opImg = opCard != null ? opCard.GetComponent<Image>() : null;
            var opTxt = opCard != null ? opCard.GetComponentInChildren<TMP_Text>(true) : null;
            var eqParent = _equation != null ? _equation.transform.parent : null;

            Vector2 startA = groupA != null ? groupA.anchoredPosition : Vector2.zero;
            Vector2 startB = groupB != null ? groupB.anchoredPosition : Vector2.zero;
            Color opImgBase = opImg != null ? opImg.color : Color.white;
            Color opTxtBase = opTxt != null ? opTxt.color : UIKit.Ink;
            Vector3 eqBaseScale = eqParent != null ? eqParent.localScale : Vector3.one;

            const float dur = 0.45f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (_groupB == null) { _busy = false; yield break; }
                float u = Mathf.Clamp01(t / dur);
                float s = Mathf.SmoothStep(0f, 1f, u);
                if (groupA != null) groupA.anchoredPosition = Vector2.Lerp(startA, startA + new Vector2(75f, 0f), s);
                if (groupB != null) groupB.anchoredPosition = Vector2.Lerp(startB, startB + new Vector2(-75f, 0f), s);
                float alphaMul = Mathf.Lerp(1f, 0.15f, s);
                if (opImg != null) opImg.color = new Color(opImgBase.r, opImgBase.g, opImgBase.b, opImgBase.a * alphaMul);
                if (opTxt != null) opTxt.color = new Color(opTxtBase.r, opTxtBase.g, opTxtBase.b, opTxtBase.a * alphaMul);
                if (eqParent != null)
                    eqParent.localScale = eqBaseScale * (1f + 0.12f * Mathf.Sin(u * Mathf.PI));
                yield return null;
            }
            if (eqParent != null) eqParent.localScale = eqBaseScale;
            _busy = false;
            FinishCorrect();
        }

        void FinishCorrect()
        {
            if (_equation != null && _stepIdx < 0)
                _equation.text = C.media.a + " " + C.media.op + " " + C.media.b + " = " + C.correctAnswer;
            Answer(true);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _busy = false;
        }
    }
}
