@echo off
echo ╔══════════════════════════════════════════════════════════╗
echo ║         Rebuild War3Net.Common.dll                       ║
echo ║   (Required for ObjectDataType enum fix)                ║
echo ╚══════════════════════════════════════════════════════════╝
echo.

echo [Step 1] Building War3Net.Common...
dotnet build src\War3Net.Common\War3Net.Common.csproj -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ✗ Build failed! Make sure .NET SDK is installed.
    echo   Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo.
echo [Step 2] Copying new DLL to Libs folder...
copy /Y "src\War3Net.Common\bin\Release\net8.0\War3Net.Common.dll" "Libs\War3Net.Common.dll"

if %ERRORLEVEL% NEQ 0 (
    echo ✗ Copy failed!
    pause
    exit /b 1
)

echo.
echo ✓ War3Net.Common.dll has been rebuilt and updated!
echo.
echo The fix allows ObjectMerger to load maps with undefined ObjectDataType values.
echo You can now run ObjectMerger again.
echo.
pause
