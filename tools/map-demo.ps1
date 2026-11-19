# Yolculuk haritası hareketi videoları (2026-11-19): Logs/map_demo/<sahne>/ kareleri (tools/unity.ps1 map-demo) → mp4.
# Çıktı: Builds/Video/StoneBurst_harita_hareket_<bahce|sahil|col>.mp4 (1080x1920, 30 fps, sessiz) ve hepsi bir arada
# StoneBurst_harita_hareket.mp4 (720p, telefonda paylaşılabilir boy).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root 'Logs\map_demo'
$vid = Join-Path $root 'Builds\Video'
$parts = @()
foreach ($s in @(@('m2_sahil', 'sahil'), @('m1_bahce', 'bahce'), @('m3_col', 'col'))) {
    $dir = Join-Path $src $s[0]
    if (-not (Test-Path (Join-Path $dir 'f_00000.png'))) { continue }
    $out = Join-Path $vid ("StoneBurst_harita_hareket_{0}.mp4" -f $s[1])
    & ffmpeg -loglevel error -y -framerate 30 -i (Join-Path $dir 'f_%05d.png') -c:v libx264 -pix_fmt yuv420p -crf 20 -preset medium -movflags +faststart $out
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg $($s[0])" }
    $parts += $out
}
if ($parts.Count -gt 1) {
    $list = Join-Path $src 'list.txt'
    [IO.File]::WriteAllLines($list, ($parts | ForEach-Object { "file '" + ($_ -replace '\\', '/') + "'" }))
    $all = Join-Path $vid 'StoneBurst_harita_hareket.mp4'
    & ffmpeg -loglevel error -y -f concat -safe 0 -i $list -vf scale=720:-2 -c:v libx264 -pix_fmt yuv420p -crf 24 -preset medium -movflags +faststart $all
    if ($LASTEXITCODE -ne 0) { throw 'ffmpeg birleştirme' }
    $parts += $all
}
$parts | ForEach-Object { Get-Item $_ } | Select-Object Name, @{n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }
