# WTG Merger for Warcraft 3 1.27 (Old Format)

## Purpose

This tool **correctly** merges triggers from one WC3 map into another map while maintaining **full compatibility with World Editor 1.27**.

The key difference from other merger tools: **This implementation understands and enforces the position-based category ID system used by WC3 1.27's old format.**

---

## 🎯 What Makes This Different?

### The Critical 1.27 Old Format Rules

WC3 1.27 uses an **old format** where:

1. **Category ID MUST Equal Position**
   - Category at position 0 → ID must be 0
   - Category at position 32 → ID must be 32
   - WC3 1.27 uses trigger `ParentId` as a **POSITION INDEX**, not an ID lookup!

2. **Category ParentId MUST Be 0**
   - All categories in old format have `ParentId=0`
   - Old format does NOT support nested categories
   - ParentId is NOT saved to file, always reads as 0

3. **Trigger ParentId = Category Position**
   - Trigger in category at position 0 → `ParentId=0`
   - Trigger in category at position 32 → `ParentId=32`

4. **SubVersion MUST Remain null**
   - NEVER change `SubVersion` from `null` to anything else
   - NEVER upgrade old format to enhanced format
   - Changing this breaks World Editor 1.27 compatibility

5. **Trigger/Variable IDs Are NOT Saved**
   - Trigger IDs are not saved in old format (all become 0 in memory - this is NORMAL)
   - Variable IDs are not saved in old format (all become 0 - this is NORMAL)
   - There's NO collision risk!

---

## 🔧 Key Functions Implemented

### `FixCategoryIdsForOldFormat()`

**THE CORE FIX** - Called immediately after loading any old format map.

**What it does:**
- Detects old format (`SubVersion == null`)
- Sets every category ID to match its position (0, 1, 2, 3, ...)
- Sets all category `ParentId` values to 0 (old format requirement)
- Updates trigger `ParentId` values to reference correct positions

**Why it's critical:**
When you load an old format file, categories might have IDs that don't match their positions. This function fixes them BEFORE any merging operations.

### `CopySpecificTriggers()` - Position-Based IDs

**What it does:**
```csharp
// For OLD FORMAT (SubVersion == null):
var existingCategoryCount = target.TriggerItems
    .OfType<TriggerCategoryDefinition>()
    .Count();

int newCategoryId = existingCategoryCount;  // ID = Position!
int newParentId = 0;  // Always 0 for old format

// Create category with position-based ID
destCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
{
    Id = newCategoryId,      // CRITICAL: Matches position
    ParentId = newParentId,  // CRITICAL: 0 for old format
    Name = destCategoryName
};
```

**Why it's correct:**
- New category goes at position = `existingCategoryCount`
- Its ID MUST equal that position
- Triggers reference this category using `ParentId = newCategoryId`

### `AutoFixCategoriesForFormat()` - Final Validation

**Called before save** to ensure:
- All category `ParentId` values are 0 (old format)
- All trigger `ParentId` values reference valid category positions
- No orphaned triggers

### `SaveMergedMap()` - Format Preservation

**CRITICAL CHECKS:**
```csharp
// NEVER change SubVersion for old format
if (triggers.SubVersion == null)
{
    Console.WriteLine("✓ SubVersion=null preserved (WC3 1.27 old format)");
}

// Validate category IDs match positions
for (int i = 0; i < categories.Count; i++)
{
    if (categories[i].Id != i)
    {
        ERROR: Category at position {i} has wrong ID!
    }
}
```

---

## 📊 Old Format File Structure Reference

| Item Type | ID Saved? | ParentId Saved? | Notes |
|-----------|-----------|-----------------|-------|
| Category | ✅ YES | ❌ NO | ParentId always reads as 0 |
| Trigger | ❌ NO | ✅ YES | ParentId references category POSITION |
| Variable | ❌ NO | N/A | All variables have ID=0 (normal) |

**What this means:**

- **Categories**: ID is saved (must be correct before write), ParentId is NOT saved (always 0 after read)
- **Triggers**: ID is NOT saved (don't worry about it), ParentId IS saved (POSITION INDEX)
- **Variables**: ID is NOT saved (all 0 is normal), names are unique identifiers

---

## 🧪 Testing & Verification

### In-Memory Verification (Before Write)
- ✓ All categories have `ParentId=0`
- ✓ Category ID equals position for each category
- ✓ Trigger ParentId references valid category position
- ✓ `SubVersion=null` (old format preserved)

### File Verification (After Write/Read)
- ✓ File can be read back without errors
- ✓ `SubVersion=null` still (format not changed)
- ✓ Variables count matches
- ✓ Categories count matches
- ✓ Triggers count matches

### World Editor 1.27 Verification (FINAL TEST)
✅ **Open merged map in World Editor 1.27**
✅ **Open Trigger Editor**
✅ **Check each copied trigger appears in the correct category**
✅ **Trigger is NOT nested incorrectly**
✅ **Trigger code is intact (events, conditions, actions)**
✅ **Variables are accessible**

---

## 🚀 Usage

### Quick Start

1. Put your **source map** in `../Source/` folder (the map you're copying triggers FROM)
2. Put your **target map** in `../Target/` folder (the map you're copying triggers INTO)
3. Run `run.bat`
4. Follow the interactive menu

### Folder Structure

```
War3Net/
├── WTGMerger127/
│   ├── run.bat           ← Run this!
│   ├── Program.cs
│   └── WTGMerger127.csproj
├── Source/
│   └── your_source_map.w3x
└── Target/
    └── your_target_map.w3x
```

### Supported File Types

- **Map archives**: `.w3x`, `.w3m` (automatically extracts/updates `war3map.wtg`)
- **Raw trigger files**: `war3map.wtg` (direct file operations)

---

## ⚠️ Critical "DO NOT" List

### ❌ NEVER Shift Category IDs

```csharp
// WRONG (causes all triggers to nest incorrectly):
category at position 0 → assign ID=1
category at position 1 → assign ID=2

// CORRECT:
category at position 0 → assign ID=0
category at position 1 → assign ID=1
```

### ❌ NEVER Try to Avoid Trigger ID Collisions in Old Format

- Trigger IDs are NOT saved in old format
- All triggers have ID=0 in memory (this is NORMAL)
- There's NO collision risk!

### ❌ NEVER Set Category ParentId to Anything Other Than 0

- Old format doesn't support nested categories
- Category ParentId is NOT saved, always reads as 0

### ❌ NEVER Change SubVersion

- Keep `SubVersion=null` for old format
- Changing this breaks World Editor 1.27 compatibility

---

## 🎯 The Golden Rules

For WC3 1.27 Old Format:

1. **Category ID = Category Position** (always)
2. **Category ParentId = 0** (always)
3. **Trigger ParentId = Category Position** (where it belongs)
4. **SubVersion = null** (never change)
5. **Trigger IDs = 0** (normal, they're not saved)
6. **Variable IDs = 0** (normal, they're not saved)

---

## 🔑 The Core Truth

**In WC3 1.27 old format, trigger ParentId is a POSITION INDEX.**

**Category IDs MUST equal their positions for the lookup to work.**

**This is not optional - this is how the format works.**

---

## 📝 Example: How WC3 1.27 Loads a Trigger

1. Read trigger from file
2. Read trigger's `ParentId` (e.g., `ParentId=32`)
3. Look up category at **POSITION 32** in the category array
4. If position 32 exists → place trigger in that category ✓
5. If position 32 doesn't exist → default to position 0 ✗

**Example category array:**
```
TriggerItems array (in order):
  [0] Category "Initialization" - ID=0
  [1] Category "Load Heroes" - ID=1
  [2] Category "DEBUG" - ID=2
  ...
  [32] Category "Spels Heroes" - ID=32

Trigger has ParentId=32:
  → WC3 looks at TriggerItems[32] (position 32)
  → Finds "Spels Heroes" → places trigger there ✓
```

**What happens when IDs don't match positions:**
```
BROKEN EXAMPLE:
  TriggerItems array:
    [0] Category "Initialization" - ID=1  ← ID doesn't match position!
    ...
    [32] Category "Spels Heroes" - ID=33

  Trigger has ParentId=33:
    → WC3 looks at position 33 → doesn't exist!
    → WC3 defaults to position 0 → trigger appears in "Initialization" ✗
```

---

## 🛠️ Requirements

- .NET 8.0 SDK
- War3Net libraries (already referenced in `../Libs/`)

---

## 📜 License

Same as War3Net project

---

## 👤 Author

Created for WC3 1.27 map merging with proper old format support.

**Target**: World Editor 1.27 is the authority.
**Optional**: BetterTriggers compatibility is nice-to-have but not required.
