# docs/v6 renkli görsellerini oyuna hazırlar → Assets/KatKat/Resources/PremiumV6/
#   stones.png   : 1024 atlas, 2x2 hücre (512), her taş 496 px + 8 px şeffaf pay (mip taşmasın).
#                  Hücre sırası LayerPalette.IconCell ile aynı (UV, y yukarı): mercan(0,0) turkuaz(1,0) inci(0,1) mor(1,1).
#   ice-solid/ice-cracked : buzun gövdesi yarı saydam yapılır (taş rengi alttan okunsun), parlak kenar opak kalır.
#   gold-roll, sponge-ring, envelope, clip : görünen sınıra kırpılır.
# Kullanım: powershell -File tools/ArtV6/prepare.ps1
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$src = Join-Path $root 'docs\gorseller'
$dst = Join-Path $root 'Assets\KatKat\Resources\PremiumV6'
New-Item -ItemType Directory -Force $dst | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class ArtV6
{
    static void Draw(Graphics g, Bitmap src, Rectangle from, Rectangle to)
    {
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingMode = CompositingMode.SourceCopy;
        using (var ia = new ImageAttributes())
        {
            ia.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(src, to, from.X, from.Y, from.Width, from.Height, GraphicsUnit.Pixel, ia);
        }
    }

    public static Bitmap Crop(string path, int x, int y, int w, int h, int outW, int outH)
    {
        using (var s = new Bitmap(path))
        {
            var b = new Bitmap(outW, outH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(b)) { g.Clear(Color.Transparent); Draw(g, s, new Rectangle(x, y, w, h), new Rectangle(0, 0, outW, outH)); }
            return b;
        }
    }

    public static void Atlas(string[] paths, int x, int y, int w, string outPath)
    {
        // paths: mercan, turkuaz, inci, mor. Görüntü koordinatı (y aşağı): mercan sol alt, turkuaz sağ alt, inci sol üst, mor sağ üst.
        var b = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(b))
        {
            g.Clear(Color.Transparent);
            for (int i = 0; i < 4; i++)
            {
                int cx = (i % 2) * 512, cy = (i / 2 == 0) ? 512 : 0;
                using (var s = new Bitmap(paths[i])) Draw(g, s, new Rectangle(x, y, w, w), new Rectangle(cx + 8, cy + 8, 496, 496));
            }
        }
        b.Save(outPath, ImageFormat.Png);
        b.Dispose();
    }

    /// <summary>
    /// Arka plan kaldırma kırıntılarını siler (ana nesneden kopuk küçük lekeler: alfa ≥ 24 bağlı bölgelerden en büyüğünün
    /// %1'inden küçük olanlar), görünen sınıra %2 payla kırpar, uzun kenarı en çok maxSide olacak şekilde küçültür.
    /// </summary>
    public static void CleanCrop(string path, string outPath, int maxSide)
    {
        using (var src = new Bitmap(path))
        {
            int w = src.Width, h = src.Height;
            var b = src.Clone(new Rectangle(0, 0, w, h), PixelFormat.Format32bppArgb);
            var d = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var px = new byte[d.Stride * h];
            Marshal.Copy(d.Scan0, px, 0, px.Length);
            var label = new int[w * h];
            var sizes = new System.Collections.Generic.List<int> { 0 };
            var stack = new System.Collections.Generic.Stack<int>();
            for (int i = 0; i < w * h; i++)
            {
                if (label[i] != 0 || px[(i / w) * d.Stride + (i % w) * 4 + 3] < 24) continue;
                int id = sizes.Count, count = 0;
                label[i] = id; stack.Push(i);
                while (stack.Count > 0)
                {
                    int p = stack.Pop(); count++;
                    int x = p % w, y = p / w;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int q = ny * w + nx;
                        if (label[q] != 0 || px[ny * d.Stride + nx * 4 + 3] < 24) continue;
                        label[q] = id; stack.Push(q);
                    }
                }
                sizes.Add(count);
            }
            int biggest = 0; foreach (var s in sizes) biggest = Math.Max(biggest, s);
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int i = 0; i < w * h; i++)
            {
                int o = (i / w) * d.Stride + (i % w) * 4;
                if (label[i] == 0 || sizes[label[i]] < biggest / 100) { if (label[i] != 0 || px[o + 3] < 24) px[o + 3] = 0; }
                if (px[o + 3] < 24) continue;
                int x = i % w, y = i / w;
                x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
            }
            // Ana nesneye bitişik olmayan soluk (alfa < 24) pikseller: yalnız bir bağlı bölgenin yanındaysa kalır.
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int o = y * d.Stride + x * 4;
                if (px[o + 3] == 0 || px[o + 3] >= 24) continue;
                bool near = false;
                for (int k = 0; k < 4 && !near; k++)
                {
                    int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (nx >= 0 && ny >= 0 && nx < w && ny < h && label[ny * w + nx] != 0 && sizes[label[ny * w + nx]] >= biggest / 100) near = true;
                }
                if (!near) px[o + 3] = 0;
            }
            Marshal.Copy(px, 0, d.Scan0, px.Length);
            b.UnlockBits(d);
            int pad = (int)(Math.Max(x1 - x0, y1 - y0) * 0.02f);
            x0 = Math.Max(0, x0 - pad); y0 = Math.Max(0, y0 - pad); x1 = Math.Min(w - 1, x1 + pad); y1 = Math.Min(h - 1, y1 + pad);
            int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
            float scale = Math.Min(1f, (float)maxSide / Math.Max(cw, ch));
            int ow = (int)Math.Round(cw * scale), oh = (int)Math.Round(ch * scale);
            using (var o2 = new Bitmap(ow, oh, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(o2)) { g.Clear(Color.Transparent); Draw(g, b, new Rectangle(x0, y0, cw, ch), new Rectangle(0, 0, ow, oh)); }
                o2.Save(outPath, ImageFormat.Png);
            }
            b.Dispose();
        }
    }

    // ---- Harita şeridi: üç parça alt alta, döngü ---------------------------------------------------------

    static byte[] Pixels(Bitmap b, out int stride)
    {
        var d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        stride = d.Stride;
        var px = new byte[d.Stride * b.Height];
        Marshal.Copy(d.Scan0, px, 0, px.Length);
        b.UnlockBits(d);
        return px;
    }

    /// <summary>Yol döşemesi ve kenar şeridi (açık bej-turuncu); köprü tahtası (mavi &lt; 95) ve açık kumsal (mavi &gt; 170) hariç.</summary>
    static bool IsRoadLoose(byte[] px, int o)
    {
        int bl = px[o], g = px[o + 1], r = px[o + 2];
        return r >= 225 && g >= 165 && g <= 236 && bl >= 95 && bl <= 170 && r - bl >= 65 && g < r;
    }

    static bool IsRoad(byte[] px, int o)
    {
        int bl = px[o], g = px[o + 1], r = px[o + 2];
        return r >= 236 && g >= 196 && g <= 235 && bl >= 118 && bl <= 168 && r - bl >= 70;
    }

    /// <summary>
    /// Parçaları (sırayla) W genişliğe ölçekler, her parçanın ilk O satırını bir öncekinin son O satırıyla
    /// yumuşakça karıştırır (döngü: ilk parça sonuncuyla) ve outPrefix-i.png olarak yazar (yükseklik H-O).
    /// Yolun orta çizgisini satır satır izleyip outPrefix-path.txt'ye yazar: ilk satır "periyot adım", sonra
    /// her adım için x (0..1). Kontrol görüntüsü: checkPath (yolun üstünde kırmızı çizgi, küçültülmüş).
    /// </summary>
    public static string MapStrip(string[] paths, string outPrefix, string checkPath, int W, int O)
    {
        int n = paths.Length;
        var parts = new Bitmap[n];
        // Ortak yükseklik: parçalar 1–2 px farklı olabilir; hepsi ilk parçanın oranına ölçeklenir.
        int H;
        using (var s0 = new Bitmap(paths[0])) H = (int)Math.Round(s0.Height * (double)W / s0.Width);
        for (int i = 0; i < n; i++)
        {
            using (var s = new Bitmap(paths[i]))
            {
                parts[i] = new Bitmap(W, H, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(parts[i])) Draw(g, s, new Rectangle(0, 0, s.Width, s.Height), new Rectangle(0, 0, W, H));
            }
        }
        int ph = H - O, P = ph * n;
        int stride;
        var src = new byte[n][];
        for (int i = 0; i < n; i++) src[i] = Pixels(parts[i], out stride);
        stride = W * 4;
        var strip = new byte[stride * P];
        for (int i = 0; i < n; i++)
        {
            var cur = src[i]; var prev = src[(i + n - 1) % n];
            for (int s = 0; s < ph; s++)
            {
                int dst = (i * ph + s) * stride;
                if (s < O)
                {
                    float w = (s + 0.5f) / O;
                    w = w * w * (3f - 2f * w);
                    int a = (ph + s) * stride, b = s * stride;
                    for (int k = 0; k < stride; k++) strip[dst + k] = (byte)Math.Round(prev[a + k] * (1f - w) + cur[b + k] * w);
                }
                else Buffer.BlockCopy(cur, s * stride, strip, dst, stride);
            }
        }
        for (int i = 0; i < n; i++)
        {
            using (var o = new Bitmap(W, ph, PixelFormat.Format32bppArgb))
            {
                var d = o.LockBits(new Rectangle(0, 0, W, ph), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(strip, i * ph * stride, d.Scan0, ph * stride);
                o.UnlockBits(d);
                o.Save(outPrefix + "-" + i + ".png", ImageFormat.Png);
            }
            parts[i].Dispose();
        }

        // Yol orta çizgisi (Viterbi): her satırda yol genişliğinde pencerenin "yol pikseli" oranı puandır; bütün
        // şerit boyunca puan toplamı en yüksek, satırdan satıra en çok ±12 px kayan tek eğri seçilir. Şeridin başı
        // ve sonu köprünün ortasına sabit (döngüde ek yeri köprüden geçer). Kumsal/iskele gibi kısa yanılmalar,
        // yolun tam genişliğini tutturamadıkları için seçilmez.
        const int ys = 2, xs = 4;
        int rows = P / ys, states = W / xs;
        int win = (int)(W * 0.17f) / xs;
        var score = new float[rows, states];
        var roadCol = new int[states];
        for (int r = 0; r < rows; r++)
        {
            int row = r * ys * stride;
            for (int s = 0; s < states; s++) roadCol[s] = IsRoadLoose(strip, row + (s * xs + xs / 2) * 4) ? 1 : 0;
            int acc = 0;
            for (int s = 0; s < states + win; s++)
            {
                if (s < states) acc += roadCol[s];
                if (s - win >= 0) acc -= roadCol[s - win];
                int center = s - win / 2;
                if (center >= 0 && center < states) score[r, center] = acc / (float)win;
            }
        }
        int bridge = (int)Math.Round(W * 0.497f / xs);
        // Köprü bölgeleri (her ek yerinin ~200 satır üstü, ~130 satır altı): çizgi köprünün ortasında kalır.
        for (int r = 0; r < rows; r++)
        {
            int y = r * ys, inPart = y % ph;
            bool onBridge = inPart < 130 || inPart > ph - 200;
            if (!onBridge) continue;
            for (int s = 0; s < states; s++) score[r, s] = s == bridge ? 5f : -5f;
        }
        const float lam = 0.03f;
        var best = new float[states]; var next = new float[states];
        var from = new short[rows, states];
        for (int s = 0; s < states; s++) best[s] = s == bridge ? 0f : -1e9f;
        for (int r = 0; r < rows; r++)
        {
            for (int s = 0; s < states; s++)
            {
                float m = -1e9f; short arg = (short)s;
                for (int d = -3; d <= 3; d++)
                {
                    int q = s + d;
                    if (q < 0 || q >= states) continue;
                    float v = best[q] - lam * Math.Abs(d);
                    if (v > m) { m = v; arg = (short)q; }
                }
                next[s] = m + score[r, s];
                from[r, s] = arg;
            }
            var t = best; best = next; next = t;
        }
        var cx = new float[P];
        int at = bridge;
        for (int r = rows - 1; r >= 0; r--)
        {
            for (int k = 0; k < ys; k++) if (r * ys + k < P) cx[r * ys + k] = at * xs + xs / 2f;
            at = from[r, at];
        }
        // Yumuşatma (döngüsel, 31 satır).
        var sm = new float[P];
        for (int y = 0; y < P; y++)
        {
            float sum = 0;
            for (int k = -15; k <= 15; k++) sum += cx[(y + k + P) % P];
            sm[y] = sum / 31f;
        }
        const int step = 4;
        var sb = new System.Text.StringBuilder();
        sb.Append(P).Append(' ').Append(step).Append('\n');
        for (int y = 0; y < P; y += step) sb.Append((sm[y] / W).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        System.IO.File.WriteAllText(outPrefix + "-path.txt", sb.ToString());

        // Kontrol görüntüsü: şerit 1/3 ölçek, kırmızı orta çizgi, mavi ek yerleri.
        int cw = W / 3, ch = P / 3;
        using (var full = new Bitmap(W, P, PixelFormat.Format32bppArgb))
        {
            var d = full.LockBits(new Rectangle(0, 0, W, P), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(strip, 0, d.Scan0, strip.Length);
            full.UnlockBits(d);
            using (var small = new Bitmap(cw, ch, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(small))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g.DrawImage(full, 0, 0, cw, ch);
                    using (var pen = new Pen(Color.Red, 3))
                        for (int y = 3; y < P; y += 3) g.DrawLine(pen, sm[y - 3] / 3f, (y - 3) / 3f, sm[y] / 3f, y / 3f);
                    using (var pen = new Pen(Color.Blue, 1))
                        for (int i = 0; i < n; i++) g.DrawLine(pen, 0, i * ph / 3f, cw, i * ph / 3f);
                }
                small.Save(checkPath, ImageFormat.Png);
            }
        }
        return P + " " + ph;
    }

    /// <summary>
    /// Kopuk kırıntıları (en büyük bağlı bölgenin %1'inden küçük, alfa ≥ 24) siler; maske = alfa &gt;= 24 bağlı bölgeler.
    /// Döner: temizlenmiş kopya ve görünür sınır (x0,y0,x1,y1).
    /// </summary>
    static Bitmap CleanCopy(string path, out Rectangle bounds)
    {
        var src = new Bitmap(path);
        int w = src.Width, h = src.Height;
        var b = src.Clone(new Rectangle(0, 0, w, h), PixelFormat.Format32bppArgb);
        src.Dispose();
        var d = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var px = new byte[d.Stride * h];
        Marshal.Copy(d.Scan0, px, 0, px.Length);
        var label = new int[w * h];
        var sizes = new System.Collections.Generic.List<int> { 0 };
        var stack = new System.Collections.Generic.Stack<int>();
        for (int i = 0; i < w * h; i++)
        {
            if (label[i] != 0 || px[(i / w) * d.Stride + (i % w) * 4 + 3] < 24) continue;
            int id = sizes.Count, count = 0;
            label[i] = id; stack.Push(i);
            while (stack.Count > 0)
            {
                int p = stack.Pop(); count++;
                int x = p % w, y = p / w;
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    int q = ny * w + nx;
                    if (label[q] != 0 || px[ny * d.Stride + nx * 4 + 3] < 24) continue;
                    label[q] = id; stack.Push(q);
                }
            }
            sizes.Add(count);
        }
        int biggest = 0; foreach (var s in sizes) biggest = Math.Max(biggest, s);
        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int i = 0; i < w * h; i++)
        {
            int o = (i / w) * d.Stride + (i % w) * 4;
            bool keep = label[i] != 0 && sizes[label[i]] >= biggest / 100;
            if (!keep) { px[o + 3] = 0; continue; }
            int x = i % w, y = i / w;
            x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
        }
        Marshal.Copy(px, 0, d.Scan0, px.Length);
        b.UnlockBits(d);
        bounds = Rectangle.FromLTRB(x0, y0, x1 + 1, y1 + 1);
        return b;
    }

    /// <summary>
    /// Maskot çifti: iki PNG'yi temizler ve ikisini de AYNI dikdörtgenle (görünür sınırların birleşimi + 8 px pay)
    /// kırpar ki yüz parçaları gövdeye birebir otursun. Döner: "x0 y0 w h" (kaynak piksel, y aşağı).
    /// </summary>
    public static string MascotPair(string fullPath, string bodyPath, string outFull, string outBody)
    {
        Rectangle rf, rb;
        using (var full = CleanCopy(fullPath, out rf))
        using (var body = CleanCopy(bodyPath, out rb))
        {
            var r = Rectangle.Union(rf, rb);
            r.Inflate(8, 8);
            r.Intersect(new Rectangle(0, 0, full.Width, full.Height));
            using (var a = full.Clone(r, PixelFormat.Format32bppArgb)) a.Save(outFull, ImageFormat.Png);
            using (var c = body.Clone(r, PixelFormat.Format32bppArgb)) c.Save(outBody, ImageFormat.Png);
            return r.X + " " + r.Y + " " + r.Width + " " + r.Height;
        }
    }

    /// <summary>Gövdeyi saydamlaştırır: koyu/orta tonlar minAlpha'ya iner, açık parlamalar opak kalır.</summary>
    public static void IceAlpha(Bitmap b, float minAlpha)
    {
        var r = new Rectangle(0, 0, b.Width, b.Height);
        var d = b.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var px = new byte[d.Stride * b.Height];
        Marshal.Copy(d.Scan0, px, 0, px.Length);
        for (int i = 0; i < px.Length; i += 4)
        {
            float bl = px[i] / 255f, gr = px[i + 1] / 255f, rd = px[i + 2] / 255f;
            float lum = rd * 0.3f + gr * 0.59f + bl * 0.11f;
            float t = Math.Max(0f, Math.Min(1f, (lum - 0.62f) / 0.33f));
            float k = minAlpha + (1f - minAlpha) * t * t;
            px[i + 3] = (byte)Math.Round(px[i + 3] * k);
        }
        Marshal.Copy(px, 0, d.Scan0, px.Length);
        b.UnlockBits(d);
    }
}
'@

# Kaynak klasörler (2026-11-06 düzeni): docs/gorseller/{taslar, mekanikler, maskot, harita, arayuz}.
$stoneDir = Join-Path $src 'taslar'
$mechDir = Join-Path $src 'mekanikler'
Write-Host "tas kaynagi: $stoneDir"
$stones = '01-mercan-daire','02-turkuaz-ucgen','03-inci-dalga','04-mor-elmas' | ForEach-Object { Join-Path $stoneDir "$_.png" }
[ArtV6]::Atlas($stones, 96, 96, 833, (Join-Path $dst 'stones.png'))

foreach ($p in @(@('05-buz-saglam','ice-solid'), @('06-buz-catlak','ice-cracked'))) {
    $b = [ArtV6]::Crop((Join-Path $mechDir "$($p[0]).png"), 96, 96, 833, 833, 512, 512)
    [ArtV6]::IceAlpha($b, 0.5)
    $b.Save((Join-Path $dst "$($p[1]).png"), [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
}
$crops = @(
    @('07-altin-rulo', 'gold-roll',   96,  96, 833, 833, 512, 512),
    @('08-sunger-halka','sponge-ring', 72,  72, 881, 881, 512, 512),
    @('09-zarf-kaplama','envelope',    72,  72, 881, 881, 512, 512),
    @('10-gumus-atas', 'clip',        572,  96, 376, 372, 376, 372)
)
foreach ($c in $crops) {
    $b = [ArtV6]::Crop((Join-Path $mechDir "$($c[0]).png"), $c[2], $c[3], $c[4], $c[5], $c[6], $c[7])
    $b.Save((Join-Path $dst "$($c[1]).png"), [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
}

# Ana ekran simgeleri (docs/gorseller/arayuz: region-*.png, map.png) → PremiumV6/ui/.
$uiSrc = Join-Path $src 'arayuz'
if (Test-Path $uiSrc) {
    New-Item -ItemType Directory -Force (Join-Path $dst 'ui') | Out-Null
    Get-ChildItem $uiSrc -Filter *.png | Where-Object { $_.BaseName -match '^(region-.*|map)$' } | ForEach-Object {
        [ArtV6]::CleanCrop($_.FullName, (Join-Path $dst "ui\$($_.Name)"), 512)
    }
}

# Ana sayfa logosu (docs/gorseller/arayuz/logo.png) → PremiumV6/ui/logo.png (kırıntılar silinir, görünen sınıra kırpılır).
$logoSrc = Join-Path $uiSrc 'logo.png'
if (Test-Path $logoSrc) {
    New-Item -ItemType Directory -Force (Join-Path $dst 'ui') | Out-Null
    [ArtV6]::CleanCrop($logoSrc, (Join-Path $dst 'ui\logo.png'), 1400)
}

# Kristal maskot (docs/Kristal-Maskot-Claude): tam karakter + temiz yüzlü gövde, aynı kırpımla →
# Resources/PremiumV4/mascot-crystal/mascot-full-v1.png, mascot-body-v1.png (Sprite, tek, mipmap kapalı, bilinear).
$mascotSrc = Join-Path $src 'maskot'
if (Test-Path (Join-Path $mascotSrc '01-onayli-maskot.png')) {
    $mDst = Join-Path $root 'Assets\KatKat\Resources\PremiumV4\mascot-crystal'
    New-Item -ItemType Directory -Force $mDst | Out-Null
    $crop = [ArtV6]::MascotPair((Join-Path $mascotSrc '01-onayli-maskot.png'), (Join-Path $mascotSrc '02-animasyon-govdesi.png'),
        (Join-Path $mDst 'mascot-full-v1.png'), (Join-Path $mDst 'mascot-body-v1.png'))
    Write-Host "maskot kirpimi (x y w h): $crop"
    $mt = Get-Content (Join-Path $root 'Assets\KatKat\Resources\PremiumV4\tiles\face-neutral.png.meta') -Raw
    $mt = $mt -replace '(?s)internalIDToNameTable:.*?externalObjects', "internalIDToNameTable: []`n  externalObjects"
    $mt = $mt -replace '(?s)    sprites:\r?\n    - serializedVersion: 2.*?\r?\n    outline: \[\]', "    sprites: []`n    outline: []"
    $mt = $mt -replace 'enableMipMap: 1', 'enableMipMap: 0' -replace 'filterMode: 2', 'filterMode: 1' -replace 'isReadable: 1', 'isReadable: 0'
    foreach ($f in 'mascot-full-v1.png','mascot-body-v1.png') {
        $meta = Join-Path $mDst "$f.meta"
        if (-not (Test-Path $meta)) { [IO.File]::WriteAllText($meta, ($mt -replace 'guid: [0-9a-f]{32}', "guid: $([guid]::NewGuid().ToString('N'))")) }
    }
    if (-not (Test-Path "$mDst.meta")) {
        [IO.File]::WriteAllText("$mDst.meta", "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
    }
}

# Harita şeridi (docs/gorseller/harita: sahil_a/b/c, bahce_a/b/c, col_a/b/c) → PremiumV6/map/<tema>-0..2.png + -path.txt.
$mapSrc = Join-Path $src 'harita'
if (Test-Path $mapSrc) {
    $mapDst = Join-Path $dst 'map'
    New-Item -ItemType Directory -Force $mapDst | Out-Null
    $all = Get-ChildItem $mapSrc -Filter *.png | Where-Object { $_.Name -notlike '_*' }
    $regions = @{
        'coast'  = $all | Where-Object { $_.BaseName -match '^sahil_[abc]$' } | Sort-Object BaseName
        'garden' = $all | Where-Object { $_.BaseName -match '^bahce_[abc]$' } | Sort-Object BaseName
        'desert' = $all | Where-Object { $_.BaseName -match '^col_[abc]$' } | Sort-Object BaseName
    }
    foreach ($id in 'coast','garden','desert') {
        $parts = @($regions[$id] | ForEach-Object { $_.FullName })
        if ($parts.Count -lt 1) { continue }
        $r = [ArtV6]::MapStrip([string[]]$parts, (Join-Path $mapDst $id), (Join-Path $mapSrc "_kontrol-$id.png"), 1080, 24)
        Write-Host "harita seridi $id ($($parts.Count) parca; periyot, parca yuksekligi): $r"
    }
}

# .meta: PremiumV4 taş ayarları (Sprite, mipmap, okunabilir, şeffaflık), yeni guid. Mevcut .meta korunur.
$template = Get-Content (Join-Path $root 'Assets\KatKat\Resources\PremiumV4\tiles\face-neutral.png.meta') -Raw
$template = $template -replace '(?s)internalIDToNameTable:.*?externalObjects', "internalIDToNameTable: []`n  externalObjects"
$template = $template -replace '(?s)    sprites:\r?\n    - serializedVersion: 2.*?\r?\n    outline: \[\]', "    sprites: []`n    outline: []"
Get-ChildItem $dst -Filter *.png -Recurse | ForEach-Object {
    $meta = "$($_.FullName).meta"
    if (-not (Test-Path $meta)) {
        $guid = [guid]::NewGuid().ToString('N')
        [IO.File]::WriteAllText($meta, ($template -replace 'guid: [0-9a-f]{32}', "guid: $guid"))
    }
}
foreach ($dir in @($dst, (Join-Path $dst 'ui'), (Join-Path $dst 'map'))) {
    if ((Test-Path $dir) -and -not (Test-Path "$dir.meta")) {
        [IO.File]::WriteAllText("$dir.meta", "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
    }
}
Get-ChildItem $dst | Select-Object Name, Length
