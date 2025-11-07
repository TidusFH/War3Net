@echo off
echo ============================================================
echo           WTGFixer - Manual File Mode
echo ============================================================
echo.

if "%~1"=="" (
    echo Usage: fix-file.bat ^<merged.wtg^> ^<original.wtg^> [source.wtg] [output.wtg]
    echo.
    echo Arguments:
    echo   merged.wtg   - The merged/corrupted file to fix
    echo   original.wtg - Original target file (for variable reference)
    echo   source.wtg   - (Optional) Source file to check added triggers
    echo   output.wtg   - (Optional) Output file path
    echo.
    echo Example:
    echo   fix-file.bat ../Target/war3map_merged.wtg ../Target/war3map.wtg ../Source/war3map.wtg
    echo.
    pause
    exit /b 1
)

cd /d "%~dp0"

REM Build if needed
if not exist "bin\Release\net8.0\win-x64\publish\WTGFixer.exe" (
    echo Executable not found. Building first...
    call build-exe.bat
)

echo.
echo Running WTGFixer...
echo   Merged:   %~1
echo   Original: %~2
if not "%~3"=="" echo   Source:   %~3
if not "%~4"=="" echo   Output:   %~4
echo.

REM Pass all arguments to WTGFixer
bin\Release\net8.0\win-x64\publish\WTGFixer.exe %*

echo.
pause
