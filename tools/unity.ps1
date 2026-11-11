# Kat Kat Unity batch yardımcısı. Kullanım (proje kökünden):
#   powershell -ExecutionPolicy Bypass -File tools/unity.ps1 setup
#   powershell -ExecutionPolicy Bypass -File tools/unity.ps1 test-edit
#   powershell -ExecutionPolicy Bypass -File tools/unity.ps1 test-play [-All] [-Seed N]   (varsayılan: her 100 bölümden 5, toplam 50)
#   powershell -ExecutionPolicy Bypass -File tools/unity.ps1 test-play-visible [-Speed 2] [-Filter AllLevels]   (pencereli, izlenebilir)
#   powershell -ExecutionPolicy Bypass -File tools/unity.ps1 validate-levels
#   powershell -ExecutionPolicy Bypass -File tools/unity.ps1 build-android
#   powershell -ExecutionPolicy Bypass -File tools/unity.ps1 build-windows
# Unity editörü açıkken batch komutları aynı projeyi açamaz; önce editörü kapat.
param([Parameter(Mandatory=$true)][string]$Command, [double]$Speed = 2, [string]$Filter = "", [switch]$All, [int]$Seed = 0)
# test-play: varsayılan her 100 bölümlük dilimden 5 bölüm; -All bütün bölümler (mağaza derlemesi öncesi); -Seed N aynı örneklemi tekrarlar.
if ($All) { $env:KATKAT_PLAY_ALL = "1" } else { Remove-Item Env:KATKAT_PLAY_ALL -ErrorAction SilentlyContinue }
if ($Seed -gt 0) { $env:KATKAT_PLAY_SEED = [string]$Seed } else { Remove-Item Env:KATKAT_PLAY_SEED -ErrorAction SilentlyContinue }

$ErrorActionPreference = 'Stop'
$Unity = if ($env:KATKAT_UNITY) { $env:KATKAT_UNITY } else { 'C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe' }
$Root = Split-Path -Parent $PSScriptRoot
$Out = Join-Path $Root 'Logs\batch'
New-Item -ItemType Directory -Force $Out | Out-Null

function Invoke-Unity([string[]]$UnityArgs, [string]$Name, [switch]$Visible) {
    $log = Join-Path $Out "$Name.log"
    $all = @(if ($Visible) { @() } else { @('-batchmode') }) + @('-projectPath', "`"$Root`"", '-logFile', "`"$log`"") + $UnityArgs
    Write-Host "Unity $($all -join ' ')"
    $p = Start-Process -FilePath $Unity -ArgumentList $all -Wait -PassThru -NoNewWindow
    Write-Host "[$Name] çıkış kodu: $($p.ExitCode)  günlük: $log"
    return $p.ExitCode
}

switch ($Command) {
    'setup'           { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.ProjectSetup.RunAll') 'setup' }
    'test-edit'       { $c = Invoke-Unity @('-runTests', '-testPlatform', 'EditMode', '-testResults', "`"$Out\editmode-results.xml`"") 'test-edit' }
    'test-play-visible' {
        # Pencereli editör: Game penceresinde bölümler oynanırken izlenir; bitince Unity kapanır.
        $env:KATKAT_TEST_SPEED = [string]$Speed
        $extra = if ($Filter) { @('-testFilter', $Filter) } else { @() }
        $c = Invoke-Unity (@('-runTests', '-testPlatform', 'PlayMode', '-testResults', "`"$Out\playmode-visible-results.xml`"") + $extra) 'test-play-visible' -Visible
    }
    'test-play'       {
        $extra = if ($Filter) { @('-testFilter', $Filter) } else { @() }
        $c = Invoke-Unity (@('-runTests', '-testPlatform', 'PlayMode', '-testResults', "`"$Out\playmode-results.xml`"") + $extra) 'test-play'
    }
    'validate-levels' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.LevelValidationMenu.RunBatch') 'validate-levels' }
    'capture'         { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.PeelCapture.CaptureAll') 'capture' }
    'capture-v3'      { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.GameCapture.CaptureVisualV3') 'capture-v3' }
    'capture-v5'      { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.GameCapture.CaptureV5') 'capture-v5' }
    'capture-usage'   { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.GameCapture.CaptureUsage') 'capture-usage' }
    'capture-size'    { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.GameCapture.CaptureBoardSize') 'capture-size' }
    'capture-tray'    { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.GameCapture.CaptureTray') 'capture-tray' }
    'capture-mascot'  { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.GameCapture.CaptureMascot') 'capture-mascot' }
    'loc-check'       { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.LocCheck.RunBatch') 'loc-check' }
    'capture-v9-before' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV9.Before') 'capture-v9-before' }
    'capture-v9-after'  { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV9.After') 'capture-v9-after' }
    'capture-daily-tier' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV9.DailyTier') 'capture-daily-tier' }
    'capture-collection' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV9.CollectionGrid') 'capture-collection' }
    'capture-shell-before' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV10Shell.Before') 'capture-shell-before' }
    'capture-shell-after' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV10Shell.After') 'capture-shell-after' }
    'capture-showcase' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV10Shell.Showcase') 'capture-showcase' }
    'stone-materials' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.StoneModelTools.BuildMaterials') 'stone-materials' }
    'stone-bounds' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.StoneModelTools.ReportBounds') 'stone-bounds' }
    'capture-game-before' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV10Game.Before') 'capture-game-before' }
    'capture-game-after' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV10Game.After') 'capture-game-after' }
    'capture-v11'     { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV11.Run') 'capture-v11' }
    'capture-v12'     { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV12.Run') 'capture-v12' }
    'capture-v13-pages' { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV13Pages.Run') 'capture-v13-pages' }
    'capture-ui1'     { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV13Pages.RunUi1') 'capture-ui1' }
    'capture-ui2'     { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV13Pages.RunUi2') 'capture-ui2' }
    'capture-vr1'     { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV13Pages.RunVr1') 'capture-vr1' }
    'capture-vr1-p1'  { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV13Pages.RunVr1P1') 'capture-vr1-p1' }
    'capture-ui3'     { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV13Pages.RunUi3') 'capture-ui3' }
    'capture-store'   { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureStore.Run') 'capture-store' }
    'usage-ui3'       { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV13Pages.UsageUi3') 'usage-ui3' }
    'trailer-frames'  { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.TrailerCapture.Run') 'trailer-frames' }
    'trailer-punch'   { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.TrailerCapture.RunPunch') 'trailer-punch' }
    'capture-v7'      { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.CaptureV7.Run') 'capture-v7' }
    'export-art'      { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.ArtExport.ExportAll') 'export-art' }
    'capture-game'    { $c = Invoke-Unity @('-quit', '-executeMethod', 'KatKat.EditorTools.GameCapture.CaptureAll') 'capture-game' }
    'build-android'   { $c = Invoke-Unity @('-quit', '-buildTarget', 'Android', '-executeMethod', 'KatKat.EditorTools.BuildScripts.BuildAndroid') 'build-android' }
    'build-android-vr1' { $c = Invoke-Unity @('-quit', '-buildTarget', 'Android', '-executeMethod', 'KatKat.EditorTools.BuildScripts.BuildAndroidVr1') 'build-android-vr1' }
    'build-android-appgallery' { $c = Invoke-Unity @('-quit', '-buildTarget', 'Android', '-executeMethod', 'KatKat.EditorTools.BuildScripts.BuildAndroidAppGallery') 'build-android-appgallery' }
    'build-android-store' { $c = Invoke-Unity @('-quit', '-buildTarget', 'Android', '-executeMethod', 'KatKat.EditorTools.BuildScripts.BuildAndroidStore') 'build-android-store' }
    'build-ios'       { $c = Invoke-Unity @('-quit', '-buildTarget', 'iOS', '-executeMethod', 'KatKat.EditorTools.BuildScripts.BuildIOS') 'build-ios' }
    'build-windows'   { $c = Invoke-Unity @('-quit', '-buildTarget', 'Win64', '-executeMethod', 'KatKat.EditorTools.BuildScripts.BuildWindows') 'build-windows' }
    default { throw "Bilinmeyen komut: $Command" }
}
exit $c

