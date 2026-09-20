@echo off
rem Lance le jeu Unity en MODE DÉVELOPPEUR : Atelier avec niveaux d'équipement +/- et or a 99 999.
rem La sauvegarde est SEPAREE (dev-profile) : la vraie progression n'est jamais touchee.
rem Prerequis : avoir construit le jeu (menu Unity « Healer > Construire Windows », ou : npm run build:unity).
set EXE=%~dp0unity\HealerGame\Builds\Windows\HealerGame.exe
if not exist "%EXE%" (
  echo Le jeu n'est pas construit : %EXE% introuvable.
  pause
  exit /b 1
)
start "" "%EXE%" -healer-dev
