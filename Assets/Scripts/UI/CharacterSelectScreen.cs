using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>Character Selection: Hero A / Hero B, saved locally (§7).</summary>
    public class CharacterSelectScreen : ScreenBase
    {
        public override string Name { get { return "CharacterSelect"; } }

        class HeroCard
        {
            public int AvatarId;
            public GameObject CardGo;
            public Image CardBg;
            public Outline Border;
            public GameObject SelectedBadge;
        }

        readonly HeroCard[] _cards = new HeroCard[2];
        Coroutine _pickRoutine;
        bool _picking;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var bg = UIKit.Bg(Panel, "BG", GameAssets.Bg("bg_kingdom"));
            if (bg.sprite == null) bg.color = UIKit.SkyBlue;
            UIKit.Stretch(bg.rectTransform);

            var scrim = UIKit.Panel(Panel, "Scrim", new Color(0.06f, 0.14f, 0.24f, 0.26f));
            scrim.raycastTarget = false;
            UIKit.Stretch(scrim.rectTransform);

            var plaque = UIKit.Card(Panel, "TitlePlaque", UIKit.DeepNavy, GameAssets.Ui("ui_panel"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(1060, 98));

            var title = UIKit.Label(Panel, "Title", "Who will you be today?", 66, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(1000, 92));
            UIKit.StyleTitle(title);

            _cards[0] = MakeHero("HeroA", "Hero A", "char_hero_a", new Vector2(0.33f, 0.485f), 1);
            _cards[1] = MakeHero("HeroB", "Hero B", "char_hero_b", new Vector2(0.67f, 0.485f), 2);

            var back = UIKit.TextButton(Panel, "Back", "Back", new Vector2(260, 80), UIKit.Coral, () => M.Show("MainMenu"), GameAssets.Ui("ui_btn"), 36);
            UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.09f, 0.91f), Vector2.zero, new Vector2(260, 80));
            UIKit.AttachButtonIcon(back, GameAssets.Ui("ui_icon_home"), 34f);
        }

        HeroCard MakeHero(string name, string label, string spriteName, Vector2 pos, int avatarId)
        {
            var card = UIKit.Card(Panel, name, UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, pos, Vector2.zero, new Vector2(490, 570));

            var border = card.gameObject.AddComponent<Outline>();
            border.effectColor = UIKit.Gold;
            border.effectDistance = new Vector2(6f, -6f);
            border.enabled = false;

            var frame = UIKit.Card(card.gameObject, "Frame", UIKit.Cream, GameAssets.Ui("ui_card"));
            frame.raycastTarget = false;
            UIKit.Place(frame.rectTransform, new Vector2(0.5f, 0.61f), Vector2.zero, new Vector2(344, 312));

            Sprite sprite = GameAssets.Char(spriteName);
            var img = UIKit.Img(card.gameObject, "Img", sprite, Color.white);
            UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.61f), Vector2.zero, new Vector2(276, 276));
            if (sprite == null)
            {
                img.color = Color.clear;
                var fb = UIKit.Label(frame.gameObject, "Fallback", label, 44, UIKit.Ink);
                UIKit.Stretch(fb.rectTransform, 16, 16);
            }

            var selectedBadge = UIKit.Card(card.gameObject, "SelectedTag", UIKit.Gold, GameAssets.Ui("ui_btn_gold"));
            selectedBadge.raycastTarget = false;
            UIKit.Place(selectedBadge.rectTransform, new Vector2(0.5f, 0.925f), Vector2.zero, new Vector2(204, 42));
            var selTxt = UIKit.Label(selectedBadge.gameObject, "Text", "Selected", 24, UIKit.Ink);
            UIKit.Stretch(selTxt.rectTransform, 10, 4);
            selectedBadge.gameObject.SetActive(false);

            var lbl = UIKit.Label(card.gameObject, "Label", label, 46, UIKit.Ink);
            UIKit.Place(lbl.rectTransform, new Vector2(0.5f, 0.265f), Vector2.zero, new Vector2(420, 60));

            var pick = UIKit.TextButton(card.gameObject, "Pick", "Choose me!", new Vector2(310, 80), UIKit.Orange, () =>
            {
                if (_picking) return;
                _picking = true;
                SaveManager.Instance.Data.avatar = avatarId;
                SaveManager.Instance.Save();
                RefreshSelection();
                M.Toast("You chose " + label + "!");
                _pickRoutine = M.StartCoroutine(BounceAndContinue(card.transform));
            }, GameAssets.Ui("ui_btn_gold"), 36);
            UIKit.Place(pick.GetComponent<RectTransform>(), new Vector2(0.5f, 0.115f), Vector2.zero, new Vector2(310, 80));

            return new HeroCard
            {
                AvatarId = avatarId,
                CardGo = card.gameObject,
                CardBg = card,
                Border = border,
                SelectedBadge = selectedBadge.gameObject
            };
        }

        void RefreshSelection()
        {
            int activeAvatar = SaveManager.Instance != null && SaveManager.Instance.Data != null
                ? SaveManager.Instance.Data.avatar
                : 1;
            for (int i = 0; i < _cards.Length; i++)
            {
                var c = _cards[i];
                if (c == null) continue;
                bool selected = c.AvatarId == activeAvatar;
                if (c.CardBg != null) c.CardBg.color = selected ? UIKit.SoftGold : UIKit.Parchment;
                if (c.Border != null) c.Border.enabled = selected;
                if (c.SelectedBadge != null) c.SelectedBadge.SetActive(selected);
            }
        }

        IEnumerator BounceAndContinue(Transform cardTr)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            if (!calm && cardTr != null)
            {
                const float dur = 0.20f;
                float t = 0f;
                while (t < dur)
                {
                    if (cardTr == null) break;
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / dur);
                    cardTr.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(u * Mathf.PI));
                    yield return null;
                }
                if (cardTr != null) cardTr.localScale = Vector3.one;
            }
            _picking = false;
            _pickRoutine = null;
            M.Show(SaveManager.Instance.Data.tutorialDone ? "WorldMap" : "Tutorial");
        }

        public override void OnShow()
        {
            base.OnShow();
            if (_pickRoutine != null && M != null)
            {
                M.StopCoroutine(_pickRoutine);
                _pickRoutine = null;
            }
            _picking = false;
            for (int i = 0; i < _cards.Length; i++)
                if (_cards[i] != null && _cards[i].CardGo != null)
                    _cards[i].CardGo.transform.localScale = Vector3.one;
            RefreshSelection();
        }

        public override void OnHide()
        {
            if (_pickRoutine != null && M != null)
            {
                M.StopCoroutine(_pickRoutine);
                _pickRoutine = null;
            }
            _picking = false;
            base.OnHide();
        }
    }
}
