# Tasarım denetimi (2026-11-23): ui3 ekranları birkaç dilde ve tablette; taşma raporları + ekran boyu ölçümü (Logs/display_sizes.txt)
$ErrorActionPreference = 'Continue'
$logs = (Resolve-Path (Join-Path $PSScriptRoot '..\Logs')).Path
if (Test-Path "$logs\display_sizes.txt") { Remove-Item "$logs\display_sizes.txt" }
$env:KATKAT_DISPLAY_LOG = "$logs\display_sizes.txt"
$env:KATKAT_CAPTURE_ONLY = ''
$runs = @(@{L='';T=''}, @{L='de';T=''}, @{L='ar';T=''}, @{L='ja';T=''}, @{L='';T='1'})
foreach ($r in $runs) {
    $env:KATKAT_CAPTURE_LANG = $r.L; $env:KATKAT_CAPTURE_TABLET = $r.T
    & (Join-Path $PSScriptRoot 'unity.ps1') capture-ui3 *> "$logs\audit_ui3_$($r.L)_$($r.T).log"
    "ui3 dil=$($r.L) tablet=$($r.T) çıkış=$LASTEXITCODE"
}
$env:KATKAT_CAPTURE_LANG = ''; $env:KATKAT_CAPTURE_TABLET = ''
foreach ($c in 'capture-v14','capture-pop','shop-shots','capture-tablet-map') { & (Join-Path $PSScriptRoot 'unity.ps1') $c *> "$logs\audit_$c.log"; "$c çıkış=$LASTEXITCODE" }
$env:KATKAT_DISPLAY_LOG = ''
