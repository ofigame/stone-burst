# Taş akışı izleme videosu (2026-11-19): Logs/anim_demo/<sahne>/ karelerini (tools/unity.ps1 anim-demo) sesleriyle
# birleştirir, her sahneye kısa Türkçe başlık koyar. Çıktı: Builds/Video/StoneBurst_tas_akisi_demo.mp4 (1080x1920, 30 fps).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root 'Logs\anim_demo'
$out = Join-Path $root 'Builds\Video\StoneBurst_tas_akisi_demo.mp4'
$tmp = Join-Path $src '_parts'
New-Item -ItemType Directory -Force $tmp | Out-Null
$scenes = @(
    @('s1_temel_l017', 'Aynı renk patlaması'),
    @('s2_roket_l095', 'Roket'),
    @('s3_rulo_l174', 'Altın Rulo kombosu'),
    @('s4_zafer_l042', 'Bölüm sonu'),
    @('s5_agir_l017', 'Ağır çekim (0,4x)')
)
$font = 'C\:/Windows/Fonts/arialbd.ttf'
$list = @()
$i = 0
foreach ($s in $scenes) {
    $dir = Join-Path $src $s[0]
    $part = Join-Path $tmp ("p{0}.mp4" -f $i)
    $label = $s[1]
    $lf = Join-Path $tmp ("t{0}.txt" -f $i)
    [IO.File]::WriteAllText($lf, $label, (New-Object Text.UTF8Encoding $false))
    $lfx = ($lf -replace "\\", "/") -replace ":", "\:"
    $vf = "drawtext=fontfile='$font':textfile='$lfx':fontsize=54:fontcolor=white:borderw=5:bordercolor=black@0.8:x=(w-text_w)/2:y=h-230"
    & ffmpeg -loglevel error -y -framerate 30 -i (Join-Path $dir 'f_%05d.png') -i (Join-Path $dir 'sfx.wav') `
        -vf $vf -c:v libx264 -pix_fmt yuv420p -crf 20 -preset medium -c:a aac -b:a 160k -ar 44100 -ac 2 -shortest $part
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg sahne $($s[0])" }
    $list += "file '" + ($part -replace '\\', '/') + "'"
    $i++
}
$listFile = Join-Path $tmp 'list.txt'
[IO.File]::WriteAllLines($listFile, $list)
& ffmpeg -loglevel error -y -f concat -safe 0 -i $listFile -c copy -movflags +faststart $out
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg birleştirme' }
Get-Item $out | Select-Object FullName, @{n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }
