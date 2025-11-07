@echo off
echo ╔══════════════════════════════════════════════════════════╗
echo ║                   WTGFixer Launcher                      ║
echo ╚══════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

REM Build if needed
if not exist "bin\Release\net8.0\win-x64\publish\WTGFixer.exe" (
    echo Executable not found. Building first...
    call build-exe.bat
)

echo.
echo Running WTGFixer with auto-detection...
echo Looking for files in:
echo   ../Merged/   - Your merged/corrupted file
echo   ../Original/ - Your original file
echo.

bin\Release\net8.0\win-x64\publish\WTGFixer.exe

echo.
pause
