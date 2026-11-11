# Stone Burst — vurucu tanıtım kurgusu (2026-11-12).
# Kullanıcı: "Arapça video editi, güzel vurucu bir edit olsun"; sonra "TR üret… harita kısmında sağ ve solda görüntü sabit
# kalıyor… oyun sayfasının üst kısmı da görünsün. Taşların parçalanma anında yavaşlat ve zoom yap ardından hızlanarak geri
# çekil. Profesyonel bir kurgu olsun".
# Kalıp (piyasadaki bulmaca reklamları): ilk saniyede en gösterişli an, 2–4 sn'lik sert kesimler (vuruşta), her kesimde
# tek kısa büyük yazı (sessiz izlemede de anlaşılır), patlamalarda hız rampası (yavaş çekim + patlama noktasına yakınlaşma
# + hafif sarsıntı, sonra hızlanarak geri çekilme), gerilim → patlama müziği, sonda logo ve çağrı. 9:16, 1080×1920, 30 fps.
# Önce kareler: tools/unity.ps1 trailer-punch   (KATKAT_TRAILER_LANGS=tr,ar; Logs/punch_<dil>/<sahne>/f_*.png, sfx.wav, meta.txt)
# Sonra:        powershell -ExecutionPolicy Bypass -File tools/trailer-punch.ps1 -Lang tr   (ya da ar)
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
$logo = 'Assets/KatKat/Resources/PremiumV4/ui/logo-stoneburst.png'
$utf8 = New-Object Text.UTF8Encoding $false
$rlm = [char]0x200F

# Metinler (satır sonu `n). Arapça: her satır RLM ile sarılır ki noktalama sağdan sola doğru yere otursun.
$T = @{
    'ar' = @{
        font   = 'Assets/KatKat/Resources/Typography/NotoSansArabic.ttf'
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
    'tr' = @{
        font   = 'Assets/KatKat/Resources/Typography/Nunito-ExtraBold.ttf'
        hook   = 'Tek dokunuş…' + "`n" + 'DEV PATLAMA!'
        core   = 'Aynı renkten 3 taşa dokun,' + "`n" + 'katları soy'
        roll   = 'Altın Rulo' + "`n" + 'o renkteki her taşı toplar!'
        world  = 'Her bölgede' + "`n" + 'yeni engeller'
        big    = 'Büyük gruplar…' + "`n" + 'süper güçler!'
        calm   = 'Süre yok…' + "`n" + 'sadece düşün!'
        levels = '1000 bölüm'
        cta    = 'Hemen indir!'
        sub    = 'Ücretsiz bulmaca oyunu'
    }
}
$tx = $T[$Lang]; if (-not $tx) { throw "Metin yok: $Lang" }
$font = $tx.font
function TextFile([string]$name, [string]$s) {
    $lines = $s -split "`n" | ForEach-Object { if ($Lang -eq 'ar') { "$rlm$_$rlm" } else { $_ } }
    $p = "$tmp/$name.txt"
    [IO.File]::WriteAllText((Join-Path $Root $p), ($lines -join "`n"), $utf8)
    return $p
}
function F([double]$v) { $v.ToString('0.####', [Globalization.CultureInfo]::InvariantCulture) }

# Büyük yazı: koyu kalın kontur + gölge, üstüne beyaz (kendi renginde ince kontur: dolgun). Fırlayarak girer.
function Caption([string]$file, [double]$a, [double]$b, [int]$size, [string]$y, [string]$color = 'white', [bool]$pop = $true) {
    $tt = "(t-$(F $a))"
    $fs = if ($pop) { "'$size*if(lt($tt,0.12),0.55+0.6*$tt/0.12,if(lt($tt,0.22),1.15-0.15*($tt-0.12)/0.10,1))'" } else { "$size" }
    $ls = if ($Lang -eq 'ar') { -22 } else { -6 }
    $common = "fontfile=${font}:textfile=${file}:text_align=C:line_spacing=${ls}:fontsize=${fs}:x=(w-text_w)/2:y=$y-text_h/2:enable='between(t,$(F $a),$(F $b))'"
    return "drawtext=${common}:fontcolor=0x0B2E2B@0.55:borderw=22:bordercolor=0x0B2E2B@0.55:shadowx=0:shadowy=10:shadowcolor=black@0.35," +
           "drawtext=${common}:fontcolor=${color}:borderw=12:bordercolor=0x0B3D3A," +
           "drawtext=${common}:fontcolor=${color}:borderw=2:bordercolor=${color}"
}

# Altta okunurluk için koyu geçiş (oyunun üst şeridi açık kalır).
$grad = "$tmp/grad.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=1080x620,format=rgba' -vf "geq=r=0:g=0:b=0:a='175*pow(Y/H\,1.5)'" -frames:v 1 $grad
if ($LASTEXITCODE -ne 0) { throw 'geçiş görseli' }

Add-Type -TypeDefinition @'
using System; using System.IO;
public static class SfxRemap {
    // 16 bit PCM mono/stereo WAV okur; çıkış örneği için kaynak zamanı = src[kare] doğrusal ara değer; doğrusal örnekleme.
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

function Smooth([double]$t) { if ($t -le 0) { return 0.0 }; if ($t -ge 1) { return 1.0 }; return $t * $t * (3 - 2 * $t) }

# Kurgu. Kesimler 0,5 sn'lik vuruşlarda; müzikte 12–14 sn gerilim, 14'te patlama, 24'te kapanış akoru (logo).
# mode hero: kahraman an (meta.txt "hero") çevresinde hız rampası; lead = patlamanın kesimdeki çıkış saniyesi.
# slow = yavaş çekim hızı, hold = yavaş çekimde geçen kaynak süre (sn), zmax = yakınlaşma.
$segs = @(
    @{ s = 'p1_kanca_l791'; d = 3.5; mode = 'hero'; lead = 0.45; slow = 0.28; hold = 0.55; zmax = 1.5;  cap = 'hook';  ca = 0.05; flash = $true }
    @{ s = 'p2_temel_l122'; d = 3.0; mode = 'normal'; at = 0.25; cap = 'core';  ca = 0.1 }
    @{ s = 'p4_rulo_l769';  d = 3.5; mode = 'hero'; lead = 1.0;  slow = 0.3;  hold = 0.5;  zmax = 1.35; cap = 'roll';  ca = 0.1 }
    @{ s = 'p5_buz_l105';   d = 2.0; mode = 'normal'; at = 0.4;  cap = 'world'; ca = 0.1 }
    @{ s = 'p6_zarf_l221';  d = 2.0; mode = 'normal'; at = 0.5;  cap = 'world'; ca = -1 }
    @{ s = 'p3_rulo_l480';  d = 3.0; mode = 'hero'; lead = 0.15; slow = 0.3;  hold = 0.5;  zmax = 1.4;  cap = 'big';   ca = 0.05; flash = $true }
    # Zafer: patlamadan sonra kutlama roketleri hızlı akar (post), kart açılırken (kaynak sonundan calm sn önce) normal hız.
    @{ s = 'p7_zafer_l131'; d = 4.5; mode = 'hero'; lead = 0.5;  slow = 0.35; hold = 0.35; zmax = 1.3;  cap = 'calm';  ca = 0.2; ce = 2.0; post = 2.6; calm = 1.7 }
    @{ s = 'p8_harita';     d = 2.5; mode = 'normal'; at = 0.3;  cap = 'levels'; ca = 0.25 }
)

$clips = @(); $cuts = @(); $heroes = @(); $t0 = 0.0; $k = 0
foreach ($g in $segs) {
    $dir = "$frames/$($g.s)"
    if (-not (Test-Path $dir)) { throw "Kare yok: $dir" }
    $fps = 30; $hero = $null
    foreach ($ln in (Get-Content "$dir/meta.txt")) {
        $p = $ln.Trim() -split ' '
        if ($p[0] -eq 'fps') { $fps = [int]$p[1] }
        if ($p[0] -eq 'hero') { $hero = @{ f = [int]$p[1] + 1; x = [double]$p[2]; y = [double]$p[3] } }
    }
    # Diskteki kareler (90 fps yakalamada kahraman an dışında her 3. kare).
    $have = @{}; $maxF = 0
    foreach ($f in (Get-ChildItem $dir -Filter 'f_*.png')) { $i = [int]$f.BaseName.Substring(2); $have[$i] = $f.FullName; if ($i -gt $maxF) { $maxF = $i } }
    $n = [int][Math]::Round($g.d * 30)
    $isHero = $g.mode -eq 'hero' -and $hero
    if ($g.mode -eq 'hero' -and -not $hero) { throw "meta.txt'de kahraman an yok: $dir" }

    # Hız eğrisi v(s) (kaynak sn / çıkış sn) ve yakınlaşma eğrisi z(s); s = kaynak (oyun) saniyesi.
    if ($isHero) {
        $P = $hero.f / $fps; $sl = $g.slow; $c = $P + $g.hold; $zm = $g.zmax
        $post = if ($g.post) { $g.post } else { 1.0 }; $calmAt = if ($g.calm) { $maxF / $fps - $g.calm } else { 1e9 }
        $speed = {
            param($s)
            if ($s -lt $P - 0.10) { return 1.0 }
            if ($s -lt $P + 0.06) { return 1.0 + ($sl - 1.0) * (Smooth (($s - ($P - 0.10)) / 0.16)) }
            if ($s -lt $c) { return $sl }
            if ($s -lt $c + 0.12) { return $sl + (1.8 - $sl) * (Smooth (($s - $c) / 0.12)) }
            if ($s -lt $c + 0.5) { return 1.8 }
            if ($s -lt $c + 0.8) { return 1.8 + ($post - 1.8) * (Smooth (($s - $c - 0.5) / 0.3)) }
            if ($post -ne 1.0 -and $s -lt $calmAt) { return $post }
            if ($post -ne 1.0 -and $s -lt $calmAt + 0.25) { return $post + (1.0 - $post) * (Smooth (($s - $calmAt) / 0.25)) }
            return 1.0
        }.GetNewClosure()
        $zoom = {
            param($s)
            if ($s -lt $P - 0.14) { return 1.0 }
            if ($s -lt $P + 0.08) { return 1.0 + ($zm - 1.0) * (Smooth (($s - ($P - 0.14)) / 0.22)) }
            if ($s -lt $c) { return $zm + 0.04 * ($s - $P - 0.08) / [Math]::Max(0.05, $c - $P - 0.08) }
            if ($s -lt $c + 0.38) { return ($zm + 0.04) - ($zm + 0.04 - 1.0) * (Smooth (($s - $c) / 0.38)) }
            return 1.0
        }.GetNewClosure()
        # Başlangıç: patlama çıkışta lead saniyesine düşsün.
        $s0 = $P - $g.lead
        for ($it = 0; $it -lt 3; $it++) {
            $s = $s0; $tOut = 0.0
            while ($s -lt $P -and $tOut -lt 10) { $s += (& $speed $s) / 30.0; $tOut += 1.0 / 30 }
            $s0 += ($tOut - $g.lead)
        }
        $s0 = [Math]::Max(0.0, $s0)
        # Yakınlaşma merkezi: patlama noktası, tahtanın ortasına biraz çekilir (bağlam kaybolmasın).
        $cx = 540 + ($hero.x - 540) * 0.7; $cy = 1050 + ($hero.y - 1050) * 0.7
    } else {
        $speed = { param($s) 1.0 }
        $zoom = { param($s) 1.0 }
        $s0 = $g.at; $cx = 540; $cy = 1000
    }

    # Kare seçimi (sabit bağlantı) ve çıkış karesi başına yakınlaşma; ses için kaynak zamanları.
    $segDir = "$tmp/seg_$k"; New-Item -ItemType Directory -Force $segDir | Out-Null
    $s = $s0; $src = New-Object double[] ($n + 1); $zs = New-Object double[] $n
    for ($i = 0; $i -lt $n; $i++) {
        $src[$i] = $s
        $want = [Math]::Max(0, [Math]::Min($maxF, [int][Math]::Round($s * $fps)))
        $file = $null
        for ($o = 0; $o -le 6 -and -not $file; $o++) { if ($have.ContainsKey($want - $o)) { $file = $have[$want - $o] } elseif ($have.ContainsKey($want + $o)) { $file = $have[$want + $o] } }
        if (-not $file) { throw "Kare yok: $dir $want" }
        New-Item -ItemType HardLink -Path (Join-Path $Root ("$segDir/f_{0:00000}.png" -f $i)) -Target $file | Out-Null
        $zs[$i] = & $zoom $s
        $s += (& $speed $s) / 30.0
    }
    $src[$n] = $s
    $heroOut = -1
    if ($isHero) { for ($i = 0; $i -lt $n; $i++) { if ($src[$i] -ge $P) { $heroOut = $i; break } } }

    # Yakınlaşma ifadesi: çıkış karesine göre parçalı doğrusal (3 karede bir düğüm).
    if ($isHero) {
        $zexpr = (F $zs[$n - 1])
        for ($i = [int]([Math]::Floor(($n - 1) / 3) * 3); $i -ge 0; $i -= 3) {
            $j = [Math]::Min($i + 3, $n - 1)
            if ($j -le $i) { continue }
            if ([Math]::Abs($zs[$i] - 1) -lt 0.0005 -and [Math]::Abs($zs[$j] - 1) -lt 0.0005) { $zexpr = "if(lt(on,$j),1,$zexpr)"; continue }
            $zexpr = "if(lt(on,$j),$(F $zs[$i])+($(F ($zs[$j] - $zs[$i])))*(on-$i)/$($j - $i),$zexpr)"
        }
        # Patlamada kısa, sönümlü sarsıntı.
        $shake = if ($heroOut -ge 0) { "+gte(on,$heroOut)*14*exp(-(on-$heroOut)/7)*sin((on-$heroOut)*2.3)" } else { '' }
        $shakeY = if ($heroOut -ge 0) { "+gte(on,$heroOut)*10*exp(-(on-$heroOut)/7)*cos((on-$heroOut)*2.9)" } else { '' }
    } else {
        $zexpr = "1.0+0.035*on/$n"; $shake = ''; $shakeY = ''
    }
    $vf = "zoompan=z='max(1,$zexpr)':x='max(0,min(iw-iw/zoom,$(F $cx)-iw/zoom/2$shake))':y='max(0,min(ih-ih/zoom,$(F $cy)-ih/zoom/2$shakeY))':d=1:s=1080x1920:fps=30,format=rgba[base];" +
          "[base][1:v]overlay=0:H-h,format=yuv420p"
    $file = TextFile $g.cap $tx[$g.cap]
    $ca = if ($g.ca -lt 0) { 0 } else { $g.ca }
    $ce = if ($g.ce) { $g.ce } else { $g.d - 0.05 }
    $size = switch ($g.cap) { 'levels' { 150 } 'roll' { if ($Lang -eq 'ar') { 82 } else { 86 } } 'core' { if ($Lang -eq 'ar') { 84 } else { 76 } } default { 96 } }
    $vf += "," + (Caption $file $ca $ce $size 'h*0.84' 'white' ($g.ca -ge 0))
    if ($g.flash) { $vf += ",fade=t=in:st=0:d=0.18:color=white" }
    if ($heroOut -ge 0) { $tf = F ($heroOut / 30.0); $vf += ",eq=brightness='0.10*exp(-(t-$tf)*9)*gte(t,$tf)':eval=frame" }

    # Ses: kaynak efektleri aynı hız eğrisiyle, örnek düzeyinde yeniden örneklenir (bant gibi: yavaşta ses kalınlaşır,
    # hızlanmada incelir — sinematik yavaş çekim sesi). Çıkış karesi başına kaynak zamanı src[] arasında doğrusal.
    $segWav = "$tmp/seg_$k.wav"
    [SfxRemap]::Run((Join-Path $Root "$dir/sfx.wav"), (Join-Path $Root $segWav), $src, 30.0)
    $afc = "[2:a]aformat=channel_layouts=stereo,afade=t=in:d=0.03,afade=t=out:st=$(F ($g.d - 0.05)):d=0.05[a]"

    $out = "$tmp/seg_$k.mov"
    $fcFile = "$tmp/seg_$k.fc"
    [IO.File]::WriteAllText((Join-Path $Root $fcFile), "[0:v]$vf[v];$afc", $utf8)
    & $ff -y -loglevel error -framerate 30 -i "$segDir/f_%05d.png" -loop 1 -i $grad -i $segWav `
        -/filter_complex $fcFile -map '[v]' -map '[a]' -frames:v $n -t $g.d -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -r 30 -c:a pcm_s16le $out
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg kesim hatası: $($g.s)" }
    if ($heroOut -ge 0) { $heroes += $t0 + $heroOut / 30.0 }
    $clips += $out; $t0 += $g.d; $cuts += $t0; $k++
    "{0,-16} {1,4:0.0} sn  → {2,5:0.0}{3}" -f $g.s, $g.d, $t0, $(if ($heroOut -ge 0) { "  (patlama +$([Math]::Round($heroOut / 30.0, 2)) sn)" } else { '' })
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
        "[bg][lg]overlay=x='(W-w)/2':y='H*0.36-h/2':eval=frame,format=yuv420p,$capCta,$capSub,fade=t=in:st=0:d=0.15:color=white,fade=t=out:st=$(F ($endD - 0.4)):d=0.4[v]") `
    -map '[v]' -map 2:a -t $endD -r 30 -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -c:a pcm_s16le $end
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg kapanış hatası' }
$clips += $end
$total = $t0 + $endD

# Birleştir (sert kesim) ve sesi kur: oyun efektleri + müzik + her kesimde "vuuş" + patlamalarda ve logoda darbe.
$list = "$tmp/list.txt"
[IO.File]::WriteAllText((Join-Path $Root $list), (($clips | ForEach-Object { "file '" + (Split-Path $_ -Leaf) + "'" }) -join "`n"), $utf8)
$cat = "$tmp/cat.mov"
& $ff -y -loglevel error -f concat -safe 0 -i $list -c copy $cat
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg birleştirme hatası' }

$inputs = @('-i', $cat, '-i', 'Logs/punch_music.wav')
$fc = "[0:a]volume=${SfxGain}[sfx];[1:a]atrim=0:$(F $total),volume=${MusicGain},afade=t=out:st=$(F ($total - 1.0)):d=1.0[mus];"
$mixes = @('[sfx]', '[mus]'); $ix = 2
foreach ($c in $cuts) {
    if ($c -ge $t0) { continue } # son kesim kapanışa geçiş: orada darbe var
    $ms = [int](($c - 0.28) * 1000)
    $inputs += @('-i', 'Logs/punch_whoosh.wav')
    $fc += "[${ix}:a]adelay=${ms}|${ms},volume=0.5[w$ix];"; $mixes += "[w$ix]"; $ix++
}
foreach ($c in (@($heroes) + @($t0))) {
    $ms = [int]([Math]::Max(0, $c - 0.02) * 1000)
    $inputs += @('-i', 'Logs/punch_impact.wav')
    $fc += "[${ix}:a]adelay=${ms}|${ms},volume=0.75[i$ix];"; $mixes += "[i$ix]"; $ix++
}
$fc += ($mixes -join '') + "amix=inputs=$($mixes.Count):duration=first:normalize=0,alimiter=limit=0.95,loudnorm=I=-14:TP=-1.5:LRA=11,aresample=48000[a]"
$final = "Builds/Video/StoneBurst_promo_$Lang.mp4"
& $ff -y -loglevel error @inputs -filter_complex $fc -map 0:v -map '[a]' -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -r 30 -c:a aac -b:a 192k -movflags +faststart -t $total $final
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg son kurgu hatası' }
"Video: $final ({0:0.0} sn)" -f $total
