using UnityEngine;
using UnityEngine.UI;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// One reusable gameplay interaction module. All 8 module types (§3) implement this;
    /// modules render into the gameplay screen's fixed activity/answer areas and report
    /// answers through the screen (→ GameManager).
    /// </summary>
    public abstract class ModuleBase
    {
        public abstract string Type { get; }
        protected GameplayScreen S;
        protected ChallengeData C;

        public void Init(GameplayScreen screen, ChallengeData challenge)
        {
            S = screen;
            C = challenge;
        }

        public abstract void Build(GameObject activityArea, GameObject answerArea);
        public virtual void Tick(float dt) { }
        public virtual void Cleanup()
        {
            DragDropManager.Clear();
        }

        protected void Answer(bool correct) { S.ReportAnswer(correct); }

        protected static Sprite Obj(string name) { return GameAssets.Obj(name); }
        protected static Sprite Ui(string name) { return GameAssets.Ui(name); }

        protected static Color Tint(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.white;
        }

        protected static Color ColorOf(string name)
        {
            switch (name)
            {
                // Batch 5 fix: use the shared palette constants (same shades as
                // the old Hex literals). "yellow" stays "#FDD835" — it is NOT
                // UIKit.Yellow (#FFD166), a visibly different shade.
                case "red": return UIKit.Red;
                case "blue": return UIKit.Blue;
                case "green": return UIKit.Green;
                case "yellow": return UIKit.Hex("#FDD835");
                default: return Color.white;
            }
        }

        /// <summary>Batch 5 fix: parses a data-driven answer string without
        /// throwing — a bad value logs and falls back to 0 instead of crashing
        /// the round (int.Parse used to throw on e.g. sorting "done"). The
        /// validator rejects non-numeric answers, so this is defence in depth.</summary>
        protected static int ParseAnswer(string s, string ctx)
        {
            int v;
            if (int.TryParse(s, out v)) return v;
            Debug.LogWarning("[Module] unparsable answer '" + s + "' for " + ctx + " — using 0");
            return 0;
        }

        protected static int[] NumericOptionsAround(int value, int count)
        {
            var list = new System.Collections.Generic.List<int> { value };
            for (int d = 1; list.Count < count; d++)
            {
                if (value - d >= 0 && !list.Contains(value - d)) list.Add(value - d);
                if (list.Count < count && !list.Contains(value + d)) list.Add(value + d);
            }
            return list.ToArray();
        }

        // Batch 5 fix: shared by MathChoiceModule (its duplicate copy is
        // removed) — one ToStringArray for the whole module set.
        protected static string[] ToStringArray(int[] values)
        {
            var arr = new string[values.Length];
            for (int i = 0; i < values.Length; i++) arr[i] = values[i].ToString();
            return arr;
        }

        /// <summary>Builds the numeric answer buttons in the answer area (shared by potion/sorting/directions).</summary>
        protected void BuildNumberOptions(int correct, bool startDisabled = false)
        {
            var opts = C.options != null && C.options.Length > 0 ? C.options : ToStringArray(NumericOptionsAround(correct, 4));
            var row = UIKit.HLayout(S.AnswerArea, "Options", 32, TextAnchor.MiddleCenter, false);
            UIKit.Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160, 134));
            foreach (var o in opts)
            {
                // Batch 5 fix: a non-numeric option is corrupt data — log and
                // skip the button instead of throwing on int.Parse (the
                // validator now rejects non-numeric options for choice types).
                int val;
                if (!int.TryParse(o, out val))
                {
                    Debug.LogWarning("[Module] skipping non-numeric option '" + o + "' for " + C.id);
                    continue;
                }
                var b = UIKit.TextButton(row.gameObject, "Opt" + o, o, new Vector2(172, 122),
                    UIKit.Teal, () => Answer(val == correct), Ui("ui_btn"), 62);
                UIKit.Flex(b.gameObject, 172, 122);
                b.interactable = !startDisabled;
            }
        }

        protected void SetOptionsInteractable(bool on)
        {
            var buttons = S.AnswerArea.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons) b.interactable = on;
        }
    }
}
