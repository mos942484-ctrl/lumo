using System;

namespace Lumo.Core
{
    /// <summary>
    /// Data model mirroring Assets/Resources/Data/levels.json (single source of truth).
    /// Field names MUST match the JSON exactly — JsonUtility is name-based.
    /// </summary>
    [Serializable]
    public class LevelBank
    {
        public int version;
        public string[] skills;
        public LevelData[] levels;
        public ChallengeData[] variants;
    }

    [Serializable]
    public class LevelData
    {
        public int id;
        public string name;
        public string world;      // math | english | mixed | final
        public string bg;         // background sprite name
        public IntroData intro;
        public IloData[] ilos;    // LXD Phase B: 1-3 intended learning outcomes (validated)
        public ChallengeData[] challenges;
    }

    /// <summary>
    /// Intended Learning Outcome (LXD Phase B, Abbott 2021 §3.1): what should change
    /// in the learner. One entry per level (1-3), with a kid-facing version shown on
    /// the level intro screen. `skills` lists the tracked skills this ILO covers —
    /// the validator requires the union to cover every skill used by the level's
    /// core challenges.
    /// </summary>
    [Serializable]
    public class IloData
    {
        public string text;       // adult-facing outcome, Bloom-style verb, <=120 chars
        public string kidText;    // "I can ..." shown to the child on the intro screen
        public string verb;       // recall | recognise | identify | understand | apply | ...
        public string assess;     // knowledge | understanding | attitude
        public string[] skills;   // tracked skills covered by this ILO
    }

    [Serializable]
    public class IntroData
    {
        public string story;
        public string goal;
        public string vo;
    }

    [Serializable]
    public class ChallengeData
    {
        public string id;
        public int level;         // 1..13, 0 for variants
        public int round;         // 1..3 (1..12 for level 13), 0 for variants
        public int part;          // level 13: 1..5, otherwise 0
        public string type;       // one of the 12 module types
        public string skill;      // one of the 13 tracked skills
        public string difficulty; // base | easier | harder
        public int timer;         // seconds (30 by default)
        public string prompt;     // on-screen text + voice-over script line
        public string vo;         // voice-over clip name
        public MediaData media;
        public string correctAnswer;
        public string[] options;  // for choice-based types (empty otherwise)
        public string[] hints;    // [0] after 2 mistakes, [1] pictured step-by-step after 3
        public FeedbackData feedback;
        public string[] words;    // English words involved (word re-drill tracking)
        public string easier;     // variant id for adaptive remediation
        public string harder;     // variant id for adaptive advancement
        public string band5to7;   // concrete 5-7 age-band variant id (Phase 2)
        public string band11to12; // deductive 11-12 age-band variant id (Phase 2)
        public int ilo;           // LXD Phase B: index into the level's ilos (core challenges only; variants inherit)
    }

    [Serializable]
    public class FeedbackData
    {
        public string success;
        public string retry;
        public string explain;    // LXD Phase C4 (optional): one-sentence "why", shown above the step-by-step solution at mistake 3
    }

    /// <summary>
    /// Flattened, type-specific media description. Fields unused by a challenge's
    /// type are ignored. Kept flat so JsonUtility can parse it.
    /// </summary>
    [Serializable]
    public class MediaData
    {
        // math-choice
        public string obj;         // object sprite name ("" when equation-only)
        public int a;              // first operand / group count
        public int b;              // second operand
        public string op;          // "+" | "-"
        public string demo;        // join | disappear | none
        public bool countOnly;     // pure counting (no operation)
        public bool numberline;    // show interactive number line
        public int start;          // number line start value
        public int move;           // number line steps (signed)
        public StepData[] steps;   // two-step problems, shown one at a time

        // word-picture
        public PicData[] pictures;
        public string word;        // target English word (always shown as text)
        public string voWord;      // word pronunciation clip

        // matching
        public TripleData[] triples;

        // spelling (word/obj/voWord above)
        public string[] preset;    // scrambled letter order

        // potion
        public ColorCount[] colors;
        public int remove;         // stars to remove after adding

        // directions
        public int gw; public int gh;   // grid size
        public int sx; public int sy;   // start cell
        public int tx; public int ty;   // target cell
        public MoveData[] moves;
        public int collect;        // keys to collect (keys round)
        public int use;            // keys to use afterwards

        // sentence
        public string[] words;     // scrambled word cards
        public string sentence;    // target sentence
        public string voSentence;  // full-sentence pronunciation
        public string tint;        // object tint (e.g. red ball)

        // sorting
        public SortItem[] items;
        public string[] boxes;     // box colors
        public string question;    // collect-red | collect-blue | total-remove

        // choice-card (Phase 2)
        public string subtitle;
        public string scenario;
        public string[] clues;
        public string[] sequence;

        // memory-match (Phase 2)
        public MemoryPair[] pairs;

        // pixel-art (Phase 2; uses gw, gh)
        public string[] palette;
        public int[] targetGrid;
        public int[] startGrid;
        public string artMode;     // copy | symmetry | pattern

        // music-play (Phase 2)
        public string[] notes;
        public bool showGuides;
        public string musicMode;   // echo | next
    }

    [Serializable] public class StepData   { public int a; public int b; public string op; public int r; }
    [Serializable] public class PicData    { public string id; public string sprite; public string tint; public float size; }
    [Serializable] public class TripleData { public string num; public string picObj; public int picCount; public string word; }
    [Serializable] public class ColorCount { public string color; public int count; }
    [Serializable] public class MoveData   { public string dir; public int n; }
    [Serializable] public class SortItem   { public string color; public string kind; public int count; }

    [Serializable]
    public class MemoryPair
    {
        public string id;
        public string aLabel;
        public string aSprite;
        public string aTint;
        public string bLabel;
        public string bSprite;
        public string bTint;
    }
}
