using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// VR2 tahta iç alanı (docs/vr2/CLAUDE_CODE.md "Tahta iç rengi ve derinliği", docs/vr2/cerceveler/frame.mjs):
    /// bölümün mevcut etkin hücre maskesi aynen kullanılır; köşeler Vr2.CornerCut (belge 0,22; kullanıcı isteğiyle 0,10) hücre yuvarlanır (dış ve iç
    /// köşe). İç alan tek renk (temanın açık tonu); derinlik yalnız maskenin içine kırpılan iç gölgeden. Belge tek katman
    /// (#101C27 %76, 0,30/0,085/0,10 hücre) tarif eder; kullanıcı istekleriyle iki katmanlı, kısa, tema renginde gölge.
    /// Dış çerçeve, parlak kenar, dış gölge yok; maske dışı tamamen saydam (pasif hücre ve boşlukta arka plan görünür).
    /// Hesap piksel ızgarasında tam Öklid uzaklık dönüşümüyle (Felzenszwalb) yapılır; kural/dokunma alanı etkilenmez.
    /// </summary>
    public static class Vr2Floor
    {
        /// <summary>
        /// Doku pikselleri (Unity sırası: alt satır önce). <paramref name="inShape"/> satır 0 üstte, w×h hücre;
        /// <paramref name="res"/> hücre başına piksel; <paramref name="pad"/> her yanda boş piksel.
        /// </summary>
        public static Color32[] Bake(bool[] inShape, int w, int h, int res, int pad, Color floor, out int tw, out int th) =>
            Bake(inShape, w, h, res, pad, floor, KatKat.UI.Vr2.InnerShadow, out tw, out th);

        /// <param name="dark">Gölge rengi (VR2: temanın paletteki koyu tonu, <see cref="KatKat.UI.Vr2.ShadowTint"/>).</param>
        public static Color32[] Bake(bool[] inShape, int w, int h, int res, int pad, Color floor, Color dark, out int tw, out int th)
        {
            tw = w * res + 2 * pad;
            th = h * res + 2 * pad;
            int n = tw * th;
            // Görüntü düzeni: satır 0 üstte (y aşağı), contour.mjs ile aynı yön.
            var mask = new bool[n];
            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                int cx = Mathf.FloorToInt((x - pad) / (float)res), cy = Mathf.FloorToInt((y - pad) / (float)res);
                mask[y * tw + x] = x >= pad && y >= pad && cx < w && cy < h && inShape[cy * w + cx];
            }

            float r = KatKat.UI.Vr2.CornerCut * res;
            // Kapama (iç köşeler yuvarlanır) önce, asıl maskede: genişlet r, daralt r. Tek hücrelik boşluk açık kalır.
            var d = Edt(mask, tw, th, false);
            var tmp = new bool[n];
            for (int i = 0; i < n; i++) tmp[i] = d[i] <= r;
            d = Edt(tmp, tw, th, true);
            for (int i = 0; i < n; i++) tmp[i] = d[i] > r;
            // Kullanıcı 2026-11-14: "tahtanın borderlarından içe doğru boşluğu olmalı" / "border ile iç boşluğu
            // arttıralım" / "şuankinin 2 katı olsun" → iç alan her yana Vr2.Inset kadar taşar (taşlar kenara değmez).
            // Dar boşluk (hiçbir 2×2 boş hücre bloğuna girmeyen pasif hücre: tek hücrelik delik, tek hücre genişliğinde
            // oyuk) kapanmasın diye orada pay Vr2.NarrowInset ile sınırlı → en az 1 − 2×0,28 = 0,44 hücre açıklık kalır
            // ve arka plan görünür (belge: "pasif hücreler ve iç boşluklar arka planı gösterir").
            float inset = KatKat.UI.Vr2.Inset * res, narrow = Mathf.Min(KatKat.UI.Vr2.Inset, KatKat.UI.Vr2.NarrowInset) * res;
            bool Free(int cx, int cy) => cx < 0 || cy < 0 || cx >= w || cy >= h || !inShape[cy * w + cx];
            var narrowCell = new bool[w * h];
            for (int cy = 0; cy < h; cy++)
            for (int cx = 0; cx < w; cx++)
            {
                if (!Free(cx, cy)) continue;
                bool wide = false;
                for (int oy = -1; oy <= 0 && !wide; oy++)
                for (int ox = -1; ox <= 0 && !wide; ox++)
                    wide = Free(cx + ox, cy + oy) && Free(cx + ox + 1, cy + oy) && Free(cx + ox, cy + oy + 1) && Free(cx + ox + 1, cy + oy + 1);
                narrowCell[cy * w + cx] = !wide;
            }
            // Kullanıcı 2026-11-14: "yuvarlama çok az olsun" → pay KARE genişletmeyle eklenir (Öklid genişletme dış
            // köşeyi pay kadar yuvarlıyordu). Kare genişletme iç köşelerdeki r yayını korur, dış köşeleri keskin bırakır;
            // dış köşeler aşağıdaki açmayla yalnız r (Vr2.CornerCut) kadar yuvarlanır.
            var full = SquareDilate(tmp, tw, th, Mathf.RoundToInt(inset));
            var lite = SquareDilate(tmp, tw, th, Mathf.RoundToInt(narrow));
            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                int i = y * tw + x;
                int cx = Mathf.FloorToInt((x - pad) / (float)res), cy = Mathf.FloorToInt((y - pad) / (float)res);
                bool lim = cx >= 0 && cy >= 0 && cx < w && cy < h && narrowCell[cy * w + cx];
                tmp[i] = lim ? lite[i] : full[i];
            }
            // Açma (dış köşeler yuvarlanır): daralt r, genişlet r. Son uzaklık kenar yumuşatması için de kullanılır.
            d = Edt(tmp, tw, th, true);
            var core = new bool[n];
            for (int i = 0; i < n; i++) core[i] = d[i] > r;
            var dCore = Edt(core, tw, th, false);
            var alpha = new float[n];
            var shape = new bool[n];
            for (int i = 0; i < n; i++)
            {
                alpha[i] = Mathf.Clamp01(r - dCore[i] + 0.5f);
                shape[i] = alpha[i] >= 0.5f;
            }

            // İç gölge (kullanıcı 2026-11-15: "iç gölge çok uyumsuz … daha kısa ve daha az belirgin olmalı ama derinliği de
            // yansıtmalı"): iki katman, ikisi de sınır bandının aşağı kaydırılıp bulanıklaştırılmasıyla (frame.mjs yöntemi)
            // ve yalnız iç alana kırpılır. Temas katmanı: kenarın dibinde ince, keskin koyuluk (derinlik buradan). Yumuşak
            // katman: onun altında kısa, hafif geçiş. Renk temanın kendi koyu tonu (tek lacivert değil) → zeminle uyumlu.
            int TW = tw, TH = th; // yerel fonksiyonlar out parametresi kullanamaz
            var dIn = Edt(shape, tw, th, true);   // içeride: dışarıya uzaklık
            var dOut = Edt(shape, tw, th, false); // dışarıda: şekle uzaklık
            float[] Band(float strokeCells, float sigmaCells)
            {
                float half = strokeCells * res * 0.5f;
                var s = new float[n];
                for (int i = 0; i < n; i++) s[i] = Mathf.Clamp01(half - (shape[i] ? dIn[i] : dOut[i]) + 0.5f);
                Blur(s, TW, TH, sigmaCells * res);
                return s;
            }
            var contact = Band(KatKat.UI.Vr2.ContactStroke, KatKat.UI.Vr2.ContactSigma);
            var soft = Band(KatKat.UI.Vr2.SoftStroke, KatKat.UI.Vr2.SoftSigma);
            // Gölge kaynağı aşağı kayar → p noktası p − ofset'ten okur (doğrusal ara değer).
            float Sample(float[] s, int x, int y, float offCells)
            {
                float sy = y - offCells * res;
                int y0 = Mathf.FloorToInt(sy);
                float fy = sy - y0;
                float a0 = y0 >= 0 && y0 < TH ? s[y0 * TW + x] : 0f, a1 = y0 + 1 >= 0 && y0 + 1 < TH ? s[(y0 + 1) * TW + x] : 0f;
                return Mathf.Lerp(a0, a1, fy);
            }
            float oc = KatKat.UI.Vr2.ContactOpacity, os = KatKat.UI.Vr2.SoftOpacity;
            var px = new Color32[n];
            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                int i = y * tw + x;
                float a = alpha[i];
                if (a <= 0f) { px[(th - 1 - y) * tw + x] = new Color32(0, 0, 0, 0); continue; }
                float c1 = oc * Sample(contact, x, y, KatKat.UI.Vr2.ContactOffset);
                float c2 = os * Sample(soft, x, y, KatKat.UI.Vr2.SoftOffset);
                float sh = 1f - (1f - c1) * (1f - c2);
                var c = Color.Lerp(floor, dark, sh);
                c.a = a;
                px[(th - 1 - y) * tw + x] = c;
            }
            return px;
        }

        /// <summary>Kare (Chebyshev) genişletme: her pikselin ±k kare komşuluğunda "true" varsa true. Satır sonra sütun.</summary>
        public static bool[] SquareDilate(bool[] src, int w, int h, int k)
        {
            if (k <= 0) return (bool[])src.Clone();
            var row = new bool[src.Length];
            for (int y = 0; y < h; y++)
            {
                int last = -1_000_000;
                for (int x = 0; x < w; x++) { if (src[y * w + x]) last = x; row[y * w + x] = x - last <= k; }
                last = 1_000_000;
                for (int x = w - 1; x >= 0; x--) { if (src[y * w + x]) last = x; if (last - x <= k) row[y * w + x] = true; }
            }
            var res = new bool[src.Length];
            for (int x = 0; x < w; x++)
            {
                int last = -1_000_000;
                for (int y = 0; y < h; y++) { if (row[y * w + x]) last = y; res[y * w + x] = y - last <= k; }
                last = 1_000_000;
                for (int y = h - 1; y >= 0; y--) { if (row[y * w + x]) last = y; if (last - y <= k) res[y * w + x] = true; }
            }
            return res;
        }

        /// <summary>
        /// Tam Öklid uzaklık (piksel merkezleri): <paramref name="invert"/> false → her pikselin en yakın "true" pikseline,
        /// true → en yakın "false" pikseline uzaklığı. Hiç hedef yoksa çok büyük değer.
        /// </summary>
        public static float[] Edt(bool[] src, int w, int h, bool invert)
        {
            const double Inf = 1e20;
            int n = w * h;
            var f = new double[n];
            for (int i = 0; i < n; i++) f[i] = src[i] != invert ? 0.0 : Inf;
            int m = Mathf.Max(w, h);
            var line = new double[m];
            var outLine = new double[m];
            var v = new int[m];
            var z = new double[m + 1];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++) line[y] = f[y * w + x];
                Dt1(line, outLine, h, v, z);
                for (int y = 0; y < h; y++) f[y * w + x] = outLine[y];
            }
            var res = new float[n];
            for (int y = 0; y < h; y++)
            {
                System.Array.Copy(f, y * w, line, 0, w);
                Dt1(line, outLine, w, v, z);
                for (int x = 0; x < w; x++) res[y * w + x] = (float)System.Math.Sqrt(System.Math.Min(outLine[x], 1e18));
            }
            return res;
        }

        /// <summary>Felzenszwalb–Huttenlocher 1B kare uzaklık dönüşümü (alt zarf).</summary>
        private static void Dt1(double[] f, double[] d, int n, int[] v, double[] z)
        {
            const double Inf = double.PositiveInfinity;
            int k = 0;
            v[0] = 0;
            z[0] = -Inf;
            z[1] = Inf;
            for (int q = 1; q < n; q++)
            {
                double s = Sect(f, q, v[k]);
                while (s <= z[k]) { k--; s = Sect(f, q, v[k]); }
                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = Inf;
            }
            k = 0;
            for (int q = 0; q < n; q++)
            {
                while (z[k + 1] < q) k++;
                int p = v[k];
                d[q] = (double)(q - p) * (q - p) + f[p];
            }
        }

        private static double Sect(double[] f, int q, int p) => ((f[q] + (double)q * q) - (f[p] + (double)p * p)) / (2.0 * q - 2.0 * p);

        /// <summary>Ayrılabilir Gauss bulanıklığı (σ piksel, 3σ çekirdek), yerinde; kenar dışı 0.</summary>
        public static void Blur(float[] a, int w, int h, float sigma)
        {
            if (sigma < 0.3f) return;
            int rad = Mathf.CeilToInt(sigma * 3f);
            var k = new float[2 * rad + 1];
            float sum = 0f;
            for (int i = -rad; i <= rad; i++) { k[i + rad] = Mathf.Exp(-(i * i) / (2f * sigma * sigma)); sum += k[i + rad]; }
            for (int i = 0; i < k.Length; i++) k[i] /= sum;
            var t = new float[a.Length];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float acc = 0f;
                for (int i = -rad; i <= rad; i++)
                {
                    int xx = x + i;
                    if (xx >= 0 && xx < w) acc += a[y * w + xx] * k[i + rad];
                }
                t[y * w + x] = acc;
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float acc = 0f;
                for (int i = -rad; i <= rad; i++)
                {
                    int yy = y + i;
                    if (yy >= 0 && yy < h) acc += t[yy * w + x] * k[i + rad];
                }
                a[y * w + x] = acc;
            }
        }
    }
}
