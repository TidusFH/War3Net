@echo off
echo ===================================
echo    Rebuilding WTGMerger
echo ===================================
echo.

cd /d "%~dp0"

echo Cleaning old build files...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo Building project with latest changes...
dotnet build --configuration Release

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    echo Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo ===================================
echo Build successful!
echo You can now run run.bat
echo ===================================
pause
