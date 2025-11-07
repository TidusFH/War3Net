# WTGMerger127 - Quick Start Guide

## ⚡ 5-Minute Setup

### Step 1: Check Prerequisites

✅ **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)

Run this in Command Prompt to verify:
```cmd
dotnet --version
```

If you see a version number (like `8.0.x`), you're good to go!

### Step 2: Prepare Your Maps

1. **Source Map** (map you're copying triggers FROM)
   - Put it in: `War3Net/Source/`
   - Example: `War3Net/Source/hero_abilities.w3x`

2. **Target Map** (map you're copying triggers INTO)
   - Put it in: `War3Net/Target/`
   - Example: `War3Net/Target/my_map.w3x`

**Folder structure should look like:**
```
War3Net/
├── WTGMerger127/
│   └── run.bat           ← You'll run this
├── Source/
│   └── hero_abilities.w3x  ← Source map
└── Target/
    └── my_map.w3x        ← Target map
```

### Step 3: Run the Merger

1. Navigate to `War3Net/WTGMerger127/`
2. Double-click `run.bat`
3. The tool will automatically:
   - Build the project
   - Detect your maps
   - Show you the interactive menu

### Step 4: Merge Triggers

**Example workflow:**

```
═══════════════════════════════════════════════════════════
Select option (0-9): 1
```
→ Lists all categories in SOURCE map

```
═══════════════════════════════════════════════════════════
Select option (0-9): 4
Enter category name to copy: Hero Abilities
```
→ Copies entire "Hero Abilities" category to target map

```
═══════════════════════════════════════════════════════════
Select option (0-9): 9
```
→ Saves and exits

### Step 5: Test in World Editor 1.27

1. Open the merged map: `War3Net/Target/my_map_merged.w3x`
2. Open Trigger Editor (F4)
3. Verify your copied triggers appear in the correct categories
4. Test the map in-game

---

## 🎮 Interactive Menu Options

| Option | What It Does |
|--------|--------------|
| **1** | List all categories from SOURCE map |
| **2** | List all categories from TARGET map |
| **3** | List triggers in a specific category |
| **4** | Copy ENTIRE category (with all its triggers) |
| **5** | Copy SPECIFIC trigger(s) (one or more) |
| **6** | Show debug info (format, structure) |
| **7** | Manually fix category IDs (if something went wrong) |
| **8** | Toggle debug mode ON/OFF |
| **9** | **Save and exit** |
| **0** | Exit without saving |

---

## 💡 Common Workflows

### Workflow 1: Copy One Entire Category

```
Option: 1          → See source categories
Option: 4          → Copy entire category
  Category: Spels Heroes
Option: 9          → Save and exit
```

### Workflow 2: Copy Multiple Specific Triggers

```
Option: 1          → See source categories
Option: 3          → List triggers in category
  Category: Hero Abilities
Option: 5          → Copy specific triggers
  Source category: Hero Abilities
  Trigger names: Thunder Clap, Critical Strike, Bash
  Destination: Hero Abilities
Option: 9          → Save and exit
```

### Workflow 3: Copy Multiple Categories

```
Option: 4          → Copy category
  Category: Spels Heroes
Option: 4          → Copy another category
  Category: Hero Abilities
Option: 4          → Copy yet another
  Category: Item System
Option: 9          → Save and exit
```

---

## 🔍 Verification Steps

After merging, **ALWAYS** verify in World Editor 1.27:

1. ✅ **Open merged map** in World Editor 1.27
2. ✅ **Open Trigger Editor** (F4)
3. ✅ **Check each copied trigger** appears in the correct category
4. ✅ **Verify trigger is NOT nested** incorrectly (e.g., not all in "Initialization")
5. ✅ **Check trigger code** (events, conditions, actions) is intact
6. ✅ **Verify variables** are accessible and correct type
7. ✅ **Test the map** in-game to ensure triggers work

---

## ⚠️ Important Notes

### About war3map.j (JASS Script File)

When saving to a `.w3x` or `.w3m` archive, the tool will ask:

```
Delete war3map.j from output map? (y/n):
```

**Recommendation: Type `y` (YES)**

**Why?**
- `war3map.j` contains the JASS code generated from `war3map.wtg`
- After merging triggers, `war3map.j` is out of sync
- World Editor will regenerate it automatically when you open the map
- If you keep the old `war3map.j`, you might get "trigger data invalid" errors

### Old Format vs Enhanced Format

This tool is designed for **WC3 1.27 Old Format** (`SubVersion=null`).

**How to tell which format your map uses:**
- When you load a map, the tool shows:
  ```
  [SOURCE] Format: v7, SubVersion: null, Game: v2
  ```
- If `SubVersion: null` → **Old Format** (WC3 1.27) ✓
- If `SubVersion: v4` → **Enhanced Format** (newer WC3) - tool will still work but uses different rules

**What this means:**
- Old format uses **position-based category IDs** (the whole point of this tool!)
- Enhanced format uses **ID-based lookups** (more flexible)

---

## 🐛 Troubleshooting

### Build Failed

**Error:** `dotnet: command not found` or `Build failed`

**Solution:**
1. Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Restart Command Prompt
3. Run `run.bat` again

### No Map Files Found

**Error:** `No map files found in ../Source/`

**Solution:**
1. Create folders: `War3Net/Source/` and `War3Net/Target/`
2. Put your maps in the correct folders
3. Supported formats: `.w3x`, `.w3m`, or raw `war3map.wtg`

### Triggers Appear in Wrong Category in World Editor

**Symptom:** All triggers nest under "Initialization" or wrong category

**Cause:** Category IDs don't match positions (this tool fixes this!)

**Solution:**
1. Re-run the merger
2. Use Option 7 to manually fix category IDs
3. Save again
4. If still broken, check if your target map is old format (`SubVersion=null`)

### Variables Missing or Wrong Type

**Symptom:** Trigger references undefined variable

**Cause:** Variable wasn't copied or has wrong type

**Solution:**
- The tool automatically copies variables used by triggers
- Check debug info (Option 6) to see if variables were copied
- Variables are copied based on trigger function analysis

---

## 📊 What Gets Saved to File (Old Format)

| Item | ID Saved? | ParentId Saved? |
|------|-----------|-----------------|
| Category | ✅ YES | ❌ NO (always reads as 0) |
| Trigger | ❌ NO (always 0) | ✅ YES |
| Variable | ❌ NO (always 0) | N/A |

**Key takeaway:**
- Category IDs MUST be correct before write
- Trigger IDs don't matter (not saved)
- Variable IDs don't matter (not saved)

---

## 🎯 Success Criteria

You've successfully merged when:

✅ Merged map opens in World Editor 1.27 without errors
✅ Triggers appear in the correct categories (not all in "Initialization")
✅ Trigger code (events/conditions/actions) is intact
✅ Variables are accessible and have correct types
✅ Map runs in-game without errors
✅ Merged triggers function correctly in-game

---

## 🆘 Still Having Issues?

1. Enable debug mode (Option 8)
2. Check comprehensive debug info (Option 6)
3. Verify your maps are WC3 1.27 old format (`SubVersion=null`)
4. Check README.md for detailed technical explanation

---

## 📝 Example Session

```
═══════════════════════════════════════════════════════════
WTG MERGER FOR WARCRAFT 3 1.27 (OLD FORMAT)
Position-Based Category IDs | SubVersion=null
═══════════════════════════════════════════════════════════

Reading source...
  Detected: hero_abilities.w3x
  Opening MPQ archive...
  Extracting war3map.wtg...
✓ Loaded: 156 items, 42 variables
  [SOURCE] Format: v7, SubVersion: null, Game: v2

Reading target...
  Detected: my_map.w3x
  Opening MPQ archive...
  Extracting war3map.wtg...
✓ Loaded: 89 items, 28 variables
  [TARGET] Format: v7, SubVersion: null, Game: v2

═══════════════════════════════════════════════════════════
                    MERGE OPTIONS
═══════════════════════════════════════════════════════════
1. List all categories from SOURCE
2. List all categories from TARGET
3. List triggers in a specific category
4. Copy ENTIRE category
5. Copy SPECIFIC trigger(s)
6. Show format & structure debug info
7. Manually fix target category IDs (if needed)
8. Toggle debug mode (currently: ON)
9. Save and exit
0. Exit without saving

Select option (0-9): 1

=== Source Categories ===

  Total: 12 categories

  Pos | ID  | ParentId | Name
  ----|-----|----------|-----------------------------
    0 |   0 |        0 | Initialization (2 triggers)
    1 |   1 |        0 | Hero Abilities (8 triggers)
    2 |   2 |        0 | Item System (5 triggers)
  ...

Select option (0-9): 4

Enter category name to copy: Hero Abilities

Merging category 'Hero Abilities'...
  Found 8 triggers in source category
  ✓ Added category (ID=12, ParentId=0, Position=12)

  Checking 3 variable(s)...
    + Variable: HeroLevel
    + Variable: AbilityCooldown
    + Variable: DamageMultiplier
  ✓ Copied 3 variable(s)

    + Thunder Clap
    + Critical Strike
    + Bash
    + Divine Shield
    + Death Coil
    + Animate Dead
    + Wind Walk
    + Mirror Image
✓ Category merged!

Select option (0-9): 9

═══════════════════════════════════════════════════════════
                    SAVING MERGED MAP
═══════════════════════════════════════════════════════════

Format: v7
SubVersion: null (OLD FORMAT)
Game Version: 2
Variables: 31
Categories: 13
Triggers: 47

✓ SubVersion=null preserved (WC3 1.27 old format)

✓ Pre-save validation passed!

Writing to: ../Target/my_map_merged.w3x

WARNING: war3map.j must be synced!
Delete war3map.j from output map? (y/n): y

  Opening original archive...
  Creating archive builder...
  Replacing war3map.wtg...
  Removing war3map.j...
  Saving to ../Target/my_map_merged.w3x...
  ✓ Archive saved!

=== POST-SAVE VERIFICATION ===
SubVersion in file: null
Variables: 31
Categories: 13
Triggers: 47

✓✓✓ SAVE SUCCESSFUL! ✓✓✓
✓ Open in World Editor 1.27 to verify triggers appear in correct categories

═══════════════════════════════════════════════════════════
```

**Done!** Now open `my_map_merged.w3x` in World Editor 1.27 and verify the "Hero Abilities" category with all 8 triggers is there!
