using System.Collections.Generic;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Central asset loader. Every game asset lives under Assets/Resources/ and is
    /// referenced ONLY by its fixed filename — replacing a file (same name) swaps it
    /// into the game with zero code changes (BUILD_PROMPT §9 policy).
    /// Missing assets are tracked for the Lumo/Tests/Audit Assets report.
    /// </summary>
    public static class GameAssets
    {
        public static readonly HashSet<string> MissingSprites = new HashSet<string>();
        public static readonly HashSet<string> MissingAudio = new HashSet<string>();

        // Batch 4 fix: cache sprite lookups so repeated Resources.Load calls
        // (e.g. every frame) are avoided; Resources.Load returns the same
        // instance, but the lookup itself is not free.
        static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        // ---- Sprites (category: "ui", "objects", "characters", "backgrounds") ----
        public static Sprite Sprite(string category, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string path = "Art/" + category + "/" + name;
            Sprite cached;
            if (_spriteCache.TryGetValue(path, out cached)) return cached;
            var s = Resources.Load<Sprite>(path);
            if (s == null) Track(MissingSprites, path);
            _spriteCache[path] = s;
            return s;
        }

        public static Sprite Ui(string name)     { return Sprite("ui", name); }
        public static Sprite Obj(string name)    { return Sprite("objects", name); }
        public static Sprite Char(string name)   { return Sprite("characters", name); }
        public static Sprite Bg(string name)     { return Sprite("backgrounds", name); }

        // ---- Audio ----
        public static AudioClip Voice(string name)  { return LoadAudio("Audio/Voice/" + name); }
        public static AudioClip Sfx(string name)    { return LoadAudio("Audio/SFX/" + name); }
        public static AudioClip Music(string name)  { return LoadAudio("Audio/Music/" + name); }

        static AudioClip LoadAudio(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null) Track(MissingAudio, path);
            return clip;
        }

        // ---- Data ----
        public static TextAsset Levels() { return Resources.Load<TextAsset>("Data/levels"); }

        static void Track(HashSet<string> set, string path)
        {
            if (set.Add(path)) Debug.LogWarning("[GameAssets] missing asset: " + path);
        }
    }
}
