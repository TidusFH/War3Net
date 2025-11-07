# WC3 1.27 Trigger Merger (Old Format)

A production-ready C# .NET 8.0 console application for merging triggers between Warcraft 3 1.27 maps while maintaining **full compatibility** with World Editor 1.27 and the **old format** (`SubVersion=null`).

## Features

- ✅ **WC3 1.27 Old Format Support** - Preserves `SubVersion=null` for compatibility
- ✅ **Position-Based Category IDs** - Ensures category IDs match positions (critical for old format)
- ✅ **Category Structure Validation** - Automatically fixes mismatched IDs
- ✅ **Interactive Menu** - Easy-to-use interface for selective merging
- ✅ **Variable Copy** - Automatically copies variables used by triggers
- ✅ **Map Archive Support** - Works with `.w3x`, `.w3m`, and raw `.wtg` files
- ✅ **JASS Synchronization** - Option to delete `war3map.j` for regeneration
- ✅ **Debug Mode** - Detailed logging for troubleshooting

## Quick Start

### Prerequisites

- **.NET 8.0 SDK** - Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- **WC3 1.27 maps** - Source and target maps to merge

### Installation

1. Place your **source map** (with triggers to copy) in the `../Source/` folder
2. Place your **target map** (to merge triggers into) in the `../Target/` folder
3. Run `run.bat`

### Basic Usage

```bash
# Auto-detect maps in Source and Target folders
run.bat

# Or specify custom paths
dotnet run -- "path/to/source.w3x" "path/to/target.w3x" "path/to/output.w3x"
```

### Example Workflow

1. **List categories** from source and target maps
2. **Copy entire category** or **specific triggers**
3. **Save and exit** - merged map is created with `_merged` suffix
4. **Open in World Editor 1.27** - triggers appear in correct categories

---

## How It Works

### The Old Format Challenge

WC3 1.27 uses an **old format** (`SubVersion=null`) with specific requirements:

| Requirement | Description |
|-------------|-------------|
| **Category ID = Position** | Category ID MUST equal its position in `TriggerItems` array |
| **Category ParentId = 0** | All root categories MUST have `ParentId=0` (not -1) |
| **Trigger ParentId = Position** | Trigger `ParentId` references the POSITION of its category |
| **SubVersion = null** | NEVER change `SubVersion` from `null` |
| **Categories First** | ALL categories MUST appear before triggers in `TriggerItems` |

### Why Position-Based IDs?

In WC3 1.27 old format, trigger `ParentId` is a **POSITION INDEX** into the `TriggerItems` array, NOT an ID lookup.

**Example:**
```
TriggerItems array:
[0] Category "Init" (ID=0)
[1] Category "Heroes" (ID=1)
[2] Category "Spells" (ID=2)
[3] Trigger "Map Init" (ParentId=0 → looks up position 0 = "Init")
[4] Trigger "Hero Spawn" (ParentId=1 → looks up position 1 = "Heroes")
```

If category IDs don't match positions, triggers appear in wrong categories in World Editor!

### Key Functions

#### `FixCategoryIdsForOldFormat()`

The **most critical function** - ensures category IDs match positions:

1. Get all categories from `TriggerItems`
2. Build mapping: `oldId → newId` (where `newId = position`)
3. Remove ALL categories from `TriggerItems`
4. Re-insert categories at positions 0, 1, 2, 3... with `ID = position`
5. Update all trigger `ParentIds` using the mapping
6. Set all category `ParentIds` to 0

**Called:**
- Immediately after loading maps
- After merging categories
- After copying triggers
- Before saving

#### `CopySpecificTriggers()`

Copies specific triggers from source to target:

1. Find triggers by name in source category
2. Find or create destination category (with `ID = category count`, `ParentId = 0`)
3. Insert category at correct position (NOT using `.Add()`!)
4. Copy variables used by triggers
5. Copy triggers with `ParentId = destination category ID`
6. Call `FixCategoryIdsForOldFormat()` to ensure consistency

#### `MergeCategory()`

Merges entire category with all its triggers:

1. Find source category by name
2. Get all triggers in source category
3. If category exists in target, remove it and fix structure
4. Create new category with `ID = category count`, `ParentId = 0`
5. Insert at correct position
6. Copy variables and triggers
7. Call `FixCategoryIdsForOldFormat()`

#### `SaveMergedMap()`

Validates and saves with format preservation:

1. **Pre-save validation:**
   - Verify `SubVersion == null` (not changed)
   - Verify category IDs match positions
   - Verify category `ParentIds == 0`
   - Verify trigger `ParentIds` reference valid categories
2. **Save:**
   - If `.w3x/.w3m`: Update `war3map.wtg` in archive
   - Optionally delete `war3map.j` (recommended)
   - If raw `.wtg`: Write directly
3. **Post-save verification:**
   - Read file back and verify counts
   - Verify `SubVersion` still null

---

## Menu Options

```
1. List all categories from SOURCE
   - Shows all categories with IDs, positions, and trigger counts

2. List all categories from TARGET
   - Shows target categories with validation (ID vs position)

3. List triggers in a specific category
   - Shows triggers with ParentId and enabled status

4. Copy ENTIRE category
   - Copies category and all its triggers
   - Removes old category if it exists

5. Copy SPECIFIC trigger(s)
   - Copy one or more triggers (comma-separated)
   - Can specify different destination category

6. Show format & structure debug info
   - Shows format version, SubVersion, category structure
   - Highlights ID/position mismatches

7. Manually fix target category IDs
   - Force-fixes category structure if needed
   - Usually not necessary (auto-fixed after operations)

8. Toggle debug mode
   - Shows detailed logging of operations
   - Useful for troubleshooting

9. Save and exit
   - Validates structure before saving
   - Creates output file with verification

0. Exit without saving
   - Discards all changes
```

---

## Technical Details

### File Format Specifications

#### Old Format (WC3 1.27) - What Gets Saved

| Item Type | ID Saved? | ParentId Saved? | Position Saved? |
|-----------|-----------|-----------------|-----------------|
| Category | ✅ YES | ❌ NO (always reads as 0) | ❌ NO |
| Trigger | ❌ NO (becomes 0) | ✅ YES | ❌ NO |
| Variable | ❌ NO (becomes 0) | ❌ NO | ❌ NO |

#### Format Detection

```csharp
if (mapTriggers.SubVersion == null)
{
    // OLD FORMAT - WC3 1.27
    // Use position-based category IDs
    // Category ParentId = 0
}
else
{
    // ENHANCED FORMAT - WC3 Reforged
    // Use ID-based lookups
    // Category ParentId can be -1
}
```

### Project Structure

```
WTGMerger/
├── Program.cs           # Main implementation (1200+ lines)
├── WTGMerger.csproj     # .NET 8.0 project configuration
├── run.bat              # Windows batch script
├── README.md            # This file
└── ../Libs/             # War3Net DLLs
    ├── War3Net.Build.Core.dll
    ├── War3Net.Build.dll
    ├── War3Net.Common.dll
    └── War3Net.IO.Mpq.dll
```

### Dependencies

- **War3Net.Build.Core** - Core trigger structures
- **War3Net.Build** - Map building utilities
- **War3Net.Common** - Common utilities
- **War3Net.IO.Mpq** - MPQ archive reading/writing

---

## Important Rules

### ❌ DO NOT

1. **DO NOT change SubVersion**
   ```csharp
   // WRONG:
   triggers.SubVersion = MapTriggersSubVersion.v4; // Breaks 1.27!

   // CORRECT:
   // Leave SubVersion alone, never modify it
   ```

2. **DO NOT use .Add() for categories**
   ```csharp
   // WRONG:
   target.TriggerItems.Add(newCategory); // Goes to end!

   // CORRECT:
   target.TriggerItems.Insert(categoryCount, newCategory);
   ```

3. **DO NOT forget to fix structure**
   ```csharp
   // WRONG:
   RemoveCategory(target, "Heroes");
   AddNewCategory(target, "Spells");
   // Categories now have misaligned IDs!

   // CORRECT:
   RemoveCategory(target, "Heroes");
   AddNewCategory(target, "Spells");
   FixCategoryIdsForOldFormat(target); // Fix everything!
   ```

### ✅ DO

1. **Always use ParentId=0 for categories**
2. **Always set category ID = position**
3. **Always call FixCategoryIdsForOldFormat() after structural changes**
4. **Always verify SubVersion is still null before saving**
5. **Always place categories before triggers in TriggerItems**

---

## Troubleshooting

### Triggers appear in wrong categories

**Cause:** Category IDs don't match positions

**Solution:** Use option 7 to manually fix category IDs, or rebuild from scratch

### Map won't open in World Editor

**Cause:** SubVersion was changed from null, or structure is corrupted

**Solution:**
1. Check if SubVersion is still null (use debug mode)
2. Verify category structure with option 6
3. Try loading the original target map

### "Trigger data invalid" error

**Cause:** `war3map.j` is out of sync with `war3map.wtg`

**Solution:**
1. When saving, choose option 1 to delete war3map.j
2. World Editor will regenerate it when you open the map

### Variables are missing

**Cause:** Variable copying failed, or variables use different IDs

**Solution:**
1. Enable debug mode (option 8)
2. Check which variables are being detected
3. Manually verify variable names match in source and target

### Build fails

**Cause:** .NET SDK not installed or wrong version

**Solution:**
1. Install .NET 8.0 SDK from official website
2. Run `dotnet --version` to verify
3. Check that War3Net DLLs exist in ../Libs/

---

## Advanced Usage

### Command Line Arguments

```bash
# Auto-detect maps in Source and Target folders
dotnet run

# Specify source and target
dotnet run -- "C:/Maps/source.w3x" "C:/Maps/target.w3x"

# Specify source, target, and output
dotnet run -- "source.w3x" "target.w3x" "merged.w3x"
```

### Working with Raw .wtg Files

1. Extract `war3map.wtg` from your `.w3x` using an MPQ editor
2. Place `war3map.wtg` files in Source and Target folders
3. Run the merger
4. Replace `war3map.wtg` in your map archive
5. **Delete `war3map.j`** from the archive
6. Open in World Editor to regenerate

### Debug Mode

Enable debug mode (option 8) to see:
- Category ID → position mappings
- Trigger ParentId updates
- Variable detection details
- Format version information
- Structure validation results

Example debug output:
```
[DEBUG] === FixCategoryIdsForOldFormat (target) ===
[DEBUG] Old format detected (SubVersion=null)
[DEBUG] Categories found: 5
[DEBUG] Category 'Init': ID=2, Position=0, Expected ID=0
[DEBUG] Re-inserted 'Init' at position 0 with ID=0, ParentId=0
[DEBUG] Updated trigger 'Map Init': ParentId 2 → 0
[DEBUG] === Fix Complete ===
```

---

## Success Criteria

The merger is successful when:

### File Operations
- ✅ Reads WC3 1.27 maps without errors
- ✅ Writes merged maps without corruption

### Category Management
- ✅ Category IDs always equal positions
- ✅ Category ParentIds always 0 (old format)

### Trigger Management
- ✅ Trigger ParentIds reference correct category positions
- ✅ Triggers appear in correct categories in World Editor
- ✅ Trigger code intact (events, conditions, actions)

### World Editor Compatibility
- ✅ Merged map opens without errors in World Editor 1.27
- ✅ Triggers appear in correct categories (not nested)
- ✅ Variables are accessible
- ✅ Map runs in-game without errors

---

## Technical Background

### Why This Tool Exists

The original WTGMerger was designed for WC3 Reforged's **enhanced format** (`SubVersion=v4`) which uses:
- `ParentId=-1` for root categories
- ID-based lookups (not position-based)
- Category ParentId saved to file

WC3 1.27 uses the **old format** (`SubVersion=null`) which has different rules:
- `ParentId=0` for root categories (NOT -1)
- Position-based lookups (ParentId is array index)
- Category ParentId NOT saved to file (always reads as 0)

Mixing these formats causes triggers to appear in wrong categories or the map to fail to open.

### The Core Truth

**In WC3 1.27 old format, trigger `ParentId` is a POSITION INDEX into the `TriggerItems` array.**

This is not a bug or design choice - it's how the file format works. Category IDs MUST equal their positions for the lookup to work correctly.

### War3Net Library Usage

This tool uses reflection to access internal War3Net methods:

```csharp
// Reading triggers
var constructor = typeof(MapTriggers).GetConstructor(
    BindingFlags.NonPublic | BindingFlags.Instance,
    null,
    new[] { typeof(BinaryReader), typeof(TriggerData) },
    null);

// Writing triggers
var writeMethod = typeof(MapTriggers).GetMethod(
    "WriteTo",
    BindingFlags.NonPublic | BindingFlags.Instance,
    null,
    new[] { typeof(BinaryWriter) },
    null);
```

---

## Contributing

If you find issues or want to add features:

1. Test with WC3 1.27 maps first
2. Verify the output map opens in World Editor 1.27
3. Check that SubVersion remains null
4. Validate category structure with debug mode

---

## License

This project uses War3Net libraries. See War3Net repository for license details.

---

## Credits

- **War3Net** - Pik for the excellent WC3 modding libraries
- **Warcraft 3 Community** - For documenting the old format quirks

---

## Version History

### v2.0.0 - Old Format Support (Current)
- Complete rewrite for WC3 1.27 old format (`SubVersion=null`)
- Position-based category ID system
- Automatic structure fixing with `FixCategoryIdsForOldFormat()`
- ParentId=0 for root categories (old format standard)
- Pre/post-save validation
- Debug mode with detailed logging

### v1.0.0 - Enhanced Format (Previous)
- Designed for WC3 Reforged enhanced format (`SubVersion=v4`)
- Used ParentId=-1 for root categories
- ID-based lookups

---

## FAQ

**Q: Can I use this with WC3 Reforged maps?**
A: No, this version is specifically for WC3 1.27 old format. Reforged uses enhanced format with different rules.

**Q: Why do categories need ParentId=0 instead of -1?**
A: Old format doesn't save category ParentId to file, so it always reads as 0. Using -1 would be lost on save.

**Q: What if my source map is Reforged format?**
A: The tool will detect this and skip position-fixing. However, merging between formats may cause issues.

**Q: Do I need to delete war3map.j?**
A: Yes! The JASS code file must match the trigger structure file. Deleting it forces World Editor to regenerate it correctly.

**Q: Can I merge triggers between different WC3 versions?**
A: Not recommended. Stick to WC3 1.27 → 1.27 for best results.

**Q: Why does the tool use reflection?**
A: War3Net's reading/writing methods are internal. Reflection allows us to access them without modifying the library.

---

## Support

For issues, questions, or contributions:
- Check debug output (option 8)
- Verify file format (option 6)
- Test with clean WC3 1.27 maps first
- Read this documentation thoroughly

---

**Made with ❤️ for the Warcraft 3 modding community**
