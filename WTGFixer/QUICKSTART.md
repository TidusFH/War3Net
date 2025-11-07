# WTGFixer Quick Start Guide

## What is WTGFixer?

WTGFixer is a **validation and repair tool** for Warcraft III trigger files (.wtg). Use it after merging triggers to detect and fix common corruption issues.

## Common Scenarios

### Scenario 1: "All my categories are nested inside Initialization!"

**Symptom:**
```
❌ ERROR: ParentIds were NOT saved correctly!
  'Initialization': ParentId=0
  'Load Heroes': ParentId=0
  'DEBUG': ParentId=0
```

**Solution:**
```bash
# Run WTGFixer to fix ParentId values
WTGFixer war3map_merged.wtg war3map_original.wtg war3map_fixed.wtg
```

WTGFixer will:
1. Detect categories with `ParentId=0` (wrong)
2. Fix them to `ParentId=-1` (root level)
3. Set `SubVersion=v4` so ParentId saves correctly

---

### Scenario 2: "Some variables disappeared after merging!"

**Symptom:**
```
⚠ 5 variable(s) from original are missing:
  - PlayerHero (unit)
  - GameTime (real)
```

**Solution:**
WTGFixer compares your merged file with the original and restores missing variables automatically.

---

### Scenario 3: "Triggers reference variables that don't exist!"

**Symptom:**
```
⚠ 3 undefined variable(s) used in triggers:
  - TempUnit
  - HeroArray
```

**Solution:**
WTGFixer detects undefined variables. If they exist in the original, it will restore them. If not, you'll be warned.

---

## Quick Usage

### Option 1: Folder Auto-Detection (Easiest)

1. **Create folders** next to WTGFixer:
   ```
   WTGFixer/
   Merged/     ← Put your merged/corrupted file here
   Original/   ← Put your original file here
   ```

2. **Double-click** `run.bat`

3. **Follow prompts** - WTGFixer will auto-detect files and fix issues

---

### Option 2: Command Line

```bash
WTGFixer merged.wtg original.wtg fixed.wtg
```

**Arguments:**
- `merged.wtg` - Your merged/corrupted file
- `original.wtg` - Your original file (for variable reference)
- `fixed.wtg` - Output file

---

### Option 3: Drag & Drop (Windows)

Create `fix-my-file.bat`:
```batch
@echo off
WTGFixer.exe "%~1" "C:\path\to\original.wtg" "%~dp1%~n1_fixed%~x1"
pause
```

Drag your merged file onto the batch file!

---

## Validation Checks

WTGFixer performs these checks:

| Check | What it finds | Auto-fix? |
|-------|---------------|-----------|
| ✓ SubVersion | Missing SubVersion (ParentId won't save) | Yes |
| ✓ Missing Variables | Variables from original that are missing | Yes |
| ✓ Undefined Variables | Triggers using non-existent variables | Warns |
| ✓ Wrong ParentIds | Categories nested incorrectly | Yes |
| ✓ Orphaned Items | Triggers pointing to deleted categories | Yes |
| ✓ Duplicate IDs | Multiple items with same ID | Yes |

---

## Example Output

```
╔══════════════════════════════════════════════════════════╗
║              War3Net WTG Fixer Utility                   ║
║  Repairs corrupted/merged WTG files with validation      ║
╚══════════════════════════════════════════════════════════╝

Merged file:   C:\Maps\war3map_merged.wtg
Original file: C:\Maps\war3map_original.wtg
Output file:   C:\Maps\war3map_merged_fixed.wtg

Reading merged file...
✓ Merged: 156 items, 38 variables

Reading original file...
✓ Original: 120 items, 42 variables

╔══════════════════════════════════════════════════════════╗
║                    VALIDATION PHASE                      ║
╚══════════════════════════════════════════════════════════╝

Checking SubVersion...
  ⚠ SubVersion is null - ParentId won't be saved!

Checking for missing variables...
  ⚠ 4 variable(s) from original are missing:
    - PlayerHero (unit)
    - GameTime (real)
    - TempGroup (unitgroup)
    - IsGameStarted (boolean)

Checking ParentId values...
  ⚠ 5 categor(ies) with ParentId >= 0 (should be -1 for root):
    - 'Initialization' (ParentId=0)
    - 'Load Heroes' (ParentId=0)
    - 'DEBUG' (ParentId=0)

╔══════════════════════════════════════════════════════════╗
║                   VALIDATION SUMMARY                     ║
╚══════════════════════════════════════════════════════════╝

Found 10 issue(s) that need fixing:
  - SubVersion issues: 1
  - Missing variables: 4
  - Undefined variables: 0
  - Wrong ParentIds: 5
  - Orphaned items: 0
  - Duplicate IDs: 0

Attempt automatic fix? (y/n): y

╔══════════════════════════════════════════════════════════╗
║                      FIXING PHASE                        ║
╚══════════════════════════════════════════════════════════╝

Setting SubVersion to v4...
  ✓ SubVersion set

Copying 4 missing variable(s) from original...
  + PlayerHero (unit)
  + GameTime (real)
  + TempGroup (unitgroup)
  + IsGameStarted (boolean)
  ✓ Copied 4 variable(s)

Fixing 5 categor(ies) with wrong ParentId...
  - 'Initialization': 0 → -1
  - 'Load Heroes': 0 → -1
  - 'DEBUG': 0 → -1
  ✓ All categories set to root level

✓ Applied 10 fix(es)

Saving fixed file to: C:\Maps\war3map_merged_fixed.wtg

╔══════════════════════════════════════════════════════════╗
║                     VERIFICATION                         ║
╚══════════════════════════════════════════════════════════╝

✓ All issues fixed successfully!
✓ Variables: 42
✓ Trigger items: 156

✓ Fixed file saved: C:\Maps\war3map_merged_fixed.wtg
```

---

## Build from Source

```bash
cd WTGFixer
dotnet build -c Release
```

Or use batch files:
```bash
build-exe.bat   # Build executable
```

---

## When to Use WTGFixer

| Use WTGFixer when... | Don't use WTGFixer when... |
|----------------------|----------------------------|
| ✓ Triggers nested incorrectly after merge | ✗ You need to merge two different maps |
| ✓ Variables are missing after merge | ✗ You want to copy specific categories |
| ✓ World Editor shows "trigger data invalid" | ✗ Initial merge (use WTGMerger first) |
| ✓ ParentId not saving to disk | |
| ✓ Validating a merge result | |

**Workflow:**
1. **Merge** with WTGMerger
2. **Fix** with WTGFixer (if needed)
3. **Test** in World Editor

---

## Troubleshooting

### "File format not supported"
- WTGFixer requires .wtg files or .w3x/.w3m archives
- Make sure you're pointing to a valid WC3 map file

### "No map files found"
- Check that files exist in `../Merged/` and `../Original/` folders
- Or use command line to specify exact paths

### "Could not find internal MapTriggers constructor"
- War3Net DLLs are missing from `../Libs/` folder
- Copy DLLs from WTGMerger's Libs folder

### "Some issues remain after fix"
- Some issues may require manual intervention
- Check the validation output for details
- Consider opening in World Editor to inspect

---

## Tips

1. **Always keep backups** of your original files before fixing
2. **Run WTGFixer after every merge** to catch issues early
3. **Use with WTGMerger** for best results
4. **Check World Editor** after fixing to verify everything works
5. **Delete war3map.j** after fixing (see SYNCING-WTG-WITH-J.md)

---

## See Also

- `README.md` - Full documentation
- `../WTGMerger/` - Interactive trigger merger
- `SYNCING-WTG-WITH-J.md` - WTG/JASS synchronization guide
