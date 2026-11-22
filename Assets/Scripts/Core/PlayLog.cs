using System;
using System.Collections.Generic;

namespace Lumo.Core
{
    /// <summary>One completed challenge attempt row (LXD Phase E1).</summary>
    [Serializable]
    public struct PlayLogRow
    {
        public int levelId;
        public string challengeId;
        public string skill;
        public int attempts;
        public int hintsUsed;
        public float secondsToCorrect;
        public bool firstTry;
        public string timestamp;
    }

    /// <summary>
    /// LXD Phase E1: session-only play log for gameplay-loop analysis (Abbott §3.9).
    /// One row per solved challenge with attempts/hints/seconds aggregated. In-memory
    /// ring buffer (cap 500), NOT persisted (aggregates already live in SkillStats),
    /// practice rounds excluded at the call site. Privacy invariant: exported only to
    /// the clipboard, never to files or network.
    /// </summary>
    public static class PlayLog
    {
        public const int Cap = 500;
        static readonly List<PlayLogRow> Rows = new List<PlayLogRow>();

        public static void Record(int levelId, string challengeId, string skill, int attempts, int hintsUsed, float secondsToCorrect, bool firstTry)
        {
            Rows.Add(new PlayLogRow
            {
                levelId = levelId,
                challengeId = challengeId ?? "",
                skill = skill ?? "",
                attempts = Mathf0(attempts),
                hintsUsed = Mathf0(hintsUsed),
                secondsToCorrect = secondsToCorrect < 0f ? 0f : secondsToCorrect,
                firstTry = firstTry,
                timestamp = DateTime.Now.ToString("HH:mm:ss")
            });
            if (Rows.Count > Cap) Rows.RemoveRange(0, Rows.Count - Cap);
        }

        static int Mathf0(int v) { return v < 0 ? 0 : v; }

        public static List<PlayLogRow> Snapshot()
        {
            return new List<PlayLogRow>(Rows);
        }

        public static int Count { get { return Rows.Count; } }

        public static void Clear() { Rows.Clear(); }

        /// <summary>Median of the seconds-to-correct values (0 when empty).</summary>
        public static float MedianSeconds(List<PlayLogRow> rows)
        {
            if (rows == null || rows.Count == 0) return 0f;
            var sorted = new List<float>();
            foreach (var r in rows) sorted.Add(r.secondsToCorrect);
            sorted.Sort();
            int n = sorted.Count;
            return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) * 0.5f;
        }
    }

    /// <summary>
    /// LXD Phase E2: CSV session report (per-challenge rows + per-skill summary).
    /// Pure string building so tests can verify the shape; the caller copies it to
    /// the clipboard only (no file IO, no network).
    /// </summary>
    public static class SessionReport
    {
        public static string BuildCsv(List<PlayLogRow> rows, string[] skillOrder)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("level,challenge,skill,attempts,hints,seconds_to_correct,first_try,timestamp");
            if (rows != null)
                foreach (var r in rows)
                    sb.AppendLine(r.levelId + "," + Csv(r.challengeId) + "," + Csv(r.skill) + "," +
                                  r.attempts + "," + r.hintsUsed + "," +
                                  r.secondsToCorrect.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "," +
                                  (r.firstTry ? "true" : "false") + "," + r.timestamp);
            sb.AppendLine("");
            sb.AppendLine("skill,attempts,first_try_pct,hints,median_seconds");
            if (skillOrder != null && rows != null)
                foreach (var skill in skillOrder)
                {
                    var subset = rows.FindAll(r => r.skill == skill);
                    if (subset.Count == 0) continue;
                    int attempts = 0, firstTry = 0, hints = 0;
                    foreach (var r in subset)
                    {
                        attempts += r.attempts;
                        hints += r.hintsUsed;
                        if (r.firstTry) firstTry++;
                    }
                    int pct = attempts > 0 ? MathfPct(100f * firstTry / subset.Count) : 0;
                    sb.AppendLine(Csv(skill) + "," + attempts + "," + pct + "," + hints + "," +
                                  PlayLog.MedianSeconds(subset).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
                }
            return sb.ToString();
        }

        static int MathfPct(float v) { return (int)(v + 0.5f); }

        static string Csv(string s) { return "\"" + (s ?? "").Replace("\"", "'") + "\""; }
    }
}
