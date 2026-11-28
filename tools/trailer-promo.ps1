# Stone Burst — reklam videosu v2 (2026-11-28), 9:16 tam ekran (1080×1920, 30 fps), Türkçe ve Arapça.
# Kullanıcı: "çok çarpıcı bir kapakla başlayacak Stone Burst logomuz ve uygulama iconumuz bir pencere açar gibi oyunun anasayfasını
# açacak. anasayfada yolculuk haritasından karakterimiz bölüm bölüm zıplayarak sulu bölümlerden birine konacak ve oyunun içerisine
# girecek. sulu bölümde suyun kovaya akışını kovanın hedefe doluşunu gösterecek. Bölüm tamamlanacak. maskot torbalı bir bölüme
# gidecek. oyun oynanacak ve kapanışta yine Stone Burst logomuz ile google play indirme bilgileri".
# Önce kareler (her sahne ayrı Unity çalıştırması): KATKAT_TRAILER_LANGS=tr KATKAT_TRAILER_ONLY=p1_harita tools/unity.ps1 trailer-promo
#   sahneler p1_harita, p5_reklam, p2_su (eski: p3_harita, p4_torba) → Logs/promo2_<dil>/<sahne>/f_*.png + sfx.wav
# Sonra:        powershell -ExecutionPolicy Bypass -File tools/trailer-promo.ps1 -Lang tr   (ve -Lang ar)
# Çıktı:        Builds/Video/StoneBurst_reklam_<dil>_9x16.mp4
param([switch]$Cover, [string]$Lang = 'tr', [double]$MusicGain = 0.55, [double]$SfxGain = 1.25, [double]$WaterFrom = 2.0, [double]$WaterTo = 13.0)

$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root
$ff = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
if (-not $ff) { $ff = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter ffmpeg.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName }
if (-not $ff) { throw 'ffmpeg bulunamadı' }

$frames = "Logs/promo2_$Lang"
$tmp = "Logs/promo2_tmp_$Lang"
if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
New-Item -ItemType Directory -Force $tmp | Out-Null
New-Item -ItemType Directory -Force 'Builds/Video' | Out-Null
$logo = 'Assets/KatKat/Resources/PremiumV4/ui/logo-stoneburst.png'
$icon = 'docs/varliklar/gorseller/arayuz/uygulama-ikonu-v5.png'
$music = 'Assets/KatKat/Resources/Audio/TriKat/game-music.mp3'
$utf8 = New-Object Text.UTF8Encoding $false
$rlm = [char]0x200F

$fontSrc = if ($Lang -eq 'ar' -and (Test-Path "$env:WINDIR\Fonts\segoeuib.ttf")) { "$env:WINDIR\Fonts\segoeuib.ttf" }
           elseif ($Lang -eq 'ar') { 'Assets/KatKat/Resources/Typography/NotoSansArabic.ttf' }
           else { 'Assets/KatKat/Resources/Typography/Nunito-ExtraBold.ttf' }
Copy-Item $fontSrc "$tmp/font.ttf"
$font = "$tmp/font.ttf"

# Metinler. Arapça: satırlar RLM ile sarılır; hareke yok (kalın yazı tipinde üst üste biniyor).
$T = @{
    'tr' = @{
        map   = 'Yolculuğa çık!'
        water = 'Suyu kovaya akıt,' + "`n" + 'gönder, hedefi doldur!'
        map2  = 'Her bölüm' + "`n" + 'yeni bir bulmaca!'
        bag   = 'Taşları torbaya topla!'
        think = 'Önce düşün!'
        peel  = 'Üstteki katı soy,' + "`n" + 'altındakini ortaya çıkar!'
        big   = 'Dev grubu patlat!'
        cta   = 'Hemen indir'
        store = "Google Play'de"
        sub   = 'Ücretsiz bulmaca oyunu'
    }
    'ar' = @{
        map   = 'انطلق في الرحلة!'                                   # Yolculuğa çık!
        water = 'اسكب الماء في الدلو' + "`n" + 'وأرسله لتملأ الهدف!'  # Suyu kovaya dök / gönder, hedefi doldur!
        map2  = 'كل مرحلة' + "`n" + 'لغز جديد!'                       # Her bölüm yeni bir bulmaca!
        think = 'فكر أولا!'                                           # Önce düşün!
        peel  = 'قشر الطبقة العليا' + "`n" + 'واكشف ما تحتها!'         # Üstteki katı soy, altındakini ortaya çıkar!
        big   = 'فجر المجموعة الكبيرة!'                               # Dev grubu patlat!
        bag   = 'اجمع الأحجار في الكيس!'                              # Taşları torbaya topla!
        cta   = 'حملها الآن'                                          # Hemen indir
        store = 'Google Play على'                                     # Google Play'de
        sub   = 'لعبة ألغاز مجانية'                                    # Ücretsiz bulmaca oyunu
    }
}
$tx = $T[$Lang]; if (-not $tx) { throw "Metin yok: $Lang" }
function TextFile([string]$name, [string]$s) {
    $lines = $s -split "`n" | ForEach-Object { if ($Lang -eq 'ar') { "$rlm$_$rlm" } else { $_ } }
    $p = "$tmp/$name.txt"
    [IO.File]::WriteAllText((Join-Path $Root $p), ($lines -join "`n"), $utf8)
    return $p
}
function F([double]$v) { $v.ToString('0.####', [Globalization.CultureInfo]::InvariantCulture) }
function Smooth([double]$t) { if ($t -le 0) { return 0.0 }; if ($t -ge 1) { return 1.0 }; return $t * $t * (3 - 2 * $t) }

# Büyük yazı: koyu kalın kontur + gölge, renkli dolgu; fırlayarak girer, sonunda söner.
function Caption([string]$file, [double]$a, [double]$b, [int]$size, [string]$y, [string]$color = 'white', [int]$bw = 14) {
    $tt = "(t-$(F $a))"
    $fs = "'$size*if(lt($tt,0.12),0.55+0.6*$tt/0.12,if(lt($tt,0.22),1.15-0.15*($tt-0.12)/0.10,1))'"
    $al = "'if(lt(t,$(F ($b - 0.2))),1,max(0,($(F $b)-t)/0.2))'"
    $common = "fontfile=${font}:textfile=${file}:text_align=C:line_spacing=8:fontsize=${fs}:x=(w-text_w)/2:y=$y-text_h/2:alpha=${al}:enable='between(t,$(F $a),$(F $b))'"
    return "drawtext=${common}:fontcolor=0x10082A@0.6:borderw=$($bw + 12):bordercolor=0x10082A@0.5:shadowx=0:shadowy=12:shadowcolor=black@0.4," +
           "drawtext=${common}:fontcolor=${color}:borderw=${bw}:bordercolor=0x2A0F4A"
}

Add-Type -TypeDefinition @'
using System; using System.IO;
public static class PromoSfxRemap {
    // 16 bit PCM WAV; çıkış örneği için kaynak zamanı = src[kare] doğrusal ara değer.
    public static void Run(string inPath, string outPath, double[] src, double fps) {
        byte[] b = File.ReadAllBytes(inPath);
        int pos = 12, ch = 1, sr = 44100, dataAt = -1, dataLen = 0;
        while (pos + 8 <= b.Length) {
            string id = System.Text.Encoding.ASCII.GetString(b, pos, 4); int len = BitConverter.ToInt32(b, pos + 4);
            if (id == "fmt ") { ch = BitConverter.ToInt16(b, pos + 10); sr = BitConverter.ToInt32(b, pos + 12); }
            if (id == "data") { dataAt = pos + 8; dataLen = Math.Min(len, b.Length - dataAt); break; }
            pos += 8 + len + (len & 1);
        }
        int n = dataLen / 2 / ch; var x = new float[n];
        for (int i = 0; i < n; i++) x[i] = BitConverter.ToInt16(b, dataAt + i * 2 * ch) / 32768f;
        int frames = src.Length - 1; int outN = (int)Math.Round(frames / fps * sr);
        var o = new short[outN];
        for (int k = 0; k < outN; k++) {
            double f = k / (double)sr * fps; int fi = Math.Min(frames - 1, (int)f); double fr = f - fi;
            double s = (src[fi] + (src[fi + 1] - src[fi]) * fr) * sr;
            int i0 = (int)Math.Floor(s); double a = s - i0; float v = 0f;
            if (i0 >= 0 && i0 + 1 < n) v = (float)(x[i0] * (1 - a) + x[i0 + 1] * a);
            o[k] = (short)Math.Max(-32768, Math.Min(32767, Math.Round(v * 32767)));
        }
        using (var w = new BinaryWriter(File.Create(outPath))) {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + outN * 2);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(outN * 2);
            foreach (var v in o) w.Write(v);
        }
    }
}
'@

function Frames([string]$scene) {
    $dir = "$frames/$scene"
    if (-not (Test-Path $dir)) { throw "Kare yok: $dir" }
    $have = @{}; $maxF = 0
    foreach ($f in (Get-ChildItem $dir -Filter 'f_*.png')) { $i = [int]$f.BaseName.Substring(2); $have[$i] = $f.FullName; if ($i -gt $maxF) { $maxF = $i } }
    return @{ dir = $dir; have = $have; max = $maxF }
}

# ---- Ortak görseller --------------------------------------------------------------------------------------------------
$band = "$tmp/band.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=1080x320,format=rgba' -vf "geq=r=0:g=0:b=0:a='125*pow(sin(PI*Y/H)\,1.6)'" -frames:v 1 $band
if ($LASTEXITCODE -ne 0) { throw 'bant görseli' }
# Kapak zemini: koyu mor–lacivert ışınsal geçiş, ortada turkuaz parıltı.
$coverBg = "$tmp/cover_bg.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=1080x1920,format=rgb24' -vf (
    "geq=r='clip(40+70*exp(-pow(hypot(X-540,Y-820)/620,2))+18*Y/H,0,255)':" +
    "g='clip(14+120*exp(-pow(hypot(X-540,Y-820)/520,2)),0,255)':" +
    "b='clip(70+120*exp(-pow(hypot(X-540,Y-820)/700,2))+30*Y/H,0,255)'") -frames:v 1 $coverBg
if ($LASTEXITCODE -ne 0) { throw 'kapak zemini' }
# Işık hüzmeleri (dönen, saydam) — kapakta logonun arkasında.
$rays = "$tmp/rays.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=1400x1400,format=rgba' -vf (
    "geq=r=255:g=245:b=210:a='70*pow(max(0,sin(12*atan2(Y-700,X-700))),6)*exp(-pow(hypot(X-700,Y-700)/560,2))'") -frames:v 1 $rays
if ($LASTEXITCODE -ne 0) { throw 'ışın görseli' }
# İkon: yuvarlak köşe (%22), beyaz çerçeve ve gölge.
$iconR = "$tmp/icon_round.png"
& $ff -y -loglevel error -i $icon -filter_complex (
    "[0:v]scale=360:360,format=rgba,geq=r='r(X,Y)':g='g(X,Y)':b='b(X,Y)':a='255*lte(hypot(max(0,abs(X-179.5)-100),max(0,abs(Y-179.5)-100)),80)'[ic];" +
    "color=white@0:s=400x420,format=rgba,geq=r=0:g=0:b=0:a='110*exp(-pow(max(0,hypot(max(0,abs(X-199.5)-104),max(0,abs(Y-219.5)-104))-80)/16,2))'[sh];" +
    "color=white:s=380x380,format=rgba,geq=r=255:g=255:b=255:a='255*lte(hypot(max(0,abs(X-189.5)-100),max(0,abs(Y-189.5)-100)),90)'[rim];" +
    "[sh][rim]overlay=10:10[a];[a][ic]overlay=20:20") -frames:v 1 $iconR
if ($LASTEXITCODE -ne 0) { throw 'ikon görseli' }
# Pencere maskesi: yuvarlak köşeli tam ekran (köşe 96 px).
$mask = "$tmp/mask.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=1080x1920,format=gray' -vf "geq=lum='255*lte(hypot(max(0,abs(X-539.5)-443),max(0,abs(Y-959.5)-863)),96)'" -frames:v 1 $mask
if ($LASTEXITCODE -ne 0) { throw 'maske' }
# Düğme: turkuaz-yeşil, yuvarlak uçlu (oyun düğmeleri gibi).
$btn = "$tmp/button.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=780x170,format=rgba' -vf (
    "geq=r='if(gt(hypot(max(0,abs(X-389.5)-305),max(0,abs(Y-84.5)-0)),76),255,40+20*Y/H)':" +
    "g='if(gt(hypot(max(0,abs(X-389.5)-305),max(0,abs(Y-84.5)-0)),76),255,215-70*Y/H)':" +
    "b='if(gt(hypot(max(0,abs(X-389.5)-305),max(0,abs(Y-84.5)-0)),76),255,140-50*Y/H)':" +
    "a='255*lte(hypot(max(0,abs(X-389.5)-305),max(0,abs(Y-84.5)-0)),84)'") -frames:v 1 $btn
if ($LASTEXITCODE -ne 0) { throw 'düğme görseli' }

function Pop([double]$a, [double]$amp = 0.0) {
    $u = "(n/30-$(F $a))"
    return "if(lt($u,0),0.01,if(lt($u,0.14),0.5+0.62*$u/0.14,if(lt($u,0.26),1.12-0.12*($u-0.14)/0.12,1+$(F $amp)*sin(2*PI*($u-0.26)/0.9))))"
}

$clips = @(); $cuts = @(); $t0 = 0.0; $k = 0

if ($Cover) { # 2026-11-28 kullanıcı: "ilk girişteki kapak ve haritayı kaldıralım. Direkt oyunla başlasın" (eski açılış -Cover ile)
# ---- 1) Kapak + pencere açılışı ------------------------------------------------------------------------------------------
# 0–2,3 sn: dönen ışınlar, ikon fırlar (0,1), logo çarpar (0,4), hafif nabız. 2,3–3,0 sn: ikonun yerinden oyunun ana sayfası
# yuvarlak köşeli bir pencere gibi büyüyerek açılır (ikon ve logo söner).
$coverD = 2.3; $openD = 0.7; $d1 = $coverD + $openD
$p1 = Frames 'p1_harita'
$segDir = "$tmp/seg_open"; New-Item -ItemType Directory -Force $segDir | Out-Null
$nOpen = [int][Math]::Round($openD * 30)
for ($i = 0; $i -lt $nOpen; $i++) { New-Item -ItemType HardLink -Path (Join-Path $Root ("$segDir/f_{0:00000}.png" -f $i)) -Target $p1.have[[Math]::Min($p1.max, $i)] | Out-Null }
$sExpr = "(0.36+0.64*(3*pow(min(1,max(0,(t-$(F $coverD))/$(F $openD))),2)-2*pow(min(1,max(0,(t-$(F $coverD))/$(F $openD))),3)))"
$iconFade = "'if(lt(t,$(F $coverD)),1,max(0,1-(t-$(F $coverD))/0.35))'"
$open = "$tmp/seg_00.mov"
& $ff -y -loglevel error -loop 1 -framerate 30 -t $d1 -i $coverBg -loop 1 -framerate 30 -t $d1 -i $rays -loop 1 -framerate 30 -t $d1 -i $iconR -loop 1 -framerate 30 -t $d1 -i $logo `
    -framerate 30 -itsoffset $coverD -i "$segDir/f_%05d.png" -loop 1 -framerate 30 -t $d1 -i $mask -f lavfi -t $d1 -i 'anullsrc=r=44100:cl=stereo' `
    -filter_complex ("[1:v]format=rgba,rotate=a='t*0.35':c=none:ow=1400:oh=1400[ry];" +
        "[0:v][ry]overlay=x=-160:y='820-700':format=auto[b0];" +
        "[2:v]scale=w='430*($(Pop 0.1))':h=-1:eval=frame,format=rgba,colorchannelmixer=aa=1,pad=w=520:h=540:x=(ow-iw)/2:y=(oh-ih)/2:color=black@0:eval=frame[ic];" +
        "[3:v]scale=w='900*($(Pop 0.4 0.02))':h=-1:eval=frame,format=rgba,pad=w=1000:h=700:x=(ow-iw)/2:y=(oh-ih)/2:color=black@0:eval=frame[lg];" +
        "[b0][ic]overlay=x='(W-w)/2':y='640-h/2':eval=frame:enable='lt(t,$(F ($coverD + 0.35)))'[b1];" +
        "[b1][lg]overlay=x='(W-w)/2':y='1150-h/2':eval=frame:enable='lt(t,$(F ($coverD + 0.35)))'[b2];" +
        "[4:v]format=rgba[pg];[5:v]format=gray[mk];[pg][mk]alphamerge,scale=w='1080*$sExpr':h='1920*$sExpr':eval=frame[win];" +
        "[b2][win]overlay=x='(W-w)/2':y='(640+(960-640)*($sExpr-0.36)/0.64)-h/2':eval=frame:enable='gte(t,$(F $coverD))',format=yuv420p,fade=t=in:st=0:d=0.2[v]") `
    -map '[v]' -map 6:a -t $d1 -r 30 -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -c:a pcm_s16le $open
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg kapak hatası' }
$clips += $open; $t0 += $d1; $cuts += $t0; $k++
"kapak+açılış      {0,4:0.0} sn  → {1,5:0.0}" -f $d1, $t0
}

# ---- 2) Oyun sahneleri ---------------------------------------------------------------------------------------------------
# from/to: kaynakta başlangıç–bitiş sn (to yoksa sona dek); speed: hız; caps: yazılar (k metin, a–b sn; m: marks.txt anına göre).
# Kullanıcı (2026-11-28): "Oyun oynanan kısım çok hızlı akıyor. Reklamda asıl göstermek istediğim kısım o kısım" → oyun gerçek hızda.
# Aynı gün: "Bir bölüm komple bitmek zorunda değil … Parmağın düşünüyormuş gibi …, 2 li tekli gruba tıkladığı ve uyarı sesi çıktığı,
# üst katmanda küçük grupları soyduğumuz ve büyük gruba ulaştığımız bir edit" → p5_reklam (docs/reklam_bolum), su kovası kısa an.
function Marks([string]$scene) {
    # Aynı ad yinelenirse ikincisi ad2, üçüncüsü ad3 … (ör. gonder, gonder2).
    $m = @{}; $p = "$frames/$scene/marks.txt"
    if (Test-Path $p) {
        foreach ($l in Get-Content $p) {
            $kv = $l -split '='; $key = $kv[0]; $j = 2
            while ($m.ContainsKey($key)) { $key = $kv[0] + $j; $j++ }
            $m[$key] = [double]$kv[1] / 30.0
        }
    }
    return $m
}
$mk = Marks 'p5_reklam'
$ms = Marks 'p6_harita_su'
# Kullanıcı (2026-11-28): "ilk girişteki kapak ve haritayı kaldıralım. Direkt oyunla başlasın. kova bölümüne geçerken haritadan
# bölümü parmak seçsin. en sondaki mevcut kapakta kalsın" → reklam bölümü, harita + su kovası (iki gönderim), kapanış.
$suAt = $ms['su']; $waterEnd = $(if ($ms['gonder2']) { $ms['gonder2'] + 2.4 } else { $suAt + 9.0 })
$segs = @(
    @{ s = 'p5_reklam'; from = 0.0; speed = 1.0; caps = @(
        @{ k = 'think'; a = $mk['dusun'] + 0.1; b = $mk['soy'] - 0.1;   y = 1805 }
        @{ k = 'peel';  a = $mk['soy'];         b = $mk['buyuk'] - 0.1; y = 1805 }
        @{ k = 'big';   a = $mk['buyuk'] + 0.2; b = $mk['rulo'] + 0.5;  y = 1805 }) }
    @{ s = 'p6_harita_su'; from = 0.0; to = $waterEnd; speed = 1.0; flash = $true; caps = @(
        @{ k = 'map2';  a = 0.2;          b = $suAt - 1.6;  y = 1500 }
        @{ k = 'water'; a = $suAt + 0.2;  b = $suAt + 3.6;  y = 600 }) }
)
foreach ($g in $segs) {
    $fr = Frames $g.s
    $srcEnd = if ($g.to) { [Math]::Min($g.to, $fr.max / 30.0) } else { $fr.max / 30.0 }
    $segDir = "$tmp/seg_$k"; New-Item -ItemType Directory -Force $segDir | Out-Null
    $s = $g.from; $list = New-Object System.Collections.Generic.List[double]
    $i = 0
    while ($s -le $srcEnd) {
        $list.Add($s)
        $want = [Math]::Min($fr.max, [int][Math]::Round($s * 30))
        $file = $null
        for ($o = 0; $o -le 3 -and -not $file; $o++) { if ($fr.have.ContainsKey($want - $o)) { $file = $fr.have[$want - $o] } elseif ($fr.have.ContainsKey($want + $o)) { $file = $fr.have[$want + $o] } }
        New-Item -ItemType HardLink -Path (Join-Path $Root ("$segDir/f_{0:00000}.png" -f $i)) -Target $file | Out-Null
        $s += $g.speed / 30.0; $i++
    }
    $list.Add($s)
    $n = $i; $d = $n / 30.0
    $src = $list.ToArray()
    $segWav = "$tmp/seg_$k.wav"
    [PromoSfxRemap]::Run((Join-Path $Root "$($fr.dir)/sfx.wav"), (Join-Path $Root $segWav), $src, 30.0)
    $nc = $g.caps.Count
    $vf = "format=rgba[base];[1:v]split=$nc" + (-join (0..($nc - 1) | ForEach-Object { "[b$_]" }))
    $last = 'base'; $ci = 0; $capFiles = @()
    foreach ($c in $g.caps) {
        $capFiles += TextFile "$($g.s)_$($c.k)" $tx[$c.k]
        $vf += ";[$last][b$ci]overlay=0:$($c.y - 160):enable='between(t,$(F $c.a),$(F $c.b))'[c$ci]"; $last = "c$ci"; $ci++
    }
    $vf += ";[$last]format=yuv420p"
    for ($j = 0; $j -lt $nc; $j++) { $c = $g.caps[$j]; $vf += "," + (Caption $capFiles[$j] $c.a $c.b 92 "$($c.y)") }
    if ($g.flash) { $vf += ",fade=t=in:st=0:d=0.12:color=white" }
    $afc = "[2:a]aformat=channel_layouts=stereo,afade=t=in:d=0.03,afade=t=out:st=$(F ($d - 0.05)):d=0.05[a]"
    $out = "$tmp/seg_{0:00}.mov" -f $k
    $fcFile = "$tmp/seg_$k.fc"
    [IO.File]::WriteAllText((Join-Path $Root $fcFile), "[0:v]$vf[v];$afc", $utf8)
    & $ff -y -loglevel error -framerate 30 -i "$segDir/f_%05d.png" -loop 1 -i $band -i $segWav `
        -/filter_complex $fcFile -map '[v]' -map '[a]' -frames:v $n -t $d -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -r 30 -c:a pcm_s16le $out
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg kesim hatası: $($g.s)" }
    $clips += $out; $t0 += $d; $cuts += $t0; $k++
    "{0,-17} {1,4:0.0} sn  → {2,5:0.0}" -f $g.s, $d, $t0
}

# ---- 3) Kapanış: reklam bölümünün son karesi bulanık ve koyu; ikon, logo, "indir" düğmesi, Google Play ve alt yazı -------------
$endD = 3.8
$last = (Get-ChildItem "$frames/p5_reklam" -Filter 'f_*.png' | Sort-Object Name | Select-Object -Last 1).FullName
$cta = TextFile 'cta' $tx.cta; $store = TextFile 'store' $tx.store; $sub = TextFile 'sub' $tx.sub
$end = "$tmp/seg_{0:00}.mov" -f $k
$btnY = 1265
$capCta = Caption $cta 0.55 99 100 "$btnY" 'white' 8
$capStore = Caption $store 0.8 99 86 '1410' '0xFFD84A' 12
$capSub = Caption $sub 1.0 99 60 '1520' 'white' 8
& $ff -y -loglevel error -loop 1 -framerate 30 -i $last -loop 1 -framerate 30 -i $iconR -loop 1 -framerate 30 -i $logo -loop 1 -framerate 30 -i $btn -loop 1 -framerate 30 -i $rays -f lavfi -t $endD -i 'anullsrc=r=44100:cl=stereo' `
    -filter_complex ("[0:v]scale=1080:1920,gblur=sigma=26,eq=brightness=-0.24:saturation=1.1,format=rgba[bg0];" +
        "[4:v]format=rgba,rotate=a='t*0.35':c=none:ow=1400:oh=1400[ry];[bg0][ry]overlay=x=-160:y='880-700'[bg];" +
        "[1:v]scale=w='400*($(Pop 0.0))':h=-1:eval=frame,format=rgba,pad=w=460:h=480:x=(ow-iw)/2:y=(oh-ih)/2:color=black@0:eval=frame[ic];" +
        "[2:v]scale=w='800*($(Pop 0.25))':h=-1:eval=frame,format=rgba,pad=w=900:h=600:x=(ow-iw)/2:y=(oh-ih)/2:color=black@0:eval=frame[lg];" +
        "[3:v]scale=w='780*($(Pop 0.5 0.035))':h=-1:eval=frame,format=rgba,pad=w=910:h=200:x=(ow-iw)/2:y=(oh-ih)/2:color=black@0:eval=frame[bt];" +
        "[bg][ic]overlay=x='(W-w)/2':y='500-h/2':eval=frame[v1];" +
        "[v1][lg]overlay=x='(W-w)/2':y='915-h/2':eval=frame[v2];" +
        "[v2][bt]overlay=x='(W-w)/2':y='$btnY-h/2':eval=frame,format=yuv420p,$capCta,$capStore,$capSub,fade=t=in:st=0:d=0.15:color=white,fade=t=out:st=$(F ($endD - 0.3)):d=0.3[v]") `
    -map '[v]' -map 5:a -t $endD -r 30 -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -c:a pcm_s16le $end
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg kapanış hatası' }
$clips += $end
$total = $t0 + $endD
"kapanış           {0,4:0.0} sn  → {1,5:0.0}" -f $endD, $total

# ---- Birleştir, ses: oyun efektleri + oyunun müziği (sonda söner) --------------------------------------------------------
$list = "$tmp/list.txt"
[IO.File]::WriteAllText((Join-Path $Root $list), (($clips | ForEach-Object { "file '" + (Split-Path $_ -Leaf) + "'" }) -join "`n"), $utf8)
$cat = "$tmp/cat.mov"
& $ff -y -loglevel error -f concat -safe 0 -i $list -c copy $cat
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg birleştirme hatası' }
$fc = "[0:a]volume=${SfxGain}[sfx];[1:a]atrim=0:$(F $total),asetpts=PTS-STARTPTS,aformat=channel_layouts=stereo,volume=${MusicGain},afade=t=in:d=0.3,afade=t=out:st=$(F ($total - 1.0)):d=1.0[mus];" +
      "[sfx][mus]amix=inputs=2:duration=first:normalize=0,alimiter=limit=0.95,loudnorm=I=-14:TP=-1.5:LRA=11,aresample=48000[a]"
$final = "Builds/Video/StoneBurst_reklam_${Lang}_9x16.mp4"
& $ff -y -loglevel error -i $cat -i $music -filter_complex $fc -map 0:v -map '[a]' -c:v libx264 -preset slow -crf 20 -profile:v high -pix_fmt yuv420p -r 30 -c:a aac -b:a 160k -movflags +faststart -t $total $final
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg son kurgu hatası' }
"Video: $final ({0:0.0} sn)" -f $total
