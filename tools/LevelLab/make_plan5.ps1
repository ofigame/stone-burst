# v5 plan üretici: 6–400 profilleri (docs/v5/KURALLAR_v5.md §6–7).
# Mekanik takvimi eski planlardan korunur (ataş 21, yıldız 41, buz 61, kilit 121, zarf 201, döner taban 301);
# tahta, katman, hedef ve zorluk v5 eğrisinden gelir. Rulo yuvası yok (rulo roketi yerini aldı).
param([int]$From = 6, [int]$To = 400, [string]$Out = 'plan5_006_400.txt')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$old = @{}
foreach ($f in Get-ChildItem plan_*.txt) {
    foreach ($l in Get-Content $f -Encoding utf8) {
        if ($l -match '^\s*(\d{3})\s*\|(.*)$') {
            $kv = @{}
            foreach ($p in $matches[2].Split('|')) { $i = $p.IndexOf('='); if ($i -gt 0) { $kv[$p.Substring(0, $i).Trim()] = $p.Substring($i + 1).Trim() } }
            $old[[int]$matches[1]] = $kv
        }
    }
}
function Lerp($a, $b, $t) { $a + ($b - $a) * $t }
$intro = @(6, 21, 41, 61, 121, 201, 301, 401, 501, 601, 701)
$special = @{
    6  = @{ title = 'Roket Zamanı'; tutorial = 'Beş ya da daha fazla taşı birden soy: rulo roketi alttaki tepsine gelir!' }
    7  = @{ title = 'Roket Yolu'; tutorial = 'Tepsideki rokete dokun, sonra bir taşa: o satırı (ya da sütunu) sarar. Hamle harcamaz!' }
    9  = @{ title = 'Altın Rulo'; tutorial = 'Sekiz ya da daha fazla taş: Altın Rulo tepsine gelir. Bir taşa dokun, o rengin hepsini toplar!' }
    12 = @{ title = 'Büyük Gösteri'; tutorial = 'Özel taşları biriktir: tepside dört yer var, istediğin an kullan!' }
    15 = @{ title = 'Domino'; tutorial = 'Alttan aynı renk açılınca kendiliğinden soyulur: dominoyu hazırla!' }
}
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('// v5 planı 6–400 (make_plan5.ps1 ile üretildi; elle düzenleme yerine betiği değiştir).')
for ($n = $From; $n -le $To; $n++) {
    $o = if ($old.ContainsKey($n)) { $old[$n] } else { @{} }
    $r0 = $n % 10
    # 401+ mekanik takvimi (rehber §11): 401 sünger, 501 bağlı ataş, 601 düğme/geçit, 701 boya, 801+ karışım.
    # x5–x6 bölümlerine eski mekaniklerden biri serpiştirilir (çeşit).
    if ($n -gt 400) {
        $o = @{}
        $ir = ($n - 1) % 100
        $blk = [math]::Floor(($n - 1) / 100)
        $old4 = @('ice1=4', 'lock=c:6', 'env=b,y', 'rotor=2')
        switch ($blk) {
            4 { $o['sponge'] = if ($ir -lt 30) { 'cb' } elseif ($ir -lt 70) { 'cb;by' } else { 'cby;cb' } }
            5 { $o['link'] = if ($ir -lt 30) { '2x1' } elseif ($ir -lt 70) { '2x2' } else { '2x1,3x2' } }
            6 { $o['gate'] = if ($ir -lt 30) { 2 } else { 3 } }
            7 { $o['paint'] = 1; if ($r0 -ge 4) { $o['sponge'] = 'cb' } }
            default {
                $mix = @('sponge', 'link', 'gate', 'paint', 'ice', 'lock', 'env', 'rotor')
                $a = $mix[$n % $mix.Count]; $b = $mix[($n * 3 + 1) % $mix.Count]
                foreach ($m in @($a, $b) | Select-Object -Unique) {
                    switch ($m) { 'sponge' { $o['sponge'] = 'cby' } 'link' { $o['link'] = '2x2' } 'gate' { $o['gate'] = 3 } 'paint' { $o['paint'] = 1 }
                        'ice' { $o['ice1'] = 3; $o['ice2'] = 2 } 'lock' { $o['lock'] = 'c:5,b:5' } 'env' { $o['env'] = 'y,p' } 'rotor' { $o['rotor'] = 2 } }
                }
            }
        }
        if ($blk -le 7 -and ($r0 -eq 5 -or $r0 -eq 6)) { $kv = $old4[$n % 4].Split('='); $o[$kv[0]] = $kv[1] }
        if ($n -eq 401) { $o['title'] = 'Sünger'; $o['tutorial'] = 'Süngerin yanında istenen renkleri soy: her renk bir yuva doldurur, hepsi dolunca sünger kalkar.' }
        if ($n -eq 501) { $o['title'] = 'Bağlı Ataş'; $o['tutorial'] = 'Bağlı ataşlar tek sayaç taşır: herhangi birinin yanında soy, hepsi birlikte gevşer.' }
        if ($n -eq 601) { $o['title'] = 'Gizli Geçit'; $o['tutorial'] = 'Düğmenin üstündeki yığını bitir: soluk geçit taşları tahtaya katılır.' }
        if ($n -eq 701) { $o['title'] = 'Boya Damlası'; $o['tutorial'] = 'Boya damlası: bir taşın üst rengini değiştir, yeni grubu sen seç. Bir hamle sayılır.' }
        if ($n % 100 -eq 0) { $o['title'] = "$n. Bölüm Finali" }
    }
    # Uzun ölçek (kullanıcı: önce 1000, sonra 10 000 bölüm): taban zorluk doygunlaşan eğri,
    # 100 → 0.21, 400 → 0.58, 1000 → 0.80; 1000 sonrası yavaş log artış (10 000 → 0.85, tavan 0.9).
    $base = if ($n -le 1000) { 0.85 * (1 - [math]::Exp(-($n - 6) / 350.0)) } else { [math]::Min(0.9, 0.80 + 0.05 * [math]::Log10($n / 1000.0)) }
    # Bölge dalgası (100'lük bölge): bölge içinde +0.08'e kadar tırmanır; yeni bölgenin ilk 5 bölümü nefes.
    $inRegion = (($n - 1) % 100)
    $wave = 0.08 * $inRegion / 99.0
    if ($inRegion -lt 5) { $wave = -0.10 }
    # 10'luk ritim: x1–x3 nefes, x4–x6 orta, x7–x9 zor, x0 final.
    $r = $n % 10
    $adj = 0.0
    if ($r -ge 1 -and $r -le 3) { $adj = -0.12 } elseif ($r -ge 7) { $adj = 0.08 } elseif ($r -eq 0) { $adj = 0.12 }
    $isIntro = $false
    foreach ($s in $intro) { if ($n -ge $s -and $n -le $s + 2) { $isIntro = $true } }
    if ($isIntro) { $adj = [math]::Min($adj, -0.15) }
    $d = [math]::Max(0.0, [math]::Min(1.0, $base + $wave + $adj))   # etkin zorluk 0..1
    $c = Lerp 0.85 0.30 $d
    $lo = [math]::Max(0.12, $c - 0.12); $hi = [math]::Min(0.95, $c + 0.12)

    if ($n -lt 31) { $w = 6; $h = 7 } elseif ($n -lt 121) { $w = 7; $h = 8 } else { $w = 7; $h = 9 }
    if ($r -eq 0 -and $n -ge 100) { $w = 8; $h = 9 }
    $layers = if ($n -lt 16) { '2-3' } elseif ($d -lt 0.3) { '3-3' } elseif ($d -lt 0.6 -or $adj -lt 0) { '3-4' } else { '4-4' }
    $colors = if ($n -lt 21) { 'cby' } else { 'cbyp' }
    $ng = if ($n -lt 16) { 1 } elseif ($d -lt 0.45 -or $adj -lt 0) { 2 } else { 3 }
    $ratio = [math]::Round((Lerp 0.62 0.92 $d), 2)
    $cols = $colors.ToCharArray()
    $goals = @()
    for ($k = 0; $k -lt $ng; $k++) {
        $ch = $cols[($n * 7 + $k * 3) % $cols.Length]
        while ($goals -match "^${ch}:") { $ch = $cols[([array]::IndexOf($cols, $ch) + 1) % $cols.Length] }
        $rr = [math]::Round($ratio - 0.1 * $k, 2)
        $goals += "${ch}:$rr"
    }
    $lenLo = [int][math]::Round((Lerp 6 12 $d)); $lenHi = $lenLo + 5
    $slack = if ($d -lt 0.35) { 4 } else { 3 }
    $spec = 1 + [int]($d -gt 0.3) + [int]($r -eq 0)
    $chains = 1 + [int]($d -gt 0.5)

    $parts = @("{0:000}" -f $n)
    $title = if ($special.ContainsKey($n)) { $special[$n].title } elseif ($o.title) { $o.title } else { "Bölüm $n" }
    $parts += "title=$title"
    $parts += "w=$w"; $parts += "h=$h"; $parts += "layers=$layers"; $parts += "colors=$colors"
    $parts += "goals=" + ($goals -join ',')
    $parts += "len=$lenLo-$lenHi"; $parts += "slack=$slack"
    $parts += ("bot={0}-{1}" -f [math]::Round($lo, 2).ToString([Globalization.CultureInfo]::InvariantCulture), [math]::Round($hi, 2).ToString([Globalization.CultureInfo]::InvariantCulture))
    $parts += "specials=$spec"; $parts += "chains=$chains"
    # Büyük tahtada örtü sayıları ×1.5 (yuvarlama yukarı), kilit miktarı ×1.6.
    $scale = if ($w * $h -ge 56) { 1.5 } else { 1.2 }
    foreach ($k in 'clip1', 'clip2', 'stars', 'ice1', 'ice2', 'rotor', 'gate') {
        if ($o.ContainsKey($k)) { $v = [int][math]::Ceiling([int]$o[$k] * $scale); if ($v -gt 0) { $parts += "$k=$v" } }
    }
    if ($o.ContainsKey('lock')) {
        $locks = foreach ($l in $o['lock'].Split(',')) { $p = $l.Split(':'); "{0}:{1}" -f $p[0], [int][math]::Ceiling([int]$p[1] * 1.6) }
        $parts += 'lock=' + ($locks -join ',')
    }
    if ($o.ContainsKey('env')) { $parts += "env=$($o['env'])" }
    foreach ($k in 'sponge', 'link', 'paint') { if ($o.ContainsKey($k)) { $parts += "$k=$($o[$k])" } }
    if ($o.ContainsKey('rotorMin')) { $parts += "rotorMin=$($o['rotorMin'])" }
    if ($o.ContainsKey('holes')) { $parts += "holes=$([int]$o['holes'])" }
    if ($colors -eq 'cby' -and $n -lt 12) { $parts += 'blobMin=3'; $parts += 'blobMax=8' }
    $parts += 'iters=100'; $parts += 'beam=50'; $parts += 'games=20'
    $tut = if ($special.ContainsKey($n)) { $special[$n].tutorial } elseif ($o.tutorial -and $o.tutorial -notmatch '[Rr]ulo') { $o.tutorial } else { $null }
    if ($tut) { $parts += "tutorial=$tut" }
    [void]$sb.AppendLine(($parts -join ' | '))
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot $Out), $sb.ToString(), (New-Object Text.UTF8Encoding $false))
"$Out`: $($To - $From + 1) profil"
