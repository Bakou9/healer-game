@echo off
rem Lance le jeu en mode développement : la page se recharge toute seule a chaque modification.
cd /d "%~dp0"
if not exist node_modules call npm install
call npm run dev -- --open
pause
