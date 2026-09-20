# Tests de bout en bout du jeu Windows, avec la VRAIE souris et le VRAI clavier (fenêtre du jeu uniquement).
# Vérifie par le journal du jeu (Player.log) et par le fichier de sauvegarde que chaque geste produit son effet.
#   A : menu -> choix du niveau -> niveau 1 -> Jouer -> cibler (colonne gauche) -> sort (colonne droite) -> pause Espace / Échap
#   B : niveau verrouillé (clic sans effet), défaite sans joueur, clic sur Recommencer, retour à la carte
#   C : Espace démarre, Entrée redémarre après la défaite
#   D : victoire avec le bot -> étoiles, or, sauvegarde sur disque -> « Niveau suivant » -> relance du jeu :
#       la progression est rechargée et le niveau 2 est jouable
#   E : menu de pause -> « Quitter le niveau » -> retour au choix du niveau -> retour au menu
# Toutes les parties utilisent un dossier de sauvegarde temporaire : la vraie sauvegarde n'est jamais touchée.
# À lancer quand personne n'utilise souris ni clavier. Code de sortie 1 si une vérification échoue.
# Usage : powershell -File tools/unity-e2e.ps1 [-SkipBuild] [-Scenario A|B|C|D|E]
param([switch]$SkipBuild, [ValidateSet("all","A","B","C","D","E")][string]$Scenario = "all")
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
function New-ProfileDir($name) { $d = Join-Path $env:TEMP ("healer-e2e-" + $name + "-" + [Guid]::NewGuid().ToString("N").Substring(0, 6)); New-Item -ItemType Directory -Path $d | Out-Null; return $d }
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
function Check($ok, $label) {
  if ($ok) { Write-Output "  OK      $label" } else { Write-Output "  ÉCHEC   $label"; $script:failures += $label }
}
function Want($scn) { return ($Scenario -eq "all" -or $Scenario -eq $scn) }

# Positions (Healer.Ui.Layout) : menu Jouer (640,362) ; cartes de niveau 1/2/3 au centre x = 312 / 640 / 968, y = 340 ;
# Jouer du combat (640,500) ; colonne gauche = alliés (Garde 144,148) ; colonne droite = sorts (Soin 1136,190) ;
# fin de combat : 2 boutons -> Recommencer (522,618) ; 3 boutons -> Recommencer (404,618), Niveau suivant (640,618) ;
# pause : Quitter le niveau (640,430) ; Retour (87,32).
$menuPlay = @(640, 362); $level1 = @(312, 340); $level2 = @(640, 340); $level3 = @(968, 340); $fightPlay = @(640, 500)

# ---- Scénario A -----------------------------------------------------------------------------
if (Want "A") {
Write-Output "Scénario A : menu, niveau 1, cibler, lancer un sort, pause au clavier"
Start-Game @("-healer-profile-dir", (New-ProfileDir "A"))
Tap $menuPlay[0] $menuPlay[1] "Jouer (menu)"
Tap $level1[0] $level1[1] "niveau 1"
Tap $fightPlay[0] $fightPlay[1] "Jouer (combat)"
Tap 144 148 "carte Garde (colonne gauche)"
Tap 1136 190 "sort Soin (colonne droite)"
Press 0x39 "Espace (pause)"
Press 0x39 "Espace (reprise)"
Press 0x01 "Échap (pause)"
Press 0x01 "Échap (reprise)"
Stop-Game
$a = Log
Expect $a "écran : choix du niveau" "le clic sur Jouer du menu ouvre le choix du niveau"
Expect $a "niveau : l1" "le clic sur la carte du niveau 1 le lance"
Expect $a "état : combat démarré" "le clic sur Jouer démarre le combat"
$ha = @($a | Select-String -Pattern "\[Healer\] (état|geste)" | ForEach-Object { $_.Line })
Check ($ha.Count -ge 2 -and $ha[0] -match "combat démarré" -and $ha[1] -match "geste : carte tank") "le clic sur Jouer ne déclenche aucun autre geste (ordre : démarrage, puis carte)"
Expect $a "geste : carte tank → cible = tank" "un clic sur une carte cible l'allié"
Expect $a "geste : sort heal_single → Cast \(cible tank\)" "un clic sur un sort le lance sur la cible"
Expect $a "état : pause" "Espace met en pause"
Expect $a "état : reprise" "Espace reprend"
}

# ---- Scénario B -----------------------------------------------------------------------------
if (Want "B") {
Write-Output "Scénario B : niveau verrouillé, défaite sans joueur, Recommencer, retour à la carte"
Start-Game @("-healer-timescale", "40", "-healer-profile-dir", (New-ProfileDir "B"))
Tap $menuPlay[0] $menuPlay[1] "Jouer (menu)"
Tap $level3[0] $level3[1] "niveau 3 (verrouillé)"
Tap $level2[0] $level2[1] "niveau 2 (verrouillé)"
Tap $level1[0] $level1[1] "niveau 1"
Tap $fightPlay[0] $fightPlay[1] "Jouer (combat)"
Start-Sleep -Seconds 12
$b = Log
Forbid $b "niveau : l[23]" "un niveau verrouillé ne se lance pas"
Expect $b "état : combat terminé \(defeat\)" "le combat sans joueur se termine par une défaite"
Tap 522 618 "Recommencer"
$b2 = Log
Expect $b2 "état : nouveau combat" "le clic sur Recommencer relance un combat"
Press 0x01 "Échap (pause)"
Tap 640 430 "Quitter le niveau"
Stop-Game
$b3 = Log
Expect $b3 "écran : choix du niveau" "quitter le niveau ramène au choix du niveau"
}

# ---- Scénario C -----------------------------------------------------------------------------
if (Want "C") {
Write-Output "Scénario C : Espace démarre, Entrée redémarre après la défaite"
Start-Game @("-healer-timescale", "40", "-healer-level", "l1", "-healer-profile-dir", (New-ProfileDir "C"))
Press 0x39 "Espace (jouer)"
Start-Sleep -Seconds 12
Press 0x1C "Entrée (recommencer)"
Stop-Game
$c = Log
Expect $c "état : combat démarré" "Espace démarre le combat"
Expect $c "état : combat terminé" "le combat se termine"
Expect $c "état : nouveau combat" "Entrée relance un combat après la fin"
}

# ---- Scénario D -----------------------------------------------------------------------------
if (Want "D") {
Write-Output "Scénario D : victoire, étoiles, or, sauvegarde, niveau suivant, relance avec la progression"
$dir = New-ProfileDir "D"
Start-Game @("-healer-autoplay", "-healer-timescale", "20", "-healer-profile-dir", $dir)
Tap $menuPlay[0] $menuPlay[1] "Jouer (menu)"
Tap $level1[0] $level1[1] "niveau 1"
Tap $fightPlay[0] $fightPlay[1] "Jouer (combat)"
Start-Sleep -Seconds 14
$d1 = Log
Expect $d1 "état : combat terminé \(victory\)" "le bot gagne le niveau 1"
Expect $d1 "progression : l1 [123] étoile" "la victoire donne au moins une étoile"
Expect $d1 "débloque l2" "la victoire débloque le niveau 2"
Expect $d1 "sauvegarde : écrite" "la progression est sauvegardée"
$profilePath = Join-Path $dir "profile.json"
Check (Test-Path $profilePath) "le fichier de sauvegarde existe sur le disque"
if (Test-Path $profilePath) {
  $json = Get-Content $profilePath -Raw -Encoding UTF8 | ConvertFrom-Json
  Check ($json.levels.l1.completed -eq $true) "la sauvegarde marque le niveau 1 comme terminé"
  Check ($json.levels.l1.bestStars -ge 1) "la sauvegarde contient les étoiles"
  Check ($json.wallet.gold -gt 0) "la sauvegarde contient l'or gagné"
}
Tap 640 618 "Niveau suivant"
Start-Sleep -Milliseconds 500
Stop-Game
$d2 = Log
Expect $d2 "niveau : l2" "le bouton Niveau suivant lance le niveau 2"
Write-Output "  -- relance du jeu avec la même sauvegarde"
Start-Game @("-healer-profile-dir", $dir)
Tap $menuPlay[0] $menuPlay[1] "Jouer (menu)"
Tap $level2[0] $level2[1] "niveau 2 (débloqué par la sauvegarde)"
Stop-Game
$d3 = Log
Expect $d3 "sauvegarde : chargée" "la sauvegarde est rechargée au démarrage"
Expect $d3 "niveau : l2" "le niveau 2 est jouable après relance"
}

# ---- Scénario E -----------------------------------------------------------------------------
if (Want "E") {
Write-Output "Scénario E : pause, quitter le niveau, retour au menu"
Start-Game @("-healer-profile-dir", (New-ProfileDir "E"))
Tap $menuPlay[0] $menuPlay[1] "Jouer (menu)"
Tap $level1[0] $level1[1] "niveau 1"
Tap $fightPlay[0] $fightPlay[1] "Jouer (combat)"
Press 0x01 "Échap (pause)"
Tap 640 430 "Quitter le niveau"
Tap 87 32 "← Menu"
Stop-Game
$e = Log
Expect $e "état : pause" "Échap met en pause"
Expect $e "écran : menu principal" "le bouton Menu ramène au menu principal"
}

if ($failures.Count -gt 0) { Write-Output ""; Write-Output "$($failures.Count) vérification(s) en échec."; exit 1 }
Write-Output ""; Write-Output "Tous les tests de bout en bout passent."
