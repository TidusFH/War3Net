═══════════════════════════════════════════════════════════════════════════════
  ObjectMerger - ObjectDataType Enum Error Fix
═══════════════════════════════════════════════════════════════════════════════

ERROR YOU'RE SEEING:
  "Value '1551069797' is not defined for enum of type ObjectDataType"

CAUSE:
  Your target map (UndeadX08.w3x) contains object modifications with data type
  values that are not in War3Net's ObjectDataType enum. War3Net's strict parsing
  throws an exception when it encounters these values.

THE FIX:
  I've already modified the War3Net source code to accept undefined enum values.
  However, you need to rebuild the War3Net.Common.dll file to apply the fix.

═══════════════════════════════════════════════════════════════════════════════
  HOW TO REBUILD (EASY METHOD)
═══════════════════════════════════════════════════════════════════════════════

1. Open Command Prompt in the War3Net folder

2. Run the rebuild script:
   rebuild-war3net-common.bat

3. Done! The new DLL will be copied to Libs\War3Net.Common.dll

═══════════════════════════════════════════════════════════════════════════════
  HOW TO REBUILD (MANUAL METHOD)
═══════════════════════════════════════════════════════════════════════════════

If the batch file doesn't work:

1. Open Command Prompt in the War3Net folder

2. Build War3Net.Common:
   dotnet build src\War3Net.Common\War3Net.Common.csproj -c Release

3. Copy the new DLL:
   copy /Y "src\War3Net.Common\bin\Release\net8.0\War3Net.Common.dll" "Libs\War3Net.Common.dll"

═══════════════════════════════════════════════════════════════════════════════
  REQUIREMENTS
═══════════════════════════════════════════════════════════════════════════════

- .NET 8.0 SDK must be installed
- Download from: https://dotnet.microsoft.com/download

To check if you have it:
  dotnet --version

═══════════════════════════════════════════════════════════════════════════════
  WHAT WAS CHANGED
═══════════════════════════════════════════════════════════════════════════════

File: src\War3Net.Common\Extensions\BinaryReaderExtensions.cs
Line 100: Changed to pass allowNoFlags: false

This allows the parser to accept enum values that aren't defined in the enum,
instead of throwing an ArgumentException. Maps with unusual/corrupted data can
now be loaded successfully.

═══════════════════════════════════════════════════════════════════════════════
  AFTER REBUILDING
═══════════════════════════════════════════════════════════════════════════════

Once you've rebuilt War3Net.Common.dll:

1. Run ObjectMerger again
2. The error should be gone
3. You can now merge objects between your maps

If you still get errors after rebuilding, let me know and I'll investigate further.

═══════════════════════════════════════════════════════════════════════════════
