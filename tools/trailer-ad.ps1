# Stone Burst — Meta (Facebook/Instagram Reels, Stories) reklamı (2026-11-19).
# Kullanıcı: "senden çok güzel reklam için edit istiyorum" — Mısır'daki Huawei kullanıcıları, Arapça, 9:16 (1080×1920, 30 fps),
# ~20 sn. Kalıp: ilk saniyede en gösterişli an (Altın Rulo yıldırımı, yavaş çekim + yakınlaşma), 2–4 sn'lik sert kesimler
# (temel kural, Roket, buz/sarmaşık, "hamleler sınırlı, planla" + zafer, Yolculuk haritası), sonda ikon + logo + AppGallery çağrısı.
# Yalnız gerçek özellikler (güncel metin: docs/yayin/APPGALLERY_1_0_2.md): zincirleme patlama yok, "1000 bölüm" iddiası yok.
# Yazılar Instagram/Facebook arayüzünün kapattığı alanların dışında (üst ~250 px, alt ~350 px): oyun yazıları y≈600'de
# (hedef şeridi ile tahta arası), kapanış öğeleri 330–1520 px arasında. 4:5 (1080×1350) kesim 9:16'dan y=285'ten kırpılır;
# bu yüzden önemli her şey 285–1570 px aralığında tutuldu.
# Vurucu kurgunun (tools/trailer-punch.ps1, 2026-11-12) hız rampası / yakınlaşma / ses yeniden örnekleme mantığı aynen kullanılır.
# Önce kareler: tools/unity.ps1 trailer-ad   (KATKAT_TRAILER_LANGS=ar; Logs/ad_<dil>/<sahne>/f_*.png, sfx.wav, meta.txt;
#               müzik ve efektler Logs/ad_music.wav, ad_whoosh.wav, ad_impact.wav)
# Sonra:        powershell -ExecutionPolicy Bypass -File tools/trailer-ad.ps1 [-Lang ar] [-No45]
# Çıktı:        Builds/Video/StoneBurst_meta_<dil>_9x16.mp4 ve _4x5.mp4
param([string]$Lang = 'ar', [double]$MusicGain = 0.8, [double]$SfxGain = 1.3, [switch]$No45)

$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture # filtrelerde ondalık nokta
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root
$ff = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
if (-not $ff) { $ff = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter ffmpeg.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName }
if (-not $ff) { throw 'ffmpeg bulunamadı' }

$frames = "Logs/ad_$Lang"
$tmp = "Logs/ad_tmp_$Lang"
if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
New-Item -ItemType Directory -Force $tmp | Out-Null
New-Item -ItemType Directory -Force 'Builds/Video' | Out-Null
$logo = 'Assets/KatKat/Resources/PremiumV4/ui/logo-stoneburst.png'
$icon = 'docs/varliklar/gorseller/arayuz/uygulama-ikonu-v5.png'
$utf8 = New-Object Text.UTF8Encoding $false
$rlm = [char]0x200F

# Yazı tipi: Arapçada kalın ve dolgun görünsün diye Segoe UI Bold (Windows; Arapça harfleri var). Yoksa oyunun Noto Sans Arabic'i.
# Yol filtre içinde iki nokta içermesin diye geçici klasöre kopyalanır.
$fontSrc = if ($Lang -eq 'ar' -and (Test-Path "$env:WINDIR\Fonts\segoeuib.ttf")) { "$env:WINDIR\Fonts\segoeuib.ttf" }
           elseif ($Lang -eq 'ar') { 'Assets/KatKat/Resources/Typography/NotoSansArabic.ttf' }
           else { 'Assets/KatKat/Resources/Typography/Nunito-ExtraBold.ttf' }
Copy-Item $fontSrc "$tmp/font.ttf"
$font = "$tmp/font.ttf"

# Metinler (satır sonu `n). Arapça: her satır RLM ile sarılır (noktalama doğru uca otursun). Hareke (tenvin, şedde) yok:
# kalın yazı tipinde üst üste biniyor. Terimler mağaza metniyle aynı: اللفافة الذهبية (Altın Rulo), الصاروخ (Roket),
# الجليد (buz), اللبلاب (sarmaşık).
$T = @{
    'ar' = @{
        hook  = 'اللفافة الذهبية' + "`n" + 'تجمع كل أحجار اللون!'          # Altın Rulo / o rengin bütün taşlarını toplar!
        core  = 'المس 3 أحجار متشابهة' + "`n" + 'وانزع الطبقات!'           # 3 aynı taşa dokun / ve katmanları soy!
        rocket = 'الصاروخ' + "`n" + 'يضرب العمود كله!'                     # Roket / bütün sütunu vurur!
        obst  = 'جليد ولبلاب…' + "`n" + 'عقبات تتحداك!'                    # Buz ve sarmaşık… / sana meydan okuyan engeller!
        plan  = 'الحركات محدودة' + "`n" + 'خطط لكل حركة!'                  # Hamleler sınırlı / her hamleni planla!
        map   = 'خريطة رحلة' + "`n" + 'مليئة بالألغاز'                      # Yolculuk haritası / bulmacalarla dolu
        cta   = 'حملها الآن'                                                 # Hemen indir
        store = 'من AppGallery'                                              # AppGallery'den
        sub   = 'لعبة ألغاز مجانية'                                          # Ücretsiz bulmaca oyunu
    }
    'tr' = @{
        hook  = 'Altın Rulo' + "`n" + 'o rengin bütün taşlarını toplar!'
        core  = 'Aynı renkten 3 taşa dokun,' + "`n" + 'katları soy'
        rocket = 'Roket' + "`n" + 'bütün sütunu vurur!'
        obst  = 'Buz, sarmaşık…' + "`n" + 'zorlu engeller!'
        plan  = 'Hamleler sınırlı' + "`n" + 'her hamleni planla!'
        map   = 'Yolculuk haritası' + "`n" + 'bulmacalarla dolu'
        cta   = 'Hemen indir'
        store = "AppGallery'den"
        sub   = 'Ücretsiz bulmaca oyunu'
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

# Büyük yazı: koyu kalın kontur + gölge, üstüne renkli dolgu. Fırlayarak girer (pop). y = satır bloğunun merkezi.
function Caption([string]$file, [double]$a, [double]$b, [int]$size, [string]$y, [string]$color = 'white', [bool]$pop = $true, [int]$bw = 14) {
    $tt = "(t-$(F $a))"
    $fs = if ($pop) { "'$size*if(lt($tt,0.12),0.55+0.6*$tt/0.12,if(lt($tt,0.22),1.15-0.15*($tt-0.12)/0.10,1))'" } else { "$size" }
    $common = "fontfile=${font}:textfile=${file}:text_align=C:line_spacing=6:fontsize=${fs}:x=(w-text_w)/2:y=$y-text_h/2:enable='between(t,$(F $a),$(F $b))'"
    return "drawtext=${common}:fontcolor=0x10082A@0.6:borderw=$($bw + 12):bordercolor=0x10082A@0.5:shadowx=0:shadowy=12:shadowcolor=black@0.4," +
           "drawtext=${common}:fontcolor=${color}:borderw=${bw}:bordercolor=0x2A0F4A"
}

# Yazı arkası: yatay, yumuşak kenarlı koyu bant (parlak arka planlarda da okunur).
$band = "$tmp/band.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=1080x300,format=rgba' -vf "geq=r=0:g=0:b=0:a='120*pow(sin(PI*Y/H)\,1.6)'" -frames:v 1 $band
if ($LASTEXITCODE -ne 0) { throw 'bant görseli' }

Add-Type -TypeDefinition @'
using System; using System.IO;
public static class AdSfxRemap {
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

# Kurgu (120 BPM; kesimler 0,5 sn'lik vuruşlarda). Müzik: 0'da sert giriş, 14–16 sn gerilim (harita), 16 sn kapanış akoru.
# mode hero: kahraman an (meta.txt "hero") çevresinde hız rampası + yakınlaşma + sarsıntı; lead = patlamanın kesimdeki saniyesi.
# mode normal: at = kaynakta başlangıç saniyesi. ca = yazının giriş saniyesi (-1: önceki yazı fırlamadan sürer), ce = bitiş.
$segs = @(
    @{ s = 'a1_rulo_l174';     d = 2.5; mode = 'hero';   lead = 0.6;  slow = 0.3;  hold = 0.6;  zmax = 1.4;  cap = 'hook';   ca = 0.08 }
    @{ s = 'a2_temel_l017';    d = 2.5; mode = 'normal'; at = 0.3;  cap = 'core';   ca = 0.1; flash = $true }
    @{ s = 'a3_roket_l095';    d = 2.5; mode = 'hero';   lead = 0.9;  slow = 0.3;  hold = 0.55; zmax = 1.25; cy = 1080; cap = 'rocket'; ca = 0.1 }
    # Engeller: 7 buz + 7 sarmaşıklı bölüm (L860; a4_buz_l087 ve a5_sarmasik_l059 da çekildi, engeller orada az görünüyor).
    @{ s = 'a4b_engel_l860';   d = 2.5; mode = 'normal'; at = 0.35; cap = 'obst';   ca = 0.1 }
    # Zafer: son hamle yavaş çekim; ardından kutlama hızlı akar (post), kart açılırken (kaynak sonundan calm sn önce) normal hız.
    @{ s = 'a6_zafer_l042';    d = 4.0; mode = 'hero';   lead = 0.7;  slow = 0.35; hold = 0.3;  zmax = 1.3;  cap = 'plan';   ca = 0.1; ce = 2.0; post = 4.0; calm = 0.9; flash = $true }
    @{ s = 'a7_harita';        d = 2.0; mode = 'normal'; at = 0.2;  cap = 'map';    ca = 0.15 }
)
$capY = 600      # oyun yazılarının merkezi (hedef şeridi ile tahta arası; güvenli bölgede)
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
    $have = @{}; $maxF = 0
    foreach ($f in (Get-ChildItem $dir -Filter 'f_*.png')) { $i = [int]$f.BaseName.Substring(2); $have[$i] = $f.FullName; if ($i -gt $maxF) { $maxF = $i } }
    $n = [int][Math]::Round($g.d * 30)
    $isHero = $g.mode -eq 'hero' -and $hero
    if ($g.mode -eq 'hero' -and -not $hero) { throw "meta.txt'de kahraman an yok: $dir" }

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
        $s0 = $P - $g.lead
        for ($it = 0; $it -lt 3; $it++) {
            $s = $s0; $tOut = 0.0
            while ($s -lt $P -and $tOut -lt 10) { $s += (& $speed $s) / 30.0; $tOut += 1.0 / 30 }
            $s0 += ($tOut - $g.lead)
        }
        $s0 = [Math]::Max(0.0, $s0)
        $cx = 540 + ($hero.x - 540) * 0.7; $cy = 1050 + ($hero.y - 1050) * 0.7
        if ($g.cx) { $cx = $g.cx }; if ($g.cy) { $cy = $g.cy } # elle merkez (ör. roket sütun boyunca iner)
    } else {
        $speed = { param($s) 1.0 }
        $zoom = { param($s) 1.0 }
        $s0 = $g.at; $cx = 540; $cy = 1050
    }

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

    if ($isHero) {
        $zexpr = (F $zs[$n - 1])
        for ($i = [int]([Math]::Floor(($n - 1) / 3) * 3); $i -ge 0; $i -= 3) {
            $j = [Math]::Min($i + 3, $n - 1)
            if ($j -le $i) { continue }
            if ([Math]::Abs($zs[$i] - 1) -lt 0.0005 -and [Math]::Abs($zs[$j] - 1) -lt 0.0005) { $zexpr = "if(lt(on,$j),1,$zexpr)"; continue }
            $zexpr = "if(lt(on,$j),$(F $zs[$i])+($(F ($zs[$j] - $zs[$i])))*(on-$i)/$($j - $i),$zexpr)"
        }
        $shake = if ($heroOut -ge 0) { "+gte(on,$heroOut)*14*exp(-(on-$heroOut)/7)*sin((on-$heroOut)*2.3)" } else { '' }
        $shakeY = if ($heroOut -ge 0) { "+gte(on,$heroOut)*10*exp(-(on-$heroOut)/7)*cos((on-$heroOut)*2.9)" } else { '' }
    } else {
        # Normal kesimlerde yavaş "itme" yakınlaşması: durağan görünmesin.
        $zexpr = "1.0+0.05*on/$n"; $shake = ''; $shakeY = ''
    }
    $vf = "zoompan=z='max(1,$zexpr)':x='max(0,min(iw-iw/zoom,$(F $cx)-iw/zoom/2$shake))':y='max(0,min(ih-ih/zoom,$(F $cy)-ih/zoom/2$shakeY))':d=1:s=1080x1920:fps=30,format=rgba[base];" +
          "[base][1:v]overlay=0:$($capY - 150),format=yuv420p"
    $file = TextFile $g.cap $tx[$g.cap]
    $ca = if ($g.ca -lt 0) { 0 } else { $g.ca }
    $ce = if ($g.ce) { $g.ce } else { $g.d }
    $size = switch ($g.cap) { 'core' { 84 } 'hook' { 96 } default { 100 } }
    $vf += "," + (Caption $file $ca $ce $size "$capY" 'white' ($g.ca -ge 0))
    if ($g.flash) { $vf += ",fade=t=in:st=0:d=0.15:color=white" }
    if ($heroOut -ge 0) { $tf = F ($heroOut / 30.0); $vf += ",eq=brightness='0.10*exp(-(t-$tf)*9)*gte(t,$tf)':eval=frame" }

    $segWav = "$tmp/seg_$k.wav"
    [AdSfxRemap]::Run((Join-Path $Root "$dir/sfx.wav"), (Join-Path $Root $segWav), $src, 30.0)
    $afc = "[2:a]aformat=channel_layouts=stereo,afade=t=in:d=0.03,afade=t=out:st=$(F ($g.d - 0.05)):d=0.05[a]"

    $out = "$tmp/seg_$k.mov"
    $fcFile = "$tmp/seg_$k.fc"
    [IO.File]::WriteAllText((Join-Path $Root $fcFile), "[0:v]$vf[v];$afc", $utf8)
    & $ff -y -loglevel error -framerate 30 -i "$segDir/f_%05d.png" -loop 1 -i $band -i $segWav `
        -/filter_complex $fcFile -map '[v]' -map '[a]' -frames:v $n -t $g.d -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -r 30 -c:a pcm_s16le $out
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg kesim hatası: $($g.s)" }
    if ($heroOut -ge 0) { $heroes += $t0 + $heroOut / 30.0 }
    $clips += $out; $t0 += $g.d; $cuts += $t0; $k++
    "{0,-18} {1,4:0.0} sn  → {2,5:0.0}{3}" -f $g.s, $g.d, $t0, $(if ($heroOut -ge 0) { "  (patlama +$([Math]::Round($heroOut / 30.0, 2)) sn)" } else { '' })
}

# ---- Kapanış kartı: haritanın son karesi bulanık ve koyu; ikon, logo, "indir" düğmesi, AppGallery ve alt yazı sırayla fırlar.
$endD = 3.5
$last = (Get-ChildItem "$frames/a7_harita" -Filter 'f_*.png' | Sort-Object Name | Select-Object -Last 1).FullName
# İkon: yuvarlak köşe (yarıçap %22), beyaz çerçeve ve gölge.
$iconR = "$tmp/icon_round.png"
& $ff -y -loglevel error -i $icon -filter_complex (
    "[0:v]scale=360:360,format=rgba,geq=r='r(X,Y)':g='g(X,Y)':b='b(X,Y)':a='255*lte(hypot(max(0,abs(X-179.5)-100),max(0,abs(Y-179.5)-100)),80)'[ic];" +
    "color=white@0:s=400x420,format=rgba,geq=r=0:g=0:b=0:a='110*exp(-pow(max(0,hypot(max(0,abs(X-199.5)-104),max(0,abs(Y-219.5)-104))-80)/16,2))'[sh];" +
    "color=white:s=380x380,format=rgba,geq=r=255:g=255:b=255:a='255*lte(hypot(max(0,abs(X-189.5)-100),max(0,abs(Y-189.5)-100)),90)'[rim];" +
    "[sh][rim]overlay=10:10[a];[a][ic]overlay=20:20") -frames:v 1 $iconR
if ($LASTEXITCODE -ne 0) { throw 'ikon görseli' }
# Düğme: yeşil, yuvarlak uçlu, beyaz kenarlı (oyunun turkuaz-yeşil düğmeleri gibi).
$btn = "$tmp/button.png"
& $ff -y -loglevel error -f lavfi -i 'color=black:s=780x170,format=rgba' -vf (
    "geq=r='if(gt(hypot(max(0,abs(X-389.5)-305),max(0,abs(Y-84.5)-0)),76),255,40+20*Y/H)':" +
    "g='if(gt(hypot(max(0,abs(X-389.5)-305),max(0,abs(Y-84.5)-0)),76),255,215-70*Y/H)':" +
    "b='if(gt(hypot(max(0,abs(X-389.5)-305),max(0,abs(Y-84.5)-0)),76),255,140-50*Y/H)':" +
    "a='255*lte(hypot(max(0,abs(X-389.5)-305),max(0,abs(Y-84.5)-0)),84)'") -frames:v 1 $btn
if ($LASTEXITCODE -ne 0) { throw 'düğme görseli' }

$cta = TextFile 'cta' $tx.cta; $store = TextFile 'store' $tx.store; $sub = TextFile 'sub' $tx.sub
function Pop([double]$a, [double]$amp = 0.0) {
    $u = "(n/30-$(F $a))" # scale süzgecinde döngülü görselin t'si ilerlemiyor (ffmpeg 9): kare sayısından
    return "if(lt($u,0),0.01,if(lt($u,0.14),0.5+0.62*$u/0.14,if(lt($u,0.26),1.12-0.12*($u-0.14)/0.12,1+$(F $amp)*sin(2*PI*($u-0.26)/0.9))))"
}
$end = "$tmp/seg_end.mov"
$btnY = 1255
$capCta = (Caption $cta 0.55 $endD 104 "$btnY" 'white' $true 8) -replace 'borderw=20', 'borderw=12'
$capStore = Caption $store 0.8 $endD 84 '1395' '0xFFD84A' $true 12
$capSub = (Caption $sub 1.0 $endD 60 '1500' 'white' $true 8) -replace 'borderw=20', 'borderw=12'
& $ff -y -loglevel error -loop 1 -framerate 30 -i $last -loop 1 -framerate 30 -i $iconR -loop 1 -framerate 30 -i $logo -loop 1 -framerate 30 -i $btn -f lavfi -t $endD -i 'anullsrc=r=44100:cl=stereo' `
    -filter_complex ("[0:v]scale=1080:1920,gblur=sigma=26,eq=brightness=-0.22:saturation=1.1,format=rgba[bg];" +
        "[1:v]scale=w='400*($(Pop 0.0))':h=-1:eval=frame,format=rgba,pad=w=460:h=480:x=(ow-iw)/2:y=(oh-ih)/2:color=black@0:eval=frame[ic];" +
        "[2:v]scale=w='780*($(Pop 0.25))':h=-1:eval=frame,format=rgba,pad=w=880:h=580:x=(ow-iw)/2:y=(oh-ih)/2:color=black@0:eval=frame[lg];" +
        "[3:v]scale=w='780*($(Pop 0.5 0.035))':h=-1:eval=frame,format=rgba,pad=w=910:h=200:x=(ow-iw)/2:y=(oh-ih)/2:color=black@0:eval=frame[bt];" +
        "[bg][ic]overlay=x='(W-w)/2':y='505-h/2':eval=frame[v1];" +
        "[v1][lg]overlay=x='(W-w)/2':y='915-h/2':eval=frame[v2];" +
        "[v2][bt]overlay=x='(W-w)/2':y='$btnY-h/2':eval=frame,format=yuv420p,$capCta,$capStore,$capSub,fade=t=in:st=0:d=0.15:color=white,fade=t=out:st=$(F ($endD - 0.3)):d=0.3[v]") `
    -map '[v]' -map 4:a -t $endD -r 30 -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -c:a pcm_s16le $end
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg kapanış hatası' }
$clips += $end
$total = $t0 + $endD

# Birleştir (sert kesim) ve sesi kur: oyun efektleri + müzik + her kesimde "vuuş" + patlamalarda ve kapanışta darbe.
$list = "$tmp/list.txt"
[IO.File]::WriteAllText((Join-Path $Root $list), (($clips | ForEach-Object { "file '" + (Split-Path $_ -Leaf) + "'" }) -join "`n"), $utf8)
$cat = "$tmp/cat.mov"
& $ff -y -loglevel error -f concat -safe 0 -i $list -c copy $cat
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg birleştirme hatası' }

$inputs = @('-i', $cat, '-i', 'Logs/ad_music.wav')
$fc = "[0:a]volume=${SfxGain}[sfx];[1:a]atrim=0:$(F $total),asetpts=PTS-STARTPTS,volume=${MusicGain},afade=t=out:st=$(F ($total - 0.8)):d=0.8[mus];"
$mixes = @('[sfx]', '[mus]'); $ix = 2
foreach ($c in $cuts) {
    if ($c -ge $t0) { continue }
    $ms = [int](($c - 0.28) * 1000)
    $inputs += @('-i', 'Logs/ad_whoosh.wav')
    $fc += "[${ix}:a]adelay=${ms}|${ms},volume=0.5[w$ix];"; $mixes += "[w$ix]"; $ix++
}
foreach ($c in (@($heroes) + @($t0))) {
    $ms = [int]([Math]::Max(0, $c - 0.02) * 1000)
    $inputs += @('-i', 'Logs/ad_impact.wav')
    $fc += "[${ix}:a]adelay=${ms}|${ms},volume=0.75[i$ix];"; $mixes += "[i$ix]"; $ix++
}
$fc += ($mixes -join '') + "amix=inputs=$($mixes.Count):duration=first:normalize=0,alimiter=limit=0.95,loudnorm=I=-14:TP=-1.5:LRA=11,aresample=48000[a]"
$final = "Builds/Video/StoneBurst_meta_${Lang}_9x16.mp4"
# Meta: H.264 High, yuv420p, 30 fps, AAC 48 kHz; CRF 20 ile ~20 sn ≈ 15–25 MB.
& $ff -y -loglevel error @inputs -filter_complex $fc -map 0:v -map '[a]' -c:v libx264 -preset slow -crf 20 -profile:v high -pix_fmt yuv420p -r 30 -c:a aac -b:a 160k -movflags +faststart -t $total $final
if ($LASTEXITCODE -ne 0) { throw 'ffmpeg son kurgu hatası' }
"Video: $final ({0:0.0} sn)" -f $total

# 4:5 (1080×1350, akış): 9:16'nın y=285–1635 bandı (yazılar ve kapanış öğeleri bu bantta).
if (-not $No45) {
    $final45 = "Builds/Video/StoneBurst_meta_${Lang}_4x5.mp4"
    & $ff -y -loglevel error -i $final -vf 'crop=1080:1350:0:285' -c:v libx264 -preset slow -crf 20 -profile:v high -pix_fmt yuv420p -r 30 -c:a copy -movflags +faststart $final45
    if ($LASTEXITCODE -ne 0) { throw 'ffmpeg 4:5 hatası' }
    "Video: $final45"
}
