# ui3 onaylı tasarım (docs/ui3_onayli_tasarim) varlıklarını Resources/UI3 altına hazırlar.
#  - parcalar: modal-seramik, kart-seramik, buton-birincil, buton-ikincil, baslik-plakasi, ikon-dugmesi (aynen kopya;
#    9 dilim sınırları asset-manifest.json'dan ShellAssetImporter.Ui3Texture'da).
#  - arkaplanlar: Z1–Z5 → UI3/bg/Z1..Z5.png (aynen kopya).
#  - simgeler: UI1/icon-*.png içindeki krem glif ayıklanır, ikon-dugmesi (petrol yeşili disk + pirinç kenar) üstüne
#    çapın %50'si (112 → 56) boyunda ortalanır → UI3/icon-*.png. Glif rengi onTeal #FFF9ED.
# Kullanım (proje kökünden): powershell -ExecutionPolicy Bypass -File tools/ui3_hazirla.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root 'docs\ui3_onayli_tasarim'
$dst = Join-Path $root 'Assets\KatKat\Resources\UI3'
New-Item -ItemType Directory -Force $dst | Out-Null
New-Item -ItemType Directory -Force (Join-Path $dst 'bg') | Out-Null

foreach ($n in 'modal-seramik','kart-seramik','buton-birincil','buton-ikincil','baslik-plakasi','ikon-dugmesi') {
    Copy-Item (Join-Path $src "parcalar\$n.png") (Join-Path $dst "$n.png") -Force
}
$bgMap = @{ 'Z1' = 'Z1_seftali_kum'; 'Z2' = 'Z2_adacayi'; 'Z3' = 'Z3_petrol_inci'; 'Z4' = 'Z4_dumanli_lavanta'; 'Z5' = 'Z5_tas_mavisi' }
foreach ($k in $bgMap.Keys) { Copy-Item (Join-Path $src "arkaplanlar\$($bgMap[$k]).png") (Join-Path $dst "bg\$k.png") -Force }

# Simge kaynağı: UI1 simgeleri 2026-11-08 APK temizliğinde arşive taşındı (oyunda UI3 karşılıkları kullanılır).
$ui1Src = Join-Path $root 'Assets\KatKat\Resources\UI1'
$ui1Arc = 'E:\OFIGAME\OYUNLAR\KatKat_Arsiv_2026-11-06\apk_temizlik_2026-11-08\Resources\UI1'
if (-not (Test-Path (Join-Path $ui1Src 'icon-back.png'))) { $ui1Src = $ui1Arc }
# Simge: disk + glif
$disc = [System.Drawing.Bitmap]::FromFile((Join-Path $src 'parcalar\ikon-dugmesi.png'))
$D = 256
foreach ($f in Get-ChildItem $ui1Src -Filter 'icon-*.png') {
    if ($f.Name -eq 'icon-zpe.png') { continue }
    $ic = [System.Drawing.Bitmap]::FromFile($f.FullName)
    $w = $ic.Width; $h = $ic.Height; $cx = $w / 2.0; $cy = $h / 2.0; $R = [Math]::Min($w, $h) / 2.0
    # Glif maskesi (krem: R yüksek, R ≥ B; petrol diski: R düşük, B > R). Yalnız iç disk (dış altın halka hariç).
    $g = New-Object System.Drawing.Bitmap -ArgumentList $w, $h
    $minX = $w; $minY = $h; $maxX = 0; $maxY = 0
    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {
            $dx = $x - $cx; $dy = $y - $cy
            if ([Math]::Sqrt($dx * $dx + $dy * $dy) -gt $R * 0.74) { continue }
            $p = $ic.GetPixel($x, $y)
            if ($p.A -lt 8) { continue }
            $a1 = [Math]::Min(1.0, [Math]::Max(0.0, ($p.R - $p.B + 10) / 40.0))
            $a2 = [Math]::Min(1.0, [Math]::Max(0.0, ($p.R - 120) / 90.0))
            $a = [int](255 * $a1 * $a2 * $p.A / 255.0)
            if ($a -lt 6) { continue }
            $g.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($a, 0xFF, 0xF9, 0xED))
            if ($x -lt $minX) { $minX = $x }; if ($y -lt $minY) { $minY = $y }
            if ($x -gt $maxX) { $maxX = $x }; if ($y -gt $maxY) { $maxY = $y }
        }
    }
    $out = New-Object System.Drawing.Bitmap -ArgumentList $D, $D
    $gr = [System.Drawing.Graphics]::FromImage($out)
    $gr.InterpolationMode = 'HighQualityBicubic'; $gr.SmoothingMode = 'HighQuality'; $gr.PixelOffsetMode = 'HighQuality'
    $gr.Clear([System.Drawing.Color]::Transparent)
    $gr.DrawImage($disc, 0, 0, $D, $D)
    # Glif kutusu: uzun kenar çapın %50'si; diskin optik merkezine (ikon-dugmesi alt gölgesi için 2 px yukarı).
    $gw = $maxX - $minX + 1; $gh = $maxY - $minY + 1
    $s = ($D * 0.5) / [Math]::Max($gw, $gh)
    $tw = $gw * $s; $th = $gh * $s
    $rect = New-Object System.Drawing.RectangleF -ArgumentList (($D - $tw) / 2.0), (($D - $th) / 2.0 - $D * 0.012), $tw, $th
    $srcRect = New-Object System.Drawing.RectangleF -ArgumentList $minX, $minY, $gw, $gh
    $gr.DrawImage($g, $rect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $gr.Dispose()
    $out.Save((Join-Path $dst $f.Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose(); $g.Dispose(); $ic.Dispose()
    Write-Host "simge: $($f.Name)"
}
$disc.Dispose()
Write-Host "UI3 hazır: $dst"

# Kurdele: UI1/ribbon-purple'ın mor gövdesi token tonlarına (mor: purpleDark→#8E7BA8, petrol: tealBottom→tealTop)
# yeniden boyanır; altın çerçeve ve defneler aynı kalır. → UI3/ribbon-purple.png, UI3/ribbon-teal.png
function Recolor($srcPath, $dstPath, [int[]]$dark, [int[]]$light) {
    $b = [System.Drawing.Bitmap]::FromFile($srcPath)
    $o = New-Object System.Drawing.Bitmap -ArgumentList $b.Width, $b.Height
    for ($y = 0; $y -lt $b.Height; $y++) {
        for ($x = 0; $x -lt $b.Width; $x++) {
            $p = $b.GetPixel($x, $y)
            $h = $p.GetHue(); $s = $p.GetSaturation(); $v = [Math]::Max($p.R, [Math]::Max($p.G, $p.B)) / 255.0
            if ($p.A -gt 0 -and $h -ge 250 -and $h -le 330 -and $s -gt 0.2) {
                $k = [Math]::Min(1.0, [Math]::Max(0.0, ($v - 0.30) / 0.60))
                $r = [int]($dark[0] + ($light[0] - $dark[0]) * $k); $g = [int]($dark[1] + ($light[1] - $dark[1]) * $k); $bb = [int]($dark[2] + ($light[2] - $dark[2]) * $k)
                $o.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($p.A, $r, $g, $bb))
            } else { $o.SetPixel($x, $y, $p) }
        }
    }
    $o.Save($dstPath, [System.Drawing.Imaging.ImageFormat]::Png); $o.Dispose(); $b.Dispose()
}
$rp = Join-Path $ui1Src 'ribbon-purple.png'
Recolor $rp (Join-Path $dst 'ribbon-purple.png') @(0x42, 0x37, 0x4F) @(0x8E, 0x7B, 0xA8)
Recolor $rp (Join-Path $dst 'ribbon-teal.png') @(0x06, 0x5A, 0x55) @(0x2B, 0xA8, 0x9C)
Write-Host "kurdele: ribbon-purple, ribbon-teal"
