using System;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Tracks daily and lifetime play time locally (offline), per-island seconds,
    /// daily date rollover, and enforces the optional daily time limit.
    /// </summary>
    public class SessionTimeManager : MonoBehaviour
    {
        public static SessionTimeManager Instance { get; private set; }

        public bool TrackingPaused { get; set; }

        public event Action OnDailyLimitReached;

        float _accum;
        float _saveAccum;
        bool _limitFired;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public int EffectiveDailyLimitMinutes
        {
            get
            {
                var d = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
                return (d == null || d.dailyLimitMinutes <= 0) ? 0 : d.dailyLimitMinutes + d.extraMinutesToday;
            }
        }

        public bool IsDailyLimitReached
        {
            get
            {
                var d = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
                int eff = EffectiveDailyLimitMinutes;
                return d != null && eff > 0 && d.todaySecondsPlayed >= eff * 60;
            }
        }

        public int RemainingSecondsToday
        {
            get
            {
                var d = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
                int eff = EffectiveDailyLimitMinutes;
                return (d == null || eff <= 0) ? 0 : Mathf.Max(0, eff * 60 - d.todaySecondsPlayed);
            }
        }

        public void GrantExtraMinutes(int minutes)
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            var d = SaveManager.Instance.Data;
            d.extraMinutesToday = Mathf.Clamp(d.extraMinutesToday + minutes, 0, 240);
            SaveManager.Instance.Save();
            _limitFired = IsDailyLimitReached;
        }

        public void NotifySettingsChanged()
        {
            _limitFired = IsDailyLimitReached;
        }

        void Update()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null || TrackingPaused) return;
            var d = SaveManager.Instance.Data;

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (d.lastPlayDate != today)
            {
                d.lastPlayDate = today;
                d.todaySecondsPlayed = 0;
                d.extraMinutesToday = 0;
                _limitFired = false;
                SaveManager.Instance.Save();
            }

            _accum += Time.unscaledDeltaTime;
            _saveAccum += Time.unscaledDeltaTime;

            if (_accum >= 1f)
            {
                int whole = Mathf.FloorToInt(_accum);
                _accum -= whole;
                d.todaySecondsPlayed += whole;
                d.totalSecondsPlayed += whole;

                if (Lumo.UI.UIManager.Instance != null &&
                    Lumo.UI.UIManager.Instance.CurrentScreenName == "Gameplay" &&
                    GameManager.Instance != null &&
                    GameManager.Instance.CurrentLevelId >= 1 &&
                    GameManager.Instance.CurrentLevelId <= 13 &&
                    d.islandSeconds != null &&
                    d.islandSeconds.Length == 13)
                {
                    d.islandSeconds[GameManager.Instance.CurrentLevelId - 1] += whole;
                }
            }

            if (_saveAccum >= 30f)
            {
                _saveAccum = 0f;
                SaveManager.Instance.Save();
            }

            bool reached = IsDailyLimitReached;
            if (reached && !_limitFired)
            {
                _limitFired = true;
                SaveManager.Instance.Save();
                if (OnDailyLimitReached != null) OnDailyLimitReached();
            }
            else if (!reached)
            {
                _limitFired = false;
            }
        }

        void OnApplicationPause(bool pause)
        {
            if (pause && SaveManager.Instance != null && SaveManager.Instance.Data != null)
                SaveManager.Instance.Save();
        }

        void OnApplicationQuit()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
                SaveManager.Instance.Save();
        }
    }
}
