# Mağaza simgeleri (2026-11-20; kullanıcı: "Evet başla" — dükkan, külçe, güçlendiriciler). docs/yeni_v2 kaynaklarını
# Resources/Shop/<ad>.png'ye alır: çok soluk kenar pikselleri (alfa < 24) atılır, boş kenar kırpılır, kare 512 px.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root 'docs\yeni_v2'
$dst = Join-Path $root 'Assets\KatKat\Resources\Shop'
New-Item -ItemType Directory -Force $dst | Out-Null
$map = [ordered]@{
    'moves'     = 'Parlak Kum Saati ve Altın Oklar.png'
    'recolor'   = 'Büyülü Renk Dönüşümü Sihirbazlığı.png'
    'bomb'      = 'Parlayan Turkuaz Bomba Patlaması.png'
    'ingot'     = 'Parlak Yıldızlı Altın Külçe Ikonu.png'
    'pack1'     = 'Parlak Altın Külçe Yığını.png'
    'pack2'     = 'Zümrüt Kesede Altın Külçeler.png'
    'pack3'     = 'Yıldızlı Altın Hazine Sandığı.png'
    'pack4'     = 'Parıldayan Teal Hazine Sandığı.png'
    'noads'     = 'Engellenmiş Video ve Altın Külçeler.png'
    'starter'   = 'Büyülü Teal Hazine Kutusu.png'
    'tab-shop'  = 'Parlak Altın Külçeli Teal Pazar Tezgâhı.png'
}
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class ShopIcon
{
    public static string Make(string src, string dst, int size)
    {
        using (var bmp = new Bitmap(src))
        {
            int W = bmp.Width, H = bmp.Height;
            var d = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var px = new byte[d.Stride * H];
            Marshal.Copy(d.Scan0, px, 0, px.Length);
            int x0 = W, y0 = H, x1 = -1, y1 = -1;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * d.Stride + x * 4;
                if (px[i + 3] < 24) { px[i] = px[i + 1] = px[i + 2] = px[i + 3] = 0; continue; }
                if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
            }
            Marshal.Copy(px, 0, d.Scan0, px.Length);
            bmp.UnlockBits(d);
            int bw = x1 - x0 + 1, bh = y1 - y0 + 1, side = Math.Max(bw, bh);
            int pad = side / 40; side += pad * 2;
            using (var o = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(o))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingMode = CompositingMode.SourceCopy;
                float s = size / (float)side;
                float dx = (side - bw) / 2f * s, dy = (side - bh) / 2f * s;
                g.DrawImage(bmp, new RectangleF(dx, dy, bw * s, bh * s), new RectangleF(x0, y0, bw, bh), GraphicsUnit.Pixel);
                o.Save(dst, ImageFormat.Png);
            }
            return string.Format("{0}x{1} -> kırpım {2}x{3}", W, H, bw, bh);
        }
    }
}
'@
foreach ($k in $map.Keys) {
    $r = [ShopIcon]::Make((Join-Path $src $map[$k]), (Join-Path $dst "$k.png"), 512)
    "{0,-9} {1}" -f $k, $r
}
