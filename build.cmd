@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
set RC=%errorlevel%
echo.
if "%RC%"=="0" (echo BUILD OK) else (echo BUILD FAILED - exit %RC%)
pause
exit /b %RC%
