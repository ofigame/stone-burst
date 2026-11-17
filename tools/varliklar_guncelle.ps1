# docs/varliklar güncelleme (2026-11-18): oyunun kullandığı görsel, ses, yazı tipi ve 3B model dosyalarını
# Assets/KatKat/Resources ve Assets/KatKat/Art'tan docs/varliklar/oyunda/ altına aynalar (yerel kopya, depoya girmez)
# ve docs/varliklar/LISTE.md dosyasını yeniden yazar (depoda). Oyuna görsel/ses eklenince ya da çıkınca çalıştır:
#   powershell -NoProfile -File tools/varliklar_guncelle.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dst = Join-Path $root 'docs\varliklar\oyunda'
$types = @('*.png', '*.jpg', '*.jpeg', '*.wav', '*.ogg', '*.mp3', '*.ttf', '*.otf', '*.fbx')
$sources = [ordered]@{ 'Resources' = 'Assets\KatKat\Resources'; 'Art' = 'Assets\KatKat\Art' }

foreach ($k in $sources.Keys) {
    $src = Join-Path $root $sources[$k]
    $args = @($src, (Join-Path $dst $k)) + $types + @('/MIR', '/NFL', '/NDL', '/NJH', '/NJS', '/NP', '/R:1', '/W:1')
    & robocopy @args | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy hata ($k): $LASTEXITCODE" }
}

function Kind($ext) {
    switch ($ext.ToLowerInvariant()) {
        { $_ -in '.png', '.jpg', '.jpeg' } { 'görsel' }
        { $_ -in '.wav', '.ogg', '.mp3' } { 'ses' }
        { $_ -in '.ttf', '.otf' } { 'yazı tipi' }
        '.fbx' { '3B model' }
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# Oyunda kullanılan görsel ve sesler')
[void]$sb.AppendLine()
[void]$sb.AppendLine('Bu dosya `tools/varliklar_guncelle.ps1` ile üretilir; elle düzenleme. Oyunun yüklediği asıl dosyalar')
[void]$sb.AppendLine('`Assets/KatKat/Resources` ve `Assets/KatKat/Art` altındadır (yerleri değişirse oyun bulamaz).')
[void]$sb.AppendLine('Aynı dosyaların yerel kopyası `docs/varliklar/oyunda/` altındadır (depoya girmez).')
[void]$sb.AppendLine()
[void]$sb.AppendLine("Son güncelleme: $(Get-Date -Format 'yyyy-MM-dd')")
[void]$sb.AppendLine()

$all = @()
foreach ($k in $sources.Keys) {
    $base = Join-Path $root $sources[$k]
    Get-ChildItem $base -Recurse -File | Where-Object { Kind $_.Extension } | ForEach-Object {
        $rel = $_.FullName.Substring($base.Length + 1).Replace('\', '/')
        $top = if ($rel.Contains('/')) { $rel.Substring(0, $rel.IndexOf('/')) } else { '(kök)' }
        $all += [pscustomobject]@{ Src = $k; Top = $top; Rel = $rel; Kind = (Kind $_.Extension); Kb = [math]::Round($_.Length / 1KB) }
    }
}

[void]$sb.AppendLine('## Özet')
[void]$sb.AppendLine()
[void]$sb.AppendLine('| Klasör | Görsel | Ses | Yazı tipi | 3B model | Boyut |')
[void]$sb.AppendLine('|---|---:|---:|---:|---:|---:|')
foreach ($g in ($all | Group-Object { "$($_.Src)/$($_.Top)" } | Sort-Object Name)) {
    $c = { param($t) @($g.Group | Where-Object Kind -eq $t).Count }
    $mb = '{0:0.0} MB' -f (($g.Group | Measure-Object Kb -Sum).Sum / 1024)
    [void]$sb.AppendLine("| $($g.Name) | $(& $c 'görsel') | $(& $c 'ses') | $(& $c 'yazı tipi') | $(& $c '3B model') | $mb |")
}
$tot = '{0:0.0} MB' -f (($all | Measure-Object Kb -Sum).Sum / 1024)
[void]$sb.AppendLine("| **Toplam** | $(@($all | ? Kind -eq 'görsel').Count) | $(@($all | ? Kind -eq 'ses').Count) | $(@($all | ? Kind -eq 'yazı tipi').Count) | $(@($all | ? Kind -eq '3B model').Count) | $tot |")
[void]$sb.AppendLine()

foreach ($kind in 'ses', 'görsel', 'yazı tipi', '3B model') {
    $items = @($all | Where-Object Kind -eq $kind | Sort-Object Src, Rel)
    if ($items.Count -eq 0) { continue }
    $title = (Get-Culture).TextInfo.ToTitleCase($kind)
    [void]$sb.AppendLine("## $title ($($items.Count))")
    [void]$sb.AppendLine()
    foreach ($i in $items) { [void]$sb.AppendLine("- ``$($i.Src)/$($i.Rel)`` ($($i.Kb) KB)") }
    [void]$sb.AppendLine()
}

$out = Join-Path $root 'docs\varliklar\LISTE.md'
[IO.File]::WriteAllText($out, $sb.ToString().Replace("`r`n", "`n"), (New-Object System.Text.UTF8Encoding($false)))
"varliklar: $($all.Count) dosya ($tot) -> docs\varliklar\oyunda, docs\varliklar\LISTE.md"
