@echo off
echo ╔══════════════════════════════════════════════════════════╗
echo ║              Building WTGFixer Executable                ║
echo ╚══════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

echo Building Release version...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Build successful!
    echo.
    echo Executable location:
    echo   %~dp0bin\Release\net8.0\win-x64\publish\WTGFixer.exe
    echo.
) else (
    echo.
    echo ❌ Build failed!
    exit /b 1
)

pause
