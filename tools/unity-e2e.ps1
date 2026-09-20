# Tests de bout en bout du jeu Windows. Par défaut les gestes sont INJECTÉS dans le jeu (souris et clavier virtuels de
# l'Input System, option -healer-e2e) : la fenêtre n'a pas besoin de focus, votre souris et votre clavier restent libres,
# et les scénarios tournent en parallèle (-Jobs). Avec -RealInput, on pilote la VRAIE souris et le VRAI clavier de Windows
# (un seul scénario à la fois, personne ne doit toucher à la machine) : à réserver à une vérification avant livraison.
# Vérifie par le journal du jeu (Player.log) et par le fichier de sauvegarde que chaque geste produit son effet.
#   A : menu -> choix du niveau -> niveau 1 -> Jouer -> cibler (colonne gauche) -> sort (colonne droite) -> pause Espace / Échap
#   B : niveau verrouillé (clic sans effet), défaite sans joueur, clic sur Recommencer, retour à la carte
#   C : Espace démarre, Entrée redémarre après la défaite
#   D : victoire avec le bot -> étoiles, or, sauvegarde sur disque -> « Niveau suivant » -> relance du jeu :
#       la progression est rechargée et le niveau 2 est jouable
#   E : menu de pause -> « Quitter le niveau » -> retour au choix du niveau -> retour au menu
#   F : atelier -> acheter de l'équipement, choisir et changer un talent, or insuffisant, palier verrouillé,
#       sauvegarde sur disque, relance : l'équipement et le talent sont appliqués au combat
#   H : le Seigneur de Cendre s'enrage si le combat s'éternise (l'enrage est un réglage du boss)
#   I : réglages (volumes, secousse) : changés à la souris, sauvegardés, rechargés après relance
#   J : note de playtest (F8) : saisie, fichier de notes, capture, pause puis reprise
#   G : maintenir un sort (souris puis clavier) l'enchaîne ; re-toucher un allié ne le désélectionne pas
# Toutes les parties utilisent un dossier de sauvegarde temporaire : la vraie sauvegarde n'est jamais touchée.
# Code de sortie 1 si une vérification échoue.
# Usage : powershell -File tools/unity-e2e.ps1 [-SkipBuild] [-Scenario A|B|C|D|E|F|G|H|I] [-Jobs 4] [-RealInput]
param([switch]$SkipBuild, [ValidateSet("all","A","B","C","D","E","F","G","H","I","J")][string]$Scenario = "all", [int]$Jobs = 4, [switch]$RealInput, [int]$Show = 6)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
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
  public static void MouseDown(int x, int y) { SetCursorPos(x, y); System.Threading.Thread.Sleep(120); mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero); }
  public static void MouseUp() { mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); }
  public static void KeyDown(byte scan) { keybd_event(0, scan, 8, UIntPtr.Zero); }
  public static void KeyUp(byte scan) { keybd_event(0, scan, 8 | 2, UIntPtr.Zero); }
  public static void Key(byte scan) { keybd_event(0, scan, 8, UIntPtr.Zero); System.Threading.Thread.Sleep(80); keybd_event(0, scan, 8 | 2, UIntPtr.Zero); }
}
"@

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Launcher {
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct STARTUPINFO { public int cb; public string lpReserved, lpDesktop, lpTitle; public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags; public short wShowWindow, cbReserved2; public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError; }
  [StructLayout(LayoutKind.Sequential)] struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }
  [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool CreateProcess(string app, string cmd, IntPtr pa, IntPtr ta, bool inherit, uint flags, IntPtr env, string dir, ref STARTUPINFO si, out PROCESS_INFORMATION pi);
  // Lance le jeu avec un état de fenêtre imposé (6 = réduite dans la barre des tâches : ne vole pas le focus).
  public static int Start(string exe, string args, int show) {
    var si = new STARTUPINFO(); si.cb = Marshal.SizeOf(si); si.dwFlags = 1; si.wShowWindow = (short)show;
    PROCESS_INFORMATION pi;
    if (!CreateProcess(exe, "\"" + exe + "\" " + args, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    return pi.dwProcessId;
  }
}
"@
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "unity\HealerGame\Builds\Windows\HealerGame.exe"
$plog = Join-Path $env:TEMP ("healer-e2e-" + $Scenario + ".log")
$cmdFile = Join-Path $env:TEMP ("healer-e2e-cmd-" + $Scenario + ".txt")
$script:cmdN = 0
$failures = @()

if (-not $SkipBuild) {
  $unity = "C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe"
  $blog = Join-Path $env:TEMP "unity-e2e-build.log"
  $b = Start-Process -FilePath $unity -ArgumentList @("-batchmode", "-nographics", "-quit", "-projectPath", (Join-Path $root "unity\HealerGame"), "-executeMethod", "Healer.EditorTools.Builder.BuildWindows", "-logFile", $blog) -WindowStyle Hidden -PassThru
  if (-not $b.WaitForExit(500000)) { try { Stop-Process -Id $b.Id -Force } catch {}; Write-Output "ÉCHEC : build trop long"; exit 1 }
  if ($b.ExitCode -ne 0) { Write-Output "ÉCHEC : build (code $($b.ExitCode)), voir $blog"; exit 1 }
}

# ---- Exécution parallèle : un processus par scénario (les gestes sont injectés, aucun ne dispute la souris) ----
if ($Scenario -eq "all" -and -not $RealInput -and $Jobs -gt 1) {
  $order = "D","G","F","H","B","C","A","E","I","J"  # les plus longs d'abord
  $queue = New-Object System.Collections.Queue; foreach ($s in $order) { $queue.Enqueue($s) }
  $running = @{}; $outs = @{}; $codes = @{}
  while ($queue.Count -gt 0 -or $running.Count -gt 0) {
    while ($queue.Count -gt 0 -and $running.Count -lt $Jobs) {
      $s = $queue.Dequeue(); $out = Join-Path $env:TEMP "healer-e2e-out-$s.txt"; $outs[$s] = $out
      $running[$s] = Start-Process powershell -ArgumentList (@("-NoProfile", "-File", $PSCommandPath, "-SkipBuild", "-Scenario", $s) + @("-Show", $Show)) -RedirectStandardOutput $out -WindowStyle Hidden -PassThru
      $null = $running[$s].Handle  # sans cela, ExitCode reste vide une fois le processus terminé
    }
    foreach ($s in @($running.Keys)) { if ($running[$s].HasExited) { $codes[$s] = $running[$s].ExitCode; $running.Remove($s) } }
    Start-Sleep -Milliseconds 500
  }
  $bad = 0
  foreach ($s in ($outs.Keys | Sort-Object)) {
    Get-Content $outs[$s] -Encoding UTF8 | Where-Object { $_ -notmatch "^Tous les tests|vérification\(s\) en échec|^\s*$" }
    if ($codes[$s] -ne 0) { $bad++ }
  }
  Write-Output ""
  if ($bad -gt 0) { Write-Output "$bad scénario(s) en échec."; Write-Output "[e2e-fin] code=1"; exit 1 }
  Write-Output "Tous les tests de bout en bout passent."; Write-Output "[e2e-fin] code=0"; exit 0
}

# Grille logique 1280x720 (Healer.Ui.Layout) -> écran, comme Healer.Ui.ScreenFit.
function New-ProfileDir($name) { $d = Join-Path $env:TEMP ("healer-e2e-" + $name + "-" + [Guid]::NewGuid().ToString("N").Substring(0, 6)); New-Item -ItemType Directory -Path $d | Out-Null; return $d }
function Start-Game($extraArgs) {
  if (Test-Path $plog) { Remove-Item $plog -Force -ErrorAction SilentlyContinue }  # sinon le « jeu prêt » d'une partie précédente ferait croire au démarrage
  [IO.File]::WriteAllText($cmdFile, ""); $script:cmdN = 0
  $inject = if ($RealInput) { @() } else { @("-healer-e2e", $cmdFile) }
  $all = @("-screen-width", "1280", "-screen-height", "720", "-screen-fullscreen", "0", "-logFile", $plog) + $inject + $extraArgs
  if ($RealInput) { $script:p = Start-Process -FilePath $exe -ArgumentList $all -PassThru }
  else { $quoted = ($all | ForEach-Object { if ($_ -match "\s") { '"' + $_ + '"' } else { $_ } }) -join " "; $script:p = Get-Process -Id ([Launcher]::Start($exe, $quoted, $Show)) }
  # On attend le signal « jeu prêt » du journal (le premier lancement après une compilation est bien plus lent qu'un lancement à chaud).
  $deadline = (Get-Date).AddSeconds(60)
  while ((Get-Date) -lt $deadline) { if ((LogText) -match "jeu prêt") { break }; Start-Sleep -Milliseconds 200 }
  Start-Sleep -Milliseconds 500
  if (-not $RealInput) { return }
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
# Contenu du journal du jeu (lecture partagée : le jeu l'écrit encore).
function LogText() {
  if (-not (Test-Path $plog)) { return "" }
  try { $fs = New-Object IO.FileStream($plog, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite); $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8); try { return $sr.ReadToEnd() } finally { $sr.Dispose() } } catch { return "" }
}
# Attend qu'une ligne du journal corresponde (au lieu d'une pause fixe) ; abandonne au bout de $timeout s (la vérification échouera alors d'elle-même).
function WaitLog($pattern, $timeout) {
  $deadline = (Get-Date).AddSeconds($timeout)
  while ((Get-Date) -lt $deadline) { if ((LogText) -match $pattern) { return }; Start-Sleep -Milliseconds 200 }
}
# Envoie une commande au jeu (entrées injectées) et attend son accusé dans le journal.
function Send($command) {
  $script:cmdN++
  [IO.File]::AppendAllText($cmdFile, $command + "`n")
  $tag = "e2e $($script:cmdN) ok"; $deadline = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $deadline) { if ((LogText).Contains($tag)) { return }; Start-Sleep -Milliseconds 30 }
  throw "le jeu n'a pas joué la commande : $command"
}
$keyNames = @{ 0x39 = "Space"; 0x01 = "Escape"; 0x1C = "Enter"; 0x02 = "Digit1"; 0x10 = "Q"; 0x42 = "F8"; 0x4F = "Numpad1" }
function Tap($lx, $ly, $label) {
  Write-Output ("  clic {0} : logique ({1},{2})" -f $label, $lx, $ly)
  if ($RealInput) { $sx = [int]($script:origin.X + $script:offX + $lx * $script:scale); $sy = [int]($script:origin.Y + $script:offY + $ly * $script:scale); [Win]::Click($sx, $sy); Start-Sleep -Milliseconds 800 }
  else { Send "click $lx $ly"; Start-Sleep -Milliseconds 300 }
}
function HoldMouse($lx, $ly, $seconds, $label) {
  Write-Output ("  maintenir {0} pendant {1} s : logique ({2},{3})" -f $label, $seconds, $lx, $ly)
  if ($RealInput) { $sx = [int]($script:origin.X + $script:offX + $lx * $script:scale); $sy = [int]($script:origin.Y + $script:offY + $ly * $script:scale); [Win]::MouseDown($sx, $sy) }
  else { Send "down $lx $ly" }
  Start-Sleep -Milliseconds ([int]($seconds * 1000))
  if ($RealInput) { [Win]::MouseUp() } else { Send "up" }
  Start-Sleep -Milliseconds 600
}
function HoldKey($scan, $seconds, $label) {
  Write-Output "  maintenir la touche $label pendant $seconds s"
  if ($RealInput) { [Win]::KeyDown([byte]$scan) } else { Send ("keydown " + $keyNames[[int]$scan]) }
  Start-Sleep -Milliseconds ([int]($seconds * 1000))
  if ($RealInput) { [Win]::KeyUp([byte]$scan) } else { Send ("keyup " + $keyNames[[int]$scan]) }
  Start-Sleep -Milliseconds 600
}
function Count($lines, $pattern) { return @($lines | Select-String -Pattern $pattern).Count }
function Press($scan, $label) {
  Write-Output "  touche $label"
  if ($RealInput) { [Win]::Key([byte]$scan); Start-Sleep -Milliseconds 800 } else { Send ("key " + $keyNames[[int]$scan]); Start-Sleep -Milliseconds 300 }
}
function Stop-Game() { try { $null = $script:p.CloseMainWindow(); if (-not $script:p.WaitForExit(6000)) { Stop-Process -Id $script:p.Id -Force } } catch {}; Start-Sleep -Milliseconds 800 }
function Log() { (LogText) -split "\r?\n" }
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

# Positions (Healer.Ui.Layout) : menu Jouer (640,302), Atelier (640,374), Réglages (640,442) ; atelier : achat de la 1re carte (318,120), talents palier 1 (903,196) / (1149,196) ; cartes de niveau 1/2/3 au centre x = 312 / 640 / 968, y = 340 ;
# Jouer du combat (640,500) ; colonne gauche = alliés (Garde 144,148) ; colonne droite = sorts (Soin 1136,190) ;
# fin de combat : 2 boutons -> Recommencer (522,618) ; 3 boutons -> Recommencer (404,618), Niveau suivant (640,618) ;
# pause : Quitter le niveau (640,430) ; Retour (87,32).
$menuPlay = @(640, 302); $menuWorkshop = @(640, 374); $menuSettings = @(640, 442); $level1 = @(312, 340); $level2 = @(640, 340); $level3 = @(968, 340); $fightPlay = @(640, 500)

# ---- Scénario A -----------------------------------------------------------------------------
if (Want "A") {
Write-Output "Scénario A : menu, niveau 1, cibler, lancer un sort, pause au clavier"
Start-Game @("-healer-profile-dir", (New-ProfileDir "A"))
Tap $menuPlay[0] $menuPlay[1] "Jouer (menu)"
Tap $level1[0] $level1[1] "niveau 1"
Tap $fightPlay[0] $fightPlay[1] "Jouer (combat)"
Tap 144 148 "carte Garde (colonne gauche)"
Tap 1068 184 "sort Soin (icône, colonne droite)"
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
Expect $a "fiche des sorts : 4 sorts" "la pause affiche la fiche détaillée des 4 sorts"
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
WaitLog "combat terminé" 60
$b = Log
Forbid $b "niveau : l[23]" "un niveau verrouillé ne se lance pas"
Expect $b "état : combat terminé \(defeat\)" "le combat sans joueur se termine par une défaite"
Tap 522 618 "Recommencer"
$b2 = Log
Expect $b2 "état : nouveau combat" "le clic sur Recommencer relance un combat"
Press 0x01 "Échap (pause)"
Tap 640 480 "Quitter le niveau"
Stop-Game
$b3 = Log
Expect $b3 "écran : choix du niveau" "quitter le niveau ramène au choix du niveau"
}

# ---- Scénario C -----------------------------------------------------------------------------
if (Want "C") {
Write-Output "Scénario C : Espace démarre, Entrée redémarre après la défaite"
Start-Game @("-healer-timescale", "40", "-healer-level", "l1", "-healer-profile-dir", (New-ProfileDir "C"))
Press 0x39 "Espace (jouer)"
WaitLog "combat terminé" 60
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
WaitLog "sauvegarde : écrite" 60
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
Tap 640 480 "Quitter le niveau"
Tap 87 32 "← Menu"
Stop-Game
$e = Log
Expect $e "état : pause" "Échap met en pause"
Expect $e "écran : menu principal" "le bouton Menu ramène au menu principal"
}

# ---- Scénario F -----------------------------------------------------------------------------
if (Want "F") {
Write-Output "Scénario F : atelier (achats, talents, or insuffisant, palier verrouillé), sauvegarde, effet en combat après relance"
$dirF = New-ProfileDir "F"
Start-Game @("-healer-gold", "400", "-healer-progress", "l1:2", "-healer-profile-dir", $dirF)
Tap $menuWorkshop[0] $menuWorkshop[1] "Atelier (menu)"
Tap 318 120 "acheter l'Épée du Garde (50 or)"
Tap 1149 196 "talent palier 1 : Économe (150 or)"
Tap 903 196 "talent palier 1 : Soins vifs (changement gratuit)"
Tap 318 120 "Épée niveau 2 (80 or)"
Tap 318 120 "Épée niveau 3 (110 or)"
Tap 318 120 "Épée niveau 4 (150 or) : or insuffisant"
Tap 903 395 "talent palier 2 (verrouillé : 2 étoiles seulement)"
Press 0x01 "Échap (retour au menu)"
Stop-Game
$f1 = Log
Expect $f1 "écran : atelier" "le bouton Atelier ouvre l'atelier"
Expect $f1 "atelier : achat tank_weapon niveau 1" "un achat d'équipement réussit"
Expect $f1 "atelier : talent 1 thrifty" "le premier choix d'un palier réussit"
Expect $f1 "atelier : talent 1 quick_heal" "changer d'option dans un palier acheté réussit"
Expect $f1 "atelier : achat tank_weapon niveau 3" "les niveaux s'enchaînent tant que l'or suffit"
Expect $f1 "atelier : refus tank_weapon .NotEnoughGold." "sans assez d'or l'achat est refusé"
Forbid $f1 "atelier : talent 2" "un palier verrouillé ne s'achète pas"
Expect $f1 "écran : menu principal" "Échap revient au menu"
$profileF = Join-Path $dirF "profile.json"
Check (Test-Path $profileF) "la sauvegarde existe"
if (Test-Path $profileF) {
  $jf = Get-Content $profileF -Raw -Encoding UTF8 | ConvertFrom-Json
  Check ($jf.equipment.tank_weapon -eq 3) "la sauvegarde contient l'équipement (Épée niveau 3)"
  Check ($jf.talents.'1' -eq "quick_heal") "la sauvegarde contient le talent choisi en dernier"
  Check ($jf.wallet.gold -eq 10) "l'or restant est exact (400 - 50 - 150 - 80 - 110 = 10)"
}
Write-Output "  -- relance du jeu : l'équipement et le talent sont appliqués au combat"
Start-Game @("-healer-profile-dir", $dirF)
Tap $menuPlay[0] $menuPlay[1] "Jouer (menu)"
Tap $level1[0] $level1[1] "niveau 1"
Stop-Game
$f2 = Log
Expect $f2 "sauvegarde : chargée" "la sauvegarde est rechargée"
Expect $f2 "équipe : tank atk 39 " "l'Épée niveau 3 donne +12 % d'attaque au Garde (35 -> 39)"
Expect $f2 "Soin 15 mana" "le talent Soins vifs baisse le coût du Soin (18 -> 15)"
}

# ---- Scénario G -----------------------------------------------------------------------------
if (Want "G") {
Write-Output "Scénario G : sélection stable, maintenir un sort à la souris puis au clavier"
Start-Game @("-healer-profile-dir", (New-ProfileDir "G"))
Tap $menuPlay[0] $menuPlay[1] "Jouer (menu)"
Tap $level1[0] $level1[1] "niveau 1"
Tap $fightPlay[0] $fightPlay[1] "Jouer (combat)"
Tap 144 148 "carte Garde"
Tap 144 148 "carte Garde (encore)"
Press 0x10 "A (Garde, clavier AZERTY)"
Press 0x10 "A (Garde, encore)"
HoldMouse 1068 184 5 "le sort Soin (souris)"
$g1 = Log
$mouseCasts = Count $g1 "geste : sort heal_single → Cast"
HoldKey 0x02 4 "1 (Soin)"
$keyCasts = Count (Log) "geste : sort heal_single → Cast"
Start-Sleep -Milliseconds 1500
Press 0x4F "1 du pavé numérique (Soin)"
Stop-Game
$g2 = Log
$totalCasts = Count $g2 "geste : sort heal_single → Cast"
Forbid $g2 "cible = aucune" "re-toucher un allié (carte ou touche A) ne le désélectionne jamais"
Check ($mouseCasts -ge 3) "maintenir le sort à la souris 5 s l'enchaîne (lancers : $mouseCasts, au moins 3 attendus)"
Check ($mouseCasts -le 6) "la recharge est respectée (lancers : $mouseCasts, au plus 6 en 5 s)"
Check (($keyCasts - $mouseCasts) -ge 2) "maintenir la touche 1 pendant 4 s enchaîne aussi (lancers supplémentaires : $($keyCasts - $mouseCasts))"
Check ($totalCasts -gt $keyCasts) "le 1 du pavé numérique lance aussi le sort (lancers : $keyCasts puis $totalCasts)"
Expect $g2 "geste : sort heal_single → Cast \(cible tank\)" "le sort maintenu vise la cible sélectionnée"
}

# ---- Scénario H -----------------------------------------------------------------------------
if (Want "H") {
Write-Output "Scénario H : le Seigneur de Cendre s'enrage, le Golem (tutoriel) jamais"
Start-Game @("-healer-autoplay", "-healer-timescale", "40", "-healer-level", "l3", "-healer-profile-dir", (New-ProfileDir "H"))
Press 0x39 "Espace (jouer)"
WaitLog "boss enragé palier 1" 40
Stop-Game
$h1 = Log
Expect $h1 "état : boss enragé palier 1 .\+5 %." "le palier 1 d'enrage arrive (+5 %)"
Start-Game @("-healer-autoplay", "-healer-timescale", "40", "-healer-level", "l1", "-healer-profile-dir", (New-ProfileDir "H2"))
Press 0x39 "Espace (jouer)"
WaitLog "combat terminé" 40
Stop-Game
$h2 = Log
Forbid $h2 "boss enragé" "le Golem, boss tutoriel, ne s'enrage jamais"
}

# ---- Scénario I -----------------------------------------------------------------------------
if (Want "I") {
Write-Output "Scénario I : réglages (musique, effets, secousse), sauvegarde et rechargement"
$dirI = New-ProfileDir "I"
Start-Game @("-healer-profile-dir", $dirI)
Tap $menuSettings[0] $menuSettings[1] "Réglages (menu)"
Tap 692 200 "musique : moins"
Tap 692 200 "musique : moins"
Tap 920 296 "effets : plus"
Tap 920 296 "effets : plus"
Tap 692 392 "secousse : Non"
Press 0x01 "Échap (retour au menu)"
Stop-Game
$i1 = Log
Expect $i1 "écran : réglages" "le bouton Réglages ouvre les réglages"
Expect $i1 "réglage : musique 50" "un clic sur moins baisse la musique de 10 (60 -> 50)"
Expect $i1 "réglage : musique 40" "un second clic la baisse encore (40)"
Expect $i1 "réglage : effets 90" "un clic sur plus monte les effets (80 -> 90)"
Expect $i1 "réglage : effets 100" "et jusqu'au maximum (100)"
Expect $i1 "réglage : secousse non" "l'interrupteur coupe la secousse"
Expect $i1 "écran : menu principal" "Échap revient au menu"
$profileI = Join-Path $dirI "profile.json"
Check (Test-Path $profileI) "la sauvegarde existe"
if (Test-Path $profileI) {
  $ji = Get-Content $profileI -Raw -Encoding UTF8 | ConvertFrom-Json
  Check ($ji.settings.musicVolume -eq 40) "la sauvegarde contient le volume de la musique (40)"
  Check ($ji.settings.sfxVolume -eq 100) "la sauvegarde contient le volume des effets (100)"
  Check ($ji.settings.screenShake -eq $false) "la sauvegarde contient la secousse coupée"
}
Write-Output "  -- relance : les réglages sont rechargés (un clic sur moins part de 40, pas de 60)"
Start-Game @("-healer-profile-dir", $dirI)
Tap $menuSettings[0] $menuSettings[1] "Réglages (menu)"
Tap 692 200 "musique : moins"
Stop-Game
$i2 = Log
Expect $i2 "sauvegarde : chargée" "la sauvegarde est rechargée"
Expect $i2 "réglage : musique 30" "les réglages rechargés servent de point de départ (40 -> 30)"
}

# ---- Scénario J -----------------------------------------------------------------------------
if (Want "J") {
Write-Output "Scénario J : note de playtest (F8) : saisie, fichier de notes, capture, pause puis reprise"
$notesDir = Join-Path $env:TEMP ("healer-e2e-notes-" + [Guid]::NewGuid().ToString("N").Substring(0, 6)); New-Item -ItemType Directory -Path $notesDir | Out-Null
Start-Game @("-healer-notes-dir", $notesDir, "-healer-level", "l1", "-healer-profile-dir", (New-ProfileDir "J"))
Press 0x39 "Espace (jouer)"
Press 0x42 "F8 (ouvrir la note)"
Start-Sleep -Milliseconds 600
Send "text Le soin est trop lent"
Press 0x1C "Entrée (enregistrer)"
Start-Sleep -Milliseconds 400
$j = Log
Expect $j "note : Le soin est trop lent" "la note est enregistrée avec son texte"
$notesFile = Join-Path $notesDir "notes.md"
Check (Test-Path $notesFile) "le fichier de notes existe"
if (Test-Path $notesFile) {
  $md = Get-Content $notesFile -Raw -Encoding UTF8
  Check ($md -match "Le soin est trop lent") "le fichier contient la remarque"
  Check ($md -match "Boss :") "le fichier contient l'état du combat"
}
Check ((Get-ChildItem $notesDir -Filter "note_*.png" -ErrorAction SilentlyContinue).Count -ge 1) "une capture d'écran est jointe"
Expect $j "état : reprise" "le combat reprend après la note"
Stop-Game
}

if ($failures.Count -gt 0) { Write-Output ""; Write-Output "$($failures.Count) vérification(s) en échec."; Write-Output "[e2e-fin] code=1"; exit 1 }
Write-Output ""; Write-Output "Tous les tests de bout en bout passent."; Write-Output "[e2e-fin] code=0"
