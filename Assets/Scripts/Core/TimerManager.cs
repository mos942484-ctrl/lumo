using System;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Per-challenge 30 s timer (§6.1): starts once per challenge, stops on the answer,
    /// resets each round, pauses with Pause. After expiry stars become 0 but the child
    /// can still finish — no game over (§6.2).
    /// </summary>
    public class TimerManager : MonoBehaviour
    {
        public static TimerManager Instance { get; private set; }

        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public float Elapsed { get; private set; }
        public int Duration { get; private set; }

        /// <summary>Seconds remaining, clamped at 0 (display only).</summary>
        public float SecondsLeft { get { return Mathf.Max(0f, Duration - Elapsed); } }

        public event Action<float> OnTick;          // remaining seconds
        public event Action OnExpired;              // 30 s reached (stars now 0)
        bool _expiredFired;
        float _lastTickWhole;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only — destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
        }

        /// <summary>Starts a fresh countdown for one challenge. Starts exactly once per call.</summary>
        public void StartChallenge(int durationSeconds)
        {
            Duration = durationSeconds;
            Elapsed = 0f;
            Running = true;
            Paused = false;
            _expiredFired = false;
            _lastTickWhole = durationSeconds;
        }

        public void Stop() { Running = false; }

        public void Pause() { if (Running) Paused = true; }
        public void Resume() { Paused = false; }

        public void Reset() { Running = false; Paused = false; Elapsed = 0f; }

        void Update()
        {
            if (!Running || Paused) return;
            Elapsed += Time.deltaTime;

            int whole = Mathf.CeilToInt(SecondsLeft);
            if (whole != _lastTickWhole)
            {
                _lastTickWhole = whole;
                if (OnTick != null) OnTick(SecondsLeft);
            }
            if (!_expiredFired && Elapsed >= Duration)
            {
                _expiredFired = true;
                Running = false;            // star counting stops; challenge continues
                if (OnExpired != null) OnExpired();
            }
        }

        public string Format()
        {
            int s = Mathf.CeilToInt(SecondsLeft);
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }
    }
}
