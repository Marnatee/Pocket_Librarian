@echo off
setlocal

cd /d "%~dp0pocket_librarian.client"

where node >nul 2>nul
if errorlevel 1 (
    echo Node.js was not found.
    echo Install the Node.js LTS version, then run this file again.
    pause
    exit /b 1
)

if not exist "node_modules" (
    echo Installing the React client packages for the first run...
    call npm.cmd install
    if errorlevel 1 (
        echo Package installation failed.
        pause
        exit /b 1
    )
)

echo Starting the Pocket Librarian React client...
call npm.cmd run dev

if errorlevel 1 pause
