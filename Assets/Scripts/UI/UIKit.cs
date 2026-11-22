using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>
    /// Programmatic UI factory — every screen is built from code with this kit.
    /// Sprites are looked up by fixed filename (ui_*), so art swaps in with no code change.
    /// </summary>
    public static class UIKit
    {
        // ---- Palette (child-friendly, high contrast) ----
        // Batch 6 refresh: deeper, warmer "candy" shades — the old flat tints
        // looked washed out on cream. Ink is warmer, Teal/Yellow/Green are a
        // touch deeper so white and ink text both stay readable on them.
        public static readonly Color Cream  = Hex("#FFF7E6");
        public static readonly Color Paper  = Hex("#FFFDF7");
        public static readonly Color Ink    = Hex("#33272A");
        public static readonly Color Orange = Hex("#FF9F1C");
        public static readonly Color Teal   = Hex("#14B8A6");
        public static readonly Color Coral  = Hex("#FF5A5A");
        public static readonly Color Yellow = Hex("#FFC531");
        public static readonly Color Purple = Hex("#7E5BEF");
        public static readonly Color Green  = Hex("#34A853");
        public static readonly Color Blue   = Hex("#1E88E5");
        public static readonly Color SkyBlue = Hex("#8EC9F5");
        public static readonly Color Gold   = Hex("#FFC53D");
        // Batch 6: new accents for badges, highlights and the world map.
        public static readonly Color Pink   = Hex("#FF6FA5");
        public static readonly Color Brown  = Hex("#8D6E63");
        // Cozy Island Storybook / Crystal Storybook (Batch 7) semantic Palette v2 roles
        public static readonly Color Parchment   = Hex("#FDF6E7");
        public static readonly Color WarmWood    = Hex("#6D4C3D");
        public static readonly Color DeepNavy    = new Color(0.11f, 0.17f, 0.27f, 0.88f);
        public static readonly Color Seafoam     = Hex("#D8F5EE");
        public static readonly Color SoftGold    = Hex("#FFE8A3");
        public static readonly Color BorderColor = new Color(0.20f, 0.15f, 0.16f, 0.34f);

        public static readonly Color Primary   = Teal;
        public static readonly Color Secondary = Orange;
        public static readonly Color Accent    = Yellow;
        public static readonly Color Magic     = Purple;
        public static readonly Color Success   = Green;
        public static readonly Color Danger    = Coral;
        public static readonly Color Info      = SkyBlue;

        // Batch 7 Typography scale (§4)
        public const int DisplaySize = 64;
        public const int TitleSize   = 44;
        public const int BodySize    = 32;
        public const int SmallSize   = 24;
        public const int TinySize    = 20;

        /// <summary>Maps an island subject or world key to its semantic Palette v2 role colour (§3).</summary>
        public static Color SubjectColor(string subject)
        {
            if (string.IsNullOrEmpty(subject)) return Primary;
            string s = subject.ToLowerInvariant();
            if (s.Contains("math") || s.Contains("number") || s.Contains("count")) return Secondary;
            if (s.Contains("read") || s.Contains("word") || s.Contains("english") || s.Contains("sea")) return Primary;
            if (s.Contains("science") || s.Contains("sky") || s.Contains("weather")) return Info;
            if (s.Contains("kind") || s.Contains("art") || s.Contains("craft")) return Pink;
            if (s.Contains("magic") || s.Contains("logic") || s.Contains("space") || s.Contains("crystal")) return Magic;
            if (s.Contains("memory") || s.Contains("puzzle")) return Success;
            if (s.Contains("dino") || s.Contains("discover")) return Brown;
            return Primary;
        }
        // Batch 6: single shadow colour for every drop shadow (see AddShadow).
        public static readonly Color ShadowColor = new Color(0.23f, 0.16f, 0.10f, 0.28f);
        // Batch 5 fix: red was a repeated "#E53935" literal (ModuleBase.ColorOf,
        // SortingModule); one constant keeps the shade identical everywhere.
        // NOTE: UIKit.Yellow (#FFC531) is a different shade from the "#FDD835"
        // used by ModuleBase.ColorOf("yellow") and the matching highlight
        // "#FFE0A3" — those literals are deliberately NOT folded into Yellow.
        public static readonly Color Red    = Hex("#E53935");

        // Batch 5 fix: the cream card colour was the repeated literal
        // new Color(1f, 0.99f, 0.95f, a) in ~17 places (alpha varies 0.9–0.97);
        // one helper keeps the shade identical. NOTE: this is NOT UIKit.Cream
        // (#FFF7E6) — a slightly different shade — so it gets its own helper.
        public static Color CreamAlpha(float alpha) { return new Color(1f, 0.99f, 0.95f, alpha); }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        // ---- Fonts (TMP assets baked by Lumo/Setup/Bake Fonts) ----
        static TMP_FontAsset _main;
        public static TMP_FontAsset MainFont
        {
            get { if (_main == null) _main = Resources.Load<TMP_FontAsset>("Fonts/font_main"); return _main; }
        }

        // ---- Node helpers ----
        public static GameObject Node(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        /// <summary>Full-stretch rect inside parent.</summary>
        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Full-stretch rect inside parent with symmetric horizontal/vertical padding.</summary>
        public static RectTransform Stretch(RectTransform rt, float padX, float padY)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
            return rt;
        }

        // Batch 4 fix: minimum touch target in reference pixels (§7/§11) —
        // documented here so new buttons meet it; existing buttons are NOT
        // resized by this batch (see item 21 note).
        public const float MinTouchTargetPx = 88f;

        /// <summary>Batch 4 fix: insets a full-stretch root rect to the OS safe
        /// area (notch / gesture bar). No-op when the safe area is full-screen.</summary>
        public static void ApplySafeArea(RectTransform rt)
        {
            if (rt == null) return;
            var safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0 || Screen.width <= 0 || Screen.height <= 0) return;
            if (safe.x <= 0 && safe.y <= 0 && safe.width >= Screen.width && safe.height >= Screen.height) return;
            rt.anchorMin = new Vector2(safe.x / Screen.width, safe.y / Screen.height);
            rt.anchorMax = new Vector2((safe.x + safe.width) / Screen.width, (safe.y + safe.height) / Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Batch 4 fix: title materials are cached PER FONT — the old single
        // static _titleMat was built from the first title's font.material and
        // then applied to every title, so a title in another font rendered
        // with the wrong font's SDF material.
        static readonly System.Collections.Generic.Dictionary<TMP_FontAsset, Material> _titleMats =
            new System.Collections.Generic.Dictionary<TMP_FontAsset, Material>();

        /// <summary>Applies a cached TMP SDF outline + underlay material so titles read cleanly over bright skies and walls.</summary>
        public static void StyleTitle(TMP_Text t)
        {
            if (t == null || t.font == null || t.font.material == null) return;
            Material mat;
            if (!_titleMats.TryGetValue(t.font, out mat) || mat == null)
            {
                mat = new Material(t.font.material);
                mat.name = "LumoTitleTMPMat_" + t.font.name;
                mat.EnableKeyword("OUTLINE_ON");
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(42, 26, 12, 255));
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color32(25, 15, 6, 190));
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.45f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.45f);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.2f);
                _titleMats[t.font] = mat;
            }
            t.fontSharedMaterial = mat;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot = default)
        {
            if (pivot == default) pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        // ---- Image / Panel ----
        public static Image Panel(GameObject parent, string name, Color color, Sprite sprite = null)
        {
            var go = Node(parent, name);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = (sprite != null && sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return img;
        }

        /// <summary>Image that shows a soft gray box when the sprite is missing (layout stays stable until art lands).</summary>
        public static Image Img(GameObject parent, string name, Sprite sprite, Color tint)
        {
            var img = Panel(parent, name, sprite != null ? tint : new Color(0.82f, 0.8f, 0.75f, 1f), sprite);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Batch 6: soft drop shadow under an image — the "sticker" look
        /// that makes buttons and cards pop off the background. Renders the same
        /// sprite offset downwards; cheap (one extra quad) and safe on sliced art.</summary>
        public static void AddShadow(Image img, float distance = 6f)
        {
            if (img == null) return;
            var shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(0f, -distance);
            shadow.useGraphicAlpha = true;
        }

        /// <summary>Batch 6: a Panel with a warm storybook outline and soft drop shadow —
        /// the default look for cards, dialogs and pills.</summary>
        public static Image Card(GameObject parent, string name, Color color, Sprite sprite = null)
        {
            var img = Panel(parent, name, color, sprite);
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
            AddShadow(img, 6f);
            return img;
        }

        public static Image Bg(GameObject parent, string name, Sprite sprite)
        {
            var img = Panel(parent, name, Color.white, sprite);
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
            return img;
        }

        // ---- Label ----
        public static TextMeshProUGUI Label(GameObject parent, string name, string text, int size,
            Color color, TextAlignmentOptions align = TextAlignmentOptions.Center,
            TMP_FontAsset font = null, FontStyles style = FontStyles.Bold)
        {
            var go = Node(parent, name);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.font = font != null ? font : MainFont;
            if (t.font == null) t.font = Resources.Load<TMP_FontAsset>("Fonts/font_main"); // hard fallback
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        // ---- Buttons ----
        public static Button TextButton(GameObject parent, string name, string label, Vector2 size,
            Color bg, System.Action onClick, Sprite sprite = null, int fontSize = 40, Color? textColor = null)
        {
            var go = Node(parent, name);
            Place(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = (sprite != null && sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            img.color = bg;
            // Batch 6: every button gets the soft drop shadow (sticker look).
            AddShadow(img, 4f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.12f);
            btn.colors = colors;
            float lum = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            Color resolvedText = textColor ?? (lum < 0.52f ? Color.white : Ink);
            var t = Label(go, "Label", label, fontSize, resolvedText);
            Stretch(t.rectTransform, 14, 6);
            go.AddComponent<UIButtonMotion>();
            if (onClick != null) OnClick(btn, onClick);
            return btn;
        }

        /// <summary>Attaches a left-aligned icon sprite inside a TextButton without changing
        /// the button name or the first TMP_Text child's exact label string.</summary>
        public static Image AttachButtonIcon(Button btn, Sprite iconSprite, float iconSize = 36f)
        {
            if (btn == null || iconSprite == null) return null;
            var existing = btn.transform.Find("Icon");
            Image iconImg;
            if (existing != null)
            {
                iconImg = existing.GetComponent<Image>();
                if (iconImg != null) iconImg.sprite = iconSprite;
            }
            else
            {
                iconImg = Img(btn.gameObject, "Icon", iconSprite, Color.white);
            }
            Place(iconImg.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f + iconSize * 0.5f, 2f), new Vector2(iconSize, iconSize));
            var lbl = btn.transform.Find("Label") as RectTransform;
            if (lbl != null)
            {
                lbl.anchorMin = Vector2.zero;
                lbl.anchorMax = Vector2.one;
                lbl.offsetMin = new Vector2(20f + iconSize, 6f);
                lbl.offsetMax = new Vector2(-12f, -6f);
            }
            return iconImg;
        }

        /// <summary>Central click handler: plays the click sfx, then runs the action.</summary>
        public static void OnClick(Button btn, System.Action action)
        {
            btn.onClick.AddListener(() =>
            {
                Debug.Log("[UIKit] click fired on " + btn.name);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("sfx_click");
                action();
            });
        }

        // ---- Layout groups ----
        public static HorizontalLayoutGroup HLayout(GameObject parent, string name, float spacing,
            TextAnchor align = TextAnchor.MiddleCenter, bool force = true)
        {
            var go = Node(parent, name);
            var lg = go.AddComponent<HorizontalLayoutGroup>();
            lg.spacing = spacing; lg.childAlignment = align;
            lg.childForceExpandWidth = force; lg.childForceExpandHeight = force;
            lg.childControlWidth = true; lg.childControlHeight = true;
            return lg;
        }

        public static VerticalLayoutGroup VLayout(GameObject parent, string name, float spacing,
            TextAnchor align = TextAnchor.MiddleCenter, bool force = true)
        {
            var go = Node(parent, name);
            var lg = go.AddComponent<VerticalLayoutGroup>();
            lg.spacing = spacing; lg.childAlignment = align;
            lg.childForceExpandWidth = force; lg.childForceExpandHeight = force;
            lg.childControlWidth = true; lg.childControlHeight = true;
            return lg;
        }

        public static GridLayoutGroup Grid(GameObject parent, string name, Vector2 cell, Vector2 spacing,
            int columns, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = Node(parent, name);
            var g = go.AddComponent<GridLayoutGroup>();
            g.cellSize = cell; g.spacing = spacing; g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns; g.childAlignment = align;
            return g;
        }

        // ---- Layout element helper ----
        public static LayoutElement Flex(GameObject go, float w = -1, float h = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w;
            if (h >= 0) le.preferredHeight = h;
            return le;
        }

        // ---- Canvas ----
        public static Canvas CreateUICanvas(Camera cam, string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }
    }

    /// <summary>Gentle springy hover and press micro-interaction for buttons (respects Calm Mode).</summary>
    public class UIButtonMotion : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler
    {
        bool _hover;
        bool _pressed;

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) { _hover = true; }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) { _hover = false; _pressed = false; }
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) { _pressed = true; }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) { _pressed = false; }

        void OnDisable()
        {
            _hover = false;
            _pressed = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float target = 1f;
            if (_pressed) target = calm ? 0.98f : 0.95f;
            else if (_hover && !calm) target = 1.04f;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float s = Mathf.Lerp(transform.localScale.x, target, dt * 18f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
