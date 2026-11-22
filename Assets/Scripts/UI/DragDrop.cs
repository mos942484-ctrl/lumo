using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>
    /// Event-based drag &amp; drop plus tap-to-place fallback (mouse + touch) for chips and slots.
    /// </summary>
    public class DragChip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public string Payload;
        Vector2 _startPos;
        Vector3 _startWorldPos;
        Transform _origParent;
        int _origSibling;
        RectTransform _rt;
        CanvasGroup _cg;
        Camera _cam;
        Coroutine _returnRoutine;
        bool _dragging;
        bool _returning;

        static readonly List<DragChip> _activeDrags = new List<DragChip>();

        public static void Make(GameObject go, string payload)
        {
            var chip = go.AddComponent<DragChip>();
            chip.Payload = payload;
            chip._rt = go.GetComponent<RectTransform>();
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            chip._cg = cg;
        }

        public void OnBeginDrag(PointerEventData d)
        {
            if (_cg == null)
            {
                _cg = GetComponent<CanvasGroup>();
                if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            }
            if (!_cg.blocksRaycasts || _cg.alpha <= 0.01f) return;

            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
                _returning = false;
                RestoreHomeImmediate();
            }

            _dragging = true;
            _origParent = _rt.parent;
            _origSibling = _rt.GetSiblingIndex();
            _startPos = _rt.anchoredPosition;
            _startWorldPos = _rt.position;
            _cam = UIManager.Instance != null ? UIManager.Instance.Cam : null;
            _cg.blocksRaycasts = false;
            if (UIManager.Instance != null && UIManager.Instance.Canvas != null)
            {
                _rt.SetParent(UIManager.Instance.Canvas.transform, true);
                _rt.SetAsLastSibling();
            }
            if (!_activeDrags.Contains(this)) _activeDrags.Add(this);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDrop();
        }

        public void OnDrag(PointerEventData d)
        {
            if (!_dragging || UIManager.Instance == null || UIManager.Instance.Canvas == null) return;
            var canvasRt = (RectTransform)UIManager.Instance.Canvas.transform;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRt, d.position, _cam, out Vector3 worldPos))
                _rt.position = worldPos;
        }

        public void OnEndDrag(PointerEventData d)
        {
            if (!_dragging) return;
            _dragging = false;

            bool matchedSlot = false;
            bool accepted = false;
            foreach (var slot in DragDropManager.Slots)
            {
                if (slot == null || slot.Rect == null || !slot.Rect.gameObject.activeInHierarchy) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(slot.Rect, d.position, _cam))
                {
                    matchedSlot = true;
                    if (slot.OnDropCheck != null) accepted = slot.OnDropCheck(Payload);
                    else { slot.OnDrop?.Invoke(Payload); accepted = true; }
                    break;
                }
            }

            if (matchedSlot && accepted)
            {
                _activeDrags.Remove(this);
                RestoreHomeImmediate();
                if (_cg != null)
                {
                    _cg.alpha = 0f;
                    _cg.blocksRaycasts = false;
                }
                return;
            }

            bool calm = GameManager.Instance != null && GameManager.Instance.IsCalmMode;
            if (calm || !gameObject.activeInHierarchy)
            {
                _activeDrags.Remove(this);
                RestoreHomeImmediate();
                if (_cg != null)
                {
                    _cg.alpha = 1f;
                    _cg.blocksRaycasts = true;
                }
                return;
            }

            _returnRoutine = StartCoroutine(ReturnSmooth());
        }

        public void OnPointerClick(PointerEventData d)
        {
            if (d != null && d.dragging) return;
            if (_dragging || _returning) return;
            if (_cg == null) _cg = GetComponent<CanvasGroup>();
            if (_cg != null && (!_cg.blocksRaycasts || _cg.alpha <= 0.01f)) return;

            DragSlot target = null;
            for (int i = 0; i < DragDropManager.Slots.Count; i++)
            {
                var slot = DragDropManager.Slots[i];
                if (slot == null || slot.Rect == null || !slot.Rect.gameObject.activeInHierarchy) continue;
                if (slot.IsFilled != null && slot.IsFilled()) continue;
                if (!string.IsNullOrEmpty(slot.ExpectedPayload) && slot.ExpectedPayload == Payload)
                {
                    target = slot;
                    break;
                }
            }

            if (target == null)
            {
                for (int i = 0; i < DragDropManager.Slots.Count; i++)
                {
                    var slot = DragDropManager.Slots[i];
                    if (slot == null || slot.Rect == null || !slot.Rect.gameObject.activeInHierarchy) continue;
                    if (slot.IsFilled != null && slot.IsFilled()) continue;
                    if (!string.IsNullOrEmpty(slot.ExpectedPayload)) continue;
                    target = slot;
                    break;
                }
            }

            if (target == null) return;

            bool accepted;
            if (target.OnDropCheck != null) accepted = target.OnDropCheck(Payload);
            else { target.OnDrop?.Invoke(Payload); accepted = true; }

            if (accepted && _cg != null)
            {
                _cg.alpha = 0f;
                _cg.blocksRaycasts = false;
            }
        }

        IEnumerator ReturnSmooth()
        {
            _returning = true;
            Vector3 fromWorld = _rt.position;
            const float dur = 0.15f;
            float t = 0f;
            while (t < dur)
            {
                if (_rt == null) { _returning = false; _returnRoutine = null; _activeDrags.Remove(this); yield break; }
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                _rt.position = Vector3.Lerp(fromWorld, _startWorldPos, u);
                yield return null;
            }
            _returning = false;
            _returnRoutine = null;
            _activeDrags.Remove(this);
            RestoreHomeImmediate();
            if (_cg != null)
            {
                _cg.alpha = 1f;
                _cg.blocksRaycasts = true;
            }
        }

        void RestoreHomeImmediate()
        {
            if (_rt == null || _origParent == null) return;
            _rt.SetParent(_origParent, false);
            _rt.SetSiblingIndex(_origSibling);
            _rt.anchoredPosition = _startPos;
            if (_origParent is RectTransform parentRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
        }

        /// <summary>Returns an active or returning chip to its home position on round teardown.</summary>
        public void CancelDrag()
        {
            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }
            _dragging = false;
            _returning = false;
            RestoreHomeImmediate();
            if (_cg != null) { _cg.alpha = 1f; _cg.blocksRaycasts = true; }
        }

        void OnDestroy()
        {
            _activeDrags.Remove(this);
        }

        /// <summary>Returns every in-flight chip home. Called by DragDropManager.Clear() on round teardown.</summary>
        public static void CancelAllDrags()
        {
            for (int i = _activeDrags.Count - 1; i >= 0; i--)
            {
                var chip = _activeDrags[i];
                if (chip != null) chip.CancelDrag();
            }
            _activeDrags.Clear();
        }
    }

    public class DragSlot
    {
        public RectTransform Rect;
        public Action<string> OnDrop;
        public Func<string, bool> OnDropCheck;
        public Func<bool> IsFilled;
        public string ExpectedPayload;
    }

    public static class DragDropManager
    {
        public static readonly List<DragSlot> Slots = new List<DragSlot>();

        public static DragSlot Register(RectTransform rect, Action<string> onDrop)
        {
            var s = new DragSlot { Rect = rect, OnDrop = onDrop };
            Slots.Add(s);
            return s;
        }

        public static DragSlot Register(RectTransform rect, Func<string, bool> onDropCheck,
            Func<bool> isFilled = null, string expectedPayload = null)
        {
            var s = new DragSlot
            {
                Rect = rect,
                OnDropCheck = onDropCheck,
                IsFilled = isFilled,
                ExpectedPayload = expectedPayload
            };
            Slots.Add(s);
            return s;
        }

        /// <summary>Call when a challenge/round is torn down.</summary>
        public static void Clear()
        {
            DragChip.CancelAllDrags();
            Slots.Clear();
        }
    }
}
