# TriggerMerger - Warcraft 3 Trigger Category Manager

A robust CLI tool for managing and copying trigger categories (folders) with all their triggers between Warcraft 3 maps.

## Features

✅ **Copy trigger categories** between maps with all their triggers intact
✅ **List all categories** and triggers in a map
✅ **Version 1.27+ support** - Works with v1.27 and all other Warcraft 3 versions
✅ **Dry-run mode** - Preview changes before applying
✅ **Automatic backups** - Create backups before modifying files
✅ **Overwrite protection** - Choose whether to overwrite existing categories
✅ **Detailed logging** - See exactly what's happening
✅ **Error handling** - Comprehensive validation and error messages

## Requirements

- .NET 5.0 SDK or later
- Warcraft 3 map files (.w3x or .w3m)

## Building

```bash
cd /home/user/War3Net/tools/War3Net.Tools.TriggerMerger
dotnet build -c Release
```

The compiled executable will be in `bin/Release/net5.0/TriggerMerger.exe` (or `TriggerMerger` on Linux/Mac).

## Usage

### List Categories and Triggers

View all trigger categories and triggers in a map:

```bash
TriggerMerger list --map "path/to/map.w3x"
```

With detailed information:

```bash
TriggerMerger list --map "path/to/map.w3x" --detailed
```

**Example output:**
```
Map Triggers Information:
  Format Version: v7
  Sub Version: v4
  Game Version: 7000

Global Variables (5):
  - PlayerUnits: unitgroup
  - GameTime: integer
  - Difficulty: integer

Trigger Categories and Triggers (23 items):

[-] Initialization
  • Map Initialization [INIT]
  • Set Variables

[-] Spells
  • Fireball Cast
  • Ice Nova Effect
  • Healing Wave [DISABLED]

[-] Systems
  • Damage Detection
  • Gold System
```

### Copy a Single Category

Copy one trigger category from source to target:

```bash
TriggerMerger copy-category \
  --source "path/to/source.w3x" \
  --target "path/to/target.w3x" \
  --category "spells"
```

**Output location:** By default, creates `target_merged.w3x`. Specify with `--output`:

```bash
TriggerMerger copy-category \
  --source "source.w3x" \
  --target "target.w3x" \
  --category "spells" \
  --output "mymap_with_spells.w3x"
```

### Copy Multiple Categories

Copy several categories at once:

```bash
TriggerMerger copy-category \
  --source "source.w3x" \
  --target "target.w3x" \
  --categories "spells" "items" "abilities"
```

Or use the `--category` flag multiple times:

```bash
TriggerMerger copy-category \
  --source "source.w3x" \
  --target "target.w3x" \
  -c "spells" -c "items" -c "abilities"
```

### Overwrite Existing Categories

If a category already exists in the target, use `--overwrite` to replace it:

```bash
TriggerMerger copy-category \
  --source "source.w3x" \
  --target "target.w3x" \
  --category "spells" \
  --overwrite
```

### Dry Run (Preview Changes)

Preview what will be copied without modifying files:

```bash
TriggerMerger copy-category \
  --source "source.w3x" \
  --target "target.w3x" \
  --category "spells" \
  --dry-run
```

**Example output:**
```
TriggerMerger - Copy Category Command
=====================================

Source: /maps/source.w3x
Target: /maps/target.w3x
Categories to copy: spells
Overwrite existing: false
Dry run: true

✓ Source triggers loaded (Version: v7, Items: 45)
✓ Target triggers loaded (Version: v7, Items: 32)

Changes to be applied:
----------------------
  + Category: spells
    Triggers: 8
    Action: Add New

DRY RUN: No files were modified. Remove --dry-run to apply changes.
```

### Disable Automatic Backups

By default, a timestamped backup is created. Disable with `--backup false`:

```bash
TriggerMerger copy-category \
  --source "source.w3x" \
  --target "target.w3x" \
  --category "spells" \
  --backup false
```

## Command Reference

### `list` Command

```
TriggerMerger list --map <path> [--detailed]
```

**Options:**
- `--map, -m` (required) - Path to the Warcraft 3 map file
- `--detailed, -d` - Show detailed information including trigger contents

### `copy-category` Command

```
TriggerMerger copy-category --source <path> --target <path> --category <name> [options]
```

**Required Options:**
- `--source, -s` - Path to the source map file
- `--target, -t` - Path to the target map file
- `--category, -c` - Name of the category to copy (can be used multiple times)

**Optional Options:**
- `--output, -o` - Output file path (default: target_merged.w3x)
- `--categories` - Multiple categories (space-separated)
- `--overwrite` - Overwrite existing categories (default: false)
- `--dry-run` - Preview changes without modifying files (default: false)
- `--backup` - Create backup before modifying (default: true)

## Version Compatibility

**Supported Warcraft 3 Versions:**
- ✅ v1.27a (1.27.0.52240)
- ✅ v1.27b (1.27.1.7085)
- ✅ v1.28+
- ✅ v1.29+
- ✅ v1.30+
- ✅ v1.31+ (Reforged)
- ✅ v1.32+
- ✅ v1.33+
- ✅ v1.34+

**Trigger Format Versions:**
- Format v4 (Reign of Chaos)
- Format v7 (The Frozen Throne) - Most common

## Technical Details

### How It Works

1. **Read Source**: Opens the source map's MPQ archive and reads `war3map.wtg`
2. **Read Target**: Opens the target map's MPQ archive and reads `war3map.wtg`
3. **Find Category**: Locates the specified category in the source triggers
4. **Extract Triggers**: Gathers all triggers that belong to that category
5. **Merge**: Adds the category and its triggers to the target
6. **Write**: Saves the modified trigger data back to the map archive

### What Gets Copied

When you copy a category, the tool copies:

- ✅ The category definition (name, expanded state, comment status)
- ✅ All triggers within that category
- ✅ All trigger properties (enabled, description, init flag, etc.)
- ✅ All events, conditions, and actions
- ✅ All parameters and nested function calls
- ✅ Array indexers and variable references

### What Doesn't Get Copied

- ❌ Global variables (these remain unchanged in target)
- ❌ Custom script code from `war3map.j` (only GUI triggers from .wtg)
- ❌ Trigger strings from `war3map.wts` (may need manual copying)

## Troubleshooting

### "Could not read triggers from map"

- Ensure the map file is not corrupted
- Verify the file is a valid .w3x or .w3m file
- Check that the map contains a `war3map.wtg` file

### "Category not found"

- Use the `list` command to see available categories
- Category names are case-insensitive but must match exactly
- Check for extra spaces or special characters

### "Trigger file not found in map archive"

- The map may not have any GUI triggers
- All triggers might be in custom script (`war3map.j`)
- The map might be corrupted

## Examples

### Example 1: Migrate Spells to New Map

```bash
# First, see what's in the source map
TriggerMerger list --map "old_map.w3x" --detailed

# Copy the spells category
TriggerMerger copy-category \
  --source "old_map.w3x" \
  --target "new_map.w3x" \
  --category "spells" \
  --output "new_map_with_spells.w3x"

# Verify it worked
TriggerMerger list --map "new_map_with_spells.w3x"
```

### Example 2: Merge Multiple Systems

```bash
TriggerMerger copy-category \
  --source "systems_library.w3x" \
  --target "my_map.w3x" \
  --categories "Damage Detection" "Item System" "Gold System" \
  --output "my_map_upgraded.w3x"
```

### Example 3: Update Existing Spells

```bash
# Preview the update
TriggerMerger copy-category \
  --source "new_spells.w3x" \
  --target "my_map.w3x" \
  --category "spells" \
  --overwrite \
  --dry-run

# Apply the update
TriggerMerger copy-category \
  --source "new_spells.w3x" \
  --target "my_map.w3x" \
  --category "spells" \
  --overwrite
```

## Contributing

This tool is part of the War3Net project. For issues or contributions, please visit:
https://github.com/Drake53/War3Net

## License

MIT License - See LICENSE file in the War3Net root directory.

## Credits

Built with [War3Net](https://github.com/Drake53/War3Net) by Drake53 and Contributors.
