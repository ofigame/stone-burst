using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Ahşap kenarlı krem tepsi (kullanıcı 2026-11-17: örnek tahta görseli; "çizdireyim ben"; görseller
    /// docs/gorseller/tepsi). Tahtanın dış hattını izler: hücre birleşimi kare genişletilir (Gap + Rim), dış köşeler
    /// ConvexR, iç köşeler ConcaveR yuvarlanır. Kenar rengi dış hattan içe uzaklıkla kenar şeridinden (rim.png, satır 0 =
    /// dış kenar, ışık tam yukarıdan → her yönde aynı profil) örneklenir; şeridin altındaki iç gölge de oradan gelir.
    /// Sonra zemin dokusu (floor.png) ve her etkin hücrede yuva (socket.png). Kural/dokunma alanı etkilenmez.
    /// </summary>
    public static class TrayArt
    {
        public static bool Enabled = true;
        private static bool? _ready;
        private static Color32[] _rim, _floor, _socket;
        private static int _rw, _rh, _fw, _fh, _sw, _sh;

        /// <summary>Hücre kenarından kenarın iç çizgisine boşluk, kenar kalınlığı, köşe yarıçapları (hücre adımı).</summary>
        public const float Gap = 0.08f, Rim = 0.23f, ConvexR = 0.26f, ConcaveR = 0.14f; // kullanıcı 2026-11-17: "daha kalın olmalı çerçeve köşeleri"
        /// <summary>Hücre yuvası görselinin boyu / taş yüzü boyu (yuvanın çukuru taş yüzü kadar).</summary>
        public const float SocketOverFace = 1.12f;
        /// <summary>rim.png'de kenar kalınlığı (satır; kaynakta 318→460, yarı ölçek).</summary>
        private const float RimRows = 71f;
        /// <summary>Zemin dokusunun bir tekrarı kaç hücre.</summary>
        private const float FloorCells = 2f;

        public static bool Ready
        {
            get
            {
                if (!Enabled) return false;
                if (_ready == null) _ready = Load();
                return _ready.Value;
            }
        }

        private static bool Load()
        {
            var rim = Resources.Load<Texture2D>("TrayArt/rim");
            var floor = Resources.Load<Texture2D>("TrayArt/floor");
            var socket = Resources.Load<Texture2D>("TrayArt/socket");
            if (rim == null || floor == null || socket == null || !rim.isReadable || !floor.isReadable || !socket.isReadable)
            {
                Debug.LogWarning("[KatKat] Tepsi görselleri yok ya da okunamıyor (Resources/TrayArt).");
                return false;
            }
            _rim = rim.GetPixels32(); _rw = rim.width; _rh = rim.height;
            _floor = floor.GetPixels32(); _fw = floor.width; _fh = floor.height;
            _socket = socket.GetPixels32(); _sw = socket.width; _sh = socket.height;
            // Magenta anahtarından kalan pembe saçak: yarı saydam piksellerin rengi yuva dudağının kremi olur.
            for (int i = 0; i < _socket.Length; i++) if (_socket[i].a < 250) _socket[i] = new Color32(253, 239, 208, _socket[i].a);
            // Kullanıcı 2026-11-17: "tepsi iç zemini çok açık olmuş" → zemin, yuva ve şeridin kenar altı (iç gölge + zemin)
            // kısmı aynı sıcak tonla koyulaşır; ahşap kenar (satır < RimRows) değişmez.
            for (int i = 0; i < _floor.Length; i++) _floor[i] = Shade(_floor[i]);
            for (int i = 0; i < _socket.Length; i++) _socket[i] = Shade(_socket[i]);
            for (int y = 0; y < _rh; y++)
            {
                float k = Mathf.Clamp01((y - (RimRows - 2f)) / 4f); // kenar çizgisinde yumuşak geçiş
                if (k <= 0f) continue;
                int row = (_rh - 1 - y) * _rw;
                for (int x = 0; x < _rw; x++) _rim[row + x] = Color32.Lerp(_rim[row + x], Shade(_rim[row + x]), k);
            }
            return true;
        }

        // Kullanıcı 2026-11-17: "tepsi kenarlarının renkleri … arkaplan görselinin renk tonuna göre olsun"; "aynı ton olsun,
        // biraz koyu ve belirgin olsun arkaplanda kaybolmasın". Ahşap şeridin her pikseli HSV'de: ton = temanın tonu,
        // doygunluk özgün doygunlukla orantılı (hedef en az ToneMinSat; parlama gibi az doygun pikseller beyazımsı kalır),
        // parlaklık ToneValue ile koyulaşır. Damar, parlama ve gölge korunur. Kenar altı (iç gölge + zemin) değişmez.
        public const float ToneMinSat = 0.60f, ToneMaxSat = 0.90f, ToneValue = 0.68f;
        private static readonly System.Collections.Generic.Dictionary<Color32, Color32[]> _toned = new System.Collections.Generic.Dictionary<Color32, Color32[]>();

        public static Color32[] ToneRim(Color tone)
        {
            if (!Ready) return null;
            Color32 key = tone;
            if (_toned.TryGetValue(key, out var cached)) return cached;
            Color.RGBToHSV(tone, out float th, out float ts, out _);
            float targetSat = Mathf.Clamp(ts * 1.15f, ToneMinSat, ToneMaxSat);
            var dst = (Color32[])_rim.Clone();
            for (int y = 0; y < _rh; y++)
            {
                float k = 1f - Mathf.Clamp01((y - (RimRows - 2f)) / 4f); // yalnız ahşap satırları
                if (k <= 0f) continue;
                int row = (_rh - 1 - y) * _rw;
                for (int x = 0; x < _rw; x++)
                {
                    Color src = _rim[row + x];
                    Color.RGBToHSV(src, out _, out float s, out float v);
                    var c = Color.HSVToRGB(th, Mathf.Clamp01(s * targetSat / 0.95f), v * ToneValue);
                    dst[row + x] = Color32.Lerp(_rim[row + x], c, k);
                }
            }
            _toned[key] = dst;
            return dst;
        }

        /// <summary>İç zemin tonu (çarpan): krem → sıcak bej.</summary>
        public static readonly Color FloorShade = new Color(0.90f, 0.86f, 0.80f, 1f);

        private static Color32 Shade(Color32 c) =>
            new Color32((byte)(c.r * FloorShade.r), (byte)(c.g * FloorShade.g), (byte)(c.b * FloorShade.b), c.a);

        /// <summary>Tepsinin dış hattının hücre kenarından taşma payı (hücre adımı).</summary>
        public static float Reach => Gap + Rim;

        /// <summary>
        /// Doku pikselleri (Unity sırası: alt satır önce). <paramref name="inShape"/> satır 0 üstte; hücre resX×resY piksel
        /// (dünyada kare piksel için resY = resX·satırAdımı/adım); <paramref name="pad"/> her yanda boş piksel.
        /// <paramref name="socketDy"/>: yuva merkezinin hücre merkezinden aşağı kayması (satır adımı oranı);
        /// <paramref name="socketSize"/>: yuva görselinin boyu (hücre adımı oranı).
        /// </summary>
        public static Color32[] Bake(bool[] inShape, int w, int h, int resX, int resY, int pad, float socketDy, float socketSize, out int tw, out int th) =>
            Bake(inShape, w, h, resX, resY, pad, socketDy, socketSize, null, out tw, out th);

        /// <param name="rimTone">Kenarın tema tonu (null: özgün bal rengi ahşap). Bkz. <see cref="ToneRim"/>.</param>
        public static Color32[] Bake(bool[] inShape, int w, int h, int resX, int resY, int pad, float socketDy, float socketSize, Color? rimTone, out int tw, out int th)
        {
            tw = w * resX + 2 * pad;
            th = h * resY + 2 * pad;
            int n = tw * th;
            int TW = tw;
            // Şekil hesabı düşük çözünürlükte (Q kat seyrek; Öklid uzaklık dönüşümleri pahalı), sonra işaretli uzaklık
            // çift doğrusal büyütülür → yumuşak kenar, yuvarlak köşeler. Yüksek çözünürlükte yalnız renk örneklenir.
            const int Q = 4;
            int lw = (tw + Q - 1) / Q, lh = (th + Q - 1) / Q, ln = lw * lh;
            float lres = resX / (float)Q, lresY = resY / (float)Q, lpad = pad / (float)Q;
            var mask = new bool[ln];
            for (int y = 0; y < lh; y++)
            for (int x = 0; x < lw; x++)
            {
                float fx = (x + 0.5f - lpad) / lres, fy = (y + 0.5f - lpad) / lresY;
                int cx = Mathf.FloorToInt(fx), cy = Mathf.FloorToInt(fy);
                mask[y * lw + x] = fx >= 0f && fy >= 0f && cx < w && cy < h && inShape[cy * w + cx];
            }
            // Dış hat: kare genişletme (köşeler keskin) → açma (dış köşe yuvarlanır) → kapama (iç köşe yuvarlanır).
            var shape = Vr2Floor.SquareDilate(mask, lw, lh, Mathf.RoundToInt(Reach * lres));
            float rv = ConvexR * lres, rc = ConcaveR * lres;
            var d = Vr2Floor.Edt(shape, lw, lh, true);
            var tmp = new bool[ln];
            for (int i = 0; i < ln; i++) tmp[i] = d[i] > rv;
            d = Vr2Floor.Edt(tmp, lw, lh, false);
            for (int i = 0; i < ln; i++) shape[i] = d[i] <= rv;
            d = Vr2Floor.Edt(shape, lw, lh, false);
            for (int i = 0; i < ln; i++) tmp[i] = d[i] <= rc;
            d = Vr2Floor.Edt(tmp, lw, lh, true);
            for (int i = 0; i < ln; i++) shape[i] = d[i] > rc;
            var dIn = Vr2Floor.Edt(shape, lw, lh, true);
            var dOut = Vr2Floor.Edt(shape, lw, lh, false);
            var sd = new float[ln]; // içeride +, dışarıda − (düşük px)
            for (int i = 0; i < ln; i++) sd[i] = shape[i] ? dIn[i] - 0.5f : -Mathf.Min(dOut[i] - 0.5f, 8f);
            // İkili maskenin basamakları (±0,5 düşük px = ±2 yüksek px) köşelerde kertik yapıyordu → uzaklık alanı hafif
            // bulanıklaştırılır: düz kenar değişmez (doğrusal alan), yuvarlak köşeler pürüzsüzleşir.
            Vr2Floor.Blur(sd, lw, lh, 1.2f);

            float res = resX;
            var rim = rimTone.HasValue ? ToneRim(rimTone.Value) : _rim;
            float rowsPerPx = RimRows / (Rim * res);
            float floorPer = _fw / (FloorCells * res);
            int blend0 = _rh - 24;
            float vFloor = (_rh - 1) / rowsPerPx; // bu iç uzaklıktan sonra yalnız zemin
            var px = new Color32[n];
            var fxs = new int[tw];
            for (int x = 0; x < tw; x++) fxs[x] = ((int)(x * floorPer)) % _fw;
            for (int y = 0; y < th; y++)
            {
                float ly = (y + 0.5f) / Q - 0.5f;
                int y0 = Mathf.Clamp(Mathf.FloorToInt(ly), 0, lh - 1), y1 = Mathf.Min(y0 + 1, lh - 1);
                float fy = Mathf.Clamp01(ly - y0);
                int frow = (_fh - 1 - ((int)(y * floorPer)) % _fh) * _fw;
                int orow = (th - 1 - y) * tw;
                for (int x = 0; x < tw; x++)
                {
                    float lx = (x + 0.5f) / Q - 0.5f;
                    int x0 = lx < 0f ? 0 : (int)lx; if (x0 > lw - 1) x0 = lw - 1;
                    int x1 = x0 + 1 < lw ? x0 + 1 : lw - 1;
                    float fx = lx - x0; if (fx < 0f) fx = 0f; else if (fx > 1f) fx = 1f;
                    float a0 = sd[y0 * lw + x0], a1 = sd[y0 * lw + x1], b0 = sd[y1 * lw + x0], b1 = sd[y1 * lw + x1];
                    float s = ((a0 + (a1 - a0) * fx) * (1f - fy) + (b0 + (b1 - b0) * fx) * fy) * Q;
                    int o = orow + x;
                    if (s <= -0.5f) continue; // saydam (varsayılan 0)
                    if (s >= vFloor) { px[o] = _floor[frow + fxs[x]]; continue; } // zemin: tam opak
                    float v = s > 0f ? s * rowsPerPx : 0f;
                    // Uzunluk boyunca: x + y (yatay kenarda x, dikeyde y; köşede sürekli); ayna tekrar → ek yeri yok.
                    float u = Mathf.PingPong((x + y) * rowsPerPx, _rw - 1);
                    Color c = Sample(rim, _rw, _rh, u, v, false);
                    if (v > blend0) c = Color.Lerp(c, _floor[frow + fxs[x]], (v - blend0) / (_rh - 1 - blend0));
                    c.a = Mathf.Clamp01(s + 0.5f);
                    px[o] = c;
                }
            }

            // Hücre yuvaları (boş hücrede görünür; dolu hücrede taşın altında kalır). Yuva bir kez ölçeklenir.
            int ss = Mathf.RoundToInt(socketSize * res);
            var sock = new Color32[ss * ss];
            for (int sy = 0; sy < ss; sy++)
            for (int sx = 0; sx < ss; sx++)
                sock[sy * ss + sx] = Sample(_socket, _sw, _sh, (sx + 0.5f) * _sw / ss - 0.5f, (sy + 0.5f) * _sh / ss - 0.5f, false);
            for (int cy = 0; cy < h; cy++)
            for (int cx = 0; cx < w; cx++)
            {
                if (!inShape[cy * w + cx]) continue;
                float mx = pad + (cx + 0.5f) * resX, my = pad + (cy + 0.5f + socketDy) * resY;
                int x0 = Mathf.RoundToInt(mx - ss * 0.5f), y0 = Mathf.RoundToInt(my - ss * 0.5f);
                for (int sy = 0; sy < ss; sy++)
                {
                    int y = y0 + sy;
                    if (y < 0 || y >= th) continue;
                    int orow = (th - 1 - y) * TW;
                    for (int sx = 0; sx < ss; sx++)
                    {
                        int x = x0 + sx;
                        if (x < 0 || x >= tw) continue;
                        var s = sock[sy * ss + sx];
                        if (s.a == 0) continue;
                        int o = orow + x;
                        var b = px[o];
                        if (b.a == 0) continue;
                        int ka = s.a, kb = 255 - ka;
                        px[o] = new Color32((byte)((s.r * ka + b.r * kb) / 255), (byte)((s.g * ka + b.g * kb) / 255), (byte)((s.b * ka + b.b * kb) / 255), b.a);
                    }
                }
            }
            return px;
        }

        /// <summary>Çift doğrusal örnek; (u, v) görüntü pikseli, v aşağı (satır 0 üstte).</summary>
        private static Color Sample(Color32[] t, int w, int h, float u, float v, bool wrap)
        {
            int x0 = Mathf.FloorToInt(u), y0 = Mathf.FloorToInt(v);
            float fx = u - x0, fy = v - y0;
            Color P(int x, int y)
            {
                if (wrap) { x = ((x % w) + w) % w; y = ((y % h) + h) % h; }
                else { x = Mathf.Clamp(x, 0, w - 1); y = Mathf.Clamp(y, 0, h - 1); }
                return t[(h - 1 - y) * w + x];
            }
            return Color.Lerp(Color.Lerp(P(x0, y0), P(x0 + 1, y0), fx), Color.Lerp(P(x0, y0 + 1), P(x0 + 1, y0 + 1), fx), fy);
        }
    }
}
