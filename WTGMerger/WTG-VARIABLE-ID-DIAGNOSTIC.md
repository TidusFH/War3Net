# WTG Variable ID Diagnostic Guide

## How to Verify the Fix

After applying the fix to `ReadMapTriggersAuto()`, follow these steps to verify that variable IDs are being preserved correctly.

## Step 1: Check Console Output

When you run WTGMerger, you should now see:

```
Reading source: ../Source/map.w3x
  Opening MPQ archive...
  Extracting war3map.wtg...
  ⚠ Map has SubVersion=null (WC3 1.27 format)
  → Converting to SubVersion=v4 to enable variable ID support
  ✓ Assigned sequential IDs to 50 variable(s)
  ✓ SubVersion set to v4
✓ Source loaded: 100 items, 50 variables

Reading target: ../Target/map.w3x
  Opening MPQ archive...
  Extracting war3map.wtg...
  ⚠ Map has SubVersion=null (WC3 1.27 format)
  → Converting to SubVersion=v4 to enable variable ID support
  ✓ Assigned sequential IDs to 123 variable(s)
  ✓ SubVersion set to v4
✓ Target loaded: 200 items, 123 variables
```

**If you DON'T see these messages**, the fix didn't work or your maps already have SubVersion set.

## Step 2: Enable Debug Mode

In the interactive menu, select option `8` to enable debug mode:

```
Select option (0-9): 8

✓ Debug mode is now ON
```

Now when you copy a category (option 4), you should see detailed variable ID information:

```
[DEBUG] Variable IDs after fix:
[DEBUG]   ID=0, Name=AP1_Player
[DEBUG]   ID=1, Name=P02_IllidanMainBase
[DEBUG]   ID=2, Name=Arthas
[DEBUG]   ID=3, Name=RevivalHero
[DEBUG]   ID=4, Name=RevivalSpot
```

## Step 3: Verify Before Save

Before saving (option 9), check the pre-save verification output:

```
╔══════════════════════════════════════════════════════════╗
║              PRE-SAVE VERIFICATION                       ║
╚══════════════════════════════════════════════════════════╝
Variables in memory: 173
Trigger items: 290

[DEBUG] Sample variables before save:
[DEBUG]   ID=0, Name=AP1_Player, Type=player
[DEBUG]   ID=1, Name=P02_IllidanMainBase, Type=player
[DEBUG]   ID=2, Name=Arthas, Type=unit
[DEBUG]   ID=3, Name=RevivalHero, Type=unit
[DEBUG]   ID=4, Name=RevivalSpot, Type=location
```

**Critical Check:** IDs should be 0, 1, 2, 3, 4... NOT all 0!

## Step 4: Post-Save Verification

After the file is saved, the tool automatically reads it back for verification:

```
=== VERIFICATION: Reading saved file ===
Variables written: 173
Variables in saved file: 173
✓ All variables were saved correctly!
```

Then check the merged file debug info (option 'y'):

```
=== MERGED FILE VARIABLES ===
Total: 173

ID | Name                      | Type           | Array | Init
---|---------------------------|----------------|-------|-----
 0 | AP1_Player                | player         | No    | Yes
 1 | P02_IllidanMainBase       | player         | No    | Yes
 2 | Arthas                    | unit           | No    | No
 3 | RevivalHero               | unit           | No    | No
 4 | RevivalSpot               | location       | No    | No
```

**Success Criteria:**
- ✓ IDs are sequential: 0, 1, 2, 3, 4...
- ✓ No duplicate IDs found
- ✗ FAIL if all IDs are 0

## Step 5: Manual File Inspection

To verify the binary format is correct, use a hex editor or run this diagnostic code:

### Add Temporary Diagnostic Code

Add this to your Program.cs after line 337 (after WriteWTGFile):

```csharp
// DIAGNOSTIC: Verify file was written with SubVersion
Console.WriteLine("\n[DIAGNOSTIC] Checking written file format...");
using (var fs = File.OpenRead(outputPath))
using (var br = new BinaryReader(fs))
{
    var signature = br.ReadInt32();
    var firstInt = br.ReadInt32();

    Console.WriteLine($"  File signature: 0x{signature:X8} (expected: 0x57544700 'WTG\\0')");
    Console.WriteLine($"  First int after signature: {firstInt}");

    bool isSubVersion = Enum.IsDefined(typeof(MapTriggersSubVersion), firstInt);
    bool isFormatVersion = Enum.IsDefined(typeof(MapTriggersFormatVersion), firstInt);

    if (isSubVersion)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ File written with SubVersion = {(MapTriggersSubVersion)firstInt}");
        Console.ResetColor();

        var formatVer = br.ReadInt32();
        Console.WriteLine($"  FormatVersion = {(MapTriggersFormatVersion)formatVer}");
        Console.WriteLine($"  ✓ Variable IDs WILL be saved");
    }
    else if (isFormatVersion)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ File written with OLD format (FormatVersion = {(MapTriggersFormatVersion)firstInt})");
        Console.WriteLine($"  ✗ Variable IDs WILL NOT be saved - THIS IS THE BUG!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ Unknown format: {firstInt}");
        Console.ResetColor();
    }
}
```

**Expected Output (SUCCESS):**
```
[DIAGNOSTIC] Checking written file format...
  File signature: 0x57544700 'WTG\0'
  First int after signature: 4
  ✓ File written with SubVersion = v4
  FormatVersion = v7
  ✓ Variable IDs WILL be saved
```

**Bad Output (BUG STILL EXISTS):**
```
[DIAGNOSTIC] Checking written file format...
  File signature: 0x57544700 'WTG\0'
  First int after signature: 7
  ✗ File written with OLD format (FormatVersion = v7)
  ✗ Variable IDs WILL NOT be saved - THIS IS THE BUG!
```

## Step 6: Test in World Editor

The ultimate test is opening the merged map in World Editor:

1. Open World Editor
2. File → Open Map → Select your merged .w3x file
3. If it loads without errors: ✓ SUCCESS
4. If you see "Trigger data is invalid": ✗ FAIL

Then check triggers:

1. In World Editor, go to Trigger Editor (F6)
2. Select a copied trigger
3. Check if it references variables correctly
4. Try to edit the trigger - it should work without errors

## Troubleshooting

### Problem: Still seeing all IDs = 0

**Possible Causes:**

1. **Fix not applied correctly**
   - Verify you modified the right file (WTGMerger/Program.cs)
   - Check that ReadMapTriggersAuto contains the new code
   - Rebuild the project: `dotnet build`

2. **War3Net library caching**
   - Clean and rebuild: `dotnet clean && dotnet build`
   - Delete bin/ and obj/ directories

3. **SubVersion being reset somewhere**
   - Search for `SubVersion = null` in your code
   - Check line 277 - you may have BOTH places setting SubVersion now (that's OK)

### Problem: World Editor shows "Trigger data invalid"

**Possible Causes:**

1. **Compatibility issue with WC3 version**
   - SubVersion=v4 requires World Editor 1.31+
   - If using WC3 1.27, you cannot use variable IDs

2. **Corrupted triggers**
   - Check if trigger functions reference non-existent variables
   - Verify all variable names were copied correctly

3. **ParentId issues**
   - Categories/triggers might have invalid ParentIds
   - Use option 6 to fix category nesting

### Problem: Variables copied but IDs still wrong

Check if FixVariableIds is interfering:

1. In debug mode, check IDs immediately after ReadMapTriggersAuto
2. Check IDs after FixVariableIds (line 105-106)
3. If IDs change from correct to wrong, FixVariableIds has a bug

**To test:** Comment out lines 105-106 and see if it helps:

```csharp
// TEMPORARILY DISABLED FOR TESTING
// FixVariableIds(sourceTriggers, "source");
// FixVariableIds(targetTriggers, "target");
```

## Success Checklist

- [ ] Console shows "Converting to SubVersion=v4" message
- [ ] Console shows "Assigned sequential IDs" message
- [ ] Pre-save verification shows IDs: 0, 1, 2, 3...
- [ ] Post-save verification shows IDs: 0, 1, 2, 3...
- [ ] No "duplicate variable ID" error
- [ ] File format diagnostic shows "SubVersion = v4"
- [ ] Map opens in World Editor without errors
- [ ] Triggers work correctly in World Editor

If all items are checked ✓, the fix is working correctly!

## Additional Verification: Compare Before/After

To prove the fix works, compare variable ID distribution before and after:

### Before Fix (BUG):
```
ID | Count
---|------
 0 | 173   ← ALL VARIABLES HAVE ID 0!
```

### After Fix (SUCCESS):
```
ID | Count
---|------
 0 | 1
 1 | 1
 2 | 1
 3 | 1
...
172| 1
```

Each variable should have a unique ID.

## Need Help?

If the fix still doesn't work:

1. Enable debug mode (option 8)
2. Copy an entire category (option 4)
3. Save the output
4. Run the post-merge debug (answer 'y')
5. Share the complete console output

Include this information:
- War3Net version
- .NET version
- Source map WC3 version (1.27, 1.31, Reforged, etc.)
- Target map WC3 version
- Full console output with debug mode ON
