@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Building War3Net Core Libraries
echo With Custom API Support (YDWE, dzapi, etc.)
echo ========================================
echo.

cd /d "%~dp0"

set FAILED=0

echo [1/8] Building War3Net.Common...
dotnet build src\War3Net.Common\War3Net.Common.csproj -c Debug -v quiet
if errorlevel 1 (
    echo FAILED: War3Net.Common
    set FAILED=1
    goto :end
)
echo       SUCCESS

echo [2/8] Building War3Net.IO.Compression...
dotnet build src\War3Net.IO.Compression\War3Net.IO.Compression.csproj -c Debug -v quiet
if errorlevel 1 (
    echo FAILED: War3Net.IO.Compression
    set FAILED=1
    goto :end
)
echo       SUCCESS

echo [3/8] Building War3Net.IO.Mpq...
dotnet build src\War3Net.IO.Mpq\War3Net.IO.Mpq.csproj -c Debug -v quiet
if errorlevel 1 (
    echo FAILED: War3Net.IO.Mpq
    set FAILED=1
    goto :end
)
echo       SUCCESS

echo [4/8] Building War3Net.IO.Slk...
dotnet build src\War3Net.IO.Slk\War3Net.IO.Slk.csproj -c Debug -v quiet
if errorlevel 1 (
    echo FAILED: War3Net.IO.Slk
    set FAILED=1
    goto :end
)
echo       SUCCESS

echo [5/8] Building War3Net.CodeAnalysis...
dotnet build src\War3Net.CodeAnalysis\War3Net.CodeAnalysis.csproj -c Debug -v quiet
if errorlevel 1 (
    echo FAILED: War3Net.CodeAnalysis
    set FAILED=1
    goto :end
)
echo       SUCCESS

echo [6/8] Building War3Net.CodeAnalysis.Jass...
dotnet build src\War3Net.CodeAnalysis.Jass\War3Net.CodeAnalysis.Jass.csproj -c Debug -v quiet
if errorlevel 1 (
    echo FAILED: War3Net.CodeAnalysis.Jass
    set FAILED=1
    goto :end
)
echo       SUCCESS

echo [7/8] Building War3Net.Build.Core (contains TriggerData.txt with custom APIs)...
dotnet build src\War3Net.Build.Core\War3Net.Build.Core.csproj -c Debug -v quiet
if errorlevel 1 (
    echo FAILED: War3Net.Build.Core
    set FAILED=1
    goto :end
)
echo       SUCCESS - Contains YDWE, dzapi, dzapi2, kkapi, YDTrigger, bzapi

echo [8/8] Building War3Net.Build...
dotnet build src\War3Net.Build\War3Net.Build.csproj -c Debug -v quiet
if errorlevel 1 (
    echo FAILED: War3Net.Build
    set FAILED=1
    goto :end
)
echo       SUCCESS

:end
echo.
echo ========================================
if %FAILED%==0 (
    echo BUILD SUCCESSFUL!
    echo ========================================
    echo.
    echo Copying DLLs to Dlls folder...

    REM Create Dlls folder
    if not exist "Dlls" mkdir Dlls

    REM Copy all War3Net DLLs and their dependencies
    xcopy /Y /Q src\War3Net.Build.Core\bin\Debug\net5.0\*.dll Dlls\ >nul
    xcopy /Y /Q src\War3Net.Build\bin\Debug\net5.0\*.dll Dlls\ >nul

    echo.
    echo ========================================
    echo DLLs copied to: .\Dlls\
    echo ========================================
    echo.
    echo Copied files:
    dir /b Dlls\War3Net.*.dll
    echo.
    echo These DLLs include:
    echo   - Patches 1.20, 1.24, 1.26, 1.27 through 2.0.3
    echo   - Custom APIs: YDWE, dzapi, dzapi2, kkapi, YDTrigger, bzapi
    echo.
    echo All files are in the Dlls folder and ready to use!
) else (
    echo BUILD FAILED!
    echo ========================================
    echo Check the error messages above.
)
echo.
pause
exit /b %FAILED%
