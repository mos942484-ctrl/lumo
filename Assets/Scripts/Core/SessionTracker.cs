using System.Collections.Generic;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// LXD Phase D1: out-game session reflection. Snapshots the session start
    /// (stars, shards, per-skill mastery, review words) and accumulates what the
    /// child actually did this session (islands entered, levels completed, review
    /// words answered clean) so the SessionRecap screen can speak kid language.
    /// Session-only, in memory, never persisted (aggregates already live in the save).
    /// </summary>
    public class SessionTracker : MonoBehaviour
    {
        public static SessionTracker Instance { get; private set; }

        int _starsAtStart, _shardsAtStart, _reviewAtStart;
        int[] _masteryAtStart = new int[0];
        readonly HashSet<int> _islandsEntered = new HashSet<int>();
        readonly HashSet<int> _levelsCompleted = new HashSet<int>();
        readonly HashSet<string> _wordsClean = new HashSet<string>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BeginSession();
        }

        /// <summary>Snapshots the save at session start (safe to call again = fresh session).</summary>
        public void BeginSession()
        {
            _islandsEntered.Clear();
            _levelsCompleted.Clear();
            _wordsClean.Clear();
            _starsAtStart = 0;
            _shardsAtStart = 0;
            _reviewAtStart = 0;
            _masteryAtStart = new int[SaveManager.SkillCount];
            var d = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
            if (d == null) return;
            _starsAtStart = d.totalStars;
            _reviewAtStart = d.reviewWords != null ? d.reviewWords.Length : 0;
            if (d.skills != null)
                for (int i = 0; i < Mathf.Min(_masteryAtStart.Length, d.skills.Length); i++)
                    _masteryAtStart[i] = d.skills[i] != null ? d.skills[i].mastery : 0;
            if (ScoreManager.Instance != null) _shardsAtStart = ScoreManager.Instance.ShardsCollected();
        }

        public void RecordLevelStarted(int levelId)
        {
            if (levelId >= 1 && levelId <= 13) _islandsEntered.Add(levelId);
        }

        public void RecordLevelCompleted(int levelId)
        {
            if (levelId >= 1 && levelId <= 13) _levelsCompleted.Add(levelId);
        }

        /// <summary>A review word answered first-try clean this session ("I remembered it!").</summary>
        public void RecordWordClean(string word)
        {
            if (!string.IsNullOrEmpty(word)) _wordsClean.Add(word.ToLower());
        }

        /// <summary>At least one level was completed this session (recap trigger for the menu return).</summary>
        public bool HasCompletedLevel { get { return _levelsCompleted.Count > 0; } }

        public IReadOnlyCollection<int> IslandsEntered { get { return _islandsEntered; } }

        /// <summary>Skill indices whose mastery increased since the session snapshot.</summary>
        public List<string> MasteryGains(LevelBank bank, SaveData d)
        {
            var gains = new List<string>();
            if (bank == null || d == null || d.skills == null) return gains;
            for (int i = 0; i < bank.skills.Length && i < _masteryAtStart.Length && i < d.skills.Length; i++)
            {
                var now = d.skills[i] != null ? d.skills[i].mastery : 0;
                if (now > _masteryAtStart[i] && now > 0)
                    gains.Add(FriendlySkill(bank.skills[i]));
            }
            return gains;
        }

        static string FriendlySkill(string skill)
        {
            switch (skill)
            {
                case "addition": return "Adding";
                case "subtraction": return "Taking Away";
                case "twoStep": return "Two-Step Problems";
                case "counting": return "Counting";
                case "vocabulary": return "Words";
                case "spelling": return "Spelling";
                case "sentence": return "Sentences";
                case "directions": return "Directions";
                case "kindness": return "Kindness";
                case "science": return "Science";
                case "memory": return "Memory";
                case "art": return "Pixel Art";
                case "music": return "Music";
                default: return skill;
            }
        }

        /// <summary>Builds the recap body (kid language, tap-only screen). Static core so tests can exercise it.</summary>
        public string BuildRecapText(LevelBank bank, SaveData d)
        {
            var sb = new System.Text.StringBuilder();

            var islands = new List<string>();
            foreach (var id in _islandsEntered)
                islands.Add(IslandCatalog.Get(id).IslandName);
            sb.AppendLine(islands.Count > 0
                ? "Today you visited: " + string.Join(", ", islands.ToArray())
                : "Today you set sail - the sea awaits!");

            if (_levelsCompleted.Count > 0)
            {
                var done = new List<string>();
                foreach (var id in _levelsCompleted)
                    done.Add(IslandCatalog.Get(id).IslandName);
                sb.AppendLine("You finished: " + string.Join(", ", done.ToArray()) + "!");
            }

            if (_wordsClean.Count > 0)
                sb.AppendLine("Words you remembered: " + string.Join(", ", new List<string>(_wordsClean).ToArray()));
            else if (d != null && d.reviewWords != null && d.reviewWords.Length > 0)
                sb.AppendLine("Some words are waiting for tomorrow - that is how memory grows!");

            var gains = MasteryGains(bank, d);
            foreach (var g in gains)
                sb.AppendLine("You got better at " + g + "!");

            string next = d != null ? Lumo.UI.ParentsPanel.GetSuggestedNextIsland(d) : null;
            if (!string.IsNullOrEmpty(next))
                sb.AppendLine("Tomorrow, try: " + next + "!");
            return sb.ToString();
        }
    }
}
