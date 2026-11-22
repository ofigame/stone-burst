# Resources kullanım kapsaması (2026-11-23 temizlik): KATKAT_RES_LOG açıkken testler ve yakalamalar; sonuç Logs/res_used.txt
$ErrorActionPreference = 'Continue'
$log = Join-Path $PSScriptRoot '..\Logs\res_used.txt'
if (Test-Path $log) { Remove-Item $log }
$env:KATKAT_RES_LOG = (Resolve-Path (Join-Path $PSScriptRoot '..\Logs')).Path + '\res_used.txt'
$env:KATKAT_CAPTURE_ONLY = ''
foreach ($c in 'capture-ui3','test-play','test-edit','capture-v14','capture-pop','live-capture','shop-shots','capture-tablet-map','capture-store','capture-collection','capture-vr2') {
    $t = Get-Date
    & (Join-Path $PSScriptRoot 'unity.ps1') $c *> (Join-Path $PSScriptRoot "..\Logs\cov_$c.log")
    "$c çıkış=$LASTEXITCODE süre=$([int]((Get-Date)-$t).TotalSeconds)s"
}
$env:KATKAT_RES_LOG = ''
