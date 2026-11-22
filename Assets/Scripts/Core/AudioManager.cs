using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Central audio: voice-over, SFX and music on separate sources.
    /// Voice level sits above music (§8/§9). Settings (sound/music) come from SaveManager.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource _music, _voice, _sfx;
        string _currentMusic;
        // Batch 4 fix: true while the music is ducked under a voice clip.
        bool _ducked;

        const float MusicVolume = 0.45f;
        // Batch 4 fix: music level while a voice-over clip is playing.
        const float DuckedMusicVolume = 0.2f;
        const float VoiceVolume = 1.0f;
        const float SfxVolume = 0.9f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; } // Batch 5 fix: component only — destroying the shared "Managers" GameObject would kill all 9 managers at once
            Instance = this;
            _music = MakeSource("Music", MusicVolume);
            _voice = MakeSource("Voice", VoiceVolume);
            _sfx = MakeSource("Sfx", SfxVolume);
        }

        AudioSource MakeSource(string name, float vol)
        {
            var go = new GameObject("Audio_" + name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.volume = vol;
            return src;
        }

        bool SoundOn { get { return SaveManager.Instance == null || SaveManager.Instance.Data == null || SaveManager.Instance.Data.soundOn; } }
        bool MusicOn { get { return SaveManager.Instance == null || SaveManager.Instance.Data == null || SaveManager.Instance.Data.musicOn; } }
        // Batch 4 fix: separate voice-over switch.
        public bool VoiceOn { get { return SaveManager.Instance == null || SaveManager.Instance.Data == null || SaveManager.Instance.Data.voiceOn; } }

        /// <summary>Music level right now: off, ducked under voice, or full.</summary>
        float MusicVolumeNow() { return !MusicOn ? 0f : (_ducked ? DuckedMusicVolume : MusicVolume); }

        void Update()
        {
            // Batch 4 fix: restore the music level once the voice clip ends.
            if (_ducked && (_voice == null || !_voice.isPlaying))
            {
                _ducked = false;
                ApplySettings();
            }
        }

        public void PlaySfx(string name)
        {
            var clip = GameAssets.Sfx(name);
            if (clip == null || !SoundOn) return;
            _sfx.PlayOneShot(clip);
        }

        public void PlayStar() { PlaySfx("sfx_star"); }
        public void PlayCollect() { PlaySfx("sfx_collect"); }
        public void PlayDrop() { PlaySfx("sfx_drop"); }
        public void PlayCorrect() { PlaySfx("sfx_correct"); }
        public void PlayWrong() { PlaySfx("sfx_wrong"); }
        public void PlayComplete() { PlaySfx("sfx_complete"); }

        /// <summary>Gentle retry cue for wrong answers without harsh buzzer volume.</summary>
        public void PlaySoftRetry()
        {
            if (!SoundOn) return;
            var clip = GameAssets.Sfx("sfx_wrong");
            if (clip != null) _sfx.PlayOneShot(clip, 0.38f);
        }

        public void PlayTimerTick()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null && SaveManager.Instance.Data.calmMode) return;
            PlaySfx("sfx_timer");
        }

        /// <summary>Plays a voice-over clip by name; stops any current voice first.</summary>
        public void PlayVoice(string name)
        {
            var clip = GameAssets.Voice(name);
            if (clip == null || !VoiceOn) return;
            _voice.Stop();
            _voice.clip = clip;
            _voice.Play();
            // Batch 4 fix: duck the music under the voice-over; Update()
            // restores it once the clip finishes.
            if (_music != null && _music.isPlaying && MusicOn)
            {
                _ducked = true;
                _music.volume = DuckedMusicVolume;
            }
        }

        public void StopVoice()
        {
            if (_voice != null) _voice.Stop();
            // Batch 4 fix: stopping VO by hand also ends the duck.
            if (_ducked) { _ducked = false; ApplySettings(); }
        }
        public bool VoicePlaying { get { return _voice != null && _voice.isPlaying; } }

        public void PlayMusic(string name)
        {
            if (_currentMusic == name && _music.isPlaying) return;
            _currentMusic = name;
            var clip = GameAssets.Music(name);
            if (clip == null) return;
            _music.Stop();
            _music.clip = clip;
            _music.loop = true;
            _music.volume = MusicVolumeNow();
            _music.Play();
        }

        public void ApplySettings()
        {
            if (_music != null) _music.volume = MusicVolumeNow();
        }
    }
}
