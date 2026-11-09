# Stone Burst tanıtım videosu (eski adı TriKat) (YouTube Shorts, 1080×1920, 30 fps, sesli).
# Önce kareler ve sesler: tools/unity.ps1 trailer-frames  (Logs/trailer_<dil>/<sahne>/f_00000.png + sfx.wav, Logs/trailer_music.wav)
# Sonra:                  powershell -ExecutionPolicy Bypass -File tools/trailer.ps1 -Lang en   (ya da tr)
# Çıktı: Builds/Video/TriKat_short_<dil>.mp4
# Ses: oyunun kendi efektleri (görüntüyle eş zamanlı) + koddan üretilmiş arka plan müziği; YouTube için ~-14 LUFS.
param([string]$Lang = 'tr', [double]$Fade = 0.45, [double]$MusicGain = 0.55, [double]$SfxGain = 1.6)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root
$ff = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
if (-not $ff) { $ff = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter ffmpeg.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName }
if (-not $ff) { throw 'ffmpeg bulunamadı' }

$frames = "Logs/trailer_$Lang"
$tmp = "Logs/trailer_tmp_$Lang"
$music = 'Logs/trailer_music.wav'
New-Item -ItemType Directory -Force $tmp | Out-Null
New-Item -ItemType Directory -Force 'Builds/Video' | Out-Null
$font = 'Assets/KatKat/Resources/Typography/Nunito-ExtraBold.ttf'

# Sahne → altyazılar (başlangıç sn, bitiş sn, metin).
$captionsByLang = @{
    'tr' = @{
        '01_harita'    = @(,@(0.6, 4.2, '1000 bölümlük yolculuk'))
        '02_oyun_l038' = @(@(0.5, 3.4, 'Aynı renkten 3 taşa dokun'), @(3.8, 7.0, 'Katları soy, hedefleri topla'))
        '03_oyun_l105' = @(,@(0.3, 3.0, 'Her bölgede yeni engeller'))
        '04_oyun_l221' = @(,@(0.3, 3.0, 'Zarflar, buzlar, süngerler...'))
        '05_oyun_l052' = @(,@(0.3, 3.0, 'Acele yok, sadece düşün'))
    }
    'en' = @{
        '01_harita'    = @(,@(0.6, 4.2, 'A 1000-level journey'))
        '02_oyun_l038' = @(@(0.5, 3.4, 'Tap 3+ matching stones'), @(3.8, 7.0, 'Peel the layers, clear the goals'))
        '03_oyun_l105' = @(,@(0.3, 3.0, 'New obstacles in every region'))
        '04_oyun_l221' = @(,@(0.3, 3.0, 'Envelopes, ice, sponges...'))
        '05_oyun_l052' = @(,@(0.3, 3.0, 'No timer. Just think.'))
    }
}
$captions = $captionsByLang[$Lang]
if (-not $captions) { throw "Altyazı yok: $Lang" }

$scenes = Get-ChildItem $frames -Directory | Sort-Object Name
if ($scenes.Count -eq 0) { throw "Kare yok: $frames" }
$clips = @(); $durs = @()
foreach ($s in $scenes) {
    $n = (Get-ChildItem $s.FullName -Filter 'f_*.png').Count
    if ($n -lt 2) { continue }
    $dur = $n / 30.0
    $vf = @('format=yuv420p')
    $i = 0
    foreach ($c in $captions[$s.Name]) {
        if (-not $c) { continue }
        $txt = Join-Path $tmp "$($s.Name)_$i.txt"
        [IO.File]::WriteAllText((Join-Path $Root $txt), $c[2], (New-Object Text.UTF8Encoding $false))
        $a = $c[0]; $b = [Math]::Min($c[1], $dur - 0.2)
        $alpha = "if(lt(t,$a+0.25),(t-$a)/0.25,if(gt(t,$b-0.25),($b-t)/0.25,1))"
        $vf = @("drawtext=fontfile=${font}:textfile=$($txt -replace '\\','/'):fontsize=74:fontcolor=white:borderw=7:bordercolor=0x0E5E58:box=1:boxcolor=0x0E5E58@0.35:boxborderw=26:x=(w-text_w)/2:y=h*0.255:enable='between(t,$a,$b)':alpha='$alpha'") + $vf
        $i++
    }
    $wav = Join-Path $s.FullName 'sfx.wav'
    $out = Join-Path $tmp "$($s.Name).mp4"
    $ain = if (Test-Path $wav) { @('-i', $wav) } else { @('-f', 'lavfi', '-t', "$dur", '-i', 'anullsrc=r=44100:cl=mono') }
    & $ff -y -loglevel error -framerate 30 -i "$($s.FullName)\f_%05d.png" @ain -vf ($vf -join ',') -af "aresample=44100,aformat=channel_layouts=stereo,apad" -t "$dur" -c:v libx264 -preset slow -crf 17 -pix_fmt yuv420p -r 30 -c:a pcm_s16le $out.Replace('.mp4', '.mov')
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg sahne hatası: $($s.Name)" }
    $clips += $out.Replace('.mp4', '.mov'); $durs += $dur
    "{0}: {1} kare, {2:0.0} sn" -f $s.Name, $n, $dur
}

# Görüntü: xfade; efekt sesi: acrossfade (aynı süreler). Sonra müzik altta, sonda kararma ve ses kısılması.
$inputs = @(); foreach ($c in $clips) { $inputs += @('-i', $c) }
$inputs += @('-i', $music)
$mi = $clips.Count
$fc = ''; $pv = '[0:v]'; $pa = '[0:a]'; $offset = 0.0
for ($k = 1; $k -lt $clips.Count; $k++) {
    $offset += $durs[$k - 1] - $Fade
    $fc += "$pv[${k}:v]xfade=transition=fade:duration=${Fade}:offset=$([Math]::Round($offset,3))[x$k];"
    $fc += "$pa[${k}:a]acrossfade=d=${Fade}:c1=tri:c2=tri[y$k];"
    $pv = "[x$k]"; $pa = "[y$k]"
}
$total = 0.0; foreach ($d in $durs) { $total += $d }; $total -= $Fade * ($clips.Count - 1)
$end = [Math]::Round($total, 3); $fo = [Math]::Round($total - 1.2, 3)
$fc += "${pv}fade=t=in:st=0:d=0.3,fade=t=out:st=$([Math]::Round($total - 0.5,3)):d=0.5,format=yuv420p[v];"
$fc += "${pa}volume=${SfxGain}[sfx];"
$fc += "[${mi}:a]atrim=0:$end,volume=${MusicGain},afade=t=in:st=0:d=0.6,afade=t=out:st=${fo}:d=1.2[mus];"
$fc += "[sfx][mus]amix=inputs=2:duration=first:normalize=0,alimiter=limit=0.95,loudnorm=I=-14:TP=-1.5:LRA=11,aresample=48000[a]"
$final = "Builds/Video/TriKat_short_$Lang.mp4"
& $ff -y -loglevel error @inputs -filter_complex $fc -map '[v]' -map '[a]' -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -r 30 -c:a aac -b:a 192k -movflags +faststart -t $end $final
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg birleştirme hatası' }
"Video: $final ({0:0.0} sn)" -f $total
