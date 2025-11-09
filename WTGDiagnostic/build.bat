@echo off
echo ===============================================================
echo           Building WTG Binary Diagnostic Tool
echo ===============================================================
echo.

dotnet build WTGDiagnostic.csproj

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo [SUCCESS] Build complete!
echo.
echo To run:
echo   dotnet run source.w3x target.w3x merged.w3x
echo.
echo Or:
echo   bin\Debug\net8.0\WTGDiagnostic.exe source.w3x target.w3x merged.w3x
pause
