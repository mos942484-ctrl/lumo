using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;

namespace Lumo.Modules
{
    /// <summary>
    /// pixel-art (Phase 2): editable gw x gh pixel grid supporting copy, symmetry,
    /// and pattern modes for Art Studio (Level 5).
    /// </summary>
    public class PixelArtModule : ModuleBase
    {
        static readonly Color NormalBorder = new Color(0.76f, 0.72f, 0.65f, 1f);
        static readonly Color LockedBorder = new Color(0.42f, 0.39f, 0.35f, 1f);
        static readonly Color MismatchBorder = UIKit.Orange;

        class SwatchEntry
        {
            public int ColorIdx;
            public Image Frame;
            public TMP_Text Label;
            public string BaseLabel;
        }

        class GridCell
        {
            public Image Frame;
            public Image Fill;
            public bool Locked;
        }

        int _gw;
        int _gh;
        string[] _palette;
        int[] _userGrid;
        GridCell[] _cells;
        readonly List<SwatchEntry> _swatches = new List<SwatchEntry>();
        int _selectedColor;
        bool _solved;

        public override string Type { get { return "pixel-art"; } }

        /// <summary>Maps a palette hex string to a kid-friendly English color name.</summary>
        public static string ColorNameForHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return "Gray";
            string norm = hex.Trim().ToUpperInvariant();
            if (!norm.StartsWith("#")) norm = "#" + norm;

            switch (norm)
            {
                case "#FF5A5A":
                case "#E53935": return "Red";
                case "#FF9F1C": return "Orange";
                case "#FFC531":
                case "#FFC53D": return "Gold";
                case "#FDD835":
                case "#FFD166": return "Yellow";
                case "#34A853":
                case "#43A047":
                case "#10B981": return "Green";
                case "#14B8A6": return "Teal";
                case "#8EC9F5":
                case "#38BDF8":
                case "#0EA5E9": return "Sky Blue";
                case "#1E88E5":
                case "#6366F1": return "Blue";
                case "#7E5BEF": return "Purple";
                case "#FF6FA5": return "Pink";
                case "#8D6E63": return "Brown";
                case "#000000": return "Black";
                case "#FFFFFF": return "White";
            }

            if (!ColorUtility.TryParseHtmlString(norm, out Color col)) return "Gray";
            Color.RGBToHSV(col, out float h, out float s, out float v);
            float hue = h * 360f;

            if (v < 0.16f) return "Black";
            if (s < 0.15f) return v >= 0.90f ? "White" : "Gray";
            if (hue >= 12f && hue < 45f && v < 0.62f && s >= 0.20f) return "Brown";

            if (hue < 16f || hue >= 345f) return "Red";
            if (hue < 36f) return "Orange";
            if (hue < 48f) return "Gold";
            if (hue < 70f) return "Yellow";
            if (hue < 155f) return "Green";
            if (hue < 185f) return "Teal";
            if (hue < 210f) return (s < 0.72f || v >= 0.85f) ? "Sky Blue" : "Blue";
            if (hue < 252f) return "Blue";
            if (hue < 295f) return "Purple";
            return "Pink";
        }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _solved = false;
            _selectedColor = 0;
            _swatches.Clear();

            var m = C != null ? C.media : null;
            _gw = Mathf.Clamp(m != null && m.gw > 0 ? m.gw : 3, 1, 8);
            _gh = Mathf.Clamp(m != null && m.gh > 0 ? m.gh : 3, 1, 8);
            _palette = (m != null && m.palette != null && m.palette.Length > 0)
                ? m.palette
                : new[] { "#1E88E5", "#FF9F1C" };

            int total = _gw * _gh;
            _userGrid = new int[total];
            _cells = new GridCell[total];

            BuildPalette(activityArea);
            BuildMainGrid(activityArea, m);
            BuildRightPanel(activityArea, m);
            BuildAnswerControls(answerArea);
            SelectColor(0);

            AudioManager.Instance?.PlayVoice(C != null ? C.vo : null);
        }

        void BuildPalette(GameObject activityArea)
        {
            var card = UIKit.Card(activityArea, "PaletteCard", UIKit.CreamAlpha(0.95f), Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.14f, 0.50f), Vector2.zero, new Vector2(300f, 490f));

            var title = UIKit.Label(card.gameObject, "Title", "Colors", 32, UIKit.Ink);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(260f, 48f));

            int swatchCount = _palette.Length + 1;
            float swatchH = Mathf.Min(70f, (368f - (swatchCount - 1) * 12f) / swatchCount);
            var list = UIKit.VLayout(card.gameObject, "Swatches", 12f, TextAnchor.MiddleCenter, false);
            UIKit.Place(list.GetComponent<RectTransform>(), new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(256f, 384f));

            for (int i = 0; i < _palette.Length; i++)
            {
                Color c = Tint(_palette[i]);
                CreateSwatch(list.gameObject, i, ColorNameForHex(_palette[i]), c, swatchH);
            }
            CreateSwatch(list.gameObject, -1, "Erase", Color.white, swatchH);
        }

        void CreateSwatch(GameObject parent, int colorIdx, string text, Color fillColor, float height)
        {
            var frame = UIKit.Panel(parent, "Swatch_" + colorIdx, NormalBorder, Ui("ui_panel"));
            UIKit.Flex(frame.gameObject, 240f, height);

            var fill = UIKit.Panel(frame.gameObject, "Fill", fillColor, Ui("ui_btn"));
            UIKit.Stretch(fill.rectTransform, 5f, 5f);

            float lum = 0.299f * fillColor.r + 0.587f * fillColor.g + 0.114f * fillColor.b;
            Color txtColor = lum < 0.52f ? Color.white : UIKit.Ink;
            var lbl = UIKit.Label(fill.gameObject, "Label", text, 26, txtColor);
            UIKit.Stretch(lbl.rectTransform, 8f, 4f);

            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = fill;
            int idx = colorIdx;
            btn.onClick.AddListener(() => SelectColor(idx));

            _swatches.Add(new SwatchEntry
            {
                ColorIdx = colorIdx,
                Frame = frame,
                Label = lbl,
                BaseLabel = text
            });
        }

        void SelectColor(int colorIdx)
        {
            _selectedColor = colorIdx;
            for (int i = 0; i < _swatches.Count; i++)
            {
                var sw = _swatches[i];
                bool active = sw.ColorIdx == colorIdx;
                if (sw.Frame != null)
                {
                    sw.Frame.color = active ? UIKit.Ink : NormalBorder;
                    sw.Frame.transform.localScale = active ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
                }
                if (sw.Label != null)
                    sw.Label.text = active ? "> " + sw.BaseLabel : sw.BaseLabel;
            }
        }

        void BuildMainGrid(GameObject activityArea, MediaData m)
        {
            var card = UIKit.Card(activityArea, "CanvasCard", UIKit.CreamAlpha(0.96f), Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.49f, 0.50f), Vector2.zero, new Vector2(520f, 500f));

            float spacing = _gh >= 5 ? 6f : 8f;
            int maxDim = Mathf.Max(_gw, _gh);
            float cellSize = Mathf.Floor((440f - (maxDim - 1) * spacing) / maxDim);

            var grid = UIKit.Grid(card.gameObject, "PixelGrid", new Vector2(cellSize, cellSize),
                new Vector2(spacing, spacing), _gw, TextAnchor.MiddleCenter);
            float gridW = _gw * cellSize + (_gw - 1) * spacing;
            float gridH = _gh * cellSize + (_gh - 1) * spacing;
            UIKit.Place(grid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(gridW, gridH));

            if (m != null && m.artMode == "symmetry")
            {
                var guide = UIKit.Panel(card.gameObject, "SymmetryGuide", new Color(0.5f, 0.35f, 0.9f, 0.30f));
                guide.raycastTarget = false;
                UIKit.Place(guide.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, gridH + 18f));
            }

            for (int y = 0; y < _gh; y++)
            {
                for (int x = 0; x < _gw; x++)
                {
                    int idx = y * _gw + x;
                    int startVal = (m != null && m.startGrid != null && idx < m.startGrid.Length) ? m.startGrid[idx] : -1;
                    bool locked = startVal >= 0 && startVal < _palette.Length;

                    _userGrid[idx] = locked ? startVal : -1;
                    Color fillColor = locked ? Tint(_palette[startVal]) : Color.white;

                    var frame = UIKit.Panel(grid.gameObject, "Cell_" + idx, locked ? LockedBorder : NormalBorder);
                    UIKit.Flex(frame.gameObject, cellSize, cellSize);

                    var fill = UIKit.Panel(frame.gameObject, "Fill", fillColor);
                    UIKit.Stretch(fill.rectTransform, 4f, 4f);
                    fill.raycastTarget = false;

                    if (locked)
                    {
                        float lum = 0.299f * fillColor.r + 0.587f * fillColor.g + 0.114f * fillColor.b;
                        var lockDot = UIKit.Img(frame.gameObject, "LockDot", Ui("ui_node"),
                            lum < 0.52f ? new Color(1f, 1f, 1f, 0.65f) : new Color(0.2f, 0.2f, 0.2f, 0.45f));
                        UIKit.Place(lockDot.rectTransform, new Vector2(0.84f, 0.84f), Vector2.zero, new Vector2(10f, 10f));
                    }

                    _cells[idx] = new GridCell
                    {
                        Frame = frame,
                        Fill = fill,
                        Locked = locked
                    };

                    if (!locked)
                    {
                        var btn = frame.gameObject.AddComponent<Button>();
                        btn.targetGraphic = frame;
                        int capturedIdx = idx;
                        btn.onClick.AddListener(() => PaintCell(capturedIdx));
                    }
                }
            }
        }

        void BuildRightPanel(GameObject activityArea, MediaData m)
        {
            string mode = m != null && !string.IsNullOrEmpty(m.artMode) ? m.artMode : "copy";
            var card = UIKit.Card(activityArea, "GoalCard", UIKit.CreamAlpha(0.95f), Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.84f, 0.50f), Vector2.zero, new Vector2(380f, 440f));

            if (mode == "copy")
            {
                var title = UIKit.Label(card.gameObject, "GoalTitle", "Goal", 34, UIKit.Ink);
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(300f, 52f));

                float spacing = 4f;
                int maxDim = Mathf.Max(_gw, _gh);
                float miniCell = Mathf.Floor((270f - (maxDim - 1) * spacing) / maxDim);
                var miniGrid = UIKit.Grid(card.gameObject, "GoalGrid", new Vector2(miniCell, miniCell),
                    new Vector2(spacing, spacing), _gw, TextAnchor.MiddleCenter);
                float mgW = _gw * miniCell + (_gw - 1) * spacing;
                float mgH = _gh * miniCell + (_gh - 1) * spacing;
                UIKit.Place(miniGrid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(mgW, mgH));

                for (int i = 0; i < _gw * _gh; i++)
                {
                    int targetVal = (m != null && m.targetGrid != null && i < m.targetGrid.Length) ? m.targetGrid[i] : -1;
                    Color c = (targetVal >= 0 && targetVal < _palette.Length) ? Tint(_palette[targetVal]) : Color.white;
                    var f = UIKit.Panel(miniGrid.gameObject, "G_" + i, NormalBorder);
                    f.raycastTarget = false;
                    UIKit.Flex(f.gameObject, miniCell, miniCell);
                    var inner = UIKit.Panel(f.gameObject, "Fill", c);
                    inner.raycastTarget = false;
                    UIKit.Stretch(inner.rectTransform, 2f, 2f);
                }
            }
            else
            {
                bool isSymmetry = mode == "symmetry";
                var badge = UIKit.Panel(card.gameObject, "ModeBadge", isSymmetry ? UIKit.Purple : UIKit.Teal, Ui("ui_btn"));
                UIKit.Place(badge.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(310f, 74f));
                var badgeLbl = UIKit.Label(badge.gameObject, "ModeText", isSymmetry ? "Mirror Pattern" : "Complete Pattern", 30, Color.white);
                UIKit.Stretch(badgeLbl.rectTransform, 14f, 6f);

                string tip = isSymmetry
                    ? "Copy the locked squares across the center mirror line!"
                    : "Follow the color rule across each row to finish the art!";
                var tipLbl = UIKit.Label(card.gameObject, "ModeTip", tip, 28, UIKit.Ink);
                UIKit.Place(tipLbl.rectTransform, new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(320f, 170f));
            }
        }

        void BuildAnswerControls(GameObject answerArea)
        {
            var row = UIKit.HLayout(answerArea, "ArtControls", 32f, TextAnchor.MiddleCenter, false);
            UIKit.Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 126f));

            var checkBtn = UIKit.TextButton(row.gameObject, "CheckArt", "Check Art!", new Vector2(300f, 112f),
                UIKit.Green, CheckArt, Ui("ui_btn"), 36);
            UIKit.Flex(checkBtn.gameObject, 300f, 112f);

            var clearBtn = UIKit.TextButton(row.gameObject, "ClearMine", "Clear Mine", new Vector2(280f, 112f),
                UIKit.SkyBlue, ClearEditable, Ui("ui_btn"), 34);
            UIKit.Flex(clearBtn.gameObject, 280f, 112f);
        }

        void PaintCell(int idx)
        {
            if (_solved || idx < 0 || idx >= _cells.Length || _cells[idx].Locked) return;

            _userGrid[idx] = _selectedColor;
            Color fillColor = (_selectedColor >= 0 && _selectedColor < _palette.Length)
                ? Tint(_palette[_selectedColor])
                : Color.white;
            if (_cells[idx].Fill != null) _cells[idx].Fill.color = fillColor;
            if (_cells[idx].Frame != null) _cells[idx].Frame.color = NormalBorder;

            AudioManager.Instance?.PlaySfx("sfx_drop");
        }

        void ClearEditable()
        {
            if (_solved) return;
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i].Locked) continue;
                _userGrid[i] = -1;
                if (_cells[i].Fill != null) _cells[i].Fill.color = Color.white;
                if (_cells[i].Frame != null) _cells[i].Frame.color = NormalBorder;
            }
            AudioManager.Instance?.PlaySfx("sfx_drop");
        }

        void CheckArt()
        {
            if (_solved) return;

            var target = C != null && C.media != null ? C.media.targetGrid : null;
            int mismatches = 0;

            for (int i = 0; i < _userGrid.Length; i++)
            {
                int expected = (target != null && i < target.Length) ? target[i] : -1;
                bool match = _userGrid[i] == expected;
                if (!match) mismatches++;
                if (_cells[i].Frame != null && !_cells[i].Locked)
                    _cells[i].Frame.color = match ? NormalBorder : MismatchBorder;
            }

            if (mismatches == 0)
            {
                _solved = true;
                AudioManager.Instance?.PlayCollect();
                Answer(true);
            }
            else
            {
                S?.ShowToast("Almost there - check the glowing squares!");
                Answer(false);
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _solved = false;
            _swatches.Clear();
        }
    }
}
