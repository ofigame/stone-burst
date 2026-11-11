# Stone Burst — vurucu tanıtım kurgusu (2026-11-12; kullanıcı: "Arapça video editi, güzel vurucu bir edit olsun").
# Piyasadaki bulmaca reklamlarının kalıbı: ilk saniyede en gösterişli an (kanca), 2–4 sn'lik sert kesimler, her kesimde
# tek kısa büyük yazı (sessiz izlemede de anlaşılır), ritme oturan müzik (120 BPM, kesimler vuruşta), gerilim → patlama,
# sonda logo ve çağrı. 9:16, 1080×1920, 30 fps, ~28 sn.
# Önce kareler: tools/unity.ps1 trailer-punch   (Logs/punch_<dil>/<sahne>/f_*.png + sfx.wav, Logs/punch_*.wav)
# Sonra:        powershell -ExecutionPolicy Bypass -File tools/trailer-punch.ps1 -Lang ar
# Çıktı:        Builds/Video/StoneBurst_promo_<dil>.mp4
param([string]$Lang = 'ar', [double]$MusicGain = 0.75, [double]$SfxGain = 1.3)

$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture # filtrelerde ondalık nokta
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root
$ff = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
if (-not $ff) { $ff = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter ffmpeg.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName }
if (-not $ff) { throw 'ffmpeg bulunamadı' }

$frames = "Logs/punch_$Lang"
$tmp = "Logs/punch_tmp_$Lang"
if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
New-Item -ItemType Directory -Force $tmp | Out-Null
New-Item -ItemType Directory -Force 'Builds/Video' | Out-Null
$fontAr = 'Assets/KatKat/Resources/Typography/NotoSansArabic.ttf'
$logo = 'Assets/KatKat/Resources/PremiumV4/ui/logo-stoneburst.png'
$utf8 = New-Object Text.UTF8Encoding $false
$rlm = [char]0x200F

# Metinler (satır sonu `n). Arapça: her satır RLM ile başlar ki noktalama sağdan sola doğru yere otursun.
$T = @{
    'ar' = @{
        hook   = 'لمسة واحدة…' + "`n" + 'انفجار كامل!'
        core   = 'اضغط على 3 أحجار' + "`n" + 'متشابهة وقشّر الطبقات'
        roll   = 'اللفافة الذهبية' + "`n" + 'تزيل كل حجر من نفس اللون!'
        world  = 'تحديات جديدة' + "`n" + 'في كل منطقة'
        big    = 'مجموعات كبيرة…' + "`n" + 'قوى خاصة!'
        calm   = 'بلا مؤقت…' + "`n" + 'فقط فكّر!'
        levels = '1000 مستوى'
        cta    = 'حمّل الآن!'
        sub    = 'لعبة ألغاز مجانية'
    }
}
$tx = $T[$Lang]; if (-not $tx) { throw "Metin yok: $Lang" }
function TextFile([string]$name, [string]$s) {
    $lines = $s -split "`n" | ForEach-Object { if ($Lang -eq 'ar') { "$rlm$_$rlm" } else { $_ } }
    $p = "$tmp/$name.txt"
    [IO.File]::WriteAllText((Join-Path $Root $p), ($lines -join "`n"), $utf8)
    return $p
}

# Büyük yazı: önce koyu kalın kontur + gölge, üstüne beyaz (hafif kendi renginde kontur: daha dolgun). Fırlayarak girer.
function Caption([string]$file, [double]$a, [double]$b, [int]$size, [string]$y, [string]$color = 'white') {
    $tt = "(t-$a)"
    $fs = "'$size*if(lt($tt,0.12),0.55+0.6*$tt/0.12,if(lt($tt,0.22),1.15-0.15*($tt-0.12)/0.10,1))'"
    $common = "fontfile=${fontAr}:textfile=${file}:text_align=C:line_spacing=-22:fontsize=${fs}:x=(w-text_w)/2:y=$y-text_h/2:enable='between(t,$a,$b)'"
    return "drawtext=${common}:fontcolor=0x0B2E2B@0.55:borderw=22:bordercolor=0x0B2E2B@0.55:shadowx=0:shadowy=10:shadowcolor=black@0.35," +
           "drawtext=${common}:fontcolor=${color}:borderw=12:bordercolor=0x0B3D3A," +
           "drawtext=${common}:fontcolor=${color}:borderw=2:bordercolor=${color}"
}

# Üstte okunurluk için koyu geçiş.
$grad = "$tmp/grad.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=1080x760,format=rgba' -vf "geq=r=0:g=0:b=0:a='190*pow(1-Y/H\,1.6)'" -frames:v 1 $grad
if ($LASTEXITCODE -ne 0) { throw 'geçiş görseli' }

# Kurgu: sahne, kaynak başlangıç sn, süre, yazı, yazı başlangıcı, beyaz flaş. Kesimler 0,5 sn'lik vuruşlarda;
# 14–16 müzikte gerilim, 16'da patlama (zafer kutlaması), 24'te kapanış akoru (logo).
$segs = @(
    @{ s = 'p1_kanca_l791'; at = 0.6;  d = 3.0; cap = 'hook';  ca = 0.05; flash = $true }
    @{ s = 'p2_temel_l122'; at = 0.6;  d = 3.5; cap = 'core';  ca = 0.1 }
    @{ s = 'p4_rulo_l769';  at = 0.4;  d = 3.0; cap = 'roll';  ca = 0.1 }
    @{ s = 'p5_buz_l105';   at = 0.4;  d = 2.0; cap = 'world'; ca = 0.1 }
    @{ s = 'p6_zarf_l221';  at = 0.6;  d = 2.5; cap = 'world'; ca = -1 }
    @{ s = 'p3_rulo_l480';  at = 0.6;  d = 2.0; cap = 'big';   ca = 0.1 }
    @{ s = 'p7_zafer_l131'; at = 15.8; d = 4.0; cap = 'calm';  ca = 0.3; ce = 2.5; flash = $true } # yazı zafer kartından (رائع!) önce çekilir
    @{ s = 'p8_harita';     at = 0.1;  d = 4.0; cap = 'levels'; ca = 0.3 }
)
$clips = @(); $cuts = @(); $t0 = 0.0; $k = 0
foreach ($g in $segs) {
    $dir = "$frames/$($g.s)"
    if (-not (Test-Path $dir)) { throw "Kare yok: $dir" }
    $start = [int][Math]::Round($g.at * 30); $n = [int][Math]::Round($g.d * 30)
    $file = TextFile $g.cap $tx[$g.cap]
    # Hafif sürekli yakınlaşma (tahta büyük görünsün; üst bilgi şeridi yazıya yer açar).
    $z0 = 1.12; $zr = 0.10 / $n
    $vf = "zoompan=z='$z0+$zr*on':x='iw/2-iw/zoom/2':y='ih*0.57-ih/zoom/2':d=1:s=1080x1920:fps=30,format=rgba[base];" +
          "[base][1:v]overlay=0:0,format=yuv420p"
    $ca = if ($g.ca -lt 0) { 0 } else { $g.ca }
    $size = switch ($g.cap) { 'levels' { 150 } 'roll' { 82 } 'core' { 88 } default { 96 } }
    $ce = if ($g.ce) { $g.ce } else { $g.d - 0.05 }
    $cap = Caption $file $ca $ce $size 'h*0.155'
    if ($g.ca -lt 0) { $cap = $cap -replace "0\.55\+0\.6\*", '1+0*' -replace "1\.15-0\.15\*", '1-0*' } # önceki yazının devamı: fırlama yok
    $vf += ",$cap"
    if ($g.flash) { $vf += ",fade=t=in:st=0:d=0.18:color=white" }
    $wav = "$dir/sfx.wav"
    $out = "$tmp/seg_$k.mov"
    & $ff -y -loglevel error -start_number $start -framerate 30 -i "$dir/f_%05d.png" -loop 1 -i $grad -ss $g.at -t $g.d -i $wav `
        -filter_complex "[0:v]$vf[v];[2:a]aresample=44100,aformat=channel_layouts=stereo,apad,afade=t=in:d=0.03,afade=t=out:st=$($g.d - 0.05):d=0.05[a]" `
        -map '[v]' -map '[a]' -frames:v $n -t $g.d -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -r 30 -c:a pcm_s16le $out
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg kesim hatası: $($g.s)" }
    $clips += $out; $t0 += $g.d; $cuts += $t0; $k++
    "{0,-16} {1,4:0.0} sn  → {2,5:0.0}" -f $g.s, $g.d, $t0
}

# Kapanış kartı: haritanın son karesi bulanık ve koyu, logo vuruşta fırlar, çağrı ve alt yazı.
$endD = 4.5
$last = (Get-ChildItem "$frames/p8_harita" -Filter 'f_*.png' | Sort-Object Name | Select-Object -Last 1).FullName
$cta = TextFile 'cta' $tx.cta; $sub = TextFile 'sub' $tx.sub
$pop = "if(lt(t,0.14),0.5+0.62*t/0.14,if(lt(t,0.26),1.12-0.12*(t-0.14)/0.12,1+0.015*sin(2*PI*(t-0.26)/1.0)))"
$end = "$tmp/seg_end.mov"
$capCta = Caption $cta 0.5 $endD 128 'h*0.66' '0xFFD84A'
$capSub = (Caption $sub 0.8 $endD 64 'h*0.755') -replace 'borderw=22', 'borderw=14' -replace 'borderw=12', 'borderw=8'
& $ff -y -loglevel error -loop 1 -framerate 30 -i $last -loop 1 -framerate 30 -i $logo -f lavfi -t $endD -i 'anullsrc=r=44100:cl=stereo' `
    -filter_complex ("[0:v]scale=1080:1920,gblur=sigma=28,eq=brightness=-0.18:saturation=1.15,format=rgba[bg];" +
        "[1:v]scale=w='960*($pop)':h=-1:eval=frame,format=rgba[lg];" +
        "[bg][lg]overlay=x='(W-w)/2':y='H*0.36-h/2':eval=frame,format=yuv420p,$capCta,$capSub,fade=t=in:st=0:d=0.15:color=white,fade=t=out:st=$($endD - 0.4):d=0.4[v]") `
    -map '[v]' -map 2:a -t $endD -r 30 -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -c:a pcm_s16le $end
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg kapanış hatası' }
$clips += $end
$total = $t0 + $endD

# Birleştir (sert kesim) ve sesi kur: oyun efektleri + müzik + her kesimde "vuuş" + kanca, patlama ve logoda darbe.
$list = "$tmp/list.txt"
[IO.File]::WriteAllText((Join-Path $Root $list), (($clips | ForEach-Object { "file '" + (Split-Path $_ -Leaf) + "'" }) -join "`n"), $utf8)
$cat = "$tmp/cat.mov"
& $ff -y -loglevel error -f concat -safe 0 -i $list -c copy $cat
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg birleştirme hatası' }

$inputs = @('-i', $cat, '-i', 'Logs/punch_music.wav')
$fc = "[0:a]volume=${SfxGain}[sfx];[1:a]atrim=0:$total,volume=${MusicGain},afade=t=out:st=$($total - 1.0):d=1.0[mus];"
$mixes = @('[sfx]', '[mus]'); $ix = 2
foreach ($c in $cuts) {
    if ($c -ge $t0) { continue } # son kesim kapanışa geçiş: orada darbe var
    $ms = [int](($c - 0.28) * 1000)
    $inputs += @('-i', 'Logs/punch_whoosh.wav')
    $fc += "[${ix}:a]adelay=${ms}|${ms},volume=0.55[w$ix];"; $mixes += "[w$ix]"; $ix++
}
foreach ($c in @(0.0, 16.0, $t0)) {
    $ms = [int]($c * 1000)
    $inputs += @('-i', 'Logs/punch_impact.wav')
    $fc += "[${ix}:a]adelay=${ms}|${ms},volume=0.7[i$ix];"; $mixes += "[i$ix]"; $ix++
}
$fc += ($mixes -join '') + "amix=inputs=$($mixes.Count):duration=first:normalize=0,alimiter=limit=0.95,loudnorm=I=-14:TP=-1.5:LRA=11,aresample=48000[a]"
$final = "Builds/Video/StoneBurst_promo_$Lang.mp4"
& $ff -y -loglevel error @inputs -filter_complex $fc -map 0:v -map '[a]' -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -r 30 -c:a aac -b:a 192k -movflags +faststart -t $total $final
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg son kurgu hatası' }
"Video: $final ({0:0.0} sn)" -f $total
