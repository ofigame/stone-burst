# Bölge simgesi hareket maskeleri (2026-11-20; kullanıcı: "yeşil vadi objesine su akma efekti olsun, kum vadisinde de ağaç
# yapraklarına hareket katalım ve su hareketli olsun"). Her simge için tam çözünürlüklü maske:
#   R = su (mavi-camgöbeği; akış + parıltı + kırılma), G = salınan yaprak (yalnız çöl simgesindeki palmiye tacı).
# Çıktı: Assets/KatKat/Resources/PremiumV6/ui/region-<ad>-motion.png; kontrol görseli Logs/region_motion/<ad>-check.png.
# Kullanım: powershell -ExecutionPolicy Bypass -File tools/region_icon_masks.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dir = Join-Path $root 'Assets\KatKat\Resources\PremiumV6\ui'
$logDir = Join-Path $root 'Logs\region_motion'
New-Item -ItemType Directory -Force $logDir | Out-Null

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class RegionMask
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

    // leafBox: palmiye tacının kaba dikdörtgeni (0–1, üstten); dışındaki yeşil salınmaz.
    public static void Build(string src, string outPng, string check, bool leaves, float lx0, float ly0, float lx1, float ly1, float wy0)
    {
        int W, H; byte[] px; int stride;
        using (var bmp = new Bitmap(src))
        {
            W = bmp.Width; H = bmp.Height;
            var d = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            stride = d.Stride; px = new byte[stride * H]; Marshal.Copy(d.Scan0, px, 0, px.Length); bmp.UnlockBits(d);
        }
        var water = new float[W * H]; var leaf = new float[W * H];
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            int i = y * stride + x * 4;
            int b = px[i], g = px[i + 1], r = px[i + 2], a = px[i + 3];
            if (a < 40) continue;
            float h, s, v; Hsv(r, g, b, out h, out s, out v);
            // Su: camgöbeği–mavi, doygun ve açık (fenerin turkuaz gövdesi yalnız fener simgesinde; orada su da kullanılmıyor).
            float wv = Ss(0.46f, 0.50f, h) * (1f - Ss(0.60f, 0.64f, h)) * Ss(0.30f, 0.45f, s) * Ss(0.45f, 0.6f, v);
            water[y * W + x] = (y / (float)H >= wy0 ? wv : 0f) * (a / 255f);
            if (leaves)
            {
                float fx = x / (float)W, fy = y / (float)H;
                bool inBox = fx >= lx0 && fx <= lx1 && fy >= ly0 && fy <= ly1;
                float lv = inBox ? Ss(0.17f, 0.22f, h) * (1f - Ss(0.42f, 0.46f, h)) * Ss(0.30f, 0.45f, s) * Ss(0.25f, 0.4f, v) : 0f;
                leaf[y * W + x] = lv * (a / 255f);
            }
        }
        water = Box(water, W, H, 2);
        // Yaprak maskesi geniş bulanık: salınan bölge yumuşak sınırlı, kenarlar yırtılmaz.
        if (leaves) leaf = Box(Box(leaf, W, H, 6), W, H, 6);
        using (var o = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        using (var c = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        {
            var od = o.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var cd = c.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var ob = new byte[od.Stride * H]; var cb = new byte[cd.Stride * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                byte R = (byte)Math.Min(255, water[y * W + x] * 255f * 1.4f), G = (byte)Math.Min(255, leaf[y * W + x] * 255f * 1.6f);
                int k = y * od.Stride + x * 4;
                ob[k] = 0; ob[k + 1] = G; ob[k + 2] = R; ob[k + 3] = 255;
                int s = y * stride + x * 4;
                // Kontrol: gri görsel üstüne kırmızı su, yeşil yaprak.
                byte gray = (byte)((px[s] + px[s + 1] + px[s + 2]) / 3 * px[s + 3] / 255 / 2);
                cb[k] = gray; cb[k + 1] = (byte)Math.Max(gray, G); cb[k + 2] = (byte)Math.Max(gray, R); cb[k + 3] = 255;
            }
            Marshal.Copy(ob, 0, od.Scan0, ob.Length); o.UnlockBits(od);
            Marshal.Copy(cb, 0, cd.Scan0, cb.Length); c.UnlockBits(cd);
            o.Save(outPng, ImageFormat.Png);
            c.Save(check, ImageFormat.Png);
        }
    }
}
'@

# Çöl: palmiye tacı sol üstte. Bahçe: yalnız su (çeşme ve dere). Fener: yalnız su (zemin gölü); ışık C# tarafında.
$jobs = @(
    @{ n = 'desert'; leaves = $true;  box = @(0.0, 0.0, 0.62, 0.42); wy = 0.0 },
    @{ n = 'garden'; leaves = $false; box = @(0, 0, 0, 0); wy = 0.0 },
    @{ n = 'coast';  leaves = $false; box = @(0, 0, 0, 0); wy = 0.84 }
)
foreach ($j in $jobs)
{
    $src = Join-Path $dir ("region-" + $j.n + ".png")
    $out = Join-Path $dir ("region-" + $j.n + "-motion.png")
    $chk = Join-Path $logDir ($j.n + "-check.png")
    [RegionMask]::Build($src, $out, $chk, $j.leaves, $j.box[0], $j.box[1], $j.box[2], $j.box[3], $j.wy)
    Write-Host "maske: $out"
}
