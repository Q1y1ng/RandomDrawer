@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0test.ps1"
set RC=%errorlevel%
echo.
if "%RC%"=="0" (echo TEST OK) else (echo TEST FAILED - %RC% item^(s^))
pause
exit /b %RC%
