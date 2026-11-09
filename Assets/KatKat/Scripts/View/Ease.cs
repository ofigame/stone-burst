using UnityEngine;

namespace KatKat.View
{
    public static class Ease
    {
        /// <summary>GLSL smoothstep(e0, e1, x). Mathf.SmoothStep farklı bir işlevdir (enterpolasyon).</summary>
        public static float Smooth(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }

        public static float OutCubic(float x) { x = Mathf.Clamp01(x); float a = 1f - x; return 1f - a * a * a; }
        public static float InOutSine(float x) => -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(x)) - 1f) * 0.5f;
        public static float InOutQuad(float x) { x = Mathf.Clamp01(x); return x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) * 0.5f; }
        public static float OutBack(float x) { x = Mathf.Clamp01(x); const float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f); }
    }
}
