@echo off
echo ===================================
echo    Building Standalone EXE
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

echo .NET SDK found. Building standalone executable...
echo.
echo This will create a Windows 10 x64 compatible .exe
echo that includes the .NET runtime (self-contained).
echo.

REM Build standalone executable
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false

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
echo ===================================
echo.
echo The executable has been created at:
echo bin\Release\net8.0\win-x64\publish\ObjectDataExporter.exe
echo.
echo You can copy this .exe anywhere and run it
echo without needing to install .NET!
echo.
echo File size will be larger (~70-100MB) because
echo it includes the entire .NET runtime.
echo.
pause
