@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release-app.ps1" %*
echo.
pause