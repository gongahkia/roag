@echo off
cd /d "%~dp0.."
python scripts\build.py run -- %*
exit /b %errorlevel%
