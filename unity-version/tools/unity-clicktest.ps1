# Test de gestes réels : lance le jeu Windows, clique avec la vraie souris (fenêtre du jeu uniquement) sur
# un sort SANS cible, puis sur une carte d'allié, puis sur un sort, et lit le journal du jeu pour vérifier
# que le contrôleur a bien reçu ces gestes. À lancer quand personne n'utilise la souris (elle est déplacée).
# Usage : powershell -File tools/unity-clicktest.ps1
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
  public const uint DOWN = 0x0002, UP = 0x0004;
  public static void Click(int x, int y) { SetCursorPos(x, y); System.Threading.Thread.Sleep(120); mouse_event(DOWN, 0, 0, 0, UIntPtr.Zero); System.Threading.Thread.Sleep(80); mouse_event(UP, 0, 0, 0, UIntPtr.Zero); }
}
"@

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "unity\HealerGame\Builds\Windows\HealerGame.exe"
$plog = Join-Path $env:USERPROFILE "AppData\LocalLow\Healer\Healer Game\Player.log"
$p = Start-Process -FilePath $exe -ArgumentList @("-screen-width", "540", "-screen-height", "900", "-screen-fullscreen", "0") -PassThru
Start-Sleep -Seconds 6
$p.Refresh()
$h = $p.MainWindowHandle
if ($h -eq 0) { Write-Output "ÉCHEC : fenêtre du jeu introuvable"; try { Stop-Process -Id $p.Id -Force } catch {}; exit 1 }
[Win]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 500
$r = New-Object Win+RECT; [Win]::GetClientRect($h, [ref]$r) | Out-Null
$origin = New-Object Win+POINT; $origin.X = 0; $origin.Y = 0; [Win]::ClientToScreen($h, [ref]$origin) | Out-Null
$w = $r.Right; $hgt = $r.Bottom
$scale = [Math]::Min($w / 480.0, $hgt / 854.0)
$offX = ($w - 480 * $scale) / 2; $offY = ($hgt - 854 * $scale) / 2
function Tap($lx, $ly, $label) {
  $sx = [int]($origin.X + $offX + $lx * $scale); $sy = [int]($origin.Y + $offY + $ly * $scale)
  Write-Output ("clic {0} : logique ({1},{2}) -> écran ({3},{4})" -f $label, $lx, $ly, $sx, $sy)
  [Win]::Click($sx, $sy); Start-Sleep -Milliseconds 700
}
Write-Output "fenêtre : client ${w}x${hgt}, échelle $([math]::Round($scale,3))"
# Zones logiques (Healer.Ui.Layout) : sorts y=632..796 (4 boutons de 108 px), cartes y=340..540.
Tap 66 714 "sort Soin sans cible"
Tap 66 440 "carte Garde"
Tap 66 714 "sort Soin avec cible"
Tap 182 714 "sort Soin de zone"
Start-Sleep -Seconds 1
try { Stop-Process -Id $p.Id -Force } catch {}
Start-Sleep -Milliseconds 500
Write-Output "--- journal du jeu :"
Get-Content $plog | Select-String -Pattern "\[Healer\] geste" | ForEach-Object { $_.Line }
