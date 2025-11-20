# Quick Start Guide

## 5-Minute Setup

### 1. Build the Project

```bash
cd /home/user/War3Net/ObjectMerger
dotnet build -c Release
```

Output: `bin/Release/net8.0/ObjectMerger.exe`

### 2. Prepare Your Maps

You need:
- **Source map**: Contains custom objects you want to copy
- **Target map**: Will receive the custom objects
- Both must be .w3x files

### 3. Run

```bash
./bin/Release/net8.0/ObjectMerger.exe source.w3x target.w3x output.w3x
```

Or run without arguments for interactive mode.

## Your First Merge

Let's say you have:
- `Campaign.w3x` with custom units h001, h002, h003
- `MyMap.w3x` where you want to add them

### Step-by-Step

```bash
$ ObjectMerger.exe Campaign.w3x MyMap.w3x MyMap_merged.w3x

╔══════════════════════════════════════════════════════════╗
║          WARCRAFT 3 OBJECT MERGER v1.0                   ║
║     Copy custom units, items, abilities between maps     ║
╚══════════════════════════════════════════════════════════╝

Loading Source Map...
  Units: 15
  Items: 8
  Abilities: 12
  ...

Loading Target Map...
  Units: 5
  Items: 3
  ...

✓ Maps loaded successfully!

╔══════════════════════════════════════════════════════════╗
║                    MAIN MENU                             ║
╚══════════════════════════════════════════════════════════╝
1. List objects from SOURCE
2. List objects from TARGET
3. Copy specific objects
4. Copy all objects of a type
5. Show statistics
6. Save and exit
0. Exit without saving

Choice: 1

═══════════════════════════════════════════════════════════
OBJECTS IN SOURCE
═══════════════════════════════════════════════════════════

Units (15):
  [h001] Custom Footman (base: hfoo)
  [h002] Hero Knight (base: Hpal)
  [h003] Siege Tank (base: hmtm)
  ...

Items (8):
  [I001] Magic Sword (base: ritd)
  [I002] Health Potion (base: phed)
  ...

Choice: 3

═══════════════════════════════════════════════════════════
COPY SPECIFIC OBJECTS
═══════════════════════════════════════════════════════════

Enter object codes to copy (comma-separated):
Example: h001,h002,I001,A001

Object codes: h001,h002,h003

Found 3 object(s) to copy:
  h001 - Custom Footman (Unit)
  h002 - Hero Knight (Unit)
  h003 - Siege Tank (Unit)

Resolve conflicts? [Y/N] (or press Enter to auto-skip):
→ Auto-skipping conflicts

╔══════════════════════════════════════════════════════════╗
║                   COPYING OBJECTS                        ║
╚══════════════════════════════════════════════════════════╝

✓ Copied h001 - Custom Footman
✓ Copied h002 - Hero Knight
✓ Copied h003 - Siege Tank

═══════════════════════════════════════════════════════════
✓ Copied: 3 object(s)

Note: Changes are in memory. Use option 6 to save.

Choice: 6

═══════════════════════════════════════════════════════════
SAVING MAP
═══════════════════════════════════════════════════════════

Output path: MyMap_merged.w3x
Saving map...

✓ Map saved successfully to: MyMap_merged.w3x

You can now open the merged map in World Editor!
```

## What Just Happened?

1. ✅ ObjectMerger loaded both maps
2. ✅ Read all custom objects from source
3. ✅ You selected 3 units to copy
4. ✅ No conflicts detected (IDs didn't exist in target)
5. ✅ Objects copied to target map
6. ✅ Saved as MyMap_merged.w3x

## Common Scenarios

### Scenario 1: Copy All Items

```
Choice: 4

Select object type:
1. Unit
2. Item
3. Ability
...

Choice: 2

Found 8 Item(s) in source map:
  I001 - Magic Sword
  I002 - Health Potion
  ...

Copy all 8 Item(s)? [Y/N]: Y

✓ Copied: 8 object(s)
```

### Scenario 2: Handle Conflicts

```
Object codes: h001

⚠ Conflict: h001 (Unit)
  Source: Custom Footman
  Target: Different Custom Unit (already exists)

How to resolve?
  [S] Skip - Don't copy this object
  [O] Overwrite - Replace target's version
  [R] Rename - Find new ID (Phase 3 feature)
  [A] Skip All

Choice [S/O/R/A]: O

⚠ Overwriting h001 - Custom Footman

✓ Copied: 1 object(s)
```

### Scenario 3: Check What's in Maps

```
Choice: 5

╔══════════════════════════════════════════════════════════╗
║                    STATISTICS                            ║
╚══════════════════════════════════════════════════════════╝

SOURCE MAP:
  Total objects: 35
    Units: 15
    Items: 8
    Abilities: 12
    ...

TARGET MAP:
  Total objects: 12
    Units: 5
    Items: 3
    Abilities: 4
    ...
```

## Tips for Success

### ✅ Do's

- ✅ Backup your maps first
- ✅ List objects before copying (option 1)
- ✅ Start with a few objects, not all at once
- ✅ Test in World Editor after merging
- ✅ Use 'Skip' for conflicts unless sure

### ❌ Don'ts

- ❌ Don't overwrite without checking
- ❌ Don't copy objects with unmet dependencies (Phase 1)
- ❌ Don't forget to save (option 6)
- ❌ Don't use on important maps without backup

## Troubleshooting

**"Source file not found"**
```bash
# Use full paths
ObjectMerger.exe "C:/Maps/Source.w3x" "C:/Maps/Target.w3x" "C:/Maps/Output.w3x"
```

**"No objects found with those codes"**
```bash
# List objects first to see available codes
Choice: 1  # List source objects
# Then copy exact codes shown
```

**"Error loading maps"**
```bash
# Ensure maps are valid .w3x files
# Try opening in World Editor first
# Check if maps are corrupted
```

## Next Steps

1. **Try it with test maps** - Create simple test maps to practice
2. **Review the code** - Check `Services/` folder to understand how it works
3. **Plan Phase 2** - Think about which dependencies you need detected
4. **Extend it** - Add features specific to your workflow

## Phase Roadmap

**Phase 1 (Complete):** Basic copying with conflict detection
**Phase 2 (Next):** Automatic dependency detection
**Phase 3 (Future):** Smart ID remapping and reference updating

You're ready to go! 🚀
