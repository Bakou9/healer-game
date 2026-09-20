# Construit le jeu Windows (unity/HealerGame/Builds/Windows/HealerGame.exe) en ligne de commande, sans ouvrir l'Éditeur.
# Usage : npm run build:unity   (fermer l'Éditeur Unity avant : un projet ne s'ouvre qu'une fois)
$root = Split-Path -Parent $PSScriptRoot
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe"
$log = Join-Path $env:TEMP "unity-build.log"
$p = Start-Process -FilePath $unity -ArgumentList @("-batchmode", "-nographics", "-quit", "-projectPath", (Join-Path $root "unity\HealerGame"), "-executeMethod", "Healer.EditorTools.Builder.BuildWindows", "-logFile", $log) -WindowStyle Hidden -PassThru
if (-not $p.WaitForExit(600000)) { Stop-Process -Id $p.Id -Force; Write-Output "ÉCHEC : build trop long"; exit 1 }
Select-String -Path $log -Pattern "\[Healer\] (build|ÉCHEC)|error CS" | Select-Object -First 5 | ForEach-Object { $_.Line }
exit $p.ExitCode
