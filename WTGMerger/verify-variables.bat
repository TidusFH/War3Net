@echo off
echo ====================================
echo Variable Verification Script
echo ====================================
echo.

REM This script helps verify which files have which variables

echo Checking SOURCE file...
echo ------------
dotnet run -- --verify-vars "../Source/war3map.wtg"
echo.

echo Checking TARGET (original) file...
echo ------------
dotnet run -- --verify-vars "../Target/war3map.wtg"
echo.

echo Checking MERGED (output) file...
echo ------------
dotnet run -- --verify-vars "../Target/war3map_merged.wtg"
echo.

pause
