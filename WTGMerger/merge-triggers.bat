@echo off
setlocal enabledelayedexpansion

REM ============================================
REM   War3Net WTG Trigger Merger - Easy Mode
REM ============================================

echo.
echo ===================================
echo    WTG Trigger Merger
echo ===================================
echo.

REM Get source file path
set "SOURCE_PATH="
set /p "SOURCE_PATH=Enter SOURCE WTG file path (or drag and drop): "
set "SOURCE_PATH=!SOURCE_PATH:"=!"

if "!SOURCE_PATH!"=="" (
    echo ERROR: Source path cannot be empty!
    pause
    exit /b 1
)

if not exist "!SOURCE_PATH!" (
    echo ERROR: Source file not found: !SOURCE_PATH!
    pause
    exit /b 1
)

echo.

REM Get target file path
set "TARGET_PATH="
set /p "TARGET_PATH=Enter TARGET WTG file path (or drag and drop): "
set "TARGET_PATH=!TARGET_PATH:"=!"

if "!TARGET_PATH!"=="" (
    echo ERROR: Target path cannot be empty!
    pause
    exit /b 1
)

if not exist "!TARGET_PATH!" (
    echo ERROR: Target file not found: !TARGET_PATH!
    pause
    exit /b 1
)

echo.

REM Get output file path
set "OUTPUT_PATH="
set /p "OUTPUT_PATH=Enter OUTPUT file path (where to save merged WTG): "
set "OUTPUT_PATH=!OUTPUT_PATH:"=!"

if "!OUTPUT_PATH!"=="" (
    echo ERROR: Output path cannot be empty!
    pause
    exit /b 1
)

echo.
echo ===================================
echo Configuration:
echo   Source: !SOURCE_PATH!
echo   Target: !TARGET_PATH!
echo   Output: !OUTPUT_PATH!
echo ===================================
echo.

REM Build the project if needed
if not exist "bin\Release\net8.0\WTGMerger.dll" (
    echo Building project...
    dotnet build --configuration Release >nul 2>&1
    if !errorlevel! neq 0 (
        echo ERROR: Build failed!
        dotnet build --configuration Release
        pause
        exit /b 1
    )
)

REM Run the merger with custom paths
echo Running merger...
echo.

dotnet run --configuration Release --no-build -- "!SOURCE_PATH!" "!TARGET_PATH!" "!OUTPUT_PATH!"

echo.
echo ===================================
echo Done!
echo ===================================
pause
