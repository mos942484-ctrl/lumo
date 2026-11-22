using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>Level Completed screen: stars, points, progress, child reflection (§7, LXD D3).</summary>
    public class LevelResultScreen : ScreenBase
    {
        public override string Name { get { return "LevelResult"; } }

        GameObject _reflectionRow;
        TMP_Text _feelStatus;
        float _feelTimer; // auto-skip after 8s of no interaction (LXD D3)

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_kingdom"));
            if (bg.sprite == null) bg.color = UIKit.Teal;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.14f, 0.24f, 0.34f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            var plaque = UIKit.Card(Panel, "TitlePlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(1180, 98));

            var title = UIKit.Label(Panel, "Title", "Level Complete!", 72, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(1120, 92));
            UIKit.StyleTitle(title);

            var halo = UIKit.Img(Panel, "ShardHalo", GameAssets.Ui("ui_node"), UIKit.SoftGold);
            halo.raycastTarget = false;
            UIKit.Place(halo.rectTransform, new Vector2(0.5f, 0.785f), Vector2.zero, new Vector2(128, 128));

            var starLeft = UIKit.Img(Panel, "StarLeft", GameAssets.Ui("ui_star"), UIKit.Gold);
            starLeft.raycastTarget = false;
            UIKit.Place(starLeft.rectTransform, new Vector2(0.5f, 0.785f), new Vector2(-118, 0), new Vector2(54, 54));
            var starRight = UIKit.Img(Panel, "StarRight", GameAssets.Ui("ui_star"), UIKit.Gold);
            starRight.raycastTarget = false;
            UIKit.Place(starRight.rectTransform, new Vector2(0.5f, 0.785f), new Vector2(118, 0), new Vector2(54, 54));

            var shard = UIKit.Img(Panel, "Shard", GameAssets.Obj("obj_shard"), Color.white);
            UIKit.Place(shard.rectTransform, new Vector2(0.5f, 0.785f), Vector2.zero, new Vector2(98, 98));

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1020, 310));
            var body = UIKit.Label(card.gameObject, "Body", "", 36, UIKit.Ink, TextAlignmentOptions.Center);
            body.enableAutoSizing = true;
            body.fontSizeMin = 24;
            body.fontSizeMax = 36;
            UIKit.Stretch(body.rectTransform, 56, 30);

            // LXD Phase D3: child reflection - optional tap, auto-skips after 8s,
            // inert in practice mode. Stored per level; surfaces in the Parents Area.
            _reflectionRow = UIKit.Node(Panel, "ReflectionRow");
            UIKit.Stretch(_reflectionRow.GetComponent<RectTransform>());
            var feelLabel = UIKit.Label(_reflectionRow, "FeelAsk", "What did you like most?", 30, UIKit.Cream, TextAlignmentOptions.Center);
            UIKit.Place(feelLabel.rectTransform, new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(900, 40));
            UIKit.StyleTitle(feelLabel);
            var fun = UIKit.TextButton(_reflectionRow, "Fun", "So fun!", new Vector2(330, 72), UIKit.Green, () =>
            {
                SaveFeel(0);
            }, GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(fun.GetComponent<RectTransform>(), new Vector2(0.36f, 0.24f), Vector2.zero, new Vector2(330, 72));
            var tricky = UIKit.TextButton(_reflectionRow, "Tricky", "Tricky but I did it!", new Vector2(430, 72), UIKit.Purple, () =>
            {
                SaveFeel(1);
            }, GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(tricky.GetComponent<RectTransform>(), new Vector2(0.64f, 0.24f), Vector2.zero, new Vector2(430, 72));
            _feelStatus = UIKit.Label(_reflectionRow, "FeelStatus", "", 24, UIKit.SoftGold, TextAlignmentOptions.Center);
            UIKit.Place(_feelStatus.rectTransform, new Vector2(0.5f, 0.175f), Vector2.zero, new Vector2(900, 36));

            var next = UIKit.TextButton(Panel, "Next", "Continue", new Vector2(420, 88), UIKit.Orange, () =>
            {
                M.Show("WorldMap");
            }, GameAssets.Ui("ui_btn_gold"), 42);
            UIKit.Place(next.GetComponent<RectTransform>(), new Vector2(0.5f, 0.095f), new Vector2(-260, 0), new Vector2(420, 88));
            UIKit.AttachButtonIcon(next, GameAssets.Ui("ui_icon_next"), 36f);

            var replay = UIKit.TextButton(Panel, "Replay", "Play Again", new Vector2(420, 88), UIKit.Teal, () =>
            {
                GameManager.Instance.SelectedLevelId = GameManager.Instance.CurrentLevelId;
                GameManager.Instance.AbortLevel();
                M.Show("Gameplay");
            }, GameAssets.Ui("ui_btn"), 42);
            UIKit.Place(replay.GetComponent<RectTransform>(), new Vector2(0.5f, 0.095f), new Vector2(260, 0), new Vector2(420, 88));
            UIKit.AttachButtonIcon(replay, GameAssets.Ui("ui_icon_repeat"), 36f);
        }

        public override void OnShow()
        {
            base.OnShow();
            AudioManager.Instance.PlayComplete();
            AudioManager.Instance.PlayCollect();
            var gm = GameManager.Instance;
            var outcome = gm.LastOutcome;
            var rec = SaveManager.Instance.Record(gm.CurrentLevelId);
            int total = SaveManager.SumStars(rec);
            int max = SaveManager.RoundsForLevel(gm.CurrentLevelId) * GameRules.MaxStarsPerChallenge;

            var title = Panel.transform.Find("Title").GetComponent<TMP_Text>();
            title.text = IslandCatalog.Get(gm.CurrentLevelId).IslandName + " Complete!";

            var body = Panel.transform.Find("Card/Body").GetComponent<TMP_Text>();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Stars earned: " + outcome.StarsThisRun + "   (level best: " + total + "/" + max + ")");
            sb.AppendLine("XP: +" + outcome.XpEarned + "   (total: " + SaveManager.Instance.Data.totalXp + ")");
            if (outcome.PerfectNoHint)
                sb.AppendLine(outcome.XpEarned > 0 ? "No hints! +10 bonus XP!" : "No hints! Great job!");
            foreach (var b in outcome.NewBadges) sb.AppendLine("New badge: " + b + "!");
            if (gm.CurrentLevelId <= 12) sb.AppendLine("You earned a Crystal Shard!");
            body.text = sb.ToString();

            // LXD Phase D3: reflection is a real-round moment only (practice is free play)
            if (_reflectionRow != null)
            {
                bool show = !gm.PracticeMode;
                _reflectionRow.SetActive(show);
                if (show)
                {
                    if (_feelStatus != null) _feelStatus.text = "";
                    _feelTimer = 8f;
                }
            }

            M.StartCoroutine(ConfettiEffect.Play(Panel, gm.IsCalmMode));
        }

        void SaveFeel(int feel)
        {
            var d = SaveManager.Instance.Data;
            SaveManager.ApplyLevelFeel(d, GameManager.Instance.CurrentLevelId, feel);
            SaveManager.Instance.Save();
            if (_reflectionRow != null) _reflectionRow.SetActive(false);
            _feelTimer = 0f;
        }

        public override void Tick(float dt)
        {
            // LXD Phase D3: optional tap - auto-skip after 8s of no interaction
            if (_feelTimer > 0f)
            {
                _feelTimer -= dt;
                if (_feelTimer <= 0f && _reflectionRow != null) _reflectionRow.SetActive(false);
            }
        }

        public override void OnHide()
        {
            if (Panel != null)
            {
                var burst = Panel.transform.Find("ConfettiBurst");
                if (burst != null) Object.Destroy(burst.gameObject);
            }
            base.OnHide();
        }
    }

    /// <summary>My Progress / Progress Report (§7, §8 data) — expanded on Day 7.</summary>
    public class ProgressReportScreen : ScreenBase
    {
        public override string Name { get { return "ProgressReport"; } }

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_kingdom"));
            if (bg.sprite == null) bg.color = UIKit.Paper;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.14f, 0.24f, 0.34f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            var plaque = UIKit.Card(Panel, "TitlePlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(920, 94));

            var title = UIKit.Label(Panel, "Title", "My Progress", 68, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(880, 88));
            UIKit.StyleTitle(title);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(1560, 700));

            var strip = UIKit.Card(card.gameObject, "SummaryStrip", UIKit.SoftGold, GameAssets.Ui("ui_card"));
            strip.raycastTarget = false;
            UIKit.Place(strip.rectTransform, new Vector2(0.5f, 0.895f), Vector2.zero, new Vector2(1460, 96));

            var body = UIKit.Label(card.gameObject, "Body", "", 21, UIKit.Ink, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true;
            body.fontSizeMin = 15;
            body.fontSizeMax = 21;
            body.overflowMode = TextOverflowModes.Truncate;
            UIKit.Stretch(body.rectTransform, 64, 34);

            var back = UIKit.TextButton(Panel, "Back", "Back", new Vector2(300, 88), UIKit.Coral, () => M.Show("MainMenu"), GameAssets.Ui("ui_btn"), 40);
            UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(300, 88));
            UIKit.AttachButtonIcon(back, GameAssets.Ui("ui_icon_home"), 34f);
        }

        public override void OnShow()
        {
            base.OnShow();
            var d = SaveManager.Instance.Data;
            var bank = GameManager.Instance.Bank;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Stars: " + d.totalStars + "/" + GameRules.DynamicTotalStars() + "      XP: " + d.totalXp + "      Shards: " + ScoreManager.Instance.ShardsCollected() + "/12");
            sb.AppendLine("Badges: " + (d.badges.Length == 0 ? "none yet" : string.Join(", ", d.badges)));
            sb.AppendLine("Mastery Medals: " + ParentsUiHelper.MedalsLabel(d.medals)); // LXD Phase C3
            sb.AppendLine("");
            sb.AppendLine("Skills (mastery: Learning > Practicing > Good > Mastered):");
            string[] masteryNames = { "Learning", "Practicing", "Good", "Mastered" };
            var skillEntries = new System.Collections.Generic.List<string>();
            for (int i = 0; i < bank.skills.Length; i++)
            {
                var s = d.skills[i];
                float acc = s.attempts > 0 ? (100f * s.firstTryCorrect / s.attempts) : 0f;
                skillEntries.Add("  " + bank.skills[i].PadRight(12) + " " + masteryNames[Mathf.Clamp(s.mastery, 0, 3)]
                    + "   attempts: " + s.attempts + "   first-try: " + acc.ToString("0") + "%");
            }
            for (int i = 0; i < skillEntries.Count; i += 2)
            {
                if (i + 1 < skillEntries.Count)
                    sb.AppendLine(skillEntries[i] + "<pos=50%>" + skillEntries[i + 1]);
                else
                    sb.AppendLine(skillEntries[i]);
            }

            // LXD Phase B: "Island goals" - mastery label per level ILO
            // (only islands the child has actually attempted).
            sb.AppendLine("");
            sb.AppendLine("Island goals:");
            var iloEntries = new System.Collections.Generic.List<string>();
            if (bank.levels != null)
            {
                foreach (var lv in bank.levels)
                {
                    if (lv == null || lv.ilos == null) continue;
                    foreach (var ilo in lv.ilos)
                    {
                        if (ilo == null) continue;
                        string mastery = ParentsPanel.GetIloMasteryLabel(bank, d, lv, ilo);
                        if (mastery == null) continue;
                        iloEntries.Add("  " + lv.name + ": " + ilo.kidText + " (" + mastery + ")");
                    }
                }
            }
            if (iloEntries.Count == 0)
            {
                sb.AppendLine("  none yet - play an island to set your goals!");
            }
            else
            {
                for (int i = 0; i < iloEntries.Count; i += 2)
                {
                    if (i + 1 < iloEntries.Count)
                        sb.AppendLine(iloEntries[i] + "<pos=50%>" + iloEntries[i + 1]);
                    else
                        sb.AppendLine(iloEntries[i]);
                }
            }
            Panel.transform.Find("Card/Body").GetComponent<TMP_Text>().text = sb.ToString();
        }
    }

    /// <summary>Final Result & Certificate (§7, §6.3 ending rewards) — full ending on Day 7.</summary>
    public class FinalResultScreen : ScreenBase
    {
        public override string Name { get { return "FinalResult"; } }

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_final"));
            if (bg.sprite == null) bg.color = UIKit.Purple;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.10f, 0.22f, 0.42f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            var plaque = UIKit.Card(Panel, "TitlePlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(1540, 98));

            var title = UIKit.Label(Panel, "Title", "You Saved the Kingdom!", 62, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(1480, 92));
            UIKit.StyleTitle(title);

            var crystalHalo = UIKit.Img(Panel, "CrystalHalo", GameAssets.Ui("ui_node"), UIKit.SoftGold);
            crystalHalo.raycastTarget = false;
            UIKit.Place(crystalHalo.rectTransform, new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(160, 160));

            var trophyL = UIKit.Img(Panel, "TrophyLeft", GameAssets.Ui("ui_trophy"), UIKit.Gold);
            trophyL.raycastTarget = false;
            UIKit.Place(trophyL.rectTransform, new Vector2(0.5f, 0.77f), new Vector2(-138, 0), new Vector2(64, 64));
            var trophyR = UIKit.Img(Panel, "TrophyRight", GameAssets.Ui("ui_trophy"), UIKit.Gold);
            trophyR.raycastTarget = false;
            UIKit.Place(trophyR.rectTransform, new Vector2(0.5f, 0.77f), new Vector2(138, 0), new Vector2(64, 64));

            var crystal = UIKit.Img(Panel, "Crystal", GameAssets.Obj("obj_crystal"), Color.white);
            UIKit.Place(crystal.rectTransform, new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(124, 124));

            var kingFrame = UIKit.Card(Panel, "KingFrame", UIKit.SoftGold, GameAssets.Ui("ui_card"));
            kingFrame.raycastTarget = false;
            UIKit.Place(kingFrame.rectTransform, new Vector2(0.11f, 0.45f), Vector2.zero, new Vector2(290, 340));
            var king = UIKit.Img(Panel, "King", GameAssets.Char("char_king"), Color.white);
            UIKit.Place(king.rectTransform, new Vector2(0.11f, 0.45f), Vector2.zero, new Vector2(260, 310));

            var lexiFrame = UIKit.Card(Panel, "LexiFrame", UIKit.SoftGold, GameAssets.Ui("ui_card"));
            lexiFrame.raycastTarget = false;
            UIKit.Place(lexiFrame.rectTransform, new Vector2(0.89f, 0.56f), Vector2.zero, new Vector2(250, 290));
            var lexi = UIKit.Img(Panel, "Lexi", GameAssets.Char("char_lexi"), Color.white);
            UIKit.Place(lexi.rectTransform, new Vector2(0.89f, 0.56f), Vector2.zero, new Vector2(226, 268));

            var lumoFrame = UIKit.Card(Panel, "LumoFrame", UIKit.SoftGold, GameAssets.Ui("ui_card"));
            lumoFrame.raycastTarget = false;
            UIKit.Place(lumoFrame.rectTransform, new Vector2(0.89f, 0.24f), Vector2.zero, new Vector2(188, 188));
            var lumo = UIKit.Img(Panel, "Lumo", GameAssets.Char("char_lumo"), Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.89f, 0.24f), Vector2.zero, new Vector2(166, 166));

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(1040, 420));
            var body = UIKit.Label(card.gameObject, "Body", "", 32, UIKit.Ink, TextAlignmentOptions.Center);
            body.enableAutoSizing = true;
            body.fontSizeMin = 22;
            body.fontSizeMax = 32;
            UIKit.Stretch(body.rectTransform, 60, 40);

            var report = UIKit.TextButton(Panel, "Report", "View My Progress", new Vector2(440, 96), UIKit.Orange, () => M.Show("ProgressReport"), GameAssets.Ui("ui_btn_gold"), 38);
            UIKit.Place(report.GetComponent<RectTransform>(), new Vector2(0.5f, 0.14f), new Vector2(-260, 0), new Vector2(440, 96));
            UIKit.AttachButtonIcon(report, GameAssets.Ui("ui_icon_report"), 34f);

            var home = UIKit.TextButton(Panel, "Home", "Main Menu", new Vector2(420, 96), UIKit.Teal, () => M.Show("MainMenu"), GameAssets.Ui("ui_btn"), 38);
            UIKit.Place(home.GetComponent<RectTransform>(), new Vector2(0.5f, 0.14f), new Vector2(260, 0), new Vector2(420, 96));
            UIKit.AttachButtonIcon(home, GameAssets.Ui("ui_icon_home"), 34f);
        }

        public override void OnShow()
        {
            base.OnShow();
            AudioManager.Instance.PlayComplete();
            AudioManager.Instance.PlayMusic("music_victory");
            var d = SaveManager.Instance.Data;
            int mastered = 0;
            foreach (var s in d.skills) if (s != null && s.mastery >= 2) mastered++;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Grand Master Trophy — earned!");
            sb.AppendLine("Crystal of Knowledge Badge — earned!");
            sb.AppendLine("Certificate of Achievement — awarded to Hero " + d.avatar + "!");
            sb.AppendLine("");
            int totalSkills = SaveManager.SkillCount;
            sb.AppendLine("Final score: " + d.totalXp + " XP      Total stars: " + d.totalStars + "/" + GameRules.DynamicTotalStars());
            sb.AppendLine("Skills Good/Mastered: " + mastered + "/" + totalSkills);
            sb.AppendLine("Badges: " + (d.badges.Length == 0 ? "none" : string.Join(", ", d.badges)));
            Panel.transform.Find("Card/Body").GetComponent<TMP_Text>().text = sb.ToString();
        }
    }
}
