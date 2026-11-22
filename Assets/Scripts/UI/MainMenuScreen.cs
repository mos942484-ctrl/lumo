using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>Cozy Island Storybook Main Menu: Play, Continue, How to Play, Settings, Collection, Progress, Parents.</summary>
    public class MainMenuScreen : ScreenBase
    {
        public override string Name { get { return "MainMenu"; } }

        TextMeshProUGUI _stats;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_menu"));
            if (bg.sprite == null) bg.color = UIKit.Teal;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.14f, 0.24f, 0.24f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            // Storybook title plaque (clear below top-right stats pill)
            var plaque = UIKit.Card(Panel, "TitlePlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.865f), Vector2.zero, new Vector2(1460, 114));

            var decoLeft = UIKit.Img(plaque.gameObject, "IconLeft", GameAssets.Obj("obj_crystal"), Color.white);
            UIKit.Place(decoLeft.rectTransform, new Vector2(0.045f, 0.5f), Vector2.zero, new Vector2(64, 64));
            var decoRight = UIKit.Img(plaque.gameObject, "IconRight", GameAssets.Ui("ui_star"), UIKit.Gold);
            UIKit.Place(decoRight.rectTransform, new Vector2(0.955f, 0.5f), Vector2.zero, new Vector2(64, 64));

            var title = UIKit.Label(Panel, "Title", "Lumo and the Crystal of Knowledge", 68, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.865f), Vector2.zero, new Vector2(1280, 100));
            UIKit.StyleTitle(title);

            // Top-right parchment stats pill
            var statsPill = UIKit.Card(Panel, "StatsPill", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(statsPill.rectTransform, new Vector2(0.98f, 0.958f), Vector2.zero, new Vector2(620, 60), new Vector2(1f, 0.5f));
            var statsStar = UIKit.Img(statsPill.gameObject, "Star", GameAssets.Ui("ui_star"), Color.white);
            UIKit.Place(statsStar.rectTransform, new Vector2(0.055f, 0.5f), Vector2.zero, new Vector2(36, 36));
            _stats = UIKit.Label(statsPill.gameObject, "Stats", "", 30, UIKit.Ink, TextAlignmentOptions.Right);
            _stats.rectTransform.anchorMin = Vector2.zero;
            _stats.rectTransform.anchorMax = Vector2.one;
            _stats.rectTransform.offsetMin = new Vector2(58, 6);
            _stats.rectTransform.offsetMax = new Vector2(-22, -6);

            // Cozy Explorer Welcome scene on the left
            var lumo = UIKit.Img(Panel, "Lumo", GameAssets.Char("char_lumo"), Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.20f, 0.50f), Vector2.zero, new Vector2(290, 290));

            var bubble = UIKit.Card(Panel, "Bubble", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(bubble.rectTransform, new Vector2(0.29f, 0.21f), Vector2.zero, new Vector2(620, 174));
            var speaker = UIKit.Card(bubble.gameObject, "SpeakerTag", UIKit.SoftGold, GameAssets.Ui("ui_btn"));
            speaker.raycastTarget = false;
            UIKit.Place(speaker.rectTransform, new Vector2(0.05f, 0.97f), Vector2.zero, new Vector2(220, 38), new Vector2(0f, 0.5f));
            var speakerLbl = UIKit.Label(speaker.gameObject, "Text", "Lumo the Owl", 21, UIKit.Ink);
            UIKit.Stretch(speakerLbl.rectTransform, 10, 2);

            var bubbleText = UIKit.Label(bubble.gameObject, "Text", "Hello, young hero! Are you ready to explore the islands?", 30, UIKit.Ink);
            UIKit.Stretch(bubbleText.rectTransform, 34, 26);

            // Right storybook menu board + button column
            var menuBoard = UIKit.Card(Panel, "MenuBoard", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            menuBoard.raycastTarget = false;
            UIKit.Place(menuBoard.rectTransform, new Vector2(0.73f, 0.44f), Vector2.zero, new Vector2(580, 670));

            var col = UIKit.VLayout(Panel, "Buttons", 18, TextAnchor.MiddleCenter, false);
            UIKit.Place(col.GetComponent<RectTransform>(), new Vector2(0.73f, 0.44f), Vector2.zero, new Vector2(520, 620));

            MakeButton(col.gameObject, "Play", "Play", UIKit.Orange, GameAssets.Ui("ui_icon_play"), () =>
            {
                if (!SaveManager.Instance.Data.tutorialDone) M.Show("CharacterSelect");
                else M.Show("WorldMap");
            }, GameAssets.Ui("ui_btn_gold"));
            MakeButton(col.gameObject, "Continue", "Continue", UIKit.Teal, GameAssets.Ui("ui_icon_next"), () =>
            {
                GameManager.Instance.SelectedLevelId = LevelManager.Instance.ContinueLevel();
                M.Show("LevelIntro");
            });
            MakeButton(col.gameObject, "HowTo", "How to Play", UIKit.Yellow, GameAssets.Ui("ui_icon_hint"), () => M.ShowOverlay("HowToPlay"));
            MakeButton(col.gameObject, "Settings", "Settings", UIKit.Purple, GameAssets.Ui("ui_icon_settings"), () => M.ShowOverlay("Settings"));
            MakeButton(col.gameObject, "Collection", "My Collection", UIKit.SkyBlue, GameAssets.Ui("ui_trophy"), () => M.ShowOverlay("Collection"));
            MakeButton(col.gameObject, "Progress", "My Progress", UIKit.Green, GameAssets.Ui("ui_icon_report"), () => M.Show("ProgressReport"));

            var parents = UIKit.TextButton(Panel, "Parents", "Parents", new Vector2(260, 76), UIKit.WarmWood,
                () => M.OpenParentsArea(), GameAssets.Ui("ui_btn"), 32);
            UIKit.Place(parents.GetComponent<RectTransform>(), new Vector2(0.10f, 0.08f), Vector2.zero, new Vector2(260, 76));
            UIKit.AttachButtonIcon(parents, GameAssets.Ui("ui_icon_settings"), 34f);
        }

        void MakeButton(GameObject parent, string name, string label, Color c, Sprite icon, System.Action onClick, Sprite btnSprite = null)
        {
            var b = UIKit.TextButton(parent, name, label, new Vector2(520, 88), c, onClick, btnSprite ?? GameAssets.Ui("ui_btn"), 40);
            UIKit.Flex(b.gameObject, 520, 88);
            UIKit.AttachButtonIcon(b, icon, 36f);
        }

        public override void OnShow()
        {
            base.OnShow();
            AudioManager.Instance.PlayMusic("music_menu");
            RefreshStats();
        }

        /// <summary>Updates the top-right stats pill from the current save.</summary>
        public void RefreshStats()
        {
            if (_stats == null || SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            var d = SaveManager.Instance.Data;
            int shards = ScoreManager.Instance != null ? ScoreManager.Instance.ShardsCollected() : 0;
            _stats.text = "Stars: " + d.totalStars + "/" + GameRules.DynamicTotalStars() + "   Shards: " + shards + "/12";
        }
    }
}
