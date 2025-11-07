@echo off
echo ================================================================
echo   WARCRAFT 3 1.27 TRIGGER MERGER
echo   Old Format (SubVersion=null) - Position-Based Category IDs
echo ================================================================
echo.

REM Check .NET SDK
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found!
    echo.
    echo Please install .NET 8.0 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo Building project...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Running War3 Trigger Merger...
echo.
dotnet run --configuration Release

pause
