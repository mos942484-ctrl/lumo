#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using TMPro;
using Lumo.Core;
using Lumo.Modules;
using Lumo.UI;

/// <summary>Day-1 verification suite: data validation, rule boundary tests, and asset audits. Run via Lumo/Tests.</summary>
public static class LumoTests
{
    [MenuItem("Lumo/Tests/Run All Day1")]
    public static void RunAll()
    {
        ValidateData();
        StarRules();
        AuditAssets();
        Phase1Checks();
        Phase2Checks();
        Phase3Checks();
        LxdChecks();
    }

    [MenuItem("Lumo/Tests/Validate Data")]
    public static void ValidateData()
    {
        try
        {
            var bank = QuestionManager.LoadAndValidate();
            int core = 0, final = 0;
            int variants = bank.variants != null ? bank.variants.Length : 0;
            var coverage = new Dictionary<string, int>();
            foreach (var lv in bank.levels)
            {
                if (lv.challenges == null) continue;
                foreach (var c in lv.challenges)
                {
                    if (lv.id <= 12) core++; else final++;
                    if (!coverage.ContainsKey(c.skill)) coverage[c.skill] = 0;
                    coverage[c.skill]++;
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine("[LumoTests] levels.json VALID");
            sb.AppendLine("  levels=" + bank.levels.Length + "  core=" + core + "  finalTasks=" + final + "  variants=" + variants);
            foreach (var kv in coverage) sb.AppendLine("  skill '" + kv.Key + "': " + kv.Value + " challenges");
            Debug.Log(sb.ToString());
        }
        catch (System.Exception e)
        {
            Debug.LogError("[LumoTests] ValidateData FAILED: " + e.Message);
        }
    }

    [MenuItem("Lumo/Tests/Star Rules")]
    public static void StarRules()
    {
        // Boundary table from BUILD_PROMPT §11 — must pass exactly (first-try answers).
        float[] times   = { 8f, 10f, 15f, 20f, 24f, 30f, 31f };
        int[]  expected = { 3,   3,   2,   2,   1,   1,   0  };
        int failures = 0;
        for (int i = 0; i < times.Length; i++)
        {
            int got = GameRules.StarsForTime(times[i], true);
            bool ok = got == expected[i];
            if (!ok) failures++;
            Debug.Log("[LumoTests] stars(" + times[i] + "s) = " + got + " expected " + expected[i] + (ok ? "  PASS" : "  FAIL"));
        }

        // LXD Phase C1: timed 3 stars are gated by first-try accuracy — a
        // non-first-try answer caps at 1 star regardless of speed (and a very
        // slow non-first-try answer still earns 0).
        // rows: { seconds, firstTry?1:0, expected stars } — non-first-try caps at 1
        int[,] gateTimes = { { 9, 0, 1 }, { 9, 1, 3 }, { 15, 0, 1 }, { 31, 0, 0 } };
        for (int i = 0; i < gateTimes.GetLength(0); i++)
        {
            int got = GameRules.StarsForTime(gateTimes[i, 0], gateTimes[i, 1] == 1);
            if (got != gateTimes[i, 2])
            {
                failures++;
                Debug.LogError("[LumoTests] StarsForTime gate FAIL: " + gateTimes[i, 0] + "s firstTry=" + (gateTimes[i, 1] == 1) + " -> " + got + " expected " + gateTimes[i, 2]);
            }
        }
        if (GameRules.StarsForTime(9f, true) != 3 || GameRules.StarsForTime(9f, false) != 1)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD C1 gate check failed (9s first-try must be 3, non-first-try must be 1)");
        }
        else
        {
            Debug.Log("[LumoTests] LXD C1 timed-star accuracy gate: PASS (9s first-try=3, 9s non-first-try=1)");
        }

        var fresh = SaveManager.NewSave();
        int l13Slots = fresh.levelRecords[12].stars.Length;
        int maxStars = 0;
        foreach (var r in fresh.levelRecords) maxStars += r.stars.Length * GameRules.MaxStarsPerChallenge;
        int expectedL13 = SaveManager.RoundsForLevel(13);
        int expectedMax = GameRules.DynamicTotalStars();
        if (l13Slots != expectedL13 || maxStars != expectedMax)
        {
            failures++;
            Debug.LogError("[LumoTests] Save star capacity check FAIL: L13 slots=" + l13Slots + " (expected " + expectedL13 + ") maxStars=" + maxStars + " (expected " + expectedMax + ")");
        }
        else
        {
            Debug.Log("[LumoTests] Save star capacity check: L13 slots=" + l13Slots + " maxStars=" + maxStars + "  PASS");
        }

        Debug.Log(failures == 0 ? "[LumoTests] Star rules: ALL PASS" : "[LumoTests] Star rules: " + failures + " FAILURES");
    }

    [MenuItem("Lumo/Tests/Audit Assets")]
    public static void AuditAssets()
    {
        var missing = new List<string>();
        void CheckSprite(string cat, string name, string ctx)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (Resources.Load<Sprite>("Art/" + cat + "/" + name) == null)
                missing.Add(ctx + " -> Art/" + cat + "/" + name);
        }
        void CheckVoice(string name, string ctx)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (Resources.Load<AudioClip>("Audio/Voice/" + name) == null)
                missing.Add(ctx + " -> Audio/Voice/" + name);
        }
        void CheckChallenge(ChallengeData c)
        {
            CheckVoice(c.vo, c.id + " vo");
            CheckVoice("vo_fb_" + c.id + "_ok", c.id + " ok");
            CheckVoice("vo_fb_" + c.id + "_no", c.id + " no");
            if (c.media != null)
            {
                CheckSprite("objects", c.media.obj, c.id + " obj");
                CheckVoice(c.media.voWord, c.id + " voWord");
                CheckVoice(c.media.voSentence, c.id + " voSentence");
                if (c.media.pictures != null)
                    foreach (var p in c.media.pictures) CheckSprite("objects", p.sprite, c.id + " pic " + p.id);
                if (c.media.triples != null)
                    foreach (var t in c.media.triples) CheckSprite("objects", t.picObj, c.id + " triple");
                // Batch 5 fix: potion/sorting/directions media was never audited
                // (their sprites come from fixed names, not media.obj).
                if (c.type == "sorting" && c.media.items != null)
                    foreach (var item in c.media.items)
                        CheckSprite("objects", item.kind == "star" ? "obj_star" : "obj_gem", c.id + " sorting " + item.kind);
                if (c.type == "directions")
                {
                    if (c.media.moves != null && c.media.moves.Length > 0)
                        CheckSprite("objects", "obj_chest", c.id + " treasure chest");
                    if (c.media.collect > 0)
                        CheckSprite("objects", "obj_key", c.id + " keys");
                }
                if (c.type == "potion")
                {
                    CheckSprite("objects", "obj_cauldron", c.id + " cauldron");
                    CheckSprite("objects", "obj_star", c.id + " potion star");
                }
                if (c.type == "memory-match" && c.media.pairs != null)
                {
                    foreach (var p in c.media.pairs)
                    {
                        CheckSprite("objects", p.aSprite, c.id + " pair " + p.id + " aSprite");
                        CheckSprite("objects", p.bSprite, c.id + " pair " + p.id + " bSprite");
                    }
                }
            }
            if (c.words != null)
                foreach (var w in c.words) CheckVoice("vo_word_" + w, c.id + " word " + w);
        }

        var bank = QuestionManager.LoadAndValidate();
        foreach (var lv in bank.levels)
        {
            CheckSprite("backgrounds", lv.bg, "L" + lv.id + " bg");
            if (lv.intro != null) CheckVoice(lv.intro.vo, "L" + lv.id + " intro");
            if (lv.challenges != null)
                foreach (var c in lv.challenges) CheckChallenge(c);
        }
        if (bank.variants != null)
            foreach (var v in bank.variants) CheckChallenge(v);

        CheckSprite("backgrounds", "bg_kingdom", "UI bg_kingdom");
        CheckSprite("backgrounds", "bg_menu", "UI bg_menu");
        CheckSprite("backgrounds", "bg_final", "UI bg_final");
        CheckVoice("vo_story_intro", "Tutorial story intro");

        string[] chars = { "char_hero_a", "char_hero_b", "char_lumo", "char_king", "char_lexi", "char_spark", "char_shadow" };
        foreach (var ch in chars) CheckSprite("characters", ch, "Character");

        string[] sfx = { "sfx_click", "sfx_star", "sfx_collect", "sfx_drop", "sfx_correct", "sfx_wrong", "sfx_complete", "sfx_timer" };
        foreach (var s in sfx)
            if (Resources.Load<AudioClip>("Audio/SFX/" + s) == null) missing.Add("SFX -> Audio/SFX/" + s);

        string[] music = { "music_menu", "music_game", "music_victory" };
        foreach (var m in music)
            if (Resources.Load<AudioClip>("Audio/Music/" + m) == null) missing.Add("Music -> Audio/Music/" + m);

        string[] fonts = { "font_main" };
        foreach (var f in fonts)
            if (Resources.Load<TMP_FontAsset>("Fonts/" + f) == null) missing.Add("Font -> Fonts/" + f);

        if (missing.Count == 0)
            Debug.Log("[LumoTests] AuditAssets PASS (0 missing assets)");
        else
            Debug.LogError("[LumoTests] AuditAssets FAIL (" + missing.Count + " missing):\n  - " + string.Join("\n  - ", missing));
    }

    [MenuItem("Lumo/Tests/Phase 1 Checks")]
    public static void Phase1Checks()
    {
        int failures = 0;

        // 1. Calm star rules
        int[] calmMistakes = { 0, 0, 1, 2, 3 };
        bool[] calmHelp    = { false, true, false, false, true };
        int[] calmExpected = { 3, 2, 2, 1, 1 };
        for (int i = 0; i < calmMistakes.Length; i++)
        {
            int got = GameRules.StarsForCalmAnswer(calmMistakes[i], calmHelp[i]);
            if (got != calmExpected[i])
            {
                failures++;
                Debug.LogError("[LumoTests] StarsForCalmAnswer(" + calmMistakes[i] + ", " + calmHelp[i] + ") = " + got + " expected " + calmExpected[i]);
            }
        }

        // 2. IslandCatalog integrity
        if (IslandCatalog.All == null || IslandCatalog.All.Length != 13)
        {
            failures++;
            Debug.LogError("[LumoTests] IslandCatalog.All length != 13");
        }
        else
        {
            var seenNames = new HashSet<string>();
            for (int i = 0; i < IslandCatalog.All.Length; i++)
            {
                var info = IslandCatalog.All[i];
                if (info.LevelId != i + 1 || string.IsNullOrEmpty(info.IslandName) || !seenNames.Add(info.IslandName))
                {
                    failures++;
                    Debug.LogError("[LumoTests] IslandCatalog invalid entry at index " + i + ": " + info.IslandName);
                }
            }
        }

        // 3. SaveManager.Normalize clamping, PIN checks, islandSeconds, and v2 -> v3 upgrade
        var bad = SaveManager.NewSave();
        bad.ageBand = -5;
        bad.dailyLimitMinutes = -10;
        bad.parentPin = "12ab";
        bad.islandSeconds = new[] { -4, 10 };
        var norm = SaveManager.Normalize(bad);
        if (norm.ageBand != 0 || norm.dailyLimitMinutes != 0 || norm.parentPin != "" ||
            norm.islandSeconds == null || norm.islandSeconds.Length != 13 || norm.islandSeconds[0] != 0 || norm.islandSeconds[1] != 10)
        {
            failures++;
            Debug.LogError("[LumoTests] Normalize clamp/PIN/islandSeconds check failed");
        }

        var high = SaveManager.NewSave();
        high.ageBand = 99;
        high.dailyLimitMinutes = 999;
        high.parentPin = "1234";
        norm = SaveManager.Normalize(high);
        if (norm.ageBand != 2 || norm.dailyLimitMinutes != 240 || norm.parentPin != "1234")
        {
            failures++;
            Debug.LogError("[LumoTests] Normalize upper-clamp / valid PIN check failed");
        }

        var v2 = new SaveData
        {
            version = 2,
            ageBand = GameRules.AgeBand5To7,
            timedMode = true,
            calmMode = true
        };
        var upgraded = SaveManager.Normalize(v2);
        if (upgraded.version != SaveData.CurrentVersion || upgraded.ageBand != GameRules.AgeBand8To10 || upgraded.timedMode || upgraded.calmMode)
        {
            failures++;
            Debug.LogError("[LumoTests] v2 -> current upgrade check failed (version=" + upgraded.version +
                           " ageBand=" + upgraded.ageBand + ")");
        }

        if (failures == 0)
            Debug.Log("[LumoTests] Phase 1 checks: ALL PASS");
        else
            Debug.LogError("[LumoTests] Phase 1 checks: " + failures + " FAILURES");
    }

    [MenuItem("Lumo/Tests/Phase 2 Checks")]
    public static void Phase2Checks()
    {
        int failures = 0;
        var bank = QuestionManager.LoadAndValidate();

        // 1. 13 skills
        if (bank.skills == null || bank.skills.Length != 13)
        {
            failures++;
            Debug.LogError("[LumoTests] Phase 2: expected 13 skills, found " + (bank.skills != null ? bank.skills.Length : 0));
        }

        // 2. 12 module types, 87 core challenges, 110 variants, no Arabic text
        var seenTypes = new HashSet<string>();
        int coreCount = 0;
        foreach (var lv in bank.levels)
        {
            if (lv.challenges == null) continue;
            foreach (var c in lv.challenges)
            {
                coreCount++;
                seenTypes.Add(c.type);
                if (c.type == "choice-card")
                {
                    if (c.options == null || c.options.Length < 2 || c.options.Length > 4 || !System.Array.Exists(c.options, o => o == c.correctAnswer))
                    {
                        failures++;
                        Debug.LogError("[LumoTests] Phase 2: choice-card invalid options/answer on " + c.id);
                    }
                }
                else if (c.type == "memory-match")
                {
                    if (c.media == null || c.media.pairs == null || c.media.pairs.Length < 2)
                    {
                        failures++;
                        Debug.LogError("[LumoTests] Phase 2: memory-match missing pairs on " + c.id);
                    }
                }
                else if (c.type == "pixel-art")
                {
                    if (c.media == null || c.media.palette == null || c.media.palette.Length == 0 ||
                        c.media.targetGrid == null || c.media.targetGrid.Length != c.media.gw * c.media.gh)
                    {
                        failures++;
                        Debug.LogError("[LumoTests] Phase 2: pixel-art invalid grid/palette on " + c.id);
                    }
                }
                else if (c.type == "music-play")
                {
                    if (c.media == null || c.media.notes == null || c.media.notes.Length < 2)
                    {
                        failures++;
                        Debug.LogError("[LumoTests] Phase 2: music-play invalid notes on " + c.id);
                    }
                }
            }
        }
        foreach (var vt in QuestionManager.ValidTypes)
        {
            if (!seenTypes.Contains(vt))
            {
                failures++;
                Debug.LogError("[LumoTests] Phase 2: module type not present in levels: " + vt);
            }
        }
        int variantCount = bank.variants != null ? bank.variants.Length : 0;
        if (coreCount != 87 || variantCount != 110)
        {
            failures++;
            Debug.LogError("[LumoTests] Phase 2: expected 87 core / 110 variants, found " + coreCount + " / " + variantCount);
        }

        // 3. Zero Arabic characters in levels.json
        var rawBank = Resources.Load<TextAsset>("Data/levels");
        if (rawBank != null)
        {
            foreach (char ch in rawBank.text)
            {
                if (ch >= '\u0600' && ch <= '\u06FF')
                {
                    failures++;
                    Debug.LogError("[LumoTests] Phase 2: Arabic Unicode character found in levels.json");
                    break;
                }
            }
        }

        // 4. band5to7 / band11to12 variant resolution (and AdaptiveManager if active)
        ChallengeData p2Sample = null;
        foreach (var lv in bank.levels)
        {
            if (lv.challenges == null) continue;
            foreach (var c in lv.challenges)
                if (c.id == "P2_L3R4") { p2Sample = c; break; }
            if (p2Sample != null) break;
        }
        var v57 = p2Sample != null ? QuestionManager.GetVariant(bank, p2Sample.band5to7) : null;
        var v1112 = p2Sample != null ? QuestionManager.GetVariant(bank, p2Sample.band11to12) : null;
        if (p2Sample == null || v57 == null || v1112 == null || v57.difficulty != "easier" || v1112.difficulty != "harder")
        {
            failures++;
            Debug.LogError("[LumoTests] Phase 2: band5to7/band11to12 variant lookup failed");
        }
        if (AdaptiveManager.Instance != null && SaveManager.Instance != null && SaveManager.Instance.Data != null && p2Sample != null)
        {
            int prevBand = SaveManager.Instance.Data.ageBand;
            int idx = AdaptiveManager.SkillIndex(bank, p2Sample.skill);
            var st = idx >= 0 ? SaveManager.Instance.Skill(idx) : null;
            bool pRem = st != null && st.remediation;
            bool pHard = st != null && st.pendingHarder;
            if (st != null) { st.remediation = false; st.pendingHarder = false; }
            SaveManager.Instance.Data.ageBand = GameRules.AgeBand5To7;
            var sel57 = AdaptiveManager.Instance.SelectChallenge(bank, p2Sample);
            SaveManager.Instance.Data.ageBand = GameRules.AgeBand11To12;
            var sel1112 = AdaptiveManager.Instance.SelectChallenge(bank, p2Sample);
            SaveManager.Instance.Data.ageBand = prevBand;
            if (st != null) { st.remediation = pRem; st.pendingHarder = pHard; }
            if (sel57 == null || sel57.id != p2Sample.band5to7 || sel1112 == null || sel1112.id != p2Sample.band11to12)
            {
                failures++;
                Debug.LogError("[LumoTests] Phase 2: AdaptiveManager age-band routing mismatch");
            }
        }

        // 5. Save v3 -> v4 migration
        var v3 = new SaveData
        {
            version = 3,
            skills = new SkillStats[8],
            levelRecords = new LevelRecord[13]
        };
        v3.skills[0] = new SkillStats { attempts = 5, firstTryCorrect = 4, mastery = 1 };
        v3.levelRecords[0] = new LevelRecord { stars = new[] { 3, 2, 1 } };
        var v4 = SaveManager.Normalize(v3);
        if (v4.version != 4 || v4.skills == null || v4.skills.Length != 13 ||
            v4.skills[0].attempts != 5 || v4.skills[12] == null ||
            v4.levelRecords[0].stars.Length != SaveManager.RoundsForLevel(1) ||
            v4.levelRecords[0].stars[0] != 3 ||
            v4.levelRecords[12].stars.Length != SaveManager.RoundsForLevel(13))
        {
            failures++;
            Debug.LogError("[LumoTests] Phase 2: v3 -> v4 migration failed");
        }

        if (failures == 0)
            Debug.Log("[LumoTests] Phase 2 checks: ALL PASS");
        else
            Debug.LogError("[LumoTests] Phase 2 checks: " + failures + " FAILURES");
    }

    [MenuItem("Lumo/Tests/Phase 3 Checks")]
    public static void Phase3Checks()
    {
        int failures = 0;
        var bank = QuestionManager.LoadAndValidate();
        string[] forbiddenWords = { "WRONG", "FAIL", "LOSE", "GAME OVER" };

        void CheckChallenge(ChallengeData c)
        {
            if (c == null) return;
            if (c.feedback != null && !string.IsNullOrEmpty(c.feedback.retry))
            {
                string upper = c.feedback.retry.ToUpperInvariant();
                for (int i = 0; i < forbiddenWords.Length; i++)
                {
                    if (upper.Contains(forbiddenWords[i]))
                    {
                        failures++;
                        Debug.LogError("[LumoTests] Phase 3: discouraged word '" + forbiddenWords[i] + "' in retry feedback for " + c.id);
                    }
                }
            }
            if (c.type == "pixel-art" && c.media != null && c.media.palette != null)
            {
                for (int i = 0; i < c.media.palette.Length; i++)
                {
                    string hex = c.media.palette[i];
                    string name = PixelArtModule.ColorNameForHex(hex);
                    if (string.IsNullOrEmpty(name) || name.StartsWith("Color "))
                    {
                        failures++;
                        Debug.LogError("[LumoTests] Phase 3: ColorNameForHex('" + hex + "') returned '" + name + "' on " + c.id);
                    }
                }
            }
        }

        if (bank.levels != null)
            foreach (var lv in bank.levels)
                if (lv.challenges != null)
                    foreach (var c in lv.challenges)
                        CheckChallenge(c);
        if (bank.variants != null)
            foreach (var v in bank.variants)
                CheckChallenge(v);

        // Empty save and populated save checks for ParentsPanel island helpers
        var validNames = new HashSet<string>();
        for (int i = 0; i < IslandCatalog.All.Length; i++)
            validNames.Add(IslandCatalog.All[i].IslandName);

        var emptySave = SaveManager.NewSave();
        string emptyFav = ParentsPanel.GetMostExploredIsland(emptySave);
        string emptyNext = ParentsPanel.GetSuggestedNextIsland(emptySave);
        if (string.IsNullOrEmpty(emptyFav) || !validNames.Contains(emptyFav) ||
            string.IsNullOrEmpty(emptyNext) || !validNames.Contains(emptyNext))
        {
            failures++;
            Debug.LogError("[LumoTests] Phase 3: empty save island helper invalid (fav='" + emptyFav + "', next='" + emptyNext + "')");
        }

        var popSave = SaveManager.NewSave();
        popSave.islandSeconds[4] = 180; // Art Studio (level 5)
        popSave.levelRecords[0].completed = true;
        popSave.levelRecords[1].completed = true;
        popSave.highestUnlocked = 3;    // story path: finishing level 2 unlocks level 3
        string popFav = ParentsPanel.GetMostExploredIsland(popSave);
        string popNext = ParentsPanel.GetSuggestedNextIsland(popSave);
        if (string.IsNullOrEmpty(popFav) || !validNames.Contains(popFav) || popFav != IslandCatalog.Get(5).IslandName ||
            string.IsNullOrEmpty(popNext) || !validNames.Contains(popNext) || popNext != IslandCatalog.Get(3).IslandName)
        {
            failures++;
            Debug.LogError("[LumoTests] Phase 3: populated save island helper mismatch (fav='" + popFav + "', next='" + popNext + "')");
        }

        // Batch 7 Crystal Storybook checks: Docs/STYLE_GUIDE.md + unified texture dimensions
        string repoRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
        string styleGuidePath = System.IO.Path.Combine(repoRoot, "Docs", "STYLE_GUIDE.md");
        if (!System.IO.File.Exists(styleGuidePath))
        {
            failures++;
            Debug.LogError("[LumoTests] Phase 3: Docs/STYLE_GUIDE.md missing");
        }
        var sampleBg = Resources.Load<Sprite>("Art/Backgrounds/bg_menu");
        var sampleChar = Resources.Load<Sprite>("Art/Characters/char_lumo");
        var sampleObj = Resources.Load<Sprite>("Art/Objects/obj_crystal");
        var sampleIcon = Resources.Load<Sprite>("Art/UI/ui_icon_play");
        var sampleBadge = Resources.Load<Sprite>("Art/UI/ui_badge_hero");
        if (sampleBg == null || sampleBg.texture.width != 1920 || sampleBg.texture.height != 1080 ||
            sampleChar == null || sampleChar.texture.width != 600 || sampleChar.texture.height != 600 ||
            sampleObj == null || sampleObj.texture.width != 256 || sampleObj.texture.height != 256 ||
            sampleIcon == null || sampleIcon.texture.width != 128 || sampleIcon.texture.height != 128 ||
            sampleBadge == null || sampleBadge.texture.width != 128 || sampleBadge.texture.height != 128)
        {
            failures++;
            Debug.LogError("[LumoTests] Phase 3: Batch 7 unified texture dimensions check failed");
        }

        if (failures == 0)
            Debug.Log("[LumoTests] Phase 3 checks: ALL PASS");
        else
            Debug.LogError("[LumoTests] Phase 3 checks: " + failures + " FAILURES");
    }

    [MenuItem("Lumo/Tests/LXD Checks (Phase B)")]
    public static void LxdChecks()
    {
        int failures = 0;
        var bank = QuestionManager.LoadAndValidate(); // validator itself enforces ILO rules — throws on breach

        // 1. 13 levels, 1-3 valid ILOs each; explicit field checks (belt + braces on top of the validator)
        if (bank.levels == null || bank.levels.Length != 13)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD: expected 13 levels");
        }
        int iloTotal = 0;
        if (bank.levels != null)
        {
            foreach (var lv in bank.levels)
            {
                if (lv.ilos == null || lv.ilos.Length < 1 || lv.ilos.Length > 3)
                {
                    failures++;
                    Debug.LogError("[LumoTests] LXD: level " + lv.id + " must have 1-3 ilos, found " + (lv.ilos != null ? lv.ilos.Length : 0));
                    continue;
                }
                iloTotal += lv.ilos.Length;
                foreach (var ilo in lv.ilos)
                {
                    if (ilo == null || string.IsNullOrEmpty(ilo.text) || ilo.text.Length > QuestionManager.MaxIloTextLength ||
                        string.IsNullOrEmpty(ilo.kidText) ||
                        !System.Array.Exists(QuestionManager.ValidIloVerbs, v => v == ilo.verb) ||
                        !System.Array.Exists(QuestionManager.ValidIloAssess, a => a == ilo.assess))
                    {
                        failures++;
                        Debug.LogError("[LumoTests] LXD: invalid ILO in level " + lv.id);
                    }
                }
            }
        }
        if (iloTotal < 13 || iloTotal > 39)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD: expected 13-39 ILOs total (1-3 per level), found " + iloTotal);
        }

        // 2. Every core challenge has a valid ilo index; variants carry none
        int indexed = 0;
        foreach (var lv in bank.levels)
        {
            if (lv.challenges == null || lv.ilos == null) continue;
            foreach (var c in lv.challenges)
            {
                if (c.ilo < 0 || c.ilo >= lv.ilos.Length)
                {
                    failures++;
                    Debug.LogError("[LumoTests] LXD: challenge " + c.id + " ilo index out of range");
                }
                else indexed++;
            }
        }
        if (indexed != 87)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD: expected 87 core challenges with valid ilo indices, found " + indexed);
        }
        foreach (var v in bank.variants)
        {
            var ilo = QuestionManager.ResolveIlo(bank, v);
            if (ilo == null)
            {
                failures++;
                Debug.LogError("[LumoTests] LXD: variant " + v.id + " does not resolve to a parent ILO");
            }
        }

        // 3. Core ILO resolution + parent grouping helper
        var l1 = bank.levels[0];
        var core1 = l1.challenges[0];
        if (QuestionManager.ResolveIlo(bank, core1) != l1.ilos[core1.ilo])
        {
            failures++;
            Debug.LogError("[LumoTests] LXD: ResolveIlo mismatch for core challenge " + core1.id);
        }
        foreach (var skill in bank.skills)
            if (string.IsNullOrEmpty(ParentsPanel.GetOwningIloKidText(bank, skill)))
            {
                failures++;
                Debug.LogError("[LumoTests] LXD: skill '" + skill + "' has no owning ILO kidText (Parents grouping would be empty)");
            }

        // 4. LXD Phase C4: no bare retry lines - every retry (core + variants) names a strategy
        string[] bareRetries =
        {
            "almost! let's try again.",
            "try again!",
            "good try! let's try again!",
            "almost! let's put the words in order again.",
            "keep going! connect all three cards!",
            "let's check the boxes again!"
        };
        void CheckRetry(ChallengeData c)
        {
            if (c == null || c.feedback == null || string.IsNullOrEmpty(c.feedback.retry)) return;
            string lower = c.feedback.retry.Trim().ToLowerInvariant();
            foreach (var bare in bareRetries)
            {
                if (lower == bare)
                {
                    failures++;
                    Debug.LogError("[LumoTests] LXD C4: bare retry line (no strategy) on " + c.id + ": '" + c.feedback.retry + "'");
                }
            }
        }
        foreach (var lv in bank.levels)
            if (lv.challenges != null)
                foreach (var c in lv.challenges) CheckRetry(c);
        foreach (var v in bank.variants) CheckRetry(v);

        // 5. LXD Phase C4: feedback.explain (when present) must be non-empty and <= 160 chars
        void CheckExplain(ChallengeData c)
        {
            if (c == null || c.feedback == null || c.feedback.explain == null) return;
            if (string.IsNullOrWhiteSpace(c.feedback.explain) || c.feedback.explain.Length > 160)
            {
                failures++;
                Debug.LogError("[LumoTests] LXD C4: invalid feedback.explain on " + c.id);
            }
        }
        foreach (var lv in bank.levels)
            if (lv.challenges != null)
                foreach (var c in lv.challenges) CheckExplain(c);
        foreach (var v in bank.variants) CheckExplain(v);

        // 6. LXD Phase C3: medals normalize (dedupe + survive a save round-trip shape)
        var medalSave = SaveManager.NewSave();
        medalSave.medals = new[] { "addition", "addition", "music", "" };
        var medalNorm = SaveManager.Normalize(medalSave);
        if (medalNorm.medals == null || medalNorm.medals.Length != 2 || medalNorm.medals[0] != "addition" || medalNorm.medals[1] != "music")
        {
            failures++;
            Debug.LogError("[LumoTests] LXD C3: medals dedupe/normalize failed");
        }

        // 7. LXD Phase C1+C3 rule wiring (pure-rule checks need no live managers)
        if (GameRules.StarsForTime(20f, false) != 1)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD: non-first-try speed stars must cap at 1");
        }

        // 8. LXD Phase D3: level reflection normalize (parallel arrays, latest entry last, clamped)
        var feelSave = SaveManager.NewSave();
        SaveManager.ApplyLevelFeel(feelSave, 3, 0);
        SaveManager.ApplyLevelFeel(feelSave, 3, 1);      // latest wins
        SaveManager.ApplyLevelFeel(feelSave, 7, 0);      // applied last -> last array entry
        SaveManager.ApplyLevelFeel(feelSave, 99, 0);     // dropped (id out of range)
        if (feelSave.levelFeelIds.Length != 2 || feelSave.levelFeelIds[0] != 3 ||
            feelSave.levelFeelVals[0] != 1 || feelSave.levelFeelIds[1] != 7 || feelSave.levelFeelVals[1] != 0)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD D3: level feels normalize failed (ids=" +
                string.Join(",", System.Array.ConvertAll(feelSave.levelFeelIds, x => x.ToString())) + ")");
        }
        int feelLevelOut;
        string feelLabel = SaveManager.LastLevelFeel(feelSave, out feelLevelOut);
        if (feelLabel != "So fun!" || feelLevelOut != 7)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD D3: LastLevelFeel mismatch (got " + feelLabel + " @ " + feelLevelOut + ")");
        }

        // 9. LXD Phase D5: focus words preferred over random review words (pure pick logic)
        var order = new List<string> { "cat", "dog", "fish" };
        var focus = new List<string> { "fish" };
        var picked = AdaptiveManager.PickReviewWord(order, focus, null);
        if (picked != "fish")
        {
            failures++;
            Debug.LogError("[LumoTests] LXD D5: focus word not preferred (got '" + picked + "')");
        }
        var picked2 = AdaptiveManager.PickReviewWord(order, new List<string> { "unicorn" }, null);
        if (picked2 != "cat")
        {
            failures++;
            Debug.LogError("[LumoTests] LXD D5: fallback review word mismatch (got '" + picked2 + "')");
        }
        var picked3 = AdaptiveManager.PickReviewWord(order, focus, w => w != "fish");
        if (picked3 != "cat")
        {
            failures++;
            Debug.LogError("[LumoTests] LXD D5: eligibility filter bypassed focus word (got '" + picked3 + "')");
        }
        // 2 clean solves in 2 different levels = mastered; same level twice or 1 level = not
        if (AdaptiveManager.IsWordMastered(new[] { "fish|1", "fish|1" }, "fish") ||
            !AdaptiveManager.IsWordMastered(new[] { "fish|1", "fish|2", "fish|5" }, "fish") ||
            AdaptiveManager.IsWordMastered(new[] { "fish|1" }, "fish"))
        {
            failures++;
            Debug.LogError("[LumoTests] LXD D5: IsWordMastered 2-level rule failed");
        }

        // 10. LXD Phase D2: a scaffolding prompt exists for every tracked skill
        foreach (var skill in bank.skills)
        {
            if (string.IsNullOrEmpty(ParentTips.While(skill)) || string.IsNullOrEmpty(ParentTips.After(skill)))
            {
                failures++;
                Debug.LogError("[LumoTests] LXD D2: missing while/after prompt for skill '" + skill + "'");
            }
        }

        // 11. LXD Phase D1: session recap text builds (kid language, islands + words)
        var trackerGo = new GameObject("LxdTestSessionTracker");
        var tracker = trackerGo.AddComponent<Lumo.Core.SessionTracker>();
        tracker.RecordLevelStarted(1);
        tracker.RecordLevelStarted(2);
        tracker.RecordLevelCompleted(2);
        tracker.RecordWordClean("cat");
        string recap = tracker.BuildRecapText(bank, SaveManager.NewSave());
        if (string.IsNullOrEmpty(recap) || !recap.Contains(IslandCatalog.Get(1).IslandName) ||
            !recap.Contains(IslandCatalog.Get(2).IslandName) || !recap.Contains("cat"))
        {
            failures++;
            Debug.LogError("[LumoTests] LXD D1: session recap text missing islands/words");
        }
        UnityEngine.Object.DestroyImmediate(trackerGo);

        // 12. LXD Phase E1/E2: PlayLog ring buffer (500 cap) + CSV report shape
        PlayLog.Clear();
        PlayLog.Record(1, "L1R1", "addition", 1, 0, 8.2f, true);
        PlayLog.Record(1, "L1R2", "addition", 3, 2, 21.0f, false);
        PlayLog.Record(2, "L2R1", "vocabulary", 1, 0, 5.0f, true);
        float median = PlayLog.MedianSeconds(PlayLog.Snapshot().FindAll(r => r.skill == "addition"));
        if (System.Math.Abs(median - 14.6f) > 0.05f)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD E1: median seconds-to-correct wrong (" + median + ")");
        }
        for (int i = 0; i < 560; i++) PlayLog.Record(1, "X" + i, "addition", 1, 0, 1f, true);
        if (PlayLog.Count != PlayLog.Cap)
        {
            failures++;
            Debug.LogError("[LumoTests] LXD E1: ring buffer cap failed (" + PlayLog.Count + ")");
        }
        PlayLog.Record(2, "L2R1", "vocabulary", 2, 1, 7.5f, false); // re-add a retry row (buffer evicted the early ones)
        var csv = SessionReport.BuildCsv(PlayLog.Snapshot(), bank.skills);
        if (!csv.StartsWith("level,challenge,skill,attempts,hints,seconds_to_correct,first_try,timestamp") ||
            !csv.Contains("skill,attempts,first_try_pct,hints,median_seconds") ||
            !csv.Contains("true") || !csv.Contains("false"))
        {
            failures++;
            Debug.LogError("[LumoTests] LXD E2: session CSV shape wrong");
        }
        PlayLog.Clear(); // leave a clean log for the session

        // 13. LXD Phase F: struggle-choice offer rule truth table
        int[,] fCases =
        {
            // preStreak, inReview, practice, alreadyOffered, hasEasier, expected
            { 1, 0, 0, 0, 1, 1 },   // 2nd consecutive struggle -> offer
            { 0, 0, 0, 0, 1, 0 },   // 1st mistake -> no offer
            { 2, 0, 0, 0, 1, 0 },   // remediation already started -> no offer
            { 1, 1, 0, 0, 1, 0 },   // review round -> never
            { 1, 0, 1, 0, 1, 0 },   // practice round -> never
            { 1, 0, 0, 1, 1, 0 },   // already offered this level -> exactly once
            { 1, 0, 0, 0, 0, 0 }    // no easier variant -> no offer
        };
        for (int i = 0; i < fCases.GetLength(0); i++)
        {
            bool got = GameManager.ShouldOfferStruggleChoice(fCases[i, 0], fCases[i, 1] == 1,
                fCases[i, 2] == 1, fCases[i, 3] == 1, fCases[i, 4] == 1);
            if (got != (fCases[i, 5] == 1))
            {
                failures++;
                Debug.LogError("[LumoTests] LXD F: struggle-offer rule case " + i + " got " + got);
            }
        }

        if (failures == 0)
            Debug.Log("[LumoTests] LXD checks: ALL PASS (" + iloTotal + " ILOs across 13 levels)");
        else
            Debug.LogError("[LumoTests] LXD checks: " + failures + " FAILURES");
    }
}
#endif
