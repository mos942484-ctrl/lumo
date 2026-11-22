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
    /// music-play (Phase 2): 7-bar offline procedural xylophone supporting echo
    /// and find-the-next-note rounds (guided or ear-only) for Music Room (Level 13).
    /// </summary>
    public class MusicPlayModule : ModuleBase
    {
        static readonly string[] BlockingOverlays = { "Pause", "Hint", "Settings", "ParentalGate", "Parents", "TimeLimit" };
        static readonly string[] BarNotes = { "C", "D", "E", "F", "G", "A", "C5" };
        static readonly float[] BarFreqs = { 261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 523.25f };
        static readonly float[] BarHeights = { 360f, 336f, 312f, 288f, 264f, 240f, 216f };
        static readonly Color[] BarColors =
        {
            UIKit.Coral, UIKit.Orange, UIKit.Yellow, UIKit.Green, UIKit.Teal, UIKit.Blue, UIKit.Purple
        };

        static readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

        class BarView
        {
            public string Note;
            public Image Bg;
            public Color BaseColor;
            public RectTransform Rt;
        }

        readonly Dictionary<string, BarView> _bars = new Dictionary<string, BarView>();
        AudioSource _audioSource;
        TMP_Text _progress;
        string[] _targetNotes;
        string _mode;
        bool _showGuides;
        int _inputIdx;
        bool _playingDemo;
        bool _solved;
        bool _active;

        public override string Type { get { return "music-play"; } }
        public bool IsPlayingDemo { get { return _playingDemo; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _bars.Clear();
            _inputIdx = 0;
            _playingDemo = false;
            _solved = false;
            _active = true;

            var m = C != null ? C.media : null;
            _targetNotes = (m != null && m.notes != null && m.notes.Length > 0) ? m.notes : new[] { "C", "E", "G" };
            _mode = (m != null && m.musicMode == "next") ? "next" : "echo";
            _showGuides = m == null || m.showGuides;

            var audioGo = UIKit.Node(activityArea, "XyloAudio");
            _audioSource = audioGo.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0.85f;

            var progressPill = UIKit.Card(activityArea, "ProgressPill", UIKit.CreamAlpha(0.95f), Ui("ui_panel"));
            UIKit.Place(progressPill.rectTransform, new Vector2(0.5f, 0.89f), Vector2.zero, new Vector2(760f, 68f));
            string initProgress = _mode == "next" ? "Find the next note!" : FormatEchoProgress();
            _progress = UIKit.Label(progressPill.gameObject, "Progress", initProgress, 30, UIKit.Ink);
            UIKit.Stretch(_progress.rectTransform, 16f, 6f);

            var rack = UIKit.Card(activityArea, "XyloRack", UIKit.CreamAlpha(0.90f), Ui("ui_panel"));
            UIKit.Place(rack.rectTransform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(1320f, 410f));

            var rail = UIKit.Panel(rack.gameObject, "BaseRail", UIKit.Brown, Ui("ui_panel"));
            rail.raycastTarget = false;
            UIKit.Place(rail.rectTransform, new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(1220f, 46f));

            var barRow = UIKit.HLayout(rack.gameObject, "Bars", 24f, TextAnchor.MiddleCenter, false);
            UIKit.Place(barRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(1220f, 380f));

            for (int i = 0; i < BarNotes.Length; i++)
            {
                string note = BarNotes[i];
                Color col = BarColors[i];
                float h = BarHeights[i];

                var bg = UIKit.Card(barRow.gameObject, "Bar_" + note, col, Ui("ui_btn"));
                UIKit.Flex(bg.gameObject, 148f, h);

                var topPin = UIKit.Img(bg.gameObject, "TopPin", Ui("ui_node"), new Color(1f, 1f, 1f, 0.55f));
                UIKit.Place(topPin.rectTransform, new Vector2(0.5f, 0.87f), Vector2.zero, new Vector2(18f, 18f));
                var botPin = UIKit.Img(bg.gameObject, "BotPin", Ui("ui_node"), new Color(1f, 1f, 1f, 0.55f));
                UIKit.Place(botPin.rectTransform, new Vector2(0.5f, 0.13f), Vector2.zero, new Vector2(18f, 18f));

                float lum = 0.299f * col.r + 0.587f * col.g + 0.114f * col.b;
                var lbl = UIKit.Label(bg.gameObject, "Note", note, 42, lum < 0.52f ? Color.white : UIKit.Ink);
                UIKit.Stretch(lbl.rectTransform, 8f, 8f);

                var btn = bg.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                string captured = note;
                btn.onClick.AddListener(() => TapBar(captured));

                _bars[note] = new BarView
                {
                    Note = note,
                    Bg = bg,
                    BaseColor = col,
                    Rt = bg.rectTransform
                };
            }

            var listenBtn = UIKit.TextButton(answerArea, "ListenAgain", "Listen Again", new Vector2(340f, 110f),
                UIKit.SkyBlue, TriggerDemo, Ui("ui_btn"), 36);
            UIKit.Place(listenBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 110f));

            if (S != null) S.StartRoutine(PlayDemo(true));
        }

        void TriggerDemo()
        {
            if (!_active || _playingDemo || _solved) return;
            if (S != null) S.StartRoutine(PlayDemo(false));
        }

        IEnumerator PlayDemo(bool initialDelay)
        {
            _playingDemo = true;
            if (initialDelay) yield return new WaitForSeconds(0.35f);
            if (!_active || _audioSource == null)
            {
                _playingDemo = false;
                yield break;
            }

            int playCount = _mode == "next" ? Mathf.Max(0, _targetNotes.Length - 1) : _targetNotes.Length;
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float stepWait = calm ? 0.65f : 0.52f;
            float glowDur = calm ? 0.36f : 0.30f;

            for (int i = 0; i < playCount; i++)
            {
                if (!_active) { _playingDemo = false; yield break; }
                string n = _targetNotes[i];
                PlayNoteSound(n);

                if (_showGuides && _bars.TryGetValue(n, out var bar))
                {
                    HighlightBar(bar, true, calm);
                    yield return new WaitForSeconds(glowDur);
                    if (!_active) { _playingDemo = false; yield break; }
                    HighlightBar(bar, false, calm);
                    yield return new WaitForSeconds(Mathf.Max(0.05f, stepWait - glowDur));
                }
                else
                {
                    yield return new WaitForSeconds(stepWait);
                }
            }

            _playingDemo = false;
        }

        void TapBar(string note)
        {
            if (!_active || _playingDemo || _solved) return;

            PlayNoteSound(note);
            if (S != null && _bars.TryGetValue(note, out var bar))
                S.StartRoutine(FlashBar(bar));

            if (_mode == "next")
            {
                string expected = _targetNotes[_targetNotes.Length - 1];
                if (note == expected)
                {
                    _solved = true;
                    UpdateProgress("Great ear!");
                    AudioManager.Instance?.PlayCollect();
                    Answer(true);
                }
                else
                {
                    Answer(false);
                }
                return;
            }

            if (note == _targetNotes[_inputIdx])
            {
                _inputIdx++;
                UpdateProgress(FormatEchoProgress());
                if (_inputIdx >= _targetNotes.Length)
                {
                    _solved = true;
                    AudioManager.Instance?.PlayCollect();
                    Answer(true);
                }
            }
            else
            {
                _inputIdx = 0;
                UpdateProgress(FormatEchoProgress());
                Answer(false);
            }
        }

        public override void Tick(float dt)
        {
#if ENABLE_INPUT_SYSTEM
            if (!_active || _playingDemo || _solved || AnyOverlayOpen()) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) TapBar("C");
            else if (kb.digit2Key.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) TapBar("D");
            else if (kb.digit3Key.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) TapBar("E");
            else if (kb.digit4Key.wasPressedThisFrame || kb.fKey.wasPressedThisFrame) TapBar("F");
            else if (kb.digit5Key.wasPressedThisFrame || kb.gKey.wasPressedThisFrame) TapBar("G");
            else if (kb.digit6Key.wasPressedThisFrame || kb.hKey.wasPressedThisFrame) TapBar("A");
            else if (kb.digit7Key.wasPressedThisFrame || kb.jKey.wasPressedThisFrame) TapBar("C5");
#endif
        }

        static bool AnyOverlayOpen()
        {
            var ui = UIManager.Instance;
            if (ui == null) return false;
            for (int i = 0; i < BlockingOverlays.Length; i++)
                if (ui.IsOverlayOpen(BlockingOverlays[i])) return true;
            return false;
        }

        string FormatEchoProgress()
        {
            string baseText = "Step " + _inputIdx + " / " + _targetNotes.Length;
            if (!_showGuides || _targetNotes == null || _targetNotes.Length == 0)
                return baseText;
            var sb = new System.Text.StringBuilder(baseText);
            sb.Append("  (");
            for (int i = 0; i < _targetNotes.Length; i++)
            {
                if (i > 0) sb.Append(" - ");
                sb.Append(i < _inputIdx ? _targetNotes[i] : "_");
            }
            sb.Append(")");
            return sb.ToString();
        }

        IEnumerator FlashBar(BarView bar)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            HighlightBar(bar, true, calm);
            yield return new WaitForSeconds(0.18f);
            if (_active) HighlightBar(bar, false, calm);
        }

        static void HighlightBar(BarView bar, bool on, bool calm)
        {
            if (bar == null || bar.Bg == null) return;
            bar.Bg.color = on ? Color.Lerp(bar.BaseColor, Color.white, calm ? 0.35f : 0.55f) : bar.BaseColor;
            if (bar.Rt != null)
                bar.Rt.localScale = on ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
        }

        void UpdateProgress(string text)
        {
            if (_progress != null) _progress.text = text;
        }

        void PlayNoteSound(string note)
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null && !SaveManager.Instance.Data.soundOn)
                return;
            var clip = GetOrCreateClip(note);
            if (clip != null && _audioSource != null) _audioSource.PlayOneShot(clip);
        }

        static AudioClip GetOrCreateClip(string note)
        {
            if (string.IsNullOrEmpty(note)) return null;
            if (_clipCache.TryGetValue(note, out var cached) && cached != null) return cached;

            float freq = 261.63f;
            bool found = false;
            for (int i = 0; i < BarNotes.Length; i++)
            {
                if (BarNotes[i] == note) { freq = BarFreqs[i]; found = true; break; }
            }
            if (!found) return null;

            const int sampleRate = 44100;
            const float duration = 0.45f;
            const float attackDur = 0.008f;
            const float tailDur = 0.02f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float attack = Mathf.Clamp01(t / attackDur);
                float tail = Mathf.Clamp01((duration - t) / tailDur);
                float env = attack * tail * Mathf.Exp(-6.5f * t);
                float wave = 0.72f * Mathf.Sin(2f * Mathf.PI * freq * t)
                           + 0.22f * Mathf.Sin(2f * Mathf.PI * (2f * freq) * t);
                samples[i] = env * wave;
            }

            var clip = AudioClip.Create("Xylo_" + note, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            _clipCache[note] = clip;
            return clip;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _active = false;
            _playingDemo = false;
            _solved = false;
            _bars.Clear();
        }
    }
}
