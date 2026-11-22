using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lumo.Core;

namespace Lumo.UI
{
    /// <summary>
    /// Gentle celebratory visual burst on right answers and completed levels.
    /// Calm Mode renders a single soft golden star fade instead of drifting confetti.
    /// </summary>
    public static class ConfettiEffect
    {
        class Piece
        {
            public RectTransform Rt;
            public Image Img;
            public Color BaseColor;
            public Vector2 StartPos;
            public float FallSpeed;
            public float SwayAmp;
            public float SwayFreq;
            public float Phase;
            public float SpinSpeed;
        }

        public static IEnumerator Play(GameObject parent, bool calmMode)
        {
            if (parent == null) yield break;

            var existing = parent.transform.Find("ConfettiBurst");
            if (existing != null) Object.Destroy(existing.gameObject);

            var root = UIKit.Node(parent, "ConfettiBurst");
            UIKit.Stretch(root.GetComponent<RectTransform>());

            if (calmMode)
            {
                var star = UIKit.Img(root, "CalmStar", GameAssets.Ui("ui_star"), UIKit.Gold);
                UIKit.Place(star.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(96, 96));

                const float calmDuration = 0.9f;
                float elapsed = 0f;
                while (elapsed < calmDuration)
                {
                    if (root == null || star == null) yield break;
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / calmDuration);
                    float alpha = t < 0.3f ? (t / 0.3f) : (1f - (t - 0.3f) / 0.7f);
                    star.color = new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, Mathf.Clamp01(alpha));
                    yield return null;
                }
                if (root != null) Object.Destroy(root);
                yield break;
            }

            Color[] palette =
            {
                UIKit.Yellow, UIKit.Orange, UIKit.Teal,
                UIKit.Purple, UIKit.Pink, UIKit.Green, UIKit.SkyBlue
            };
            Sprite starSprite = GameAssets.Ui("ui_star");
            var pieces = new List<Piece>(26);

            for (int i = 0; i < 26; i++)
            {
                bool isStar = i >= 20;
                Color col = isStar ? UIKit.Gold : palette[i % palette.Length];
                Image img = isStar
                    ? UIKit.Img(root, "Star_" + i, starSprite, col)
                    : UIKit.Panel(root, "Quad_" + i, col);
                img.raycastTarget = false;

                Vector2 size = isStar
                    ? new Vector2(Random.Range(30f, 44f), Random.Range(30f, 44f))
                    : new Vector2(Random.Range(14f, 24f), Random.Range(10f, 18f));
                Vector2 startPos = new Vector2(Random.Range(-560f, 560f), Random.Range(-50f, 110f));
                UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.85f), startPos, size);
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                pieces.Add(new Piece
                {
                    Rt = img.rectTransform,
                    Img = img,
                    BaseColor = col,
                    StartPos = startPos,
                    FallSpeed = Random.Range(175f, 320f),
                    SwayAmp = Random.Range(16f, 42f),
                    SwayFreq = Random.Range(3f, 6f),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    SpinSpeed = Random.Range(-180f, 180f)
                });
            }

            const float duration = 1.35f;
            float timer = 0f;
            while (timer < duration)
            {
                if (root == null) yield break;
                timer += Time.deltaTime;
                float fade = 1f - Mathf.Clamp01((timer - 0.55f) / (duration - 0.55f));
                for (int i = 0; i < pieces.Count; i++)
                {
                    var p = pieces[i];
                    if (p.Rt == null || p.Img == null) continue;
                    float x = p.StartPos.x + Mathf.Sin(timer * p.SwayFreq + p.Phase) * p.SwayAmp;
                    float y = p.StartPos.y - p.FallSpeed * timer;
                    p.Rt.anchoredPosition = new Vector2(x, y);
                    p.Rt.Rotate(0f, 0f, p.SpinSpeed * Time.deltaTime);
                    p.Img.color = new Color(p.BaseColor.r, p.BaseColor.g, p.BaseColor.b, fade);
                }
                yield return null;
            }

            if (root != null) Object.Destroy(root);
        }
    }
}
