using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>
    /// LXD Phase D1: session recap — an out-game reflection moment (Abbott §3.4).
    /// Kid language, tap-only: islands visited today, levels finished, review words
    /// remembered, mastery gains, and tomorrow's "Try Next". Reached from the daily
    /// limit rest screen ("See my day") or after returning to the menu having
    /// completed a level. Never mid-level. Calm mode: same screen, no confetti.
    /// </summary>
    public class SessionRecapScreen : ScreenBase
    {
        public override string Name { get { return "SessionRecap"; } }
        TMP_Text _body;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_menu"));
            if (bg.sprite == null) bg.color = UIKit.Teal;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.12f, 0.22f, 0.35f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            var plaque = UIKit.Card(Panel, "TitlePlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(1240, 104));

            var title = UIKit.Label(Panel, "Title", "My Day on the Isles", 60, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(1200, 96));
            UIKit.StyleTitle(title);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.55f, 0.49f), Vector2.zero, new Vector2(1140, 530));
            _body = UIKit.Label(card.gameObject, "Body", "", 36, UIKit.Ink, TextAlignmentOptions.TopLeft);
            _body.enableAutoSizing = true;
            _body.fontSizeMin = 22;
            _body.fontSizeMax = 36;
            UIKit.Stretch(_body.rectTransform, 64, 44);

            var halo = UIKit.Card(Panel, "LumoFrame", UIKit.Parchment, GameAssets.Ui("ui_card"));
            halo.raycastTarget = false;
            UIKit.Place(halo.rectTransform, new Vector2(0.13f, 0.49f), Vector2.zero, new Vector2(260, 300));
            var lumo = UIKit.Img(Panel, "Lumo", GameAssets.Char("char_lumo"), Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.13f, 0.49f), Vector2.zero, new Vector2(210, 250));

            var done = UIKit.TextButton(Panel, "Done", "Done!", new Vector2(420, 96), UIKit.Orange, () =>
            {
                M.Show("MainMenu");
                // coming from the rest screen, keep the rest guard intact
                if (SessionTimeManager.Instance != null && SessionTimeManager.Instance.IsDailyLimitReached)
                    M.ShowOverlay("TimeLimit");
            }, GameAssets.Ui("ui_btn_gold"), 42);
            UIKit.Place(done.GetComponent<RectTransform>(), new Vector2(0.5f, 0.115f), Vector2.zero, new Vector2(420, 96));
            UIKit.AttachButtonIcon(done, GameAssets.Ui("ui_icon_home"), 34f);
        }

        public override void OnShow()
        {
            base.OnShow();
            var tracker = SessionTracker.Instance;
            var bank = GameManager.Instance != null ? GameManager.Instance.Bank : null;
            var data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
            if (_body != null)
            {
                _body.text = tracker != null
                    ? tracker.BuildRecapText(bank, data)
                    : "Today's adventure is saved for tomorrow!";
            }
            if (!GameManager.Instance.IsCalmMode)
                M.StartCoroutine(ConfettiEffect.Play(Panel, false));
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
}
