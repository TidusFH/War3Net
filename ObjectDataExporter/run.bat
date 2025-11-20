@echo off
echo ===================================
echo    Warcraft 3 Object Data Exporter
echo ===================================
echo.

REM Check if .NET is installed
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

echo .NET SDK found. Building project...
echo.

REM Build the project
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    echo Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo Build successful! Running application...
echo.
echo ===================================
echo.

REM Run the application
dotnet run --configuration Release

echo.
echo ===================================
echo Press any key to exit...
pause >nul
