# Variable ID Serialization Bug - Root Cause Analysis

## Problem Summary

All variables in the merged WTG file have `ID=0` instead of sequential IDs (0, 1, 2, 3...), causing a critical "duplicate variable ID" error that corrupts the map.

**Debug Output Evidence:**
```
ID | Name                      | Type           | Array | Init
---|---------------------------|----------------|-------|-----
 0 | AP1_Player                | player         | No    | Yes
 0 | P02_IllidanMainBase       | player         | No    | Yes
 0 | Arthas                    | unit           | No    | No
...
❌ ERROR: 1 duplicate variable ID(s) found: ID 0: [ALL 173 VARIABLES]
```

## Root Cause

### War3Net's Format Version Dependency

Variable IDs are **only serialized when `SubVersion` is NOT null**. This is in War3Net's core serialization code:

**File: `src/War3Net.Build.Core/Serialization/Binary/Script/VariableDefinition.cs`**

```csharp
// Line 35-39: READING variables
if (subVersion is not null)  // ← CRITICAL CHECK
{
    Id = reader.ReadInt32();
    ParentId = reader.ReadInt32();
}
// If subVersion is null, Id defaults to 0!

// Line 56-60: WRITING variables
if (subVersion is not null)  // ← CRITICAL CHECK
{
    writer.Write(Id);
    writer.Write(ParentId);
}
// If subVersion is null, Id is NOT written!
```

### The Bug Chain

1. **Initial Read (Source/Target maps):**
   - Your maps are WC3 1.27 format → `SubVersion = null`
   - Variables are read WITHOUT Id field
   - **All variables default to `Id = 0`**

2. **FixVariableIds() Call (Line 105-106):**
   - Correctly reassigns IDs: 0, 1, 2, 3...
   - Variables in memory now have correct IDs

3. **SubVersion Update (Line 277):**
   - Sets `targetTriggers.SubVersion = MapTriggersSubVersion.v4`
   - This should enable ID serialization

4. **Write Operation (Line 337):**
   - **EXPECTED:** Writes with SubVersion=v4, includes variable IDs
   - **ACTUAL:** Something goes wrong, IDs are lost

5. **Read Verification (Line 348):**
   - Reads file back
   - All IDs are 0 again

## Why IDs Are Lost

There are three possible failure points:

### Issue #1: File Header Format
When `WriteTo()` executes with `SubVersion=v4`, it writes:
```csharp
writer.Write((int)SubVersion);  // Writes SubVersion FIRST
writer.Write((int)FormatVersion);
```

But if the reader doesn't recognize this format marker, it might:
- Fall back to reading as old format (SubVersion=null)
- Skip the SubVersion header
- Read variables WITHOUT IDs

### Issue #2: Source File Compatibility
If your source file had `SubVersion=null`:
- It was written in old format (no variable IDs)
- When you modify and re-write with SubVersion=v4
- The format version changes from what World Editor expects
- May cause compatibility issues

### Issue #3: War3Net Version Bug
The War3Net library version you're using might have a bug where:
- SubVersion is set in the object
- But `WriteTo()` still uses the OLD SubVersion from when file was read
- This would bypass the `if (subVersion is not null)` check

## Verification Steps

Run these checks to confirm the root cause:

### Check 1: Verify SubVersion Before Write
```csharp
// Add before line 337 (WriteWTGFile call):
Console.WriteLine($"[VERIFY] SubVersion before write: {targetTriggers.SubVersion}");
Console.WriteLine($"[VERIFY] FormatVersion: {targetTriggers.FormatVersion}");
Console.WriteLine($"[VERIFY] Sample variable IDs:");
foreach (var v in targetTriggers.Variables.Take(5))
{
    Console.WriteLine($"  {v.Name}: ID={v.Id}");
}
```

### Check 2: Inspect Written File Header
```csharp
// After WriteWTGFile, read raw bytes:
using var fs = File.OpenRead(outputPath);
using var br = new BinaryReader(fs);
var signature = br.ReadInt32();
var firstInt = br.ReadInt32(); // This should be SubVersion (v4=4) if written correctly
Console.WriteLine($"[FILE] Signature: {signature}");
Console.WriteLine($"[FILE] First int after signature: {firstInt}");
Console.WriteLine($"  Is SubVersion enum? {Enum.IsDefined(typeof(MapTriggersSubVersion), firstInt)}");
Console.WriteLine($"  Is FormatVersion enum? {Enum.IsDefined(typeof(MapTriggersFormatVersion), firstInt)}");
```

### Check 3: Variable Write Trace
Add debug output to see if IDs are actually being written:
```csharp
// Modify your WriteWTGFile method to add trace before WriteTo call:
Console.WriteLine("[TRACE] Writing variables:");
foreach (var v in triggers.Variables.Take(3))
{
    Console.WriteLine($"  {v.Name}: ID={v.Id}, ParentId={v.ParentId}");
}
Console.WriteLine($"[TRACE] SubVersion for write: {triggers.SubVersion}");
```

## Solutions

### Solution 1: Force SubVersion Early (RECOMMENDED)
Set SubVersion immediately after reading files, BEFORE any manipulation:

```csharp
// Line 71 - After reading source:
MapTriggers sourceTriggers = ReadMapTriggersAuto(sourcePath);
if (sourceTriggers.SubVersion == null)
{
    sourceTriggers.SubVersion = MapTriggersSubVersion.v4;
}

// Line 91 - After reading target:
MapTriggers targetTriggers = ReadMapTriggersAuto(targetPath);
if (targetTriggers.SubVersion == null)
{
    targetTriggers.SubVersion = MapTriggersSubVersion.v4;
}

// NOW call FixVariableIds, which will properly set IDs
FixVariableIds(sourceTriggers, "source");
FixVariableIds(targetTriggers, "target");
```

### Solution 2: Custom Variable Writer
Create a custom writer that ALWAYS writes variable IDs regardless of SubVersion:

```csharp
static void WriteVariableWithId(BinaryWriter writer, VariableDefinition variable, MapTriggersFormatVersion formatVersion)
{
    writer.WriteString(variable.Name);
    writer.WriteString(variable.Type);
    writer.Write(variable.Unk);
    writer.WriteBool(variable.IsArray);
    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        writer.Write(variable.ArraySize);
    }
    writer.WriteBool(variable.IsInitialized);
    writer.WriteString(variable.InitialValue);

    // ALWAYS write ID, even if SubVersion is null
    writer.Write(variable.Id);
    writer.Write(variable.ParentId);
}
```

**WARNING:** This creates non-standard WTG format and may not be compatible with World Editor.

### Solution 3: Post-Read ID Assignment
After reading a file with SubVersion=null, immediately assign sequential IDs:

```csharp
static MapTriggers ReadMapTriggersAuto(string filePath)
{
    MapTriggers triggers;
    if (IsMapArchive(filePath))
    {
        triggers = ReadMapArchiveFile(filePath);
    }
    else
    {
        triggers = ReadWTGFile(filePath);
    }

    // Immediately fix IDs if SubVersion was null
    if (triggers.SubVersion == null)
    {
        Console.WriteLine($"  Map has SubVersion=null, assigning sequential variable IDs...");
        for (int i = 0; i < triggers.Variables.Count; i++)
        {
            triggers.Variables[i].Id = i;
        }
        // Set SubVersion to enable ID serialization
        triggers.SubVersion = MapTriggersSubVersion.v4;
    }

    return triggers;
}
```

### Solution 4: War3Net Library Update
Check if you're using the latest War3Net version. The serialization bug might be fixed in newer versions:

```bash
# Check current version
dotnet list package --include-transitive | grep War3Net

# Update to latest
dotnet add package War3Net.Build.Core --version <latest>
dotnet add package War3Net.IO.Mpq --version <latest>
```

## Recommended Fix

**Implement Solution 3** (Post-Read ID Assignment) as it's the cleanest approach:

1. Modify `ReadMapTriggersAuto()` to set SubVersion=v4 immediately after reading
2. Assign sequential IDs to all variables
3. This ensures all subsequent operations work with proper IDs
4. Output files will be in v4 format with variable IDs preserved

## Testing

After implementing the fix, verify:

1. **Before write:** All variables have unique sequential IDs
2. **After write:** File header starts with SubVersion value (4)
3. **After read-back:** All variables still have their assigned IDs
4. **In World Editor:** Map opens without "trigger data invalid" error

## Additional Notes

### About SubVersion vs FormatVersion

- **FormatVersion** (v7): Controls basic WTG structure
- **SubVersion** (v4, v7): Extended format with ParentId support
- Old WC3 1.27 maps: FormatVersion=v7, SubVersion=null
- New WC3 Reforged: FormatVersion=v7, SubVersion=v7

### Compatibility Warning

Converting from SubVersion=null to SubVersion=v4:
- ✓ World Editor 1.31+ can read
- ✗ World Editor 1.27 and older CANNOT read
- ✗ May break in old WC3 versions

If you need 1.27 compatibility, you cannot use variable IDs. In that case, you'd need to ensure unique variable names instead of relying on IDs.

## Related Files

- `/home/user/War3Net/src/War3Net.Build.Core/Serialization/Binary/Script/VariableDefinition.cs` - Variable serialization
- `/home/user/War3Net/src/War3Net.Build.Core/Serialization/Binary/Script/MapTriggers.cs` - Map serialization
- `/home/user/War3Net/WTGMerger/Program.cs` - Your merger tool (lines 105-106, 271-277, 337, 348)
