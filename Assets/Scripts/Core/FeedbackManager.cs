using System;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Immediate encouraging feedback on every answer (§6.3): visual + soft audio + voice.
    /// Guarantees gentle retry cues (PlaySoftRetry, never a buzzer) and supportive text.
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        public static FeedbackManager Instance { get; private set; }

        const string DefaultSuccessLine = "Great job! Keep going!";
        const string DefaultRetryLine = "Good try! Let's try again!";
        const string DefaultHintLine = "Take your time and look closely!";
        const string DefaultSolutionLine = "Let's walk through this step by step together!";

        static readonly string[] PunishingTokens = { "wrong", "incorrect", "failed", "failure", "error", "stupid", "bad", "buzzer" };

        // Batch 5 fix: events, not public Action fields — a field could be
        // overwritten with = and silently drop every other subscriber.
        public event Action<string, string> OnSuccess;   // (success line, challenge id)
        public event Action<string, string> OnRetry;     // (retry line, challenge id)
        public event Action<string> OnHint;              // hint text
        public event Action<string> OnSolution;          // pictured step-by-step narration

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only — destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
        }

        public void Success(ChallengeData c)
        {
            if (c == null) return;
            string cid = c.id ?? "";
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCorrect();
                string voKey = cid.StartsWith("RV_") ? "vo_word_" + c.correctAnswer : "vo_fb_" + cid + "_ok";
                AudioManager.Instance.PlayVoice(voKey);
            }
            string raw = c.feedback != null ? c.feedback.success : null;
            if (OnSuccess != null) OnSuccess(SupportiveText(raw, DefaultSuccessLine), cid);
        }

        public void Retry(ChallengeData c)
        {
            if (c == null) return;
            string cid = c.id ?? "";
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySoftRetry();
                string voKey = cid.StartsWith("RV_") ? "vo_word_" + c.correctAnswer : "vo_fb_" + cid + "_no";
                AudioManager.Instance.PlayVoice(voKey);
            }
            string raw = c.feedback != null ? c.feedback.retry : null;
            if (OnRetry != null) OnRetry(SupportiveText(raw, DefaultRetryLine), cid);
        }

        public void Hint(ChallengeData c)
        {
            if (c == null) return;
            string raw = (c.hints != null && c.hints.Length > 0) ? c.hints[0] : null;
            if (OnHint != null) OnHint(SupportiveText(raw, DefaultHintLine));
        }

        public void Solution(ChallengeData c)
        {
            if (c == null) return;
            string steps = (c.hints != null && c.hints.Length > 1) ? c.hints[1] : null;
            steps = SupportiveText(steps, DefaultSolutionLine);
            // LXD Phase C4: the optional one-sentence "why" (feedback.explain) sits
            // above the step-by-step solution; the anti-punishment filter still runs.
            string explain = c.feedback != null ? c.feedback.explain : null;
            if (!string.IsNullOrWhiteSpace(explain))
            {
                string filtered = SupportiveText(explain, "");
                if (!string.IsNullOrWhiteSpace(filtered))
                    steps = filtered + "\n" + steps;
            }
            if (OnSolution != null) OnSolution(steps);
        }

        static string SupportiveText(string text, string fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            string lower = text.ToLowerInvariant();
            for (int i = 0; i < PunishingTokens.Length; i++)
            {
                if (ContainsWholeWord(lower, PunishingTokens[i]))
                    return fallback;
            }
            return text;
        }

        static bool ContainsWholeWord(string text, string word)
        {
            int idx = 0;
            while ((idx = text.IndexOf(word, idx, StringComparison.Ordinal)) >= 0)
            {
                bool leftOk = idx == 0 || !char.IsLetterOrDigit(text[idx - 1]);
                int end = idx + word.Length;
                bool rightOk = end >= text.Length || !char.IsLetterOrDigit(text[end]);
                if (leftOk && rightOk) return true;
                idx = end;
            }
            return false;
        }
    }
}
