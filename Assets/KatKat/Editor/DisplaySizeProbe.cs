using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Doku bütçesi için ölçüm (2026-11-23): yakalamada ekrandaki her arayüz görselinin (Image, RawImage) piksel boyunu
    /// kaydeder. KATKAT_DISPLAY_LOG bir dosya yoluysa "varlık-yolu&lt;TAB&gt;en-büyük-kenar-px&lt;TAB&gt;ekran-genişliği" satırları
    /// eklenir. <see cref="TextureBudget"/> bu ölçülere göre dokuların en büyük boyunu belirler.
    /// </summary>
    public static class DisplaySizeProbe
    {
        public static void Record(GameObject root, int screenW, int screenH)
        {
            var log = Environment.GetEnvironmentVariable("KATKAT_DISPLAY_LOG");
            if (string.IsNullOrEmpty(log) || root == null) return;
            var best = new Dictionary<string, float>();
            var corners = new Vector3[4];
            foreach (var g in root.GetComponentsInChildren<Graphic>(false))
            {
                if (!g.isActiveAndEnabled) continue;
                Texture tex = g is Image im ? (im.sprite != null ? im.sprite.texture : null) : g is RawImage ri ? ri.texture : null;
                if (tex == null) continue;
                var path = AssetDatabase.GetAssetPath(tex);
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) continue;
                var canvas = g.canvas;
                if (canvas == null) continue;
                g.rectTransform.GetWorldCorners(corners);
                var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 c = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                float px = Mathf.Max(Mathf.Abs(c.x - a.x), Mathf.Abs(c.y - a.y));
                // Sprite atlasın bir parçasıysa doku boyu parçaya oranla büyür (doku tam boyu gerekir).
                if (g is Image img && img.sprite != null && img.type != Image.Type.Sliced && img.type != Image.Type.Tiled)
                {
                    var r = img.sprite.rect;
                    float frac = Mathf.Max(r.width / tex.width, r.height / tex.height);
                    if (frac > 0.01f) px /= frac;
                }
                if (!best.TryGetValue(path, out var cur) || px > cur) best[path] = px;
            }
            try
            {
                using (var w = File.AppendText(log))
                    foreach (var kv in best) w.WriteLine($"{kv.Key}\t{kv.Value:0}\t{screenW}x{screenH}");
            }
            catch { }
        }
    }
}
