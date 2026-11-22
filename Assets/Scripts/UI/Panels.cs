using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>Child-safe audio Settings overlay plus shortcut to the gated Parents Area.</summary>
    public class SettingsPanel : OverlayPanel
    {
        public override string Name { get { return "Settings"; } }
        TMP_Text _music, _sound, _voice;
        Image _musicBg, _soundBg, _voiceBg;
        static readonly Color OverlayDim = new Color(0.05f, 0.10f, 0.18f, 0.68f);

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", OverlayDim);
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 680));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.845f), Vector2.zero, new Vector2(560, 78));

            var title = UIKit.Label(card.gameObject, "Title", "Settings", 54, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.845f), Vector2.zero, new Vector2(520, 72));

            _music = MakeToggle(card.gameObject, "Music", new Vector2(0, 100), "Music", () =>
            {
                SaveManager.Instance.Data.musicOn = !SaveManager.Instance.Data.musicOn;
                SaveManager.Instance.Save();
                AudioManager.Instance.ApplySettings();
                Refresh();
            }, out _musicBg);
            _sound = MakeToggle(card.gameObject, "Sound", new Vector2(0, 0), "Sound Effects", () =>
            {
                SaveManager.Instance.Data.soundOn = !SaveManager.Instance.Data.soundOn;
                SaveManager.Instance.Save();
                Refresh();
            }, out _soundBg);
            _voice = MakeToggle(card.gameObject, "Voice", new Vector2(0, -100), "Voice", () =>
            {
                SaveManager.Instance.Data.voiceOn = !SaveManager.Instance.Data.voiceOn;
                SaveManager.Instance.Save();
                if (!SaveManager.Instance.Data.voiceOn) AudioManager.Instance.StopVoice();
                Refresh();
            }, out _voiceBg);

            var parents = UIKit.TextButton(card.gameObject, "ParentsArea", "Parents Area", new Vector2(310, 76), UIKit.WarmWood, () =>
            {
                M.HideOverlay("Settings");
                M.OpenParentsArea();
            }, GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(parents.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-180, -225), new Vector2(310, 76));
            UIKit.AttachButtonIcon(parents, GameAssets.Ui("ui_icon_report"), 32f);

            var close = UIKit.TextButton(card.gameObject, "Close", "Close", new Vector2(310, 76), UIKit.Teal,
                () => M.HideOverlay("Settings"), GameAssets.Ui("ui_btn"), 34);
            UIKit.Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(180, -225), new Vector2(310, 76));
            UIKit.AttachButtonIcon(close, GameAssets.Ui("ui_icon_close"), 32f);
        }

        TMP_Text MakeToggle(GameObject parent, string name, Vector2 pos, string label, System.Action onClick, out Image bg)
        {
            var b = UIKit.TextButton(parent, name, "", new Vector2(540, 86), UIKit.Cream, onClick, GameAssets.Ui("ui_btn"), 34);
            UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, new Vector2(540, 86));
            UIKit.AttachButtonIcon(b, GameAssets.Ui("ui_icon_audio"), 34f);
            bg = b.GetComponent<Image>();
            var t = b.GetComponentInChildren<TMP_Text>(true);
            t.name = label;
            t.fontSize = 34;
            return t;
        }

        void Refresh()
        {
            var d = SaveManager.Instance.Data;
            StyleToggle(_music, _musicBg, "Music: ", d.musicOn);
            StyleToggle(_sound, _soundBg, "Sound Effects: ", d.soundOn);
            StyleToggle(_voice, _voiceBg, "Voice: ", d.voiceOn);
        }

        static void StyleToggle(TMP_Text lbl, Image bg, string prefix, bool on)
        {
            if (lbl != null)
            {
                lbl.text = prefix + (on ? "ON" : "OFF");
                lbl.color = UIKit.Ink;
            }
            if (bg != null)
                bg.color = on ? UIKit.SoftGold : UIKit.Cream;
        }

        public override void OnShow() { Refresh(); }
    }

    /// <summary>How to Play overlay (Section 7).</summary>
    public class HowToPlayPanel : OverlayPanel
    {
        public override string Name { get { return "HowToPlay"; } }

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.68f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 760));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.855f), Vector2.zero, new Vector2(640, 78));

            var title = UIKit.Label(card.gameObject, "Title", "How to Play", 54, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.855f), Vector2.zero, new Vector2(600, 72));

            var well = UIKit.Card(card.gameObject, "BodyWell", UIKit.Cream, GameAssets.Ui("ui_card"));
            well.raycastTarget = false;
            UIKit.Place(well.rectTransform, new Vector2(0.5f, 0.485f), Vector2.zero, new Vector2(820, 468));

            var body = UIKit.Label(card.gameObject, "Body",
                "The wizard broke the crystal into 12 pieces!\n\n" +
                "12 islands to play:\n" +
                "Math islands: count, add, take away.\n" +
                "Word islands: find words, spell.\n" +
                "Puzzle islands: match, maps, potions.\n\n" +
                "Tap an island. Play! Win a shard!\n" +
                "Stuck? Tap Hint!",
                27, UIKit.Ink, TextAlignmentOptions.Center);
            body.lineSpacing = 22;
            var rt = body.GetComponent<RectTransform>();
            UIKit.Place(rt, new Vector2(0.5f, 0.485f), Vector2.zero, new Vector2(770, 430));

            var close = UIKit.TextButton(card.gameObject, "Close", "Got it!", new Vector2(360, 80), UIKit.Teal, () => M.HideOverlay("HowToPlay"), GameAssets.Ui("ui_btn"), 40);
            UIKit.Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0.105f), Vector2.zero, new Vector2(360, 80));
            UIKit.AttachButtonIcon(close, GameAssets.Ui("ui_check"), 34f);
        }
    }

    /// <summary>My Collection overlay: stars, shards, badges, words (Section 7).</summary>
    public class CollectionPanel : OverlayPanel
    {
        public override string Name { get { return "Collection"; } }
        TMP_Text _body;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.68f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 700));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.845f), Vector2.zero, new Vector2(760, 80));

            var iconStar = UIKit.Img(card.gameObject, "StarIcon", GameAssets.Ui("ui_star"), UIKit.Gold);
            iconStar.raycastTarget = false;
            UIKit.Place(iconStar.rectTransform, new Vector2(0.145f, 0.845f), Vector2.zero, new Vector2(46, 46));
            var iconShard = UIKit.Img(card.gameObject, "ShardIcon", GameAssets.Ui("ui_shard"), Color.white);
            iconShard.raycastTarget = false;
            UIKit.Place(iconShard.rectTransform, new Vector2(0.215f, 0.845f), Vector2.zero, new Vector2(46, 46));
            var iconTrophy = UIKit.Img(card.gameObject, "TrophyIcon", GameAssets.Ui("ui_trophy"), UIKit.Gold);
            iconTrophy.raycastTarget = false;
            UIKit.Place(iconTrophy.rectTransform, new Vector2(0.845f, 0.845f), Vector2.zero, new Vector2(48, 48));

            var title = UIKit.Label(card.gameObject, "Title", "My Collection", 54, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.845f), Vector2.zero, new Vector2(520, 72));

            var well = UIKit.Card(card.gameObject, "BodyWell", UIKit.Cream, GameAssets.Ui("ui_card"));
            well.raycastTarget = false;
            UIKit.Place(well.rectTransform, new Vector2(0.5f, 0.485f), Vector2.zero, new Vector2(820, 396));

            _body = UIKit.Label(card.gameObject, "Body", "", 34, UIKit.Ink, TextAlignmentOptions.Left);
            _body.enableAutoSizing = true;
            _body.fontSizeMin = 20;
            _body.fontSizeMax = 34;
            var rt = _body.GetComponent<RectTransform>();
            UIKit.Place(rt, new Vector2(0.5f, 0.485f), Vector2.zero, new Vector2(760, 360));

            var close = UIKit.TextButton(card.gameObject, "Close", "Close", new Vector2(360, 80), UIKit.Teal, () => M.HideOverlay("Collection"), GameAssets.Ui("ui_btn"), 40);
            UIKit.Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0.115f), Vector2.zero, new Vector2(360, 80));
            UIKit.AttachButtonIcon(close, GameAssets.Ui("ui_icon_close"), 34f);
        }

        public override void OnShow()
        {
            var d = SaveManager.Instance.Data;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Stars: " + d.totalStars + " / " + GameRules.DynamicTotalStars());
            sb.AppendLine("Crystal Shards: " + ScoreManager.Instance.ShardsCollected() + " / 12");
            sb.AppendLine("XP: " + d.totalXp);
            sb.AppendLine("Badges: " + (d.badges.Length == 0 ? "none yet" : string.Join(", ", d.badges)));
            // LXD Phase C3: Mastery Medals - one per mastered skill, unlocked by
            // learning (first time the skill reaches Mastered), never by play count.
            sb.AppendLine("Mastery Medals: " + ParentsUiHelper.MedalsLabel(d.medals));
            sb.AppendLine("Words to review: " + d.reviewWords.Length);
            _body.text = sb.ToString();
        }
    }

    /// <summary>Pause overlay: pause/resume/settings/home (Section 7, Section 11).</summary>
    public class PausePanel : OverlayPanel
    {
        public override string Name { get { return "Pause"; } }

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.68f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 700));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.83f), Vector2.zero, new Vector2(480, 78));

            var title = UIKit.Label(card.gameObject, "Title", "Paused", 56, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.83f), Vector2.zero, new Vector2(440, 72));

            var resume = UIKit.TextButton(card.gameObject, "Resume", "Resume", new Vector2(420, 88), UIKit.Green, () =>
            {
                M.HideOverlay("Pause");
            }, GameAssets.Ui("ui_btn"), 40);
            UIKit.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, 62), new Vector2(420, 88));
            UIKit.AttachButtonIcon(resume, GameAssets.Ui("ui_icon_play"), 36f);

            var settings = UIKit.TextButton(card.gameObject, "Settings", "Settings", new Vector2(420, 88), UIKit.Purple, () =>
            {
                M.ShowOverlay("Settings");
            }, GameAssets.Ui("ui_btn"), 40);
            UIKit.Place(settings.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(420, 88));
            UIKit.AttachButtonIcon(settings, GameAssets.Ui("ui_icon_settings"), 36f);

            var home = UIKit.TextButton(card.gameObject, "Home", "Home", new Vector2(420, 88), UIKit.Coral, () =>
            {
                GameManager.Instance.AbortLevel();
                M.HideOverlay("Pause");
                M.Show("WorldMap");
            }, GameAssets.Ui("ui_btn"), 40);
            UIKit.Place(home.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -142), new Vector2(420, 88));
            UIKit.AttachButtonIcon(home, GameAssets.Ui("ui_icon_home"), 36f);

            var exit = UIKit.TextButton(card.gameObject, "ExitGame", "Exit Game", new Vector2(420, 88), UIKit.Coral, () =>
            {
                Application.Quit();
            }, GameAssets.Ui("ui_btn"), 40);
            UIKit.Place(exit.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -244), new Vector2(420, 88));
            UIKit.AttachButtonIcon(exit, GameAssets.Ui("ui_icon_close"), 36f);
        }

        public override void OnShow()
        {
            TimerManager.Instance.Pause();
        }

        public override void OnHide()
        {
            if (!M.IsOverlayOpen("TimeLimit"))
                TimerManager.Instance.Resume();
        }
    }

    /// <summary>Hint overlay: text hint (2 mistakes) or pictured step-by-step narration (3 mistakes).</summary>
    public class HintPanel : OverlayPanel
    {
        public override string Name { get { return "Hint"; } }
        TMP_Text _title, _body;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.68f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 560));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.54f, 0.825f), Vector2.zero, new Vector2(560, 74));

            _title = UIKit.Label(card.gameObject, "Title", "Hint", 48, UIKit.Gold);
            UIKit.Place(_title.rectTransform, new Vector2(0.54f, 0.825f), Vector2.zero, new Vector2(520, 70));

            var well = UIKit.Card(card.gameObject, "SpeechWell", UIKit.Cream, GameAssets.Ui("ui_card"));
            well.raycastTarget = false;
            UIKit.Place(well.rectTransform, new Vector2(0.5f, 0.47f), Vector2.zero, new Vector2(880, 286));

            _body = UIKit.Label(card.gameObject, "Body", "", 36, UIKit.Ink);
            _body.enableAutoSizing = true;
            _body.fontSizeMin = 24;
            _body.fontSizeMax = 36;
            var rt = _body.GetComponent<RectTransform>();
            UIKit.Place(rt, new Vector2(0.5f, 0.47f), Vector2.zero, new Vector2(820, 256));

            var halo = UIKit.Img(card.gameObject, "LumoHalo", GameAssets.Ui("ui_node"), UIKit.SoftGold);
            halo.raycastTarget = false;
            UIKit.Place(halo.rectTransform, new Vector2(0.155f, 0.805f), Vector2.zero, new Vector2(118, 118));

            var lumo = UIKit.Img(card.gameObject, "Lumo", GameAssets.Char("char_lumo"), Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.155f, 0.805f), Vector2.zero, new Vector2(90, 90));

            var close = UIKit.TextButton(card.gameObject, "Close", "OK!", new Vector2(360, 80), UIKit.Teal, () => M.HideOverlay("Hint"), GameAssets.Ui("ui_btn"), 40);
            UIKit.Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(360, 80));
            UIKit.AttachButtonIcon(close, GameAssets.Ui("ui_check"), 34f);
        }

        public void ShowHint(string text)
        {
            _title.text = "Lumo says...";
            _body.text = text;
            M.ShowOverlay("Hint");
        }

        public void ShowSolution(string text)
        {
            _title.text = "Step by step";
            _body.text = text;
            M.ShowOverlay("Hint");
        }

        public override void OnShow()
        {
            if (TimerManager.Instance != null) TimerManager.Instance.Pause();
        }

        public override void OnHide()
        {
            if (TimerManager.Instance != null && !M.IsOverlayOpen("Pause") && !M.IsOverlayOpen("TimeLimit"))
                TimerManager.Instance.Resume();
        }
    }
}
