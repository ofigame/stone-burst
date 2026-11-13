# App Review videosu (2026-11-14): ReviewVideoRecorder karelerini MP4'e çevirir.
# Önce: tools/unity.ps1 test-play -Filter ReviewVideoRecorder  → docs/magaza/review/frames/f00000.jpg ...
# Çıktı: docs/magaza/review/stone_burst_review.mp4 (H.264, 30 kare/sn, 720x1280)
$root = Split-Path -Parent $PSScriptRoot
$frames = Join-Path $root 'docs\magaza\review\frames'
$out = Join-Path $root 'docs\magaza\review\stone_burst_review.mp4'
$ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
if (-not $ffmpeg) {
  $ffmpeg = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter ffmpeg.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $ffmpeg) { throw 'ffmpeg bulunamadı' }
# App Store Connect inceleme eki tek dosya: kurallı bölümler sayfası (review_sheet.ps1) videonun sonuna 5 sn kapanış karesi olarak eklenir.
$sheet = Join-Path $root 'docs\magaza\review\stone_burst_rule_levels.jpg'
if (Test-Path $sheet) {
  & $ffmpeg -y -loglevel error -framerate 30 -i (Join-Path $frames 'f%05d.jpg') -loop 1 -t 5 -framerate 30 -i $sheet `
    -filter_complex "[1:v]scale=720:-2:flags=lanczos,pad=720:1280:(ow-iw)/2:(oh-ih)/2:color=0xF6F0E4,setsar=1,fps=30[s];[0:v]setsar=1[v];[v][s]concat=n=2:v=1:a=0[o]" `
    -map "[o]" -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p -movflags +faststart $out
} else {
  & $ffmpeg -y -loglevel error -framerate 30 -i (Join-Path $frames 'f%05d.jpg') -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p -movflags +faststart $out
}
if ($LASTEXITCODE -ne 0) { throw "ffmpeg hata: $LASTEXITCODE" }
"{0} ({1:N1} MB)" -f $out, ((Get-Item $out).Length / 1MB)
