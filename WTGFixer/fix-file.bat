@echo off
echo ╔══════════════════════════════════════════════════════════╗
echo ║              WTGFixer - Manual File Mode                 ║
echo ╚══════════════════════════════════════════════════════════╝
echo.

if "%~1"=="" (
    echo Usage: fix-file.bat ^<merged.wtg^> ^<original.wtg^> [output.wtg]
    echo.
    echo Example:
    echo   fix-file.bat war3map_merged.wtg war3map_original.wtg war3map_fixed.wtg
    echo.
    pause
    exit /b 1
)

cd /d "%~dp0"

REM Build if needed
if not exist "bin\Release\net8.0\win-x64\publish\WTGFixer.exe" (
    echo Executable not found. Building first...
    call build-exe.bat
)

echo.
echo Running WTGFixer...
echo   Merged:   %~1
echo   Original: %~2
echo   Output:   %~3
echo.

if "%~3"=="" (
    bin\Release\net8.0\win-x64\publish\WTGFixer.exe "%~1" "%~2"
) else (
    bin\Release\net8.0\win-x64\publish\WTGFixer.exe "%~1" "%~2" "%~3"
)

echo.
pause
