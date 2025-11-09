@echo off
setlocal

set SCRIPT_DIR=%~dp0
set WC3LIBS_DIR=%SCRIPT_DIR%..\wc3libs

echo ===============================================================
echo           Building WTGMerger (Pure Java Edition)
echo ===============================================================
echo.

REM Create output directory
if not exist "%SCRIPT_DIR%bin" mkdir "%SCRIPT_DIR%bin"

REM Build classpath
set CP=%WC3LIBS_DIR%\*

echo Compiling WTGMerger.java...
javac -cp "%CP%" -d "%SCRIPT_DIR%bin" "%SCRIPT_DIR%src\WTGMerger.java"

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build failed!
    exit /b 1
)

echo.
echo [SUCCESS] Build complete!
echo.
echo To run:
echo   run.bat input.wtg output.wtg
