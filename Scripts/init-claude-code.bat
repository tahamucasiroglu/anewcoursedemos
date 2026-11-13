@echo off
chcp 65001 >nul
cd /d "%~dp0"
set "CHERE_INVOKING=1"

set "GITBASH=%ProgramFiles%\Git\git-bash.exe"
if not exist "%GITBASH%" set "GITBASH=%LocalAppData%\Programs\Git\git-bash.exe"
if not exist "%GITBASH%" for /f "delims=" %%G in ('where git.exe 2^>nul') do if exist "%%~dpG..\git-bash.exe" set "GITBASH=%%~dpG..\git-bash.exe"
if not exist "%GITBASH%" (
  echo Git Bash bulunamadı: https://git-scm.com/download/win
  pause
  exit /b 1
)

start "" "%GITBASH%" -c "./scripts/init claude-code; echo; read -n 1 -s -r -p 'Kapatmak için bir tuşa basın...'"
