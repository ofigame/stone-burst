# ui3 karşılaştırma: docs/ui3_onayli_tasarim/sayfalar/NN_*.png (hedef) | docs/ui3_uygulama/NN_*.png (uygulama)
# yan yana. Kullanım: tools/ui3_karsilastir.ps1 -Only 01,12 -Out <klasör>  (çıktı: <klasör>/kars_NN.jpg)
param([string]$Only = "", [string]$Out = "")
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$tgt = Join-Path $root 'docs\ui3_onayli_tasarim\sayfalar'
$app = Join-Path $root 'docs\ui3_uygulama'
if ($Out -eq "") { $Out = Join-Path $app 'karsilastirma' }
New-Item -ItemType Directory -Force $Out | Out-Null
$sel = if ($Only) { $Only.Split(',') } else { $null }
foreach ($a in Get-ChildItem $app -Filter '*.png') {
    $nn = $a.Name.Substring(0, 2)
    if ($sel -and ($sel -notcontains $nn)) { continue }
    $t = Get-ChildItem $tgt -Filter "$nn`_*.png" | Select-Object -First 1
    if (-not $t) { continue }
    $W = 540; $H = 960
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList ($W * 2 + 12), ($H + 30)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.Clear([System.Drawing.Color]::Black)
    $i1 = [System.Drawing.Image]::FromFile($t.FullName); $g.DrawImage($i1, 0, 30, $W, $H); $i1.Dispose()
    $i2 = [System.Drawing.Image]::FromFile($a.FullName); $g.DrawImage($i2, $W + 12, 30, $W, $H); $i2.Dispose()
    $f = New-Object System.Drawing.Font -ArgumentList 'Arial', 13
    $g.DrawString("HEDEF $($t.BaseName)", $f, [System.Drawing.Brushes]::White, 4, 6)
    $g.DrawString("UYGULAMA", $f, [System.Drawing.Brushes]::White, $W + 16, 6)
    $bmp.Save((Join-Path $Out "kars_$nn.jpg"), [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $g.Dispose(); $bmp.Dispose()
}
