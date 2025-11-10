# WTGMerger - Quick Start Guide (Post-Fix)

## What Was Fixed

**The Bug:** All variables had ID=0, causing "duplicate variable ID" errors.

**The Fix:** Automatically converts old WC3 1.27 maps to SubVersion=v4 format with proper sequential variable IDs.

## How to Use (After Fix)

### 1. Build the Project

```bash
cd /home/user/War3Net/WTGMerger
dotnet build
dotnet run
```

### 2. Place Your Maps

```
War3Net/
├── Source/
│   └── your_source_map.w3x
└── Target/
    └── your_target_map.w3x
```

### 3. Run and Copy Triggers

When you run, you'll now see:

```
Reading source: ../Source/your_source_map.w3x
  ⚠ Map has SubVersion=null (WC3 1.27 format)
  → Converting to SubVersion=v4 to enable variable ID support
  ✓ Assigned sequential IDs to 50 variable(s)
  ✓ SubVersion set to v4
```

This means the fix is working! ✓

### 4. Copy Your Category

```
Select option (0-9): 4
Enter category name to copy: Spels Heroes

Analyzing 50 variable(s) used by triggers:
  + Copied: 'p' (location)
  + Copied: 'WS_Caster' (unit)
  ...
✓ Copied 47 variable(s), renamed 0 variable(s)
```

### 5. Save and Verify

```
Select option (0-9): 9

PRE-SAVE VERIFICATION:
Variables in memory: 173

[DEBUG] Sample variables before save:
[DEBUG]   ID=0, Name=AP1_Player, Type=player    ← Should be 0
[DEBUG]   ID=1, Name=P02_IllidanMainBase, Type=player    ← Should be 1
[DEBUG]   ID=2, Name=Arthas, Type=unit    ← Should be 2

Writing file...
✓ All variables were saved correctly!
```

**Critical Check:** IDs should be 0, 1, 2, 3... NOT all 0!

### 6. Test in World Editor

1. Open World Editor
2. File → Open → Select `war3map_merged.w3x`
3. Should load without "trigger data invalid" error ✓
4. Open Trigger Editor (F6)
5. Check copied triggers work correctly

## Expected Output (Success)

### Before Fix (Bug):
```
ID | Name          | Type
---|---------------|------
 0 | AP1_Player    | player
 0 | Arthas        | unit      ← ALL IDs ARE 0!
 0 | RevivalHero   | unit
❌ ERROR: duplicate variable IDs!
```

### After Fix (Working):
```
ID | Name          | Type
---|---------------|------
 0 | AP1_Player    | player    ← Unique IDs!
 1 | Arthas        | unit
 2 | RevivalHero   | unit
✓ No duplicate IDs found
```

## Troubleshooting

### Still seeing all IDs = 0?

1. **Rebuild the project:**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Verify the fix was applied:**
   ```bash
   grep -A 5 "CRITICAL FIX" WTGMerger/Program.cs
   ```
   Should show the new code at line 2010.

3. **Enable debug mode:** Select option 8, then check detailed output

### Map won't open in World Editor?

Your map is now SubVersion=v4 format. This requires:
- ✓ World Editor 1.31 or later
- ✓ Warcraft 3 Reforged
- ✗ Does NOT work in 1.27

If you need 1.27 compatibility, we need a different solution.

### Variables still not copying?

Check for warnings:
```
⚠ Warning: Variable 'gg_trg_Something' not found in source map
```

Variables starting with `gg_trg_` are trigger references. You need to copy those triggers too.

## Debug Mode Features

Enable with option 8, then you'll see:

```
[DEBUG] Variable IDs after fix:
[DEBUG]   ID=0, Name=AP1_Player
[DEBUG]   ID=1, Name=P02_IllidanMainBase

[DEBUG] Function: Action - SetVariable (2 params)
[DEBUG] Param: Type=Variable, Value='Unit_VoiN'
[DEBUG]   >>> VARIABLE DETECTED: 'Unit_VoiN'
```

This helps verify:
- IDs are assigned correctly
- Variables are detected in triggers
- Copying is working as expected

## Common Issues Fixed by This Patch

### Issue 1: Orphaned Categories
**Symptom:** "32 orphaned categories (ParentId points to non-existent category)"

**Fix:** Use menu option 6 to set all categories to root level:
```
Select option (0-9): 6
Proceed? (y/n): y
✓ Fixed 32 categories to root-level
```

### Issue 2: Missing Trigger References
**Symptom:** "⚠ Warning: Variable 'gg_trg_VoiN01b' not found in source map"

**Solution:** These are trigger variables. Copy those triggers too:
1. List triggers in category (option 3)
2. Copy the missing trigger (option 5)
3. Try copying your main category again

### Issue 3: Variable Type Conflicts
**Symptom:** "⚠ CONFLICT: 'Caster' has different types"

**Auto-Fixed:** The tool automatically renames conflicting variables:
```
Source: unit
Target: player
→ Will rename source variable to 'Caster_Source'
```

## Documentation Files

- **VARIABLE-ID-BUG-ANALYSIS.md** - Deep technical analysis
- **WTG-VARIABLE-ID-DIAGNOSTIC.md** - Detailed verification steps
- **ANALYSIS-SUMMARY.md** - Complete tool overview
- **QUICK-START-GUIDE.md** - This file

## Success Checklist

After running WTGMerger, verify:

- [ ] Console shows "Converting to SubVersion=v4" during read
- [ ] Pre-save shows IDs: 0, 1, 2, 3...
- [ ] Post-save shows IDs: 0, 1, 2, 3...
- [ ] No "duplicate variable ID" error
- [ ] Map opens in World Editor without errors
- [ ] Copied triggers work in-game

If all items checked ✓, you're good to go!

## Next Steps

1. **Test with your actual maps**
2. **Verify in World Editor**
3. **Test in-game** to ensure triggers execute correctly
4. If everything works, you can safely use this tool for production

## Need Help?

If you encounter issues:

1. Enable debug mode (option 8)
2. Run your merge operation
3. Copy the full console output
4. Check the diagnostic guide: `WTG-VARIABLE-ID-DIAGNOSTIC.md`

## Git Status

Changes committed to branch: `claude/analyze-war3-tools-011CUyGQHvnL5dt1AfWgeauM`

Files modified:
- `WTGMerger/Program.cs` (lines 1993-2044)

New documentation:
- `VARIABLE-ID-BUG-ANALYSIS.md`
- `WTG-VARIABLE-ID-DIAGNOSTIC.md`
- `ANALYSIS-SUMMARY.md`
- `QUICK-START-GUIDE.md`

**Commit:** ec03682 - "Fix: Variable ID serialization bug in WTGMerger"
