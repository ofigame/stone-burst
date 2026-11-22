using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

public static class RoadTrace3
{
    static bool IsRoad(byte r, byte g, byte b)
    {
        return r >= 236 && g >= 188 && g <= 232 && b >= 118 && b <= 190 && r - g >= 18 && r - g <= 70 && g - b >= 28 && g - b <= 95;
    }

    /// <summary>Bir parçanın her satırı için yolun orta x'i (piksel); bulunamazsa -1.</summary>
    static float[] Centers(string path, out int W, out int H)
    {
        using (var src = new Bitmap(path))
        using (var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, src.Width, src.Height);
            W = bmp.Width; H = bmp.Height;
            var d = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new byte[d.Stride * H];
            Marshal.Copy(d.Scan0, px, 0, px.Length);
            bmp.UnlockBits(d);
            var res = new float[H];
            float prev = W * 0.5f;
            var mask = new bool[W];
            for (int y = 0; y < H; y++)
            {
                // Satır maskesi (±2 satır oyu: dokudaki taş çizgileri boşluk açmasın).
                for (int x = 0; x < W; x++)
                {
                    int votes = 0;
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int yy = Math.Min(H - 1, Math.Max(0, y + dy));
                        int o = yy * d.Stride + x * 4;
                        if (IsRoad(px[o + 2], px[o + 1], px[o])) votes++;
                    }
                    mask[x] = votes >= 2;
                }
                // 24 px'e kadar boşlukları kapat.
                int last = -1000;
                for (int x = 0; x < W; x++)
                {
                    if (!mask[x]) continue;
                    if (x - last > 1 && x - last <= 24) for (int k = last + 1; k < x; k++) mask[k] = true;
                    last = x;
                }
                // Koşular: en az 70 px; önceki merkeze en yakın olan.
                float best = -1, bestDist = float.MaxValue;
                int s = -1;
                for (int x = 0; x <= W; x++)
                {
                    bool m = x < W && mask[x];
                    if (m && s < 0) s = x;
                    if (!m && s >= 0)
                    {
                        int len = x - s;
                        if (len >= 70 && len <= 420)
                        {
                            float c = (s + x - 1) * 0.5f;
                            float dist = Math.Abs(c - prev);
                            if (dist < bestDist) { bestDist = dist; best = c; }
                        }
                        s = -1;
                    }
                }
                if (best >= 0 && bestDist < 60) { res[y] = best; prev = best; }
                else res[y] = -1;
            }
            return res;
        }
    }

    /// <summary>Üç parçayı yukarıdan aşağı birleştirir; boşlukları doğrusal doldurur, yumuşatır; Step satırda bir yazar.</summary>
    public static string Run(string p0, string p1, string p2, string outPath, int step)
    {
        var all = new List<float>();
        int W = 1080;
        foreach (var p in new[] { p0, p1, p2 })
        {
            int w, h;
            var c = Centers(p, out w, out h);
            W = w;
            // Köprü bölgeleri (parçanın üst/alt kenarı): yol köprünün ortasında; algı orada şaşar.
            int band = (int)(h * 0.09f);
            for (int k = 0; k < band; k++) { c[k] = -1; c[h - 1 - k] = -1; }
            c[0] = w * 0.5f; c[h - 1] = w * 0.5f;
            all.AddRange(c);
        }
        int n = all.Count;
        var a = all.ToArray();
        int missing = 0;
        // Boşluk doldurma.
        int i = 0;
        while (i < n)
        {
            if (a[i] >= 0) { i++; continue; }
            missing++;
            int j = i;
            while (j < n && a[j] < 0) j++;
            float l = i > 0 ? a[i - 1] : (j < n ? a[j] : W * 0.5f);
            float r = j < n ? a[j] : l;
            for (int k = i; k < j; k++) a[k] = l + (r - l) * (k - i + 1) / (float)(j - i + 1);
            i = j;
        }
        // Yumuşatma (±24 satır ortalama).
        var sm = new float[n];
        for (int k = 0; k < n; k++)
        {
            float sum = 0; int cnt = 0;
            for (int q = Math.Max(0, k - 24); q <= Math.Min(n - 1, k + 24); q++) { sum += a[q]; cnt++; }
            sm[k] = sum / cnt;
        }
        var sb = new StringBuilder();
        sb.Append(n).Append(' ').Append(step).Append('\n');
        for (int k = 0; k < n; k += step) sb.Append((sm[k] / W).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        System.IO.File.WriteAllText(outPath, sb.ToString());
        int missingRows = 0; foreach (var v in all) if (v < 0) missingRows++;
        return Math.Round(100.0 * missingRows / n, 1) + "% satır bulunamadı";
    }
}
