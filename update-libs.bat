@echo off
echo ========================================
echo Updating Libs folder with new DLLs
echo ========================================
echo.

cd /d "%~dp0"

if not exist "Dlls" (
    echo ERROR: Dlls folder not found!
    echo Please run build-core.bat first to build the project.
    pause
    exit /b 1
)

if not exist "Libs" (
    echo Creating Libs folder...
    mkdir Libs
)

echo Copying updated DLLs from Dlls to Libs...
xcopy /Y /Q Dlls\*.dll Libs\

echo.
echo ========================================
echo Libs folder updated successfully!
echo ========================================
echo.
echo Updated DLLs:
dir /b Libs\War3Net.*.dll

echo.
echo Next step: Rebuild WTGMerger
echo   cd WTGMerger
echo   dotnet build -c Release
echo.
pause
