@echo off
rem Lance le jeu avec les illustrations 2D (essai D-074) : Resources/Art2D a la place des modeles 3D.
set EXE=%~dp0unity\HealerGame\Builds\Windows\HealerGame.exe
if not exist "%EXE%" (
  echo Le jeu n est pas construit : %EXE% introuvable.
  pause
  exit /b 1
)
start "" "%EXE%" -healer-art2d -healer-dev
