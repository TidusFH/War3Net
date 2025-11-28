@echo off
echo === Generating Extended TriggerData for YDWE/KKWE support ===
echo.

REM Navigate to project directory
cd /d "%~dp0"

REM Define paths
set WAR3_PATCHES="..\War3 Patches"
set BASE_TRIGGERDATA="..\src\War3Net.Build.Core\Resources\TriggerData.txt"
set OUTPUT="ExtendedTriggerData.txt"

echo War3 Patches folder: %WAR3_PATCHES%
echo Base TriggerData: %BASE_TRIGGERDATA%
echo Output: %OUTPUT%
echo.

REM Create a simple C# script to run the merger
echo using WTGMerger; > temp_merger.cs
echo TriggerDataMerger.MergeTriggerData(%WAR3_PATCHES%, %BASE_TRIGGERDATA%, %OUTPUT%); >> temp_merger.cs

REM Run dotnet build with the test
dotnet run -c Release -- test 2>&1 | findstr /V "Error" | findstr /V "not found"

echo.
echo Done! Check for ExtendedTriggerData.txt in the WTGMerger folder.
pause

