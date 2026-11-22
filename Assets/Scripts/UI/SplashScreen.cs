using UnityEngine;
using UnityEngine.UI;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>Splash (3-5 s, skippable, §7).</summary>
    public class SplashScreen : ScreenBase
    {
        public override string Name { get { return "Splash"; } }
        float _t;
        RectTransform _lumoRt;
        CanvasGroup _titleCg, _subCg, _lumoCg, _tapCg, _plaqueCg, _haloCg, _tapBadgeCg;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_menu"));
            if (bg.sprite == null) bg.color = UIKit.Purple;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.14f, 0.24f, 0.26f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            var plaque = UIKit.Card(Panel, "TitlePlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(1340, 280));
            _plaqueCg = plaque.gameObject.AddComponent<CanvasGroup>();

            var logo = UIKit.Label(Panel, "Title", "Lumo", 156, UIKit.Gold);
            UIKit.Place(logo.rectTransform, new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(1200, 200));
            UIKit.StyleTitle(logo);
            _titleCg = logo.gameObject.AddComponent<CanvasGroup>();

            var sub = UIKit.Label(Panel, "Subtitle", "and the Crystal of Knowledge", 60, UIKit.Cream);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.515f), Vector2.zero, new Vector2(1240, 96));
            UIKit.StyleTitle(sub);
            _subCg = sub.gameObject.AddComponent<CanvasGroup>();

            var halo = UIKit.Img(Panel, "LumoHalo", GameAssets.Ui("ui_node"), UIKit.SoftGold);
            halo.raycastTarget = false;
            UIKit.Place(halo.rectTransform, new Vector2(0.5f, 0.295f), Vector2.zero, new Vector2(390, 390));
            _haloCg = halo.gameObject.AddComponent<CanvasGroup>();

            Sprite lumoSprite = GameAssets.Char("char_lumo");
            var lumo = UIKit.Img(Panel, "Lumo", lumoSprite, Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.5f, 0.295f), Vector2.zero, new Vector2(340, 340));
            if (lumoSprite == null) lumo.color = Color.clear;
            _lumoRt = lumo.rectTransform;
            _lumoCg = lumo.gameObject.AddComponent<CanvasGroup>();

            var tapBadge = UIKit.Card(Panel, "TapBadge", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            tapBadge.raycastTarget = false;
            UIKit.Place(tapBadge.rectTransform, new Vector2(0.5f, 0.115f), Vector2.zero, new Vector2(680, 68));
            _tapBadgeCg = tapBadge.gameObject.AddComponent<CanvasGroup>();

            var tap = UIKit.Label(Panel, "Tap", "Tap anywhere to begin...", 36, UIKit.SoftGold);
            UIKit.Place(tap.rectTransform, new Vector2(0.5f, 0.115f), Vector2.zero, new Vector2(640, 60));
            UIKit.StyleTitle(tap);
            _tapCg = tap.gameObject.AddComponent<CanvasGroup>();

            // any tap skips
            var skip = UIKit.Node(Panel, "Skip");
            UIKit.Stretch(skip.GetComponent<RectTransform>());
            var img = skip.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            var btn = skip.AddComponent<Button>();
            UIKit.OnClick(btn, () => M.Show("MainMenu"));
        }

        public override void OnShow()
        {
            base.OnShow();
            _t = 0f;
            AudioManager.Instance.PlayMusic("music_menu");
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float a = calm ? 1f : 0f;
            if (_plaqueCg != null) _plaqueCg.alpha = a;
            if (_titleCg != null) _titleCg.alpha = a;
            if (_subCg != null) _subCg.alpha = a;
            if (_haloCg != null) _haloCg.alpha = a;
            if (_lumoCg != null) _lumoCg.alpha = a;
            if (_tapBadgeCg != null) _tapBadgeCg.alpha = a;
            if (_tapCg != null) _tapCg.alpha = a;
            if (_lumoRt != null) _lumoRt.anchoredPosition = Vector2.zero;
        }

        public override void Tick(float dt)
        {
            _t += dt;
            if (_t >= 4.5f)
            {
                M.Show("MainMenu");
                return;
            }

            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            if (calm)
            {
                if (_plaqueCg != null) _plaqueCg.alpha = 1f;
                if (_titleCg != null) _titleCg.alpha = 1f;
                if (_subCg != null) _subCg.alpha = 1f;
                if (_haloCg != null) _haloCg.alpha = 1f;
                if (_lumoCg != null) _lumoCg.alpha = 1f;
                if (_tapBadgeCg != null) _tapBadgeCg.alpha = 1f;
                if (_tapCg != null) _tapCg.alpha = 1f;
                if (_lumoRt != null) _lumoRt.anchoredPosition = Vector2.zero;
                return;
            }

            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t / 0.65f));
            if (_plaqueCg != null) _plaqueCg.alpha = fade;
            if (_titleCg != null) _titleCg.alpha = fade;
            if (_subCg != null) _subCg.alpha = fade;
            if (_haloCg != null) _haloCg.alpha = fade;
            if (_lumoCg != null) _lumoCg.alpha = fade;
            float tapAlpha = _t < 0.65f ? fade : (0.72f + 0.28f * Mathf.Sin((_t - 0.65f) * 3.0f));
            if (_tapBadgeCg != null) _tapBadgeCg.alpha = _t < 0.65f ? fade : 0.94f;
            if (_tapCg != null) _tapCg.alpha = tapAlpha;
            if (_lumoRt != null)
                _lumoRt.anchoredPosition = new Vector2(0f, 8f * Mathf.Sin(_t * 2.2f));
        }
    }
}
