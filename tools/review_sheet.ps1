# App Review "Attachment" sayfası (2026-11-14, App Store 4.3): ilerideki kurallı bölümlerin ekranları tek görselde.
# Girdi: docs/magaza/ham_review/iphone/en/*.png (KATKAT_STORE_SET=review tools/unity.ps1 capture-store)
# Çıktı: docs/magaza/review/stone_burst_rule_levels.jpg
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root 'docs\magaza\ham_review\iphone\en'
$outDir = Join-Path $root 'docs\magaza\review'
New-Item -ItemType Directory -Force $outDir | Out-Null

$labels = [ordered]@{
  '01_exact_27'   = 'Level 27 - Exact Moves'
  '02_boss_30'    = 'Level 30 - first Boss level'
  '03_reverse_37' = 'Level 37 - Think Backwards'
  '04_chain_47'   = 'Level 47 - Chain Master'
  '05_protect_68' = 'Level 68 - Guard the Crystals'
  '06_pure_77'    = 'Level 77 - Pure Plan'
  '07_moment_87'  = 'Level 87 - The Right Moment'
  '08_board_27'   = 'Level 27 board, long-press layer view'
}

$tw = 480; $th = [int]($tw * 2622 / 1206); $gap = 30; $lab = 64; $head = 170; $cols = 4; $rows = 2
$W = $cols * $tw + ($cols + 1) * $gap
$H = $head + $rows * ($lab + $th + $gap) + $gap
$bmp = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'; $g.InterpolationMode = 'HighQualityBicubic'; $g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([System.Drawing.Color]::FromArgb(255, 246, 240, 228))
$ink = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 32, 56, 70))
$sub = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 83, 100, 109))
$fTitle = New-Object System.Drawing.Font 'Segoe UI', 40, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
$fSub = New-Object System.Drawing.Font 'Segoe UI', 24, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
$fLab = New-Object System.Drawing.Font 'Segoe UI', 24, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString('Stone Burst - rule levels and boss levels (later in the main path)', $fTitle, $ink, $gap, 30)
$g.DrawString('Levels unlock in order; levels 1-5 are short tutorials. Rule levels start at level 27, and every 10th level is a boss that combines rules. Screens from the current build.', $fSub, $sub, $gap, 100)

$i = 0
foreach ($k in $labels.Keys) {
  $file = Join-Path $src ($k + '.png')
  if (-not (Test-Path $file)) { throw "Eksik: $file" }
  $c = $i % $cols; $r = [math]::Floor($i / $cols)
  $x = $gap + $c * ($tw + $gap); $y = $head + $r * ($lab + $th + $gap)
  $g.DrawString($labels[$k], $fLab, $ink, $x, $y + 14)
  $img = [System.Drawing.Image]::FromFile($file)
  $g.DrawImage($img, $x, $y + $lab, $tw, $th)
  $img.Dispose(); $i++
}

$jpg = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$ep = New-Object System.Drawing.Imaging.EncoderParameters 1
$ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), 88L
$out = Join-Path $outDir 'stone_burst_rule_levels.jpg'
$bmp.Save($out, $jpg, $ep)
$g.Dispose(); $bmp.Dispose()
"{0} ({1:N1} MB, {2}x{3})" -f $out, ((Get-Item $out).Length / 1MB), $W, $H
