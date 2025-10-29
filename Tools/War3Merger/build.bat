@echo off
REM War3Merger Build Script for Windows
REM This script builds the TriggerMerger tool

echo ========================================
echo War3Merger Build Script
echo ========================================
echo.

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK is not installed
    echo.
    echo Please install .NET SDK first:
    echo Download from: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo .NET SDK found
dotnet --version
echo.

REM Navigate to the project directory
cd /d "%~dp0"

echo Building TriggerMerger...
echo.

REM Build the project
dotnet build -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Build successful!
    echo ========================================
    echo.
    echo The executable is located at:
    echo   bin\Release\net8.0\TriggerMerger.exe
    echo.
    echo Run it with:
    echo   cd bin\Release\net8.0
    echo   TriggerMerger.exe --help
    echo.
) else (
    echo.
    echo ========================================
    echo Build failed!
    echo ========================================
    echo.
    echo Please check the error messages above.
    pause
    exit /b 1
)

pause
