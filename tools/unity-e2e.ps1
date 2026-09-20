# Tests de bout en bout du jeu Windows, avec la VRAIE souris et le VRAI clavier (fenêtre du jeu uniquement).
# Remplace unity-clicktest.ps1. Vérifie par le journal du jeu (Player.log) que chaque geste atteint le contrôleur.
#   Scénario A : clic sur « Jouer » à un endroit recouvert par une carte d'allié (régression : le clic était avalé),
#                cibler, lancer un sort, pause / reprise à l'Espace et à Échap.
#   Scénario B : combat accéléré perdu sans joueur → écran de bilan → clic sur « Recommencer » (recouvre aussi des cartes).
#   Scénario C : Espace démarre, Entrée redémarre après la défaite.
# À lancer quand personne n'utilise souris ni clavier. Code de sortie 1 si une vérification échoue.
# Usage : powershell -File tools/unity-e2e.ps1 [-SkipBuild] [-Scenario A|B|C]
param([switch]$SkipBuild, [ValidateSet("all","A","B","C")][string]$Scenario = "all")
$ErrorActionPreference = "Stop"
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win {
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  public static void Click(int x, int y) { SetCursorPos(x, y); System.Threading.Thread.Sleep(120); mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero); System.Threading.Thread.Sleep(80); mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); }
  public static void Key(byte scan) { keybd_event(0, scan, 8, UIntPtr.Zero); System.Threading.Thread.Sleep(80); keybd_event(0, scan, 8 | 2, UIntPtr.Zero); }
}
"@

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "unity\HealerGame\Builds\Windows\HealerGame.exe"
$plog = Join-Path $env:USERPROFILE "AppData\LocalLow\Healer\Healer Game\Player.log"
$failures = @()

if (-not $SkipBuild) {
  $unity = "C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe"
  $blog = Join-Path $env:TEMP "unity-e2e-build.log"
  $b = Start-Process -FilePath $unity -ArgumentList @("-batchmode", "-nographics", "-quit", "-projectPath", (Join-Path $root "unity\HealerGame"), "-executeMethod", "Healer.EditorTools.Builder.BuildWindows", "-logFile", $blog) -WindowStyle Hidden -PassThru
  if (-not $b.WaitForExit(500000)) { try { Stop-Process -Id $b.Id -Force } catch {}; Write-Output "ÉCHEC : build trop long"; exit 1 }
  if ($b.ExitCode -ne 0) { Write-Output "ÉCHEC : build (code $($b.ExitCode)), voir $blog"; exit 1 }
}

# Grille logique 1280x720 (Healer.Ui.Layout) -> écran, comme Healer.Ui.ScreenFit.
function Start-Game($extraArgs) {
  $script:p = Start-Process -FilePath $exe -ArgumentList (@("-screen-width", "1280", "-screen-height", "720", "-screen-fullscreen", "0") + $extraArgs) -PassThru
  Start-Sleep -Seconds 7
  $script:p.Refresh()
  $script:h = $script:p.MainWindowHandle
  if ($script:h -eq 0) { throw "fenêtre du jeu introuvable" }
  [Win]::SetForegroundWindow($script:h) | Out-Null
  Start-Sleep -Milliseconds 500
  $r = New-Object Win+RECT; [Win]::GetClientRect($script:h, [ref]$r) | Out-Null
  $script:origin = New-Object Win+POINT; [Win]::ClientToScreen($script:h, [ref]$script:origin) | Out-Null
  $script:scale = [Math]::Min($r.Right / 1280.0, $r.Bottom / 720.0)
  $script:offX = ($r.Right - 1280 * $script:scale) / 2; $script:offY = ($r.Bottom - 720 * $script:scale) / 2
}
function Tap($lx, $ly, $label) {
  $sx = [int]($script:origin.X + $script:offX + $lx * $script:scale); $sy = [int]($script:origin.Y + $script:offY + $ly * $script:scale)
  Write-Output ("  clic {0} : logique ({1},{2}) -> écran ({3},{4})" -f $label, $lx, $ly, $sx, $sy)
  [Win]::Click($sx, $sy); Start-Sleep -Milliseconds 800
}
function Press($scan, $label) { Write-Output "  touche $label"; [Win]::Key([byte]$scan); Start-Sleep -Milliseconds 800 }
function Stop-Game() { try { $null = $script:p.CloseMainWindow(); if (-not $script:p.WaitForExit(6000)) { Stop-Process -Id $script:p.Id -Force } } catch {}; Start-Sleep -Milliseconds 800 }
function Log() { if (Test-Path $plog) { Get-Content $plog -Encoding UTF8 | ForEach-Object { $_ } } else { @() } }
function Expect($lines, $pattern, $label) {
  if ($lines | Select-String -Pattern $pattern -Quiet) { Write-Output "  OK      $label" }
  else { Write-Output "  ÉCHEC   $label   (attendu : $pattern)"; $script:failures += $label }
}
function Forbid($lines, $pattern, $label) {
  if ($lines | Select-String -Pattern $pattern -Quiet) { Write-Output "  ÉCHEC   $label   (présent : $pattern)"; $script:failures += $label }
  else { Write-Output "  OK      $label" }
}

# ---- Scénario A -----------------------------------------------------------------------------
if ($Scenario -eq "all" -or $Scenario -eq "A") {
Write-Output "Scénario A : démarrer à la souris, cibler, lancer un sort, pause au clavier"
Start-Game @()
# (700,500) est dans le bouton « Jouer » (520..760 x 470..530) ET dans la carte du Mage (x 644..888, y 388..524).
Tap 700 500 "Jouer (au-dessus de la carte du Mage)"
Tap 262 456 "carte Garde"
Tap 262 644 "sort Soin"
Press 0x39 "Espace (pause)"
Press 0x39 "Espace (reprise)"
Press 0x01 "Échap (pause)"
Press 0x01 "Échap (reprise)"
Stop-Game
$a = Log
Expect $a "état : combat démarré" "le clic sur Jouer démarre le combat"
Forbid $a "geste : carte dps2" "le clic sur Jouer n'a pas été avalé par la carte du Mage"
Expect $a "geste : carte tank → cible = tank" "un clic sur une carte cible l'allié"
Expect $a "geste : sort heal_single → Cast \(cible tank\)" "un clic sur un sort le lance sur la cible"
Expect $a "état : pause" "Espace met en pause"
Expect $a "état : reprise" "Espace reprend"

}
# ---- Scénarios B et C -----------------------------------------------------------------------
if ($Scenario -eq "all" -or $Scenario -eq "B") {
Write-Output "Scénario B : défaite sans joueur, clic sur Recommencer (recouvre des cartes)"
Start-Game @("-healer-timescale", "40")
Tap 700 500 "Jouer"
Start-Sleep -Seconds 12
$b = Log
Expect $b "état : combat terminé \(defeat\)" "le combat sans joueur se termine par une défaite"
Tap 700 480 "Recommencer (au-dessus de cartes)"
Tap 262 456 "carte Garde en fin de combat (ne doit rien faire de plus)"
Stop-Game
$b2 = Log
Expect $b2 "état : nouveau combat" "le clic sur Recommencer relance un combat"

}
if ($Scenario -eq "all" -or $Scenario -eq "C") {
Write-Output "Scénario C : Espace démarre, Entrée redémarre après la défaite"
Start-Game @("-healer-timescale", "40")
Press 0x39 "Espace (jouer)"
Start-Sleep -Seconds 12
Press 0x1C "Entrée (recommencer)"
Stop-Game
$c = Log
Expect $c "état : combat démarré" "Espace démarre le combat"
Expect $c "état : combat terminé" "le combat se termine"
Expect $c "état : nouveau combat" "Entrée relance un combat après la fin"

}
if ($failures.Count -gt 0) { Write-Output ""; Write-Output "$($failures.Count) vérification(s) en échec."; exit 1 }
Write-Output ""; Write-Output "Tous les tests de bout en bout passent."
