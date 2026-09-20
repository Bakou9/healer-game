@echo off
rem Lance le jeu Unity normalement (vraie sauvegarde).
set EXE=%~dp0unity\HealerGame\Builds\Windows\HealerGame.exe
if not exist "%EXE%" (
  echo Le jeu n'est pas construit : %EXE% introuvable.
  pause
  exit /b 1
)
start "" "%EXE%"
