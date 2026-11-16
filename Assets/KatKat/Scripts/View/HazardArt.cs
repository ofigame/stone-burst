using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Konsept pilotu görselleri: kodla çizilen yumuşak kenarlı şekiller (ayrı dosya yok; görsel kabul sonrası yerine çizim
    /// gelebilir). Her biri bir kez üretilir. Biçimler: disk, halka, yuvarlak kare çerçeve, damla, balon, kuzgun, yosun, çukur.
    /// </summary>
    public static class HazardArt
    {
        private const int N = 128;
        private static Sprite _disc, _ring, _frame, _drop, _balloon, _raven, _moss, _pit, _crack;

        public static Sprite Disc => _disc ? _disc : _disc = Make("hz-disc", (x, y) => Fill(Len(x, y) - 0.46f));
        public static Sprite Ring => _ring ? _ring : _ring = Make("hz-ring", (x, y) => Fill(Mathf.Abs(Len(x, y) - 0.40f) - 0.06f));
        public static Sprite Frame => _frame ? _frame : _frame = Make("hz-frame", (x, y) => Fill(Mathf.Abs(RoundBox(x, y, 0.44f, 0.12f)) - 0.035f));
        public static Sprite Pit => _pit ? _pit : _pit = Make("hz-pit", (x, y) => Fill(RoundBox(x, y, 0.46f, 0.12f)) * (0.75f + 0.25f * Mathf.Clamp01(Len(x, y) * 1.6f)));

        /// <summary>Çatlak çizgileri (çökecek hücre): üç kırık hat.</summary>
        public static Sprite Crack => _crack ? _crack : _crack = Make("hz-crack", (x, y) =>
        {
            float a = Seg(x, y, -0.36f, 0.30f, -0.05f, 0.02f), b = Seg(x, y, -0.05f, 0.02f, 0.10f, -0.32f), c = Seg(x, y, -0.05f, 0.02f, 0.36f, 0.16f);
            float d = Seg(x, y, 0.10f, -0.32f, 0.30f, -0.40f);
            return Fill(Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d)) - 0.022f);
        });

        /// <summary>Damla: alt daire + üst sivri uç.</summary>
        public static Sprite Drop => _drop ? _drop : _drop = Make("hz-drop", (x, y) =>
        {
            float circle = Len(x, y + 0.12f) - 0.28f;
            float tip = Mathf.Max(Mathf.Abs(x) * 1.35f + (y - 0.42f) * 0.62f, -(y + 0.12f));
            return Fill(Mathf.Min(circle, tip));
        });

        /// <summary>Balon: oval gövde, düğüm, ip (renk kodla verilir; ip koyu).</summary>
        public static Sprite Balloon => _balloon ? _balloon : _balloon = Make("hz-balloon", (x, y) =>
        {
            float body = Len(x / 0.80f, (y - 0.12f) / 1.0f) - 0.34f;
            float knot = Mathf.Max(Mathf.Abs(x) - 0.06f + (y + 0.24f) * 0.5f, -(y + 0.30f));
            float rope = Mathf.Max(Mathf.Abs(x + 0.04f * Mathf.Sin(y * 18f)) - 0.014f, Mathf.Max(y + 0.28f, -(y + 0.48f)));
            return Fill(Mathf.Min(Mathf.Min(body, knot), rope));
        });

        private static Sprite _balloonGoal;

        /// <summary>Hedef simgesi: kırmızı balon, beyaz parıltı, koyu ip (renkli; arayüzde beyazla çizilir).</summary>
        public static Sprite BalloonGoal
        {
            get
            {
                if (_balloonGoal) return _balloonGoal;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "hz-balloon-goal", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[N * N];
                for (int j = 0; j < N; j++)
                    for (int i = 0; i < N; i++)
                    {
                        float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f;
                        float body = Fill(Len(x / 0.80f, (y - 0.12f)) - 0.34f);
                        float knot = Fill(Mathf.Max(Mathf.Abs(x) - 0.06f + (y + 0.24f) * 0.5f, -(y + 0.30f)));
                        float rope = Fill(Mathf.Max(Mathf.Abs(x + 0.04f * Mathf.Sin(y * 18f)) - 0.016f, Mathf.Max(y + 0.28f, -(y + 0.48f))));
                        float shine = Fill(Len(x + 0.10f, y - 0.24f) - 0.06f);
                        var c = Color.Lerp(new Color(0.30f, 0.20f, 0.20f, rope), new Color(0.94f, 0.25f, 0.32f, 1f), Mathf.Max(body, knot));
                        c.a = Mathf.Max(Mathf.Max(body, knot), rope);
                        c = Color.Lerp(c, new Color(1f, 1f, 1f, c.a), shine * 0.8f);
                        px[j * N + i] = c;
                    }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return _balloonGoal = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            }
        }

        /// <summary>Kuzgun silueti: gövde, baş, gaga, kuyruk, kanat.</summary>
        public static Sprite Raven => _raven ? _raven : _raven = Make("hz-raven", (x, y) =>
        {
            float body = Len((x + 0.02f) / 1.25f, (y + 0.02f) / 0.9f) - 0.24f;
            float head = Len(x - 0.24f, y - 0.16f) - 0.13f;
            float beak = Mathf.Max(Mathf.Max(-(x - 0.32f), (x - 0.50f) * 0.9f + Mathf.Abs(y - 0.14f) * 2.6f - 0.02f), -1f);
            float tail = Mathf.Max(Mathf.Abs(y + 0.06f) - 0.10f - (-(x + 0.30f)) * 0.35f, Mathf.Max(x + 0.22f, -(x + 0.50f)));
            float wing = Len((x + 0.04f) / 1.4f, (y - 0.10f) / 0.55f) - 0.20f;
            float legs = Mathf.Min(Seg(x, y, -0.02f, -0.18f, -0.06f, -0.36f), Seg(x, y, 0.08f, -0.18f, 0.06f, -0.36f)) - 0.02f;
            return Fill(Mathf.Min(Mathf.Min(Mathf.Min(body, head), Mathf.Min(beak, tail)), Mathf.Min(wing, legs)));
        });

        /// <summary>Yosun: dalgalı kenarlı yumuşak leke.</summary>
        public static Sprite Moss => _moss ? _moss : _moss = Make("hz-moss", (x, y) =>
        {
            float ang = Mathf.Atan2(y, x);
            float r = 0.40f + 0.05f * Mathf.Sin(ang * 7f) + 0.03f * Mathf.Sin(ang * 13f + 1.3f);
            return Fill(Len(x, y) - r);
        });

        /// <summary>Raven göz noktası gibi küçük beyaz işaret için de disk kullanılır.</summary>
        private static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);

        private static float RoundBox(float x, float y, float half, float r)
        {
            float qx = Mathf.Abs(x) - half + r, qy = Mathf.Abs(y) - half + r;
            return Len(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay, wx = px - ax, wy = py - ay;
            float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy));
            return Len(wx - vx * t, wy - vy * t);
        }

        /// <summary>İşaretli uzaklıktan yumuşak kenarlı örtme (≈1,5 piksel).</summary>
        private static float Fill(float sd) => Mathf.Clamp01(0.5f - sd * N / 1.5f);

        private static Sprite Make(string name, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f;
                    px[j * N + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        }
    }
}
