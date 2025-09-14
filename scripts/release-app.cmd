@echo off
cd /d "%~dp0"
powershell -NoLogo -ExecutionPolicy Bypass -File "%~dp0release-app.ps1"
pause