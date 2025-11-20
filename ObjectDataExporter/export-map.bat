@echo off
setlocal enabledelayedexpansion

REM ============================================
REM   Object Data Exporter - Easy Mode
REM ============================================

echo.
echo ===================================
echo    Object Data Exporter
echo ===================================
echo.

REM Get map file path
set "MAP_PATH="
set /p "MAP_PATH=Enter MAP file path (.w3x or .w3m) - or drag and drop: "
set "MAP_PATH=!MAP_PATH:"=!"

if "!MAP_PATH!"=="" (
    echo ERROR: Map path cannot be empty!
    pause
    exit /b 1
)

if not exist "!MAP_PATH!" (
    echo ERROR: Map file not found: !MAP_PATH!
    pause
    exit /b 1
)

echo.

REM Get output folder path (optional)
set "OUTPUT_PATH="
set /p "OUTPUT_PATH=Enter OUTPUT folder path (or press Enter for auto): "
set "OUTPUT_PATH=!OUTPUT_PATH:"=!"

echo.

REM Get format choice
echo Select export format:
echo   [1] TXT - Human-readable text (recommended)
echo   [2] INI - Configuration style
echo   [3] CSV - Spreadsheet format
echo.
set "FORMAT_CHOICE="
set /p "FORMAT_CHOICE=Choice (1-3, or press Enter for TXT): "

REM Default to TXT
if "!FORMAT_CHOICE!"=="" set "FORMAT_CHOICE=1"

REM Map choice to format string
set "FORMAT=txt"
if "!FORMAT_CHOICE!"=="2" set "FORMAT=ini"
if "!FORMAT_CHOICE!"=="3" set "FORMAT=csv"

echo.
echo ===================================
echo Configuration:
echo   Map:    !MAP_PATH!
if "!OUTPUT_PATH!"=="" (
    echo   Output: [auto-generated]
) else (
    echo   Output: !OUTPUT_PATH!
)
echo   Format: !FORMAT!
echo ===================================
echo.

REM Build the project if needed
if not exist "bin\Release\net8.0\ObjectDataExporter.dll" (
    echo Building project...
    dotnet build --configuration Release >nul 2>&1
    if !errorlevel! neq 0 (
        echo ERROR: Build failed!
        dotnet build --configuration Release
        pause
        exit /b 1
    )
)

REM Run the exporter with parameters
echo Running exporter...
echo.

if "!OUTPUT_PATH!"=="" (
    dotnet run --configuration Release --no-build -- "!MAP_PATH!" "" "!FORMAT!"
) else (
    dotnet run --configuration Release --no-build -- "!MAP_PATH!" "!OUTPUT_PATH!" "!FORMAT!"
)

echo.
echo ===================================
echo Done!
echo ===================================
pause
