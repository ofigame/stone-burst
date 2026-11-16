# Beyaz zeminli görseli (Tripo çıktısı, JPG) şeffaf PNG'ye çevirir (2026-11-16).
#   Kenarlardan beyaza yakın pikseller taşma dolgusuyla zemin sayılır (alfa 0); zemine komşu ince kenar bandında alfa
#   beyazdan uzaklıkla yumuşar ve beyaz karışımı renkten çıkarılır. Sonra görünen sınıra kırpılır, kare tuvalin
#   ortasına konur (pay %6) ve en çok -Size piksele küçültülür.
# Kullanım: powershell -File white-cut.ps1 -In a.jpg -Out b.png [-Size 512]
param([Parameter(Mandatory)][string]$In, [Parameter(Mandatory)][string]$Out, [int]$Size = 512)
Add-Type -AssemblyName System.Drawing
if (-not ('WhiteCut' -as [type])) {
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
public static class WhiteCut
{
    static int Dist(Color c) { return Math.Max(255 - c.R, Math.Max(255 - c.G, 255 - c.B)); }
    public static void Run(string inPath, string outPath, int size)
    {
        var src = new Bitmap(inPath);
        int w = src.Width, h = src.Height;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = src.GetPixel(x, y);
        src.Dispose();
        const int Bg = 26, Band = 70;
        var bg = new bool[w * h];
        var q = new Queue<int>();
        for (int x = 0; x < w; x++) { q.Enqueue(x); q.Enqueue((h - 1) * w + x); }
        for (int y = 0; y < h; y++) { q.Enqueue(y * w); q.Enqueue(y * w + w - 1); }
        while (q.Count > 0)
        {
            int i = q.Dequeue();
            if (bg[i] || Dist(px[i]) > Bg) continue;
            bg[i] = true;
            int x = i % w, y = i / w;
            if (x > 0) q.Enqueue(i - 1); if (x < w - 1) q.Enqueue(i + 1);
            if (y > 0) q.Enqueue(i - w); if (y < h - 1) q.Enqueue(i + w);
        }
        // Kapalı beyaz boşluklar (ataç halkasının içi): kenara değmeyen büyük beyaz bölgeler de zemin. Küçük parlamalar kalır.
        int minArea = w * h / 600;
        var seen = new bool[w * h];
        for (int s0 = 0; s0 < w * h; s0++)
        {
            if (bg[s0] || seen[s0] || Dist(px[s0]) > Bg) continue;
            var comp = new List<int>();
            q.Enqueue(s0); seen[s0] = true;
            while (q.Count > 0)
            {
                int i = q.Dequeue(); comp.Add(i);
                int x = i % w, y = i / w;
                foreach (int j in new[] { x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1 })
                    if (j >= 0 && !seen[j] && !bg[j] && Dist(px[j]) <= Bg) { seen[j] = true; q.Enqueue(j); }
            }
            if (comp.Count >= minArea) foreach (int i in comp) bg[i] = true;
        }
        // Zemine 3 px'ten yakın ön plan pikselleri kenar bandı.
        var near = new int[w * h];
        for (int i = 0; i < w * h; i++) near[i] = bg[i] ? 0 : 99;
        for (int pass = 0; pass < 3; pass++)
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int i = y * w + x; if (near[i] <= pass) continue;
                if ((x > 0 && near[i - 1] == pass) || (x < w - 1 && near[i + 1] == pass) || (y > 0 && near[i - w] == pass) || (y < h - 1 && near[i + w] == pass)) near[i] = pass + 1;
            }
        var outPx = new Color[w * h];
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int i = 0; i < w * h; i++)
        {
            if (bg[i]) { outPx[i] = Color.FromArgb(0, 0, 0, 0); continue; }
            var c = px[i];
            float a = 1f;
            if (near[i] <= 3) a = Math.Min(1f, Math.Max(0f, (Dist(c) - Bg) / (float)(Band - Bg)));
            if (a < 1f && a > 0.02f)
            {
                // Beyazla karışımı çöz: c = a*f + (1-a)*255.
                Func<int, int> un = v => Math.Max(0, Math.Min(255, (int)((v - (1f - a) * 255f) / a)));
                c = Color.FromArgb(un(c.R), un(c.G), un(c.B));
            }
            int al = (int)(a * 255f);
            outPx[i] = Color.FromArgb(al, c.R, c.G, c.B);
            if (al > 8) { int x = i % w, y = i / w; minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
        }
        int bw = maxX - minX + 1, bh = maxY - minY + 1, side = (int)(Math.Max(bw, bh) * 1.12f);
        var full = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        int ox = (side - bw) / 2, oy = (side - bh) / 2;
        for (int y = 0; y < bh; y++) for (int x = 0; x < bw; x++) full.SetPixel(ox + x, oy + y, outPx[(minY + y) * w + minX + x]);
        int s = Math.Min(size, side);
        var dst = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(dst))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(full, new Rectangle(0, 0, s, s));
        }
        dst.Save(outPath, ImageFormat.Png);
        full.Dispose(); dst.Dispose();
        Console.WriteLine(outPath + " " + s + "px (kaynak kırpım " + bw + "x" + bh + ")");
    }
}
'@
}
[WhiteCut]::Run((Resolve-Path $In).Path, [System.IO.Path]::GetFullPath($Out), $Size)
