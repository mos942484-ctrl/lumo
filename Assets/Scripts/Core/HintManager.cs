using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Mistake rules (§6.2): after 2 mistakes → text hint; after 3 → pictured
    /// step-by-step solution. Counts per challenge, resets on each new challenge.
    /// </summary>
    public class HintManager : MonoBehaviour
    {
        public static HintManager Instance { get; private set; }

        public int Mistakes { get; private set; }
        public bool HintShown { get; private set; }
        public bool SolutionShown { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only — destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
        }

        public void BeginChallenge()
        {
            Mistakes = 0;
            HintShown = false;
            SolutionShown = false;
        }

        /// <summary>Registers one wrong answer; returns the hint level to show: 0 none, 1 hint, 2 solution.</summary>
        public int RegisterMistake()
        {
            Mistakes++;
            // Batch 4 fix: the solution is shown exactly once, on the 3rd
            // mistake — the old >= 3 repeated it on mistakes 4, 5, ...
            if (Mistakes == 3)
            {
                SolutionShown = true;
                return 2;
            }
            if (Mistakes == 2)
            {
                HintShown = true;
                return 1;
            }
            return 0;
        }

        /// <summary>Manual hint request via the Hint button — counts as used help (no first-try bonus).</summary>
        public void MarkHintUsed() { HintShown = true; }

        public bool UsedHelp { get { return HintShown || SolutionShown; } }
    }
}
