using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Bölüm haritasındaki patika: verilen noktalardan geçen üçgen şeridi (yuvarlak uçlu). İsteğe bağlı kesikli çizim
    /// (yolun ortasındaki taş çizgisi): yay uzunluğuna göre <c>dash</c> dolu, <c>gap</c> boş. Etkileşim yok.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TrailGraphic : MaskableGraphic
    {
        private readonly List<Vector2> _points = new List<Vector2>();
        private float _width = 80f, _dash, _gap;

        public void SetPath(IList<Vector2> points, float width, float dash = 0f, float gap = 0f)
        {
            _points.Clear();
            _points.AddRange(points);
            _width = width;
            _dash = dash;
            _gap = gap;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 2) return;
            Color32 c = color;
            if (_dash > 0f) { Dashes(vh, c); return; }
            float h = _width * 0.5f;
            for (int i = 0; i < _points.Count; i++)
            {
                var n = Normal(i) * h;
                vh.AddVert(_points[i] + n, c, Vector2.zero);
                vh.AddVert(_points[i] - n, c, Vector2.zero);
                if (i > 0)
                {
                    int k = i * 2;
                    vh.AddTriangle(k - 2, k - 1, k);
                    vh.AddTriangle(k - 1, k + 1, k);
                }
            }
            Cap(vh, _points[0], h, c);
            Cap(vh, _points[_points.Count - 1], h, c);
        }

        private Vector2 Normal(int i)
        {
            Vector2 a = _points[Mathf.Max(0, i - 1)], b = _points[Mathf.Min(_points.Count - 1, i + 1)];
            var dir = (b - a).normalized;
            return new Vector2(-dir.y, dir.x);
        }

        /// <summary>Kesikli çizgi: her dolu parça yuvarlak uçlu kısa bir hap (taş görünümü).</summary>
        private void Dashes(VertexHelper vh, Color32 c)
        {
            float h = _width * 0.5f, period = _dash + _gap, s = 0f;
            _dashLen = _dash;
            for (int i = 1; i < _points.Count; i++)
            {
                Vector2 a = _points[i - 1], b = _points[i];
                float len = Vector2.Distance(a, b);
                if (len < 1e-4f) continue;
                float t0 = 0f;
                while (t0 < len)
                {
                    float phase = (s + t0) % period;
                    if (phase < _dash)
                    {
                        float run = Mathf.Min(_dash - phase, len - t0);
                        var p0 = Vector2.Lerp(a, b, t0 / len);
                        var p1 = Vector2.Lerp(a, b, (t0 + run) / len);
                        Capsule(vh, p0, p1, h, c);
                        t0 += run;
                    }
                    else t0 += Mathf.Min(period - phase, len - t0);
                }
                s += len;
            }
        }

        private static float _dashLen;

        private static void Capsule(VertexHelper vh, Vector2 p0, Vector2 p1, float r, Color32 c)
        {
            var d = p1 - p0;
            if (d.sqrMagnitude < 1e-6f) { Cap(vh, p0, r, c); return; }
            var n = new Vector2(-d.y, d.x).normalized * r;
            int k = vh.currentVertCount;
            vh.AddVert(p0 + n, c, Vector2.zero);
            vh.AddVert(p0 - n, c, Vector2.zero);
            vh.AddVert(p1 + n, c, Vector2.zero);
            vh.AddVert(p1 - n, c, Vector2.zero);
            vh.AddTriangle(k, k + 1, k + 2);
            vh.AddTriangle(k + 1, k + 3, k + 2);
            // Kalın şeritte düz kesik (çizgili şerit); ince taş çizgisinde yuvarlak uç.
            if (r * 2f > _dashLen) return;
            Cap(vh, p0, r, c, 10);
            Cap(vh, p1, r, c, 10);
        }

        private static void Cap(VertexHelper vh, Vector2 p, float r, Color32 c, int seg = 16)
        {
            int start = vh.currentVertCount;
            vh.AddVert(p, c, Vector2.zero);
            for (int s = 0; s <= seg; s++)
            {
                float a = s * Mathf.PI * 2f / seg;
                vh.AddVert(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, c, Vector2.zero);
                if (s > 0) vh.AddTriangle(start, start + s, start + s + 1);
            }
        }
    }
}
