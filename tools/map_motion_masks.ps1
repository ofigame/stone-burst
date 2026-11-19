# Yolculuk haritası hareket maskeleri (2026-11-19; kullanıcı: "yolculuk görselimizdeki su ağaç çicek gibi objelere hareket
# katabilir miyiz" → "evet bahçeden başla"). Her şerit parçası için yarım çözünürlüklü maske:
#   R = su (dalga + parıltı + akış), G = yaprak/ağaç/ot (rüzgârda salınım), B = çiçek (küçük salınım).
# Çıktı: Assets/KatKat/Resources/PremiumV6/map/<ad>-motion.png ve <ad>-motion.txt (su ve çiçek noktaları: kelebek/halka),
# kontrol görselleri Logs/map_motion/<ad>-check.jpg.
# Kullanım: powershell -ExecutionPolicy Bypass -File tools/map_motion_masks.ps1 [-Names garden-0,garden-1,garden-2]
param([string[]]$Names = @("garden-0", "garden-1", "garden-2", "coast-0", "coast-1", "coast-2", "desert-0", "desert-1", "desert-2"))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$mapDir = Join-Path $root 'Assets\KatKat\Resources\PremiumV6\map'
$logDir = Join-Path $root 'Logs\map_motion'
New-Item -ItemType Directory -Force $logDir | Out-Null

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class MapMotionMask
{
    static void Hsv(int r, int g, int b, out float h, out float s, out float v)
    {
        float rf = r / 255f, gf = g / 255f, bf = b / 255f;
        float mx = Math.Max(rf, Math.Max(gf, bf)), mn = Math.Min(rf, Math.Min(gf, bf));
        v = mx; float d = mx - mn; s = mx <= 0 ? 0 : d / mx;
        if (d <= 1e-5f) { h = 0; return; }
        if (mx == rf) h = ((gf - bf) / d) / 6f; else if (mx == gf) h = (2f + (bf - rf) / d) / 6f; else h = (4f + (rf - gf) / d) / 6f;
        if (h < 0) h += 1f;
    }
    static float Ss(float a, float b, float x) { float t = Math.Max(0f, Math.Min(1f, (x - a) / (b - a))); return t * t * (3 - 2 * t); }

    static float[] Box(float[] m, int w, int h, int r)
    {
        var tmp = new float[m.Length]; var o = new float[m.Length];
        for (int y = 0; y < h; y++) { float s = 0; int n = 0;
            for (int x = -r; x < w + r; x++) {
                int a = x + r; if (a < w) { s += m[y * w + a]; n++; }
                int b = x - r - 1; if (b >= 0) { s -= m[y * w + b]; n--; }
                if (x >= 0 && x < w) tmp[y * w + x] = s / Math.Max(1, n); } }
        for (int x = 0; x < w; x++) { float s = 0; int n = 0;
            for (int y = -r; y < h + r; y++) {
                int a = y + r; if (a < h) { s += tmp[a * w + x]; n++; }
                int b = y - r - 1; if (b >= 0) { s -= tmp[b * w + x]; n--; }
                if (y >= 0 && y < h) o[y * w + x] = s / Math.Max(1, n); } }
        return o;
    }

    public static string Build(string src, string outPng, string outTxt, string check, bool yellowFlowers)
    {
        int W, H; byte[] px;
        using (var bmp = new Bitmap(src))
        {
            W = bmp.Width; H = bmp.Height;
            var d = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            px = new byte[d.Stride * H]; Marshal.Copy(d.Scan0, px, 0, px.Length); bmp.UnlockBits(d);
        }
        int w = W / 2, h = H / 2;
        var lum = new float[w * h]; var hue = new float[w * h]; var sat = new float[w * h]; var val = new float[w * h];
        var bluish = new bool[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            int r = 0, g = 0, b = 0;
            for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 2; dx++)
            { int i = ((y * 2 + dy) * W + x * 2 + dx) * 4; b += px[i]; g += px[i + 1]; r += px[i + 2]; }
            r /= 4; g /= 4; b /= 4;
            float hh, ss, vv; Hsv(r, g, b, out hh, out ss, out vv);
            int k = y * w + x; hue[k] = hh; sat[k] = ss; val[k] = vv; lum[k] = (0.3f * r + 0.59f * g + 0.11f * b) / 255f;
            bluish[k] = b + 6 >= r;
        }
        // Yerel doku (parlaklık varyansı): yapraklı taç ve çalılar dokulu, çimen düz.
        var l2 = new float[lum.Length]; for (int k = 0; k < lum.Length; k++) l2[k] = lum[k] * lum[k];
        var m1 = Box(lum, w, h, 4); var m2 = Box(l2, w, h, 4);
        var water = new float[w * h]; var leaf = new float[w * h]; var flower = new float[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            int k = y * w + x;
            float hh = hue[k], ss = sat[k], vv = val[k];
            float var = Math.Max(0f, m2[k] - m1[k] * m1[k]);
            float tex = Ss(0.0012f, 0.0045f, var);
            // Su: camgöbeği/mavi; köpük ve şelale: açık, az doygun, mavimsi beyaz.
            float blue = Ss(0.44f, 0.47f, hh) * (1 - Ss(0.62f, 0.66f, hh)) * Ss(0.18f, 0.3f, ss) * Ss(0.35f, 0.5f, vv);
            float foam = Ss(0.82f, 0.92f, vv) * (1 - Ss(0.16f, 0.26f, ss)) * (bluish[k] ? 1f : 0f) * Ss(0.40f, 0.48f, hh) * (1 - Ss(0.66f, 0.7f, hh));
            water[k] = Math.Max(blue, foam);
            // Yaprak: yeşil + dokulu (çimen düz kalır); koyu yeşil gölgeler de taç sayılır.
            float green = Ss(0.15f, 0.19f, hh) * (1 - Ss(0.42f, 0.46f, hh)) * Ss(0.25f, 0.4f, ss);
            float darkGreen = green * (1 - Ss(0.45f, 0.6f, vv));
            leaf[k] = Math.Max(green * tex, darkGreen * 0.9f);
            // Çiçek: pembe/kırmızı/mor/sarı, çok doygun ve parlak (yol bej: az doygun; gövde/köprü kahve: koyu ya da turuncu).
            bool pinkPurple = hh > 0.72f || hh < 0.02f;
            bool yellow = yellowFlowers && hh > 0.11f && hh < 0.17f; // çölde kum sarısı çiçek sayılmaz
            float f = (pinkPurple ? Ss(0.3f, 0.45f, ss) * Ss(0.5f, 0.65f, vv) : 0f) + (yellow ? Ss(0.55f, 0.7f, ss) * Ss(0.75f, 0.88f, vv) : 0f);
            flower[k] = Math.Min(1f, f);
        }
        // Köprüler (üst ve alt kenarın ortası) hareket etmez.
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float fy = y / (float)h, fx = x / (float)w;
            if ((fy < 0.075f || fy > 0.925f) && fx > 0.3f && fx < 0.7f) { int k = y * w + x; leaf[k] = 0; flower[k] = 0; }
        }
        // Yolun kenar çizgisi yaprak sayılmasın: bej yolun 7 px yakınında yaprak yok.
        var path = new float[w * h];
        for (int k = 0; k < path.Length; k++) path[k] = (hue[k] > 0.06f && hue[k] < 0.15f && sat[k] > 0.12f && sat[k] < 0.45f && val[k] > 0.72f) ? 1f : 0f;
        path = Box(path, w, h, 7);
        for (int k = 0; k < leaf.Length; k++) if (path[k] > 0.08f) leaf[k] = 0f;
        // İnce çizgiler (yol kenarı, taş gölgesi) yaprak sayılmasın: genişçe ortalayıp eşik (açma benzeri), sonra yumuşat.
        leaf = Box(leaf, w, h, 4);
        for (int k = 0; k < leaf.Length; k++) leaf[k] = Ss(0.38f, 0.62f, leaf[k]);
        // Yoğun çiçek kümeleri (çiçekli ağaç tacı, lavanta tarhı) ağaç gibi salınsın: yaprak kanalına geçer.
        var dense = Box(flower, w, h, 10);
        for (int k = 0; k < flower.Length; k++) { float d = Ss(0.35f, 0.55f, dense[k]); leaf[k] = Math.Max(leaf[k], flower[k] * d); flower[k] *= 1 - d; }
        water = Box(water, w, h, 2); leaf = Box(leaf, w, h, 2); flower = Box(flower, w, h, 2);
        for (int k = 0; k < leaf.Length; k++) { leaf[k] = Math.Min(1f, leaf[k] * 1.4f); flower[k] = Math.Min(1f, flower[k] * 1.6f); water[k] = Math.Min(1f, water[k] * 1.2f); }

        using (var o = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        {
            var d = o.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var ob = new byte[d.Stride * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int k = y * w + x, i = y * d.Stride + x * 4;
                ob[i] = (byte)(flower[k] * 255); ob[i + 1] = (byte)(leaf[k] * 255); ob[i + 2] = (byte)(water[k] * 255); ob[i + 3] = 255;
            }
            Marshal.Copy(ob, 0, d.Scan0, ob.Length); o.UnlockBits(d);
            o.Save(outPng, ImageFormat.Png);
        }
        // Noktalar (0..1, sol alt köken — Unity uv): su halkaları ve kelebeklerin uğradığı çiçekler.
        var sb = new StringBuilder();
        var rng = new Random(src.Length);
        int nw = 0, nf = 0;
        for (int tries = 0; tries < 20000 && (nw < 14 || nf < 14); tries++)
        {
            int x = rng.Next(w), y = rng.Next(h), k = y * w + x;
            string uv = (x / (float)w).ToString("0.0000", CultureInfo.InvariantCulture) + " " + (1f - y / (float)h).ToString("0.0000", CultureInfo.InvariantCulture);
            if (nw < 14 && water[k] > 0.9f && y > h * 0.1f && y < h * 0.9f) { sb.AppendLine("w " + uv); nw++; }
            else if (nf < 14 && flower[k] > 0.7f) { sb.AppendLine("f " + uv); nf++; }
        }
        File.WriteAllText(outTxt, sb.ToString());
        // Kontrol: görselin üstüne maske renkleri (su mavi, yaprak yeşil, çiçek pembe).
        using (var bmp = new Bitmap(src))
        using (var c = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        using (var gfx = Graphics.FromImage(c))
        {
            gfx.DrawImage(bmp, 0, 0, w, h);
            var d = c.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var cb = new byte[d.Stride * h]; Marshal.Copy(d.Scan0, cb, 0, cb.Length);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int k = y * w + x, i = y * d.Stride + x * 4;
                float a = 0.65f;
                float B = cb[i], G = cb[i + 1], R = cb[i + 2];
                B = B * (1 - a * water[k]) + 255 * a * water[k];
                G = G * (1 - a * leaf[k]) + 255 * a * leaf[k]; R = R * (1 - a * leaf[k]);
                R = R * (1 - a * flower[k]) + 255 * a * flower[k];
                cb[i] = (byte)B; cb[i + 1] = (byte)G; cb[i + 2] = (byte)R;
            }
            Marshal.Copy(cb, 0, d.Scan0, cb.Length); c.UnlockBits(d);
            c.Save(check, ImageFormat.Jpeg);
        }
        return string.Format("{0}: {1}x{2}, su noktası {3}, çiçek noktası {4}", Path.GetFileName(src), w, h, nw, nf);
    }
}
'@

foreach ($n in ($Names -join "," -split ",")) {
    $src = Join-Path $mapDir "$n.png"
    [MapMotionMask]::Build($src, (Join-Path $mapDir "$n-motion.png"), (Join-Path $mapDir "$n-motion.txt"), (Join-Path $logDir "$n-check.jpg"), -not $n.StartsWith("desert"))
}
