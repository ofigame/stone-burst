# Güçlendirici efektleri videosu (2026-11-20): Logs/booster_demo/b1 kareleri + sesleri (tools/unity.ps1 booster-demo) → mp4.
# Çıktı: Builds/Video/StoneBurst_guclendiriciler.mp4 (720p, telefonda açılabilir boy).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dir = Join-Path $root 'Logs\booster_demo\b1'
$out = Join-Path $root 'Builds\Video\StoneBurst_guclendiriciler.mp4'
& ffmpeg -loglevel error -y -framerate 30 -i (Join-Path $dir 'f_%05d.png') -i (Join-Path $dir 'sfx.wav') -vf scale=720:-2 -c:v libx264 -pix_fmt yuv420p -crf 22 -preset medium -c:a aac -b:a 128k -shortest -movflags +faststart $out
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg' }
Get-Item $out | Select-Object Name, @{n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }
