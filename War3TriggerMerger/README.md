# War3 Trigger Merger

**Clean, Production-Ready Tool for Warcraft 3 1.27 Trigger Merging**

Built from scratch specifically for **WC3 1.27 Old Format** (`SubVersion=null`) with position-based category IDs.

---

## 🎯 What This Does

Merges triggers between WC3 1.27 maps while **maintaining full compatibility** with World Editor 1.27:

- ✅ **Position-Based Category System** - Category IDs always equal their position
- ✅ **Old Format Preservation** - SubVersion stays null, ParentId=0 for categories
- ✅ **Automatic Structure Fixing** - Ensures correctness after every operation
- ✅ **Variable Management** - Automatically copies variables used by triggers
- ✅ **Full Validation** - Pre/post-save verification
- ✅ **Clean Architecture** - Built from scratch with proper design

---

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- WC3 1.27 maps to merge

### Installation

```bash
# 1. Place source map in ../Source/
# 2. Place target map in ../Target/
# 3. Run the tool
cd War3TriggerMerger
run.bat
```

### Usage

```bash
# Auto-detect maps from Source/Target folders
run.bat

# Or specify custom paths
dotnet run -- "path/to/source.w3x" "path/to/target.w3x" "path/to/output.w3x"
```

---

## 📋 How It Works

### The Core Problem: Position-Based Category IDs

WC3 1.27 old format uses **position-based** category references:

```
TriggerItems array structure:
[0] Category "Init"        (ID=0, ParentId=0)
[1] Category "Heroes"      (ID=1, ParentId=0)
[2] Category "Spells"      (ID=2, ParentId=0)
[3] Trigger "Map Init"     (ParentId=0 ← references position 0)
[4] Trigger "Hero Spawn"   (ParentId=1 ← references position 1)
```

**Critical Rule:** `trigger.ParentId` is a **POSITION INDEX** into the TriggerItems array!

If category IDs don't match positions, triggers appear in wrong categories.

### The Solution: FixCategoryStructure()

This function ensures category IDs always match positions:

1. Extract all categories from TriggerItems
2. Build mapping: `oldId → newId` (where `newId = position`)
3. Remove all categories
4. Re-insert at positions 0,1,2,3... with `ID = position`
5. Update trigger ParentIds using the mapping
6. Set category ParentIds to 0 (old format requirement)

**Called automatically:**
- After loading maps
- After merging categories
- After copying triggers
- Before saving

---

## 🎮 Menu Options

```
1. List SOURCE categories
   Shows categories with ID, position, ParentId, trigger count

2. List TARGET categories
   Shows target categories with validation warnings

3. List triggers in category
   Shows all triggers in a specific category

4. Copy ENTIRE category from source to target
   Merges complete category with all triggers

5. Copy SPECIFIC triggers
   Copy selected triggers (comma-separated)

6. Show structure debug info
   Detailed category structure analysis

7. Toggle debug mode
   Enable/disable detailed logging

8. Save and exit
   Validates and saves merged map

0. Exit without saving
   Discard all changes
```

---

## 🔧 Technical Details

### Old Format Requirements

| Requirement | Value | Why |
|-------------|-------|-----|
| Category ID | = Position | Used for trigger ParentId lookup |
| Category ParentId | = 0 | Not saved to file, always reads as 0 |
| Trigger ParentId | = Category Position | Direct array index |
| SubVersion | = null | Must never change |
| Structure | Categories first | Required for position-based lookup |

### File Format

**What Gets Saved (Old Format):**

| Item | ID Saved? | ParentId Saved? | Notes |
|------|-----------|-----------------|-------|
| Category | ✅ YES | ❌ NO | ParentId always reads as 0 |
| Trigger | ❌ NO | ✅ YES | ParentId is position index |
| Variable | ❌ NO | ❌ NO | Name is unique ID |

### Architecture

**Clean separation of concerns:**

```
Initialization
  ├─ Load maps from file/archive
  ├─ Validate old format
  └─ Fix category structures

Core Category Management (Position-Based)
  ├─ FixCategoryStructure() - THE critical function
  ├─ CreateCategory() - Proper old format settings
  ├─ FindCategory() - By name lookup
  └─ RemoveCategory() - With triggers

Trigger Operations
  ├─ CopyTriggers() - Copy specific triggers
  ├─ MergeCategory() - Merge entire category
  └─ CloneTrigger() - Deep copy with functions

Variable Management
  ├─ CopyRequiredVariables() - Auto-copy used vars
  └─ CollectVariableNames() - Scan trigger references

File I/O
  ├─ LoadMapTriggers() - From .wtg or archive
  └─ SaveMapTriggers() - With validation

Validation & Save
  └─ SaveWithValidation() - Pre/post checks

User Interface
  └─ InteractiveMenu() - Clean, simple menu
```

---

## ✅ Key Features

### 1. Automatic Structure Fixing

```csharp
// After ANY operation that modifies categories:
FixCategoryStructure(_target, "target");
```

This ensures the structure is **always correct**, no manual fixing needed.

### 2. Smart Variable Copying

Only copies variables that are **actually used** by the triggers being merged:

```csharp
var variablesNeeded = CollectVariableNames(triggers);
CopyRequiredVariables(variablesNeeded);
```

### 3. Deep Trigger Cloning

Properly copies:
- All trigger properties
- Events, conditions, actions
- Nested functions
- Function parameters
- Array indexers

### 4. Pre/Post-Save Validation

**Before save:**
- Verify SubVersion is still null
- Check category IDs match positions
- Verify category ParentIds are 0
- Validate trigger ParentIds

**After save:**
- Read file back
- Verify counts (categories, triggers, variables)
- Verify SubVersion is still null

### 5. JASS Synchronization

Prompts to delete `war3map.j` so World Editor regenerates it correctly.

---

## 📖 Usage Examples

### Example 1: Merge Entire Category

```
Menu: 4
Enter category name: Hero Abilities
```

Output:
```
+ Found 8 triggers in source category 'Hero Abilities'
+ Created category 'Hero Abilities' (ID=3, ParentId=0)

+ Copying triggers:
  + Hero Select
  + Hero Spawn
  + Hero Level Up
  ...

+ Merged category 'Hero Abilities' successfully!
```

### Example 2: Copy Specific Triggers

```
Menu: 5
Source category: Spells
Trigger names: Fireball, Ice Bolt, Lightning
Destination category: Combat
```

Output:
```
+ Analyzing 3 variable(s)...
  + Copied variable 'CasterUnit'
  + Copied variable 'TargetUnit'

+ Copying 3 trigger(s) to 'Combat':
  + Fireball
  + Ice Bolt
  + Lightning

+ Copied 3 trigger(s) successfully!
```

### Example 3: Debug Mode

```
Menu: 7  (Toggle debug mode ON)
Menu: 4  (Merge category)
```

Output:
```
[DEBUG] Fixing category structure for target
[DEBUG] Found 4 categories
[DEBUG] Category 'Init': OldID=2 → NewID=0
[DEBUG] Category 'Heroes': OldID=5 → NewID=1
[DEBUG] Re-inserted 'Init' at position 0 with ID=0, ParentId=0
[DEBUG] Trigger 'Map Init': ParentId 2 → 0
[DEBUG] Structure fix complete for target
```

---

## 🛡️ Safety Features

### SubVersion Protection

```csharp
if (_target.SubVersion != null)
{
    PrintError("ERROR: SubVersion was changed from null!");
    PrintError("Aborting save to prevent corruption.");
    return;
}
```

### Structure Validation

Before saving, verifies:
- All categories are at positions 0,1,2,3...
- Category IDs match positions
- Category ParentIds are 0
- Trigger ParentIds reference valid categories

### Automatic Fixes

If validation finds issues, automatically calls:
```csharp
FixCategoryStructure(_target, "target");
```

---

## 🔍 Troubleshooting

### Triggers in Wrong Categories

**Symptom:** After merging, triggers appear in different categories in World Editor

**Cause:** Category IDs don't match positions

**Solution:** Use Menu Option 6 to see structure, program auto-fixes on save

### "Trigger Data Invalid" Error

**Symptom:** World Editor shows error when opening merged map

**Cause:** `war3map.j` out of sync with `war3map.wtg`

**Solution:** When saving, choose YES to delete war3map.j (recommended)

### Variables Missing

**Symptom:** Triggers reference undefined variables

**Cause:** Variables weren't copied from source

**Solution:** Enable debug mode (option 7) to see which variables are copied

### Build Fails

**Symptom:** `dotnet build` fails

**Cause:** .NET SDK not installed or War3Net DLLs missing

**Solution:**
1. Install .NET 8.0 SDK
2. Verify War3Net DLLs exist in `../Libs/`
3. Run `dotnet --version` to verify

---

## 💡 Advanced Usage

### Command Line Arguments

```bash
# Specify all paths
dotnet run -- "C:/Maps/source.w3x" "C:/Maps/target.w3x" "C:/Maps/merged.w3x"

# Auto-output naming
dotnet run -- "source.w3x" "target.w3x"
# Output: target_merged.w3x
```

### Working with Raw .wtg Files

```bash
# 1. Extract war3map.wtg from .w3x using MPQ editor
# 2. Place in Source/Target folders
# 3. Run merger
dotnet run

# 4. Replace war3map.wtg in map archive
# 5. Delete war3map.j from archive
# 6. Open in World Editor
```

### Debug Mode Details

When enabled, shows:
- Category ID remapping
- Trigger ParentId updates
- Variable copying details
- Structure fixes in real-time

---

## 📊 Project Structure

```
War3TriggerMerger/
├── Program.cs                    # Main implementation (800+ lines)
├── War3TriggerMerger.csproj      # .NET 8.0 project config
├── run.bat                       # Windows batch script
└── README.md                     # This file

../Libs/                          # War3Net DLLs
├── War3Net.Build.Core.dll
├── War3Net.Build.dll
├── War3Net.Common.dll
└── War3Net.IO.Mpq.dll
```

---

## 🎓 Understanding the Code

### Core Concept: Position-Based System

```csharp
// OLD FORMAT RULE:
// trigger.ParentId is a POSITION INDEX, not an ID lookup

// Example:
TriggerItems[0] = Category "Init" (ID=0)
TriggerItems[1] = Category "Heroes" (ID=1)
TriggerItems[2] = Trigger "Map Init" (ParentId=0)

// When WC3 reads ParentId=0, it does:
// parent = TriggerItems[0]  ← Direct array access!

// Therefore: Category ID MUST equal position!
```

### Why FixCategoryStructure() is Critical

```csharp
// Without fix:
TriggerItems[0] = Category "Heroes" (ID=5)  ← ID doesn't match position!
TriggerItems[1] = Category "Spells" (ID=2)
TriggerItems[2] = Trigger "Fireball" (ParentId=2)

// WC3 reads ParentId=2 and does:
// parent = TriggerItems[2]  ← Gets the trigger itself! BUG!

// With fix:
TriggerItems[0] = Category "Heroes" (ID=0)  ← ID = position ✓
TriggerItems[1] = Category "Spells" (ID=1)
TriggerItems[2] = Trigger "Fireball" (ParentId=1)

// WC3 reads ParentId=1 and does:
// parent = TriggerItems[1]  ← Gets "Spells" category ✓
```

### The Golden Rules

```csharp
// RULE 1: Category ID = Position
for (int i = 0; i < categories.Count; i++)
{
    categories[i].Id = i;  // ID MUST equal position
}

// RULE 2: Category ParentId = 0 (old format)
category.ParentId = 0;  // NOT -1, MUST be 0

// RULE 3: Trigger ParentId = Category Position
trigger.ParentId = category.Id;  // Which equals position

// RULE 4: SubVersion = null (never change!)
// Don't touch this property AT ALL

// RULE 5: Categories before triggers
TriggerItems[0..N] = Categories
TriggerItems[N+1..] = Triggers
```

---

## 🏆 Success Criteria

The merge is successful when:

**File Operations:**
- ✅ Reads WC3 1.27 maps without errors
- ✅ Writes merged maps without corruption

**Category Management:**
- ✅ Category IDs always equal positions
- ✅ Category ParentIds always 0

**Trigger Management:**
- ✅ Trigger ParentIds reference correct category positions
- ✅ Triggers appear in correct categories
- ✅ All trigger code preserved

**World Editor 1.27:**
- ✅ Merged map opens without errors
- ✅ Triggers appear in correct categories
- ✅ Variables are accessible
- ✅ Map runs in-game correctly

---

## 📜 Version History

### v2.0.0 (Current) - Clean Rewrite
- Complete rewrite from scratch
- Position-based category system built-in
- Automatic structure fixing
- Clean architecture with proper separation
- Production-ready error handling
- Comprehensive validation

---

## 🤝 Contributing

This is a clean, from-scratch implementation specifically for WC3 1.27 old format.

When contributing:
1. Understand position-based category system
2. Never modify SubVersion
3. Always call FixCategoryStructure() after changes
4. Test with actual WC3 1.27 maps
5. Verify in World Editor 1.27

---

## 📄 License

Uses War3Net libraries. See War3Net repository for license details.

---

## 🙏 Credits

- **War3Net** by Pik - Excellent WC3 modding libraries
- **WC3 Community** - For documenting old format quirks

---

## ❓ FAQ

**Q: Why a new tool instead of modifying the old one?**

A: Clean slate allows proper design around position-based categories from the start. No legacy compromises.

**Q: What makes this different from the old WTGMerger?**

A:
- Built for old format from scratch (not retrofitted)
- Position-based system is core design (not added later)
- Cleaner code structure
- Better error handling
- More robust validation

**Q: Can I use with Reforged maps?**

A: No, specifically designed for WC3 1.27 old format. Reforged uses different system.

**Q: Will this work with my maps?**

A: Yes, if they're WC3 1.27 old format (SubVersion=null). Check with Menu Option 6.

**Q: Is it safe to use?**

A: Yes, includes comprehensive validation and never modifies your original maps.

---

**Built with ❤️ for the Warcraft 3 modding community**

**Clean Code. Proper Design. Zero Compromises.**
