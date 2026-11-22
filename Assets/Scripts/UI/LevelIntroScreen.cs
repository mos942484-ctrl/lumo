using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>Storybook Chapter Intro: chapter banner, age-band pill, ILO kid goals, story + quest goal, portraits, Let's Start.</summary>
    public class LevelIntroScreen : ScreenBase
    {
        public override string Name { get { return "LevelIntro"; } }
        TMP_Text _story, _goal, _title, _bandText, _ilos;
        Image _hostImg;
        int _levelId = 1;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_kingdom"));
            if (bg.sprite == null) bg.color = UIKit.Purple;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.12f, 0.22f, 0.35f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            // Chapter title banner
            var titlePlaque = UIKit.Card(Panel, "TitlePlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            titlePlaque.raycastTarget = false;
            UIKit.Place(titlePlaque.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(1320, 102));

            _title = UIKit.Label(Panel, "Title", "", 58, UIKit.Gold);
            UIKit.Place(_title.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(1280, 92));
            UIKit.StyleTitle(_title);

            var bandPill = UIKit.Card(Panel, "BandPill", UIKit.SoftGold, GameAssets.Ui("ui_btn"));
            UIKit.Place(bandPill.rectTransform, new Vector2(0.5f, 0.79f), Vector2.zero, new Vector2(860, 54));
            _bandText = UIKit.Label(bandPill.gameObject, "Text", "", 25, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Stretch(_bandText.rectTransform, 18, 4);

            // Storybook chapter card
            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(1140, 470));

            // LXD Phase B: "By the end of this island you can..." — the level's ILO kidTexts
            _ilos = UIKit.Label(card.gameObject, "IslandGoals", "", 24, UIKit.Blue, TextAlignmentOptions.Center);
            _ilos.enableAutoSizing = true;
            _ilos.fontSizeMin = 18;
            _ilos.fontSizeMax = 24;
            var irt = _ilos.GetComponent<RectTransform>();
            UIKit.Place(irt, new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(1040, 88));

            _story = UIKit.Label(card.gameObject, "Story", "", 35, UIKit.Ink, TextAlignmentOptions.Center);
            _story.enableAutoSizing = true;
            _story.fontSizeMin = 26;
            _story.fontSizeMax = 35;
            var srt = _story.GetComponent<RectTransform>();
            UIKit.Place(srt, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(1020, 170));

            var goalBox = UIKit.Card(card.gameObject, "GoalBox", UIKit.Seafoam, GameAssets.Ui("ui_card"));
            goalBox.raycastTarget = false;
            UIKit.Place(goalBox.rectTransform, new Vector2(0.5f, 0.21f), Vector2.zero, new Vector2(1020, 120));

            _goal = UIKit.Label(card.gameObject, "Goal", "", 33, UIKit.Ink, TextAlignmentOptions.Center);
            _goal.enableAutoSizing = true;
            _goal.fontSizeMin = 24;
            _goal.fontSizeMax = 33;
            var grt = _goal.GetComponent<RectTransform>();
            UIKit.Place(grt, new Vector2(0.5f, 0.21f), Vector2.zero, new Vector2(980, 104));

            // Framed portraits
            var hostFrame = UIKit.Card(Panel, "HostFrame", UIKit.Parchment, GameAssets.Ui("ui_card"));
            hostFrame.raycastTarget = false;
            UIKit.Place(hostFrame.rectTransform, new Vector2(0.11f, 0.46f), Vector2.zero, new Vector2(290, 340));
            _hostImg = UIKit.Img(Panel, "King", GameAssets.Char("char_king"), Color.white);
            UIKit.Place(_hostImg.rectTransform, new Vector2(0.11f, 0.46f), Vector2.zero, new Vector2(240, 290));

            var lumoFrame = UIKit.Card(Panel, "LumoFrame", UIKit.Parchment, GameAssets.Ui("ui_card"));
            lumoFrame.raycastTarget = false;
            UIKit.Place(lumoFrame.rectTransform, new Vector2(0.89f, 0.46f), Vector2.zero, new Vector2(280, 300));
            var lumo = UIKit.Img(Panel, "Lumo", GameAssets.Char("char_lumo"), Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.89f, 0.46f), Vector2.zero, new Vector2(230, 250));

            var start = UIKit.TextButton(Panel, "Start", "Let's Start!", new Vector2(520, 102), UIKit.Orange, () =>
            {
                GameManager.Instance.SelectedLevelId = _levelId;
                GameManager.Instance.AbortLevel();
                M.Show("Gameplay");
            }, GameAssets.Ui("ui_btn_gold"), 44);
            UIKit.Place(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0.11f), Vector2.zero, new Vector2(520, 102));
            UIKit.AttachButtonIcon(start, GameAssets.Ui("ui_icon_play"), 38f);

            var back = UIKit.TextButton(Panel, "Back", "Back", new Vector2(240, 76), UIKit.Coral,
                () => M.Show("WorldMap"), GameAssets.Ui("ui_btn"), 32);
            UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.075f, 0.90f), Vector2.zero, new Vector2(240, 76));
            UIKit.AttachButtonIcon(back, GameAssets.Ui("ui_arrow_left"), 32f);
        }

        public override void OnShow()
        {
            base.OnShow();
            _levelId = GameManager.Instance.SelectedLevelId;
            if (_levelId < 1 || _levelId > GameManager.Instance.Bank.levels.Length)
            {
                // defensive (matches GameplayScreen): never index the bank with an unset id
                _levelId = Mathf.Clamp(SaveManager.Instance != null ? SaveManager.Instance.Data.lastLevel : 1, 1, 13);
                GameManager.Instance.SelectedLevelId = _levelId;
            }
            var lv = GameManager.Instance.Bank.levels[_levelId - 1];
            var info = IslandCatalog.Get(_levelId);
            _title.text = _levelId == 13
                ? "Music Room - The Final Challenge"
                : info.IslandName + " - " + lv.name;
            if (_bandText != null && SaveManager.Instance != null && SaveManager.Instance.Data != null)
                _bandText.text = info.Subject + " | Ages " + GameRules.AgeBandDescription(SaveManager.Instance.Data.ageBand);
            _story.text = lv.intro.story;
            _goal.text = lv.intro.goal;

            // LXD Phase B: kid-facing ILOs ("I can ...") above the story.
            // VO for ILOs is optional and only played if clips exist; never blocks.
            if (_ilos != null)
            {
                if (lv.ilos != null && lv.ilos.Length > 0)
                {
                    var kids = new System.Text.StringBuilder();
                    kids.Append("By the end of this island you can:  ");
                    for (int i = 0; i < lv.ilos.Length; i++)
                    {
                        if (lv.ilos[i] != null && !string.IsNullOrEmpty(lv.ilos[i].kidText))
                        {
                            if (kids.Length > 36) kids.Append("   |   ");
                            kids.Append(lv.ilos[i].kidText);
                        }
                    }
                    _ilos.text = kids.ToString();
                }
                else _ilos.text = "";
            }
            AudioManager.Instance.PlayVoice(lv.intro.vo);

            if (_hostImg != null)
            {
                string hostSprite = (lv.world == "english" || lv.id == 6) ? "char_lexi" : (lv.id == 13 ? "char_shadow" : "char_king");
                _hostImg.sprite = GameAssets.Char(hostSprite);
            }

            var bg = Panel.transform.Find("BG").GetComponent<Image>();
            var sprite = GameAssets.Bg(lv.bg);
            bg.sprite = sprite;
            bg.color = sprite != null ? Color.white : UIKit.Purple;
        }
    }
}
