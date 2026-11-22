using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
using Lumo.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Lumo.Modules
{
    /// <summary>
    /// directions (Day 5): grid map; the child hears "Move 2 steps Right, then 1 step Up"
    /// and moves the hero with arrow buttons or keyboard — arriving at the target opens the treasure.
    /// Keys round: tap all keys to collect, tap keys again to use, then answer (§5 L9).
    /// </summary>
    public class DirectionsModule : ModuleBase
    {
        static readonly Color VisitedCellTint = new Color(1f, 0.94f, 0.72f, 0.95f);

        Transform _hero;
        RectTransform _heroLayer;
        Transform _grid;
        Coroutine _moveRoutine;
        int _cx, _cy;
        int _keysCollected;
        int _keysUsed;
        bool _keysPhase2;
        bool _solved;
        Transform _keysRow;
        readonly HashSet<GameObject> _collectedKeys = new HashSet<GameObject>();
        readonly HashSet<GameObject> _usedKeys = new HashSet<GameObject>();

        public override string Type { get { return "directions"; } }

        public override void Build(GameObject activityArea, GameObject answerArea)
        {
            _solved = false;
            _moveRoutine = null;
            _keysCollected = 0;
            _keysUsed = 0;
            _keysPhase2 = false;
            _collectedKeys.Clear();
            _usedKeys.Clear();
            _cx = C.media.sx;
            _cy = C.media.sy;

            if (C.media.collect > 0)
            {
                BuildKeysRound(activityArea, answerArea);
                AudioManager.Instance.PlayVoice(C.vo);
                return;
            }

            // ---- grid map ----
            float cellSize = C.media.gh >= 5 ? 96f : 130f;
            var grid = UIKit.Grid(activityArea, "Grid", new Vector2(cellSize, cellSize), new Vector2(6, 6), C.media.gw, TextAnchor.MiddleCenter);
            var gridRt = grid.GetComponent<RectTransform>();
            UIKit.Place(gridRt, new Vector2(0.34f, 0.50f), Vector2.zero, new Vector2(560, 560));
            _grid = grid.transform;

            for (int y = C.media.gh - 1; y >= 0; y--)
                for (int x = 0; x < C.media.gw; x++)
                {
                    var cell = UIKit.Panel(grid.gameObject, "C" + x + "_" + y,
                        (x + y) % 2 == 0 ? new Color(1f, 1f, 0.95f, 0.92f) : new Color(0.93f, 0.93f, 0.85f, 0.92f), null);
                    UIKit.Flex(cell.gameObject, cellSize, cellSize);
                    if (x == C.media.tx && y == C.media.ty)
                    {
                        var chest = UIKit.Img(cell.gameObject, "Chest", Obj("obj_chest"), Color.white);
                        UIKit.Place(chest.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellSize * 0.78f, cellSize * 0.78f));
                    }
                }

            // sibling hero layer matching grid bounds so hero glides smoothly between cells
            var heroLayer = UIKit.Node(activityArea, "HeroLayer");
            _heroLayer = heroLayer.GetComponent<RectTransform>();
            UIKit.Place(_heroLayer, new Vector2(0.34f, 0.50f), Vector2.zero, new Vector2(560, 560));

            var hero = UIKit.Img(heroLayer, "Hero", GameAssets.Char(SaveManager.Instance.Data.avatar == 2 ? "char_hero_b" : "char_hero_a"), Color.white);
            UIKit.Place(hero.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellSize * 0.82f, cellSize * 0.82f));
            _hero = hero.transform;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRt);

            var startCell = _grid.Find("C" + _cx + "_" + _cy);
            if (startCell != null)
            {
                _hero.position = startCell.position;
                TintVisitedCell(startCell);
            }

            // arrow pad
            var pad = UIKit.Node(activityArea, "Pad");
            UIKit.Place(pad.GetComponent<RectTransform>(), new Vector2(0.74f, 0.52f), Vector2.zero, new Vector2(560, 440));
            MakeArrow(pad, "Up", "Up", new Vector2(0, 120), UIKit.SkyBlue);
            MakeArrow(pad, "Left", "Left", new Vector2(-150, 0), UIKit.SkyBlue);
            MakeArrow(pad, "Right", "Right", new Vector2(150, 0), UIKit.SkyBlue);
            MakeArrow(pad, "Down", "Down", new Vector2(0, -120), UIKit.SkyBlue);

            var stepsCard = UIKit.Panel(activityArea, "StepsBg", UIKit.CreamAlpha(0.9f), Ui("ui_panel"));
            UIKit.Place(stepsCard.rectTransform, new Vector2(0.74f, 0.10f), Vector2.zero, new Vector2(400, 68));
            var steps = UIKit.Label(stepsCard.gameObject, "Steps", "Now at: " + _cx + " , " + _cy, 34, UIKit.Blue);
            UIKit.Stretch(steps.rectTransform, 16, 6);

            AudioManager.Instance.PlayVoice(C.vo);
        }

        void MakeArrow(GameObject pad, string name, string label, Vector2 pos, Color col)
        {
            var b = UIKit.TextButton(pad, "Arrow" + name, label, new Vector2(140, 108), col,
                () => Move(name), GameAssets.Ui("ui_btn"), 20);
            UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, new Vector2(140, 108));

            var lbl = b.transform.Find("Label");
            if (lbl != null)
                UIKit.Place(lbl.GetComponent<RectTransform>(), new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(126, 28));

            var icon = UIKit.Img(b.gameObject, "Icon", GameAssets.Ui("ui_arrow_" + name.ToLower()), Color.white);
            UIKit.Place(icon.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(50, 50));
        }

        Vector2 CellLocalPos(int x, int y)
        {
            var cell = _grid != null ? _grid.Find("C" + x + "_" + y) : null;
            if (cell == null || _heroLayer == null) return Vector2.zero;
            return _heroLayer.InverseTransformPoint(cell.position);
        }

        static void TintVisitedCell(Transform cell)
        {
            if (cell == null) return;
            var img = cell.GetComponent<Image>();
            if (img != null) img.color = VisitedCellTint;
        }

        void Move(string dir)
        {
            if (_solved) return;
            int nx = _cx, ny = _cy;
            if (dir == "Right") nx++;
            else if (dir == "Left") nx--;
            else if (dir == "Up") ny++;
            else if (dir == "Down") ny--;
            if (nx < 0 || nx >= C.media.gw || ny < 0 || ny >= C.media.gh)
            {
                S.ShowToast("That's the edge of the map!");
                return;
            }

            if (_moveRoutine != null)
            {
                if (S != null) S.StopTrackedRoutine(_moveRoutine);
                _moveRoutine = null;
                if (_hero != null) ((RectTransform)_hero).anchoredPosition = CellLocalPos(_cx, _cy);
            }

            Vector2 fromPos = CellLocalPos(_cx, _cy);
            _cx = nx;
            _cy = ny;
            Vector2 toPos = CellLocalPos(_cx, _cy);

            AudioManager.Instance.PlaySfx("sfx_drop");

            var targetCell = _grid != null ? _grid.Find("C" + _cx + "_" + _cy) : null;
            TintVisitedCell(targetCell);

            var steps = S.ActivityArea.transform.Find("StepsBg/Steps");
            if (steps != null) steps.GetComponent<TMP_Text>().text = "Now at: " + _cx + " , " + _cy;

            bool arrived = (_cx == C.media.tx && _cy == C.media.ty);
            if (arrived) _solved = true;

            _moveRoutine = S.StartTrackedRoutine(GlideHero(fromPos, toPos, arrived));
        }

        IEnumerator GlideHero(Vector2 from, Vector2 to, bool arrived)
        {
            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            float dur = calm ? 0.06f : 0.18f;
            float t = 0f;
            var heroRt = _hero as RectTransform;

            while (t < dur)
            {
                if (_hero == null || heroRt == null) { _moveRoutine = null; yield break; }
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float s = Mathf.SmoothStep(0f, 1f, u);
                Vector2 pos = Vector2.Lerp(from, to, s);
                if (!calm) pos.y += 12f * Mathf.Sin(u * Mathf.PI);
                heroRt.anchoredPosition = pos;
                yield return null;
            }

            if (heroRt != null) heroRt.anchoredPosition = to;
            _moveRoutine = null;

            if (arrived)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCollect();
                S.ShowToast("You found the treasure!");
                Answer(true);
            }
        }

        public override void Tick(float dt)
        {
            if (C == null || C.media.collect > 0 || _solved) return;
            var ui = UIManager.Instance;
            if (ui != null && (
                ui.IsOverlayOpen("Pause") ||
                ui.IsOverlayOpen("Hint") ||
                ui.IsOverlayOpen("Settings") ||
                ui.IsOverlayOpen("ParentalGate") ||
                ui.IsOverlayOpen("Parents") ||
                ui.IsOverlayOpen("TimeLimit")))
                return;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Move("Up");
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Move("Down");
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Move("Left");
            else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Move("Right");
#endif
        }

        void BuildKeysRound(GameObject activityArea, GameObject answerArea)
        {
            // keys pile
            _keysRow = UIKit.HLayout(activityArea, "Keys", 14, TextAnchor.MiddleCenter, false).transform;
            UIKit.Place((RectTransform)_keysRow, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(1500, 160));
            for (int i = 0; i < C.media.collect; i++)
            {
                var key = UIKit.Img(_keysRow.gameObject, "Key" + i, Obj("obj_key"), Color.white);
                key.raycastTarget = true;
                UIKit.Flex(key.gameObject, 100, 100);
                UIKit.OnClick(key.gameObject.AddComponent<Button>(), () => TapKey(key.gameObject));
            }

            var counter = UIKit.Label(activityArea, "KeyCount", "Keys collected: 0/" + C.media.collect, 44, UIKit.Blue);
            UIKit.Place(counter.rectTransform, new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(800, 80));

            // Batch 5 fix: TryParse + logged fallback (was int.Parse).
            BuildNumberOptions(ParseAnswer(C.correctAnswer, C.id), startDisabled: true);
        }

        void TapKey(GameObject key)
        {
            if (_solved) return;
            if (!_keysPhase2)
            {
                // Batch 1 fix: a key can only be collected once.
                if (_collectedKeys.Contains(key)) return;
                if (_keysCollected >= C.media.collect) return;
                _collectedKeys.Add(key);
                AudioManager.Instance.PlayCollect();
                key.GetComponent<Image>().color = UIKit.Yellow;
                _keysCollected++;
                var counter = _keysRow.parent.Find("KeyCount");
                if (counter != null)
                    counter.GetComponent<TMP_Text>().text = _keysCollected >= C.media.collect
                        ? "Now tap " + C.media.use + " keys to use them!"
                        : "Keys collected: " + _keysCollected + "/" + C.media.collect;
                if (_keysCollected >= C.media.collect) _keysPhase2 = true;
            }
            else
            {
                // Batch 1 fix: a key can only be used once, and only a collected key can be used.
                if (!_collectedKeys.Contains(key) || _usedKeys.Contains(key)) return;
                if (_keysUsed >= C.media.use || !key.activeSelf) return;
                _usedKeys.Add(key);
                AudioManager.Instance.PlaySfx("sfx_drop");
                key.SetActive(false);
                _keysUsed++;
                if (_keysUsed >= C.media.use)
                {
                    var counter = _keysRow.parent.Find("KeyCount");
                    if (counter != null)
                        counter.GetComponent<TMP_Text>().text = "How many keys are left?";
                    SetOptionsInteractable(true);
                }
            }
        }

        public override void Cleanup()
        {
            if (_moveRoutine != null && S != null)
            {
                S.StopTrackedRoutine(_moveRoutine);
                _moveRoutine = null;
            }
            base.Cleanup();
            _solved = false;
        }
    }
}
