@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish-Windows-App.ps1"
endlocal
