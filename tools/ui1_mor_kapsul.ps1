# ui1 hamle kapsülü mor türevi (kullanıcı kararı 2026-11-08: "Mor (konsept gibi)").
# Kaynak: docs/ui1/_ortak/sprite/hud-moves-pill.png (krem iç, altın çerçeve). Düşük doygunluklu krem iç mora
# boyanır (krem tonundaki parlaklık farkı gölge olarak korunur); doygun altın çerçeve aynen kalır. Kenar geçişleri
# doygunluğa göre karışır. Çıktı: Assets/KatKat/Resources/UI1/hud-moves-pill-purple.png
param(
    [string]$Src = "$PSScriptRoot\..\docs\ui1\_ortak\sprite\hud-moves-pill.png",
    [string]$Dst = "$PSScriptRoot\..\Assets\KatKat\Resources\UI1\hud-moves-pill-purple.png"
)
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class PillRecolor
{
    public static void Run(string src, string dst)
    {
        using (var input = new Bitmap(src))
        using (var bmp = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(bmp)) g.DrawImage(input, 0, 0, input.Width, input.Height);
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int n = data.Stride * data.Height;
            var px = new byte[n];
            Marshal.Copy(data.Scan0, px, 0, n);
            // Mor taban (#6C3BB4) ve üst parlak ton (#9A6BE0): krem parlaklığına göre aralarında.
            double[] dark = { 0x52, 0x2A, 0x93 }, light = { 0xA0, 0x74, 0xE6 };
            for (int i = 0; i < n; i += 4)
            {
                if (px[i + 3] == 0) continue;
                double b = px[i], gg = px[i + 1], r = px[i + 2];
                double max = Math.Max(r, Math.Max(gg, b)), min = Math.Min(r, Math.Min(gg, b));
                double sat = max <= 0 ? 0 : (max - min) / max;
                double t = Math.Max(0, Math.Min(1, (0.52 - sat) / 0.12)); // 1 = krem iç, 0 = altın çerçeve
                if (t <= 0) continue;
                double lum = (r + gg + b) / (3 * 255.0);
                double k = Math.Max(0, Math.Min(1, (lum - 0.70) / 0.26));
                for (int c = 0; c < 3; c++)
                {
                    double target = dark[2 - c] + (light[2 - c] - dark[2 - c]) * k;
                    double orig = px[i + c];
                    px[i + c] = (byte)Math.Round(orig + (target - orig) * t);
                }
            }
            Marshal.Copy(px, 0, data.Scan0, n);
            bmp.UnlockBits(data);
            bmp.Save(dst, ImageFormat.Png);
        }
    }
}
"@
[PillRecolor]::Run((Resolve-Path $Src).Path, [IO.Path]::GetFullPath($Dst))
"yazıldı: $Dst"

