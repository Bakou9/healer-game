@echo off
rem Lance le jeu avec la Soigneuse en pixel art (essai D-071) : sprites de Resources/Sprites/Druid a la place du modele 3D.
set EXE=%~dp0unity\HealerGame\Builds\Windows\HealerGame.exe
if not exist "%EXE%" (
  echo Le jeu n est pas construit : %EXE% introuvable.
  pause
  exit /b 1
)
start "" "%EXE%" -healer-sprites -healer-dev
