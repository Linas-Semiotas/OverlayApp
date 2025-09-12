@echo off
setlocal
set "SCRIPT=%~dp0commit.ps1"

if not exist "%SCRIPT%" (
  echo [commit.cmd] Missing "%SCRIPT%"
  pause
  exit /b 1
)

REM Keep the console open no matter what:
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -NoExit -File "%SCRIPT%"
echo.
pause