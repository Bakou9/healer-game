# Cycle complet de vérification de la version Unity : construit l'exécutable Windows, le lance en mode
# capture (le bot de référence joue en accéléré) et écrit les captures d'écran dans %TEMP%\healer-captures.
# Usage : powershell -File tools/unity-cycle.ps1 [-Shots "8,30,46"] [-SkipBuild]
param(
  [string]$Shots = "8,30,46",
  [switch]$SkipBuild
)
$ErrorActionPreference = "Continue"
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe"
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root "unity\HealerGame"
$exe = Join-Path $proj "Builds\Windows\HealerGame.exe"
$out = Join-Path $env:TEMP "healer-captures"

if (-not $SkipBuild) {
  $log = Join-Path $env:TEMP ("unity-build-" + (Get-Date -Format "HHmmss") + ".log")
  Set-Content -Path (Join-Path $env:TEMP "unity-build.lastlog") -Value $log
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $p = Start-Process -FilePath $unity -ArgumentList @("-batchmode", "-nographics", "-quit", "-projectPath", $proj, "-executeMethod", "Healer.EditorTools.Builder.BuildWindows", "-logFile", $log) -WindowStyle Hidden -PassThru
  if (-not $p.WaitForExit(400000)) { try { Stop-Process -Id $p.Id -Force } catch {}; Write-Output "BUILD : délai dépassé" }
  Write-Output ("BUILD : code {0} en {1:N0} s" -f $p.ExitCode, $sw.Elapsed.TotalSeconds)
  $t = Get-Content $log -ErrorAction SilentlyContinue
  $errs = $t | Select-String -Pattern "error CS\d+|\[Healer\] ÉCHEC|Exception:" | Select-Object -First 8
  if ($errs) { Write-Output "--- ERREURS :"; $errs | ForEach-Object { $_.Line.Substring(0, [Math]::Min(230, $_.Line.Length)) } }
  $t | Select-String -Pattern "\[Healer\] build" | ForEach-Object { $_.Line }
  if ($p.ExitCode -ne 0) { exit 1 }
}

New-Item -ItemType Directory -Path $out -Force | Out-Null
Get-ChildItem $out -Filter "*.png" -ErrorAction SilentlyContinue | ForEach-Object { $_.Delete() }
$g = Start-Process -FilePath $exe -ArgumentList @("-screen-width", "540", "-screen-height", "960", "-screen-fullscreen", "0", "-healer-shots", $Shots, "-healer-out", $out, "-healer-speed", "6") -PassThru
if (-not $g.WaitForExit(150000)) { try { Stop-Process -Id $g.Id -Force } catch {}; Write-Output "JEU : délai dépassé" }
Write-Output ("JEU : code {0}" -f $g.ExitCode)
Get-ChildItem $out -Filter "*.png" | ForEach-Object { Write-Output ("capture : {0} ({1} Ko)" -f $_.FullName, [math]::Round($_.Length / 1KB)) }
$plog = Join-Path $env:USERPROFILE "AppData\LocalLow\Healer\Healer Game\Player.log"
if (Test-Path $plog) { Get-Content $plog | Select-String -Pattern "Exception|NullReference|error" | Select-Object -First 6 | ForEach-Object { "LOG JOUEUR : " + $_.Line.Substring(0, [Math]::Min(200, $_.Line.Length)) } }
