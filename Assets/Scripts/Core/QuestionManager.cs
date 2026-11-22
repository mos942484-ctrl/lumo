using System.Collections.Generic;
using UnityEngine;

namespace Lumo.Core
{
    /// <summary>
    /// Loads and validates Data/levels.json (the single source of truth for all content).
    /// Validation fails LOUDLY with the offending entry id — a malformed bank must never
    /// reach gameplay silently.
    /// </summary>
    public static class QuestionManager
    {
        public static readonly string[] ValidTypes = {
            "math-choice", "word-picture", "matching", "spelling",
            "potion", "directions", "sentence", "sorting",
            "choice-card", "memory-match", "pixel-art", "music-play"
        };
        public static readonly string[] ValidSkills = {
            "addition", "subtraction", "twoStep", "counting",
            "vocabulary", "spelling", "sentence", "directions",
            "kindness", "science", "memory", "art", "music"
        };
        static readonly string[] ValidNotes = { "C", "D", "E", "F", "G", "A", "C5" };

        // LXD Phase B: fixed ILO vocabularies (Abbott 2021 §3.1 — few, specific,
        // measurable outcomes with a Bloom-style verb and an assess type).
        public static readonly string[] ValidIloVerbs = {
            "recall", "recognise", "identify", "understand", "apply", "sequence",
            "construct", "classify", "deduce", "produce", "perform", "choose", "reflect"
        };
        public static readonly string[] ValidIloAssess = { "knowledge", "understanding", "attitude" };
        public const int MaxIloTextLength = 120;

        /// <summary>Looks up a variant challenge by id, or null when missing.</summary>
        public static ChallengeData GetVariant(LevelBank bank, string id)
        {
            if (bank == null || bank.variants == null || string.IsNullOrEmpty(id)) return null;
            foreach (var v in bank.variants)
                if (v != null && v.id == id) return v;
            return null;
        }

        /// <summary>Loads Data/levels from Resources and validates it. Throws with all errors listed.</summary>
        public static LevelBank LoadAndValidate()
        {
            var asset = Resources.Load<TextAsset>("Data/levels");
            if (asset == null)
                throw new System.InvalidOperationException("[QuestionManager] Data/levels.json not found in Resources!");

            LevelBank bank;
            try { bank = JsonUtility.FromJson<LevelBank>(asset.text); }
            catch (System.Exception e) { throw new System.InvalidOperationException("[QuestionManager] JSON parse failed: " + e.Message); }
            if (bank == null || bank.levels == null || bank.levels.Length == 0)
                throw new System.InvalidOperationException("[QuestionManager] JSON parsed to an empty bank.");

            var errors = new List<string>();
            Validate(bank, errors);

            if (errors.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("[QuestionManager] VALIDATION FAILED with " + errors.Count + " error(s):");
                foreach (var e in errors) sb.AppendLine("  - " + e);
                Debug.LogError(sb.ToString());
                throw new System.InvalidOperationException("[QuestionManager] levels.json is malformed — see console for entry ids.");
            }
            return bank;
        }

        public static void Validate(LevelBank bank, List<string> errors)
        {
            if (bank.skills == null || bank.skills.Length != ValidSkills.Length)
                errors.Add("bank: skills array must list all " + ValidSkills.Length + " tracked skills");
            else
                foreach (var vs in ValidSkills)
                    if (!System.Array.Exists(bank.skills, s => s == vs))
                        errors.Add("bank: missing required skill '" + vs + "'");

            var ids = new HashSet<string>();
            // variantIds is ONLY for easier/harder/age-band link lookups below — duplicate
            // detection uses the single shared `ids` set (Batch 5 fix: core and
            // variant ids used to be checked in separate sets, so a variant
            // could silently reuse a core challenge id).
            var variantIds = new HashSet<string>();
            if (bank.variants != null)
                foreach (var v in bank.variants) variantIds.Add(v.id);

            if (bank.levels == null || bank.levels.Length != 13)
                errors.Add("bank: expected 13 levels, found " + (bank.levels != null ? bank.levels.Length : 0));

            int coreChallenges = 0, finalTasks = 0;
            if (bank.levels != null)
            {
                foreach (var lv in bank.levels)
                {
                    string where = "level " + lv.id + " (" + lv.name + ")";
                    if (lv.id < 1 || lv.id > 13) errors.Add(where + ": id out of range 1..13");
                    if (string.IsNullOrEmpty(lv.name)) errors.Add(where + ": missing name");
                    if (string.IsNullOrEmpty(lv.bg)) errors.Add(where + ": missing bg");
                    if (lv.intro == null || string.IsNullOrEmpty(lv.intro.story) ||
                        string.IsNullOrEmpty(lv.intro.goal) || string.IsNullOrEmpty(lv.intro.vo))
                        errors.Add(where + ": intro incomplete (story/goal/vo)");

                    // LXD Phase B: every level declares 1-3 ILOs covering all its skills.
                    ValidateIlos(lv, errors);

                    int expectedMin = lv.id == 13 ? 12 : 3;
                    int count = lv.challenges != null ? lv.challenges.Length : 0;
                    if (count < expectedMin)
                        errors.Add(where + ": has " + count + " challenges, expected at least " + expectedMin);

                    if (lv.challenges == null) continue;
                    foreach (var c in lv.challenges)
                    {
                        ValidateChallenge(c, errors, ids);
                        if (lv.id <= 12) coreChallenges++; else finalTasks++;
                        // LXD Phase B: every core challenge must point at a valid ILO index
                        // (variants are exempt — they inherit their parent's ILO).
                        if (lv.ilos == null || c.ilo < 0 || c.ilo >= lv.ilos.Length)
                            errors.Add(c.id + ": ilo index " + c.ilo + " out of range for level " + lv.id + " ilos");
                        // variant link integrity
                        if (!string.IsNullOrEmpty(c.easier) && !variantIds.Contains(c.easier))
                            errors.Add(c.id + ": easier variant '" + c.easier + "' not found");
                        if (!string.IsNullOrEmpty(c.harder) && !variantIds.Contains(c.harder))
                            errors.Add(c.id + ": harder variant '" + c.harder + "' not found");
                        if (!string.IsNullOrEmpty(c.band5to7) && !variantIds.Contains(c.band5to7))
                            errors.Add(c.id + ": band5to7 variant '" + c.band5to7 + "' not found");
                        if (!string.IsNullOrEmpty(c.band11to12) && !variantIds.Contains(c.band11to12))
                            errors.Add(c.id + ": band11to12 variant '" + c.band11to12 + "' not found");
                    }
                }
            }
            if (coreChallenges < 36) errors.Add("bank: expected at least 36 core challenges (L1-12), found " + coreChallenges);
            if (finalTasks < 12) errors.Add("bank: expected at least 12 final tasks (L13), found " + finalTasks);

            // Every tracked skill must have at least one base challenge with both an
            // easier/band5to7 link and a harder/band11to12 link so adaptive difficulty
            // and age-band routing always resolve.
            if (bank.skills != null)
                foreach (var skill in bank.skills)
                {
                    bool linked = false;
                    if (bank.levels != null)
                        foreach (var lv in bank.levels)
                        {
                            if (lv.challenges == null) continue;
                            foreach (var c in lv.challenges)
                            {
                                bool hasEasy = !string.IsNullOrEmpty(c.easier) || !string.IsNullOrEmpty(c.band5to7);
                                bool hasHard = !string.IsNullOrEmpty(c.harder) || !string.IsNullOrEmpty(c.band11to12);
                                if (c.skill == skill && hasEasy && hasHard) linked = true;
                            }
                        }
                    if (!linked) errors.Add("skill '" + skill + "': no base challenge has both easier/band5to7 and harder/band11to12 links");
                }

            if (bank.variants != null)
            {
                // Batch 5 fix: validate variants against the SAME id set as the
                // core challenges, so a duplicate id across core+variants is caught.
                foreach (var v in bank.variants) ValidateChallenge(v, errors, ids);
                if (bank.variants.Length < 32)
                    errors.Add("bank: expected at least 32 adaptive variants, found " + bank.variants.Length);
            }
        }

        static void ValidateChallenge(ChallengeData c, List<string> errors, HashSet<string> ids)
        {
            string where = "challenge '" + c.id + "'";
            if (string.IsNullOrEmpty(c.id)) { errors.Add("unnamed challenge: missing id"); return; }
            if (!ids.Add(c.id)) errors.Add(where + ": duplicate id");
            if (!System.Array.Exists(ValidTypes, t => t == c.type)) errors.Add(where + ": invalid type '" + c.type + "'");
            if (!System.Array.Exists(ValidSkills, s => s == c.skill)) errors.Add(where + ": invalid skill '" + c.skill + "'");
            if (c.difficulty != "base" && c.difficulty != "easier" && c.difficulty != "harder")
                errors.Add(where + ": invalid difficulty '" + c.difficulty + "'");
            if (c.timer <= 0) errors.Add(where + ": timer must be > 0");
            if (string.IsNullOrEmpty(c.prompt)) errors.Add(where + ": missing prompt");
            if (string.IsNullOrEmpty(c.vo)) errors.Add(where + ": missing vo");
            if (c.hints == null || c.hints.Length != 2 || string.IsNullOrEmpty(c.hints[0]) || string.IsNullOrEmpty(c.hints[1]))
                errors.Add(where + ": needs 2 hints (hint + pictured step-by-step)");
            if (c.feedback == null || string.IsNullOrEmpty(c.feedback.success) || string.IsNullOrEmpty(c.feedback.retry))
                errors.Add(where + ": feedback incomplete (success/retry)");
            // LXD Phase C4: explain is optional, but when present it must be usable
            if (c.feedback != null && c.feedback.explain != null)
            {
                if (string.IsNullOrWhiteSpace(c.feedback.explain))
                    errors.Add(where + ": feedback.explain present but empty");
                else if (c.feedback.explain.Length > 160)
                    errors.Add(where + ": feedback.explain is " + c.feedback.explain.Length + " chars, max 160");
            }
            if (c.media == null) { errors.Add(where + ": missing media"); return; }

            int answer;
            bool answerIsNumber = int.TryParse(c.correctAnswer, out answer);
            var m = c.media;

            switch (c.type)
            {
                case "math-choice":
                {
                    // equation-only ("harder" variants have no picture help per spec) is allowed via demo:"none"
                    if (string.IsNullOrEmpty(m.obj) && !m.numberline && !m.countOnly && m.demo != "none")
                        errors.Add(where + ": math-choice needs obj, numberline, countOnly or demo:'none' (equation-only)");
                    if (m.numberline)
                    {
                        if (m.start + m.move != answer)
                            errors.Add(where + ": numberline " + m.start + " + (" + m.move + ") != " + c.correctAnswer);
                    }
                    else if (m.steps != null && m.steps.Length >= 2)
                    {
                        int r0 = Eval(m.steps[0].a, m.steps[0].b, m.steps[0].op);
                        int r1 = Eval(m.steps[1].a, m.steps[1].b, m.steps[1].op);
                        if (r0 != m.steps[0].r) errors.Add(where + ": step0 arithmetic broken");
                        if (m.steps[1].a != m.steps[0].r) errors.Add(where + ": step1 does not chain from step0");
                        if (r1 != m.steps[1].r) errors.Add(where + ": step1 arithmetic broken");
                        if (r1 != answer) errors.Add(where + ": steps do not end at correctAnswer");
                    }
                    else if (!m.countOnly)
                    {
                        int r = Eval(m.a, m.b, m.op);
                        if (r != answer)
                            errors.Add(where + ": arithmetic " + m.a + " " + m.op + " " + m.b + " = " + r + " != " + c.correctAnswer);
                    }
                    break;
                }
                case "word-picture":
                {
                    if (m.pictures == null || m.pictures.Length == 0)
                        errors.Add(where + ": word-picture needs pictures");
                    else if (!System.Array.Exists(m.pictures, p => p.id == c.correctAnswer))
                        errors.Add(where + ": correctAnswer '" + c.correctAnswer + "' not among pictures");
                    if (string.IsNullOrEmpty(m.word)) errors.Add(where + ": word-picture needs target word (spelling is part of learning)");
                    if (string.IsNullOrEmpty(m.voWord)) errors.Add(where + ": word-picture needs voWord pronunciation");
                    break;
                }
                case "matching":
                {
                    if (m.triples == null || m.triples.Length == 0) { errors.Add(where + ": matching needs a triple"); break; }
                    foreach (var t in m.triples)
                    {
                        // Batch 5 fix: the digit must match the picture count too
                        // (the word-vs-count check below already existed).
                        if (t.num != t.picCount.ToString())
                            errors.Add(where + ": triple num '" + t.num + "' does not match picCount " + t.picCount);
                        string numWord = NumberWord(t.picCount);
                        if (numWord.ToUpper() != t.word)
                            errors.Add(where + ": triple word '" + t.word + "' does not match count " + t.picCount);
                        if (string.IsNullOrEmpty(t.picObj)) errors.Add(where + ": triple missing picObj");
                    }
                    break;
                }
                case "spelling":
                {
                    if (m.word != c.correctAnswer) errors.Add(where + ": spelling word != correctAnswer");
                    if (string.IsNullOrEmpty(m.obj)) errors.Add(where + ": spelling needs object picture");
                    if (m.preset != null && m.preset.Length > 0)
                    {
                        if (m.preset.Length != m.word.Length)
                            errors.Add(where + ": preset length != word length");
                        var a1 = new List<char>(m.word); var a2 = new List<char>();
                        foreach (var ch in m.preset) a2.Add(ch[0]);
                        a1.Sort(); a2.Sort();
                        for (int i = 0; i < System.Math.Min(a1.Count, a2.Count); i++)
                            if (a1[i] != a2[i]) { errors.Add(where + ": preset letters do not match word letters"); break; }
                    }
                    break;
                }
                case "potion":
                {
                    if (m.colors == null || m.colors.Length == 0) { errors.Add(where + ": potion needs colors"); break; }
                    int total = 0;
                    foreach (var cc in m.colors) total += cc.count;
                    total -= m.remove;
                    if (total != answer) errors.Add(where + ": potion total " + total + " != " + c.correctAnswer);
                    break;
                }
                case "directions":
                {
                    if (m.moves != null && m.moves.Length > 0)
                    {
                        int x = m.sx, y = m.sy;
                        foreach (var mv in m.moves)
                        {
                            if (mv.dir == "right") x += mv.n;
                            else if (mv.dir == "left") x -= mv.n;
                            else if (mv.dir == "up") y += mv.n;
                            else if (mv.dir == "down") y -= mv.n;
                            else errors.Add(where + ": unknown direction '" + mv.dir + "'");
                            if (x < 0 || x >= m.gw || y < 0 || y >= m.gh)
                            { errors.Add(where + ": path leaves the grid at (" + x + "," + y + ")"); break; }
                        }
                        if (x != m.tx || y != m.ty)
                            errors.Add(where + ": moves end at (" + x + "," + y + ") but target is (" + m.tx + "," + m.ty + ")");
                    }
                    else if (m.collect > 0)
                    {
                        if (m.collect - m.use != answer)
                            errors.Add(where + ": keys " + m.collect + " - " + m.use + " != " + c.correctAnswer);
                    }
                    else errors.Add(where + ": directions needs moves or a keys round");
                    break;
                }
                case "sentence":
                {
                    if (m.words == null || m.words.Length == 0 || string.IsNullOrEmpty(m.sentence))
                    { errors.Add(where + ": sentence needs words + sentence"); break; }
                    var cardWords = new List<string>();
                    foreach (var w in m.words) cardWords.Add(w.ToLower());
                    cardWords.Sort();
                    var sentWords = new List<string>();
                    foreach (var w in m.sentence.ToLower().Replace(".", "").Replace("!", "").Split(' '))
                        if (w.Length > 0) sentWords.Add(w);
                    sentWords.Sort();
                    if (cardWords.Count != sentWords.Count)
                        errors.Add(where + ": card/sentence word count mismatch");
                    else
                        for (int i = 0; i < cardWords.Count; i++)
                            if (cardWords[i] != sentWords[i]) { errors.Add(where + ": card words do not match sentence words"); break; }
                    if (string.IsNullOrEmpty(m.voSentence)) errors.Add(where + ": sentence needs voSentence");
                    break;
                }
                case "sorting":
                {
                    if (m.items == null || m.items.Length == 0 || m.boxes == null || m.boxes.Length < 2)
                    { errors.Add(where + ": sorting needs items and 2 boxes"); break; }
                    // Batch 5 fix: the old code let correctAnswer "done" skip all
                    // checks, but SortingModule parses the answer as a number —
                    // "done" would crash the round at runtime. No challenge uses
                    // "done", so the escape is removed: sorting answers must be
                    // numeric, like every other choice-based type.
                    if (!answerIsNumber) { errors.Add(where + ": sorting answer must be numeric"); break; }
                    int total = 0;
                    if (m.question == "collect-red")
                    {
                        foreach (var item in m.items) if (item.color == "red") total += item.count;
                    }
                    else if (m.question == "collect-blue")
                    {
                        foreach (var item in m.items) if (item.color == "blue") total += item.count;
                    }
                    else if (m.question == "total-remove")
                    {
                        foreach (var item in m.items) total += item.count;
                        total -= 2;
                    }
                    else errors.Add(where + ": unknown sorting question '" + m.question + "'");
                    if (total != answer) errors.Add(where + ": sorting total " + total + " != " + c.correctAnswer);
                    break;
                }
                case "choice-card":
                {
                    if (c.options == null || c.options.Length < 2 || c.options.Length > 4)
                        errors.Add(where + ": choice-card expects 2..4 options");
                    bool hasBody = !string.IsNullOrEmpty(m.scenario) ||
                                   (m.clues != null && m.clues.Length > 0) ||
                                   (m.sequence != null && m.sequence.Length > 0);
                    if (!hasBody)
                        errors.Add(where + ": choice-card needs scenario, clues, or sequence");
                    break;
                }
                case "memory-match":
                {
                    if (m.pairs == null || m.pairs.Length < 2)
                    {
                        errors.Add(where + ": memory-match needs at least 2 pairs");
                        break;
                    }
                    var pairIds = new HashSet<string>();
                    foreach (var p in m.pairs)
                    {
                        if (p == null || string.IsNullOrEmpty(p.id))
                        {
                            errors.Add(where + ": memory-match pair missing id");
                            continue;
                        }
                        if (!pairIds.Add(p.id))
                            errors.Add(where + ": duplicate memory pair id '" + p.id + "'");
                        if (string.IsNullOrEmpty(p.aLabel) && string.IsNullOrEmpty(p.aSprite))
                            errors.Add(where + ": pair '" + p.id + "' needs aLabel or aSprite");
                    }
                    break;
                }
                case "pixel-art":
                {
                    if (m.gw <= 0 || m.gh <= 0)
                    {
                        errors.Add(where + ": pixel-art invalid dimensions " + m.gw + "x" + m.gh);
                        break;
                    }
                    if (m.palette == null || m.palette.Length == 0)
                        errors.Add(where + ": pixel-art needs non-empty palette");
                    if (m.targetGrid == null || m.targetGrid.Length != m.gw * m.gh)
                        errors.Add(where + ": pixel-art targetGrid length must equal gw*gh");
                    if (m.startGrid != null && m.startGrid.Length > 0 && m.startGrid.Length != m.gw * m.gh)
                        errors.Add(where + ": pixel-art startGrid length must equal gw*gh");
                    break;
                }
                case "music-play":
                {
                    if (m.notes == null || m.notes.Length < 2)
                    {
                        errors.Add(where + ": music-play needs at least 2 notes");
                        break;
                    }
                    foreach (var n in m.notes)
                        if (!System.Array.Exists(ValidNotes, vn => vn == n))
                            errors.Add(where + ": music-play invalid note '" + n + "'");
                    break;
                }
            }

            // choice-based types must offer the correct answer
            bool numericChoiceBased = c.type == "math-choice" || c.type == "potion" ||
                                      (c.type == "directions" && answerIsNumber) ||
                                      (c.type == "sorting" && answerIsNumber);
            bool choiceBased = numericChoiceBased || c.type == "choice-card";
            if (choiceBased && (c.options == null || c.options.Length == 0))
                errors.Add(where + ": choice-based challenge has no options");
            if (c.options != null && c.options.Length > 0 && !System.Array.Exists(c.options, o => o == c.correctAnswer))
                errors.Add(where + ": correctAnswer not among options");

            // Batch 5 fix (b): options must be unique, and numeric choice-based types
            // must offer 4 options (6 for the harder adaptive variants, which
            // use a wider spread) — all numeric, since the modules parse them.
            if (c.options != null && c.options.Length > 0)
            {
                var seenOpts = new HashSet<string>();
                foreach (var o in c.options)
                    if (!seenOpts.Add(o)) errors.Add(where + ": duplicate option '" + o + "'");
                if (numericChoiceBased)
                {
                    if (c.options.Length != 4 && c.options.Length != 6)
                        errors.Add(where + ": choice-based challenge has " + c.options.Length + " options, expected 4 (or 6 for harder variants)");
                    foreach (var o in c.options)
                    {
                        int dummy;
                        if (!int.TryParse(o, out dummy)) errors.Add(where + ": non-numeric option '" + o + "'");
                    }
                }
            }

            // Batch 5 fix (d): a math prompt must state the numbers the child
            // works with — two-step prompts name the first step's operands and
            // the second step's operand (NOT the intermediate/final results,
            // which would give the answer away); number-line prompts name the
            // start and the step size; plain prompts name both operands.
            // Disappear demos are exempt from naming the first number: the
            // child counts the objects, then the prompt names what flies away.
            // countOnly rounds are exempt entirely (the child counts objects).
            if (c.type == "math-choice")
            {
                if (m.steps != null && m.steps.Length >= 2)
                {
                    if (!PromptHas(c.prompt, m.steps[0].a) || !PromptHas(c.prompt, m.steps[0].b) || !PromptHas(c.prompt, m.steps[1].b))
                        errors.Add(where + ": two-step prompt must contain the step numbers (" + m.steps[0].a + ", " + m.steps[0].b + ", " + m.steps[1].b + ")");
                }
                else if (m.numberline)
                {
                    if (!PromptHas(c.prompt, m.start) || !PromptHas(c.prompt, System.Math.Abs(m.move)))
                        errors.Add(where + ": number-line prompt must contain the start and step size (" + m.start + ", " + System.Math.Abs(m.move) + ")");
                }
                else if (!m.countOnly && m.demo != "disappear")
                {
                    if (!PromptHas(c.prompt, m.a) || !PromptHas(c.prompt, m.b))
                        errors.Add(where + ": prompt must contain the numbers (" + m.a + ", " + m.b + ")");
                }
                else if (!m.countOnly && m.demo == "disappear")
                {
                    if (!PromptHas(c.prompt, m.b))
                        errors.Add(where + ": disappear prompt must contain how many fly away (" + m.b + ")");
                }
            }
        }

        static bool PromptHas(string prompt, int n)
        {
            return !string.IsNullOrEmpty(prompt) && prompt.Contains(n.ToString());
        }

        /// <summary>LXD Phase B: validates one level's ILO block and its skills coverage.</summary>
        static void ValidateIlos(LevelData lv, List<string> errors)
        {
            string where = "level " + lv.id + " (" + lv.name + ")";
            int count = lv.ilos != null ? lv.ilos.Length : 0;
            if (count < 1 || count > 3)
            {
                errors.Add(where + ": must declare 1-3 ilos, found " + count);
                return;
            }
            var covered = new HashSet<string>();
            for (int i = 0; i < lv.ilos.Length; i++)
            {
                var ilo = lv.ilos[i];
                string w = where + " ilo[" + i + "]";
                if (ilo == null) { errors.Add(w + ": missing"); continue; }
                if (string.IsNullOrEmpty(ilo.text))
                    errors.Add(w + ": missing text");
                else if (ilo.text.Length > MaxIloTextLength)
                    errors.Add(w + ": text is " + ilo.text.Length + " chars, max " + MaxIloTextLength + " (few and specific)");
                if (string.IsNullOrEmpty(ilo.kidText))
                    errors.Add(w + ": missing kidText");
                if (!System.Array.Exists(ValidIloVerbs, v => v == ilo.verb))
                    errors.Add(w + ": invalid verb '" + ilo.verb + "'");
                if (!System.Array.Exists(ValidIloAssess, a => a == ilo.assess))
                    errors.Add(w + ": invalid assess '" + ilo.assess + "'");
                if (ilo.skills == null || ilo.skills.Length == 0)
                    errors.Add(w + ": must list the skills it covers");
                else
                    foreach (var s in ilo.skills)
                    {
                        if (!System.Array.Exists(ValidSkills, vs => vs == s))
                            errors.Add(w + ": invalid skill '" + s + "'");
                        else covered.Add(s);
                    }
            }
            // coverage rule: every distinct skill used by the level's core challenges
            // must be covered by at least one of the level's ILOs
            if (lv.challenges != null)
                foreach (var c in lv.challenges)
                    if (!string.IsNullOrEmpty(c.skill) && !covered.Contains(c.skill))
                    {
                        errors.Add(where + ": skill '" + c.skill + "' is not covered by any ilo");
                        covered.Add(c.skill); // report each missing skill once
                    }
        }

        /// <summary>
        /// LXD Phase B: resolves the ILO for a challenge. Core challenges carry their
        /// own index; variants inherit it from the core challenge that links to them
        /// (via easier/harder/band5to7/band11to12). Returns null when unresolvable.
        /// </summary>
        public static IloData ResolveIlo(LevelBank bank, ChallengeData c)
        {
            if (bank == null || bank.levels == null || c == null) return null;
            LevelData lv = null;
            if (c.level >= 1 && c.level <= bank.levels.Length) lv = bank.levels[c.level - 1];
            if (lv == null || lv.id == 0)
            {
                // variant: find the core challenge that links here
                foreach (var l in bank.levels)
                {
                    if (l.challenges == null) continue;
                    foreach (var core in l.challenges)
                    {
                        if (core == null) continue;
                        if (core.easier == c.id || core.harder == c.id ||
                            core.band5to7 == c.id || core.band11to12 == c.id)
                        { lv = l; c = core; break; }
                    }
                    if (lv != null && lv.id >= 1) break;
                }
            }
            if (lv == null || lv.ilos == null || c.ilo < 0 || c.ilo >= lv.ilos.Length) return null;
            return lv.ilos[c.ilo];
        }

        static int Eval(int a, int b, string op) { return op == "+" ? a + b : a - b; }

        static string NumberWord(int n)
        {
            string[] words = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
                               "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen",
                               "seventeen", "eighteen", "nineteen", "twenty" };
            return (n >= 0 && n < words.Length) ? words[n] : n.ToString();
        }
    }
}
