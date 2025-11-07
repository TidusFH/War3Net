# WTG Fixer

A standalone utility to validate and repair corrupted or incorrectly merged Warcraft III trigger files (.wtg).

## Purpose

WTGFixer analyzes merged WTG files and fixes common corruption issues:

- **Missing Variables** - Detects variables lost during merge and restores them from the original
- **Undefined Variables** - Identifies variables used in triggers that don't exist
- **Wrong ParentId Values** - Fixes triggers nested incorrectly (e.g., all triggers inside initialization)
- **SubVersion Missing** - Ensures SubVersion is set so ParentId saves correctly to disk
- **Orphaned Triggers** - Fixes triggers pointing to non-existent categories
- **Duplicate IDs** - Resolves ID conflicts by reassigning sequential IDs

## Usage

### Command Line

```bash
WTGFixer <merged.wtg> <original.wtg> [output.wtg]
```

**Arguments:**
- `merged.wtg` - Your merged/corrupted WTG file to fix
- `original.wtg` - Your original WTG file (for variable reference)
- `output.wtg` - (Optional) Output path (defaults to `merged_fixed.wtg`)

**Example:**
```bash
WTGFixer war3map_merged.wtg war3map_original.wtg war3map_fixed.wtg
```

### Folder Auto-Detection

If you run WTGFixer without arguments, it will look for files in these folders:

```
../Merged/    - Place your merged/corrupted file here
../Original/  - Place your original file here
```

The tool will auto-detect .w3x, .w3m, or .wtg files.

## How It Works

### 1. Validation Phase

WTGFixer performs comprehensive validation:

```
✓ Checking SubVersion...
✓ Checking for missing variables...
✓ Checking for undefined variables in triggers...
✓ Checking ParentId values...
✓ Checking for orphaned items...
✓ Checking for duplicate IDs...
```

### 2. Fixing Phase

If issues are found, WTGFixer prompts you to apply fixes:

```
Found 5 issue(s) that need fixing:
  - SubVersion issues: 1
  - Missing variables: 2
  - Wrong ParentIds: 3

Attempt automatic fix? (y/n):
```

### 3. Verification Phase

After fixing, WTGFixer re-validates the file to confirm all issues are resolved:

```
✓ All issues fixed successfully!
✓ Variables: 42
✓ Trigger items: 156
```

## Common Issues Fixed

### Issue 1: Categories Nested in Initialization

**Problem:**
```
'Initialization': ParentId=0
'Load Heroes': ParentId=0
'DEBUG': ParentId=0
```

All categories have `ParentId=0`, causing them to nest inside the initialization trigger.

**Fix:** Sets all category `ParentId = -1` for root-level placement.

### Issue 2: Missing Variables

**Problem:**
```
⚠ 5 variable(s) from original are missing:
  - PlayerHero (unit)
  - GameTime (real)
```

Variables were lost during merge.

**Fix:** Copies missing variables from the original WTG file.

### Issue 3: SubVersion Not Set

**Problem:**
```
⚠ SubVersion is null - ParentId won't be saved!
```

Without SubVersion, ParentId field is skipped during serialization, defaulting to 0 on reload.

**Fix:** Sets `SubVersion = v4` to enable ParentId serialization.

### Issue 4: Orphaned Triggers

**Problem:**
```
⚠ 3 orphaned trigger(s) (ParentId points to non-existent category):
  - 'Hero Selection' (ParentId=42)
```

Triggers reference categories that don't exist.

**Fix:** Assigns orphaned triggers to an existing category or creates a "Recovered Triggers" category.

### Issue 5: Duplicate IDs

**Problem:**
```
⚠ 2 duplicate ID(s) found:
  - ID 5: Category A, Trigger B
```

Multiple items share the same ID, causing corruption.

**Fix:** Reassigns sequential IDs (0, 1, 2, ...) to all items and updates ParentId references.

## Integration with WTGMerger

WTGFixer is designed to work with WTGMerger:

1. **Merge triggers** with WTGMerger
2. If issues occur, **run WTGFixer** to validate and repair
3. **Load in World Editor** to verify

## Build

```bash
cd WTGFixer
dotnet build -c Release
```

Or use the provided batch files (Windows):
```bash
build-exe.bat   # Build executable
run.bat         # Run with auto-detection
```

## Requirements

- .NET 8.0 Runtime
- War3Net library DLLs (in ../Libs folder)

## Technical Details

### ParentId Serialization

The critical issue WTGFixer addresses is that `ParentId` is **only saved to disk when `SubVersion != null`**.

From `TriggerCategoryDefinition.cs`:
```csharp
if (subVersion is not null)
{
    writer.WriteBool(IsExpanded);
    writer.Write(ParentId);  // Only written when SubVersion exists!
}
```

Without SubVersion, ParentId defaults to 0 when the file is reloaded, causing all categories to nest incorrectly.

### Variable Recovery

WTGFixer compares the merged file against the original and copies any missing variables with their:
- Name, Type
- Array status
- Initial values
- All metadata

### Safe ID Reassignment

When fixing duplicate IDs, WTGFixer:
1. Assigns sequential IDs to all items (0, 1, 2, ...)
2. Updates ParentId references to match new category IDs
3. Maintains the hierarchy structure

## Limitations

- Cannot recover variables that were never in the original file
- Cannot fix JASS code desync (use WTGMerger's JASS deletion feature)
- Works with WTG format versions supported by War3Net

## See Also

- **WTGMerger** - Interactive trigger merger tool
- **SYNCING-WTG-WITH-J.md** - Guide on WTG/JASS synchronization
- **DEBUGGING-GUIDE.md** - Debugging merged trigger files
