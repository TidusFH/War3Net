# Key Differences: WTGMerger vs WTGMerger127

## 🎯 Purpose

This document explains what makes **WTGMerger127** different from the original **WTGMerger** and why these changes are critical for WC3 1.27 compatibility.

---

## ❌ Critical Bugs Fixed

### 1. SubVersion Changed from null to v4

**Original WTGMerger (BROKEN):**
```csharp
// Line 277 in original Program.cs
if (targetTriggers.SubVersion == null)
{
    Console.WriteLine("⚠ WARNING: Target map has SubVersion=null");
    Console.WriteLine("   Setting SubVersion=v4 to enable ParentId support...");
    targetTriggers.SubVersion = MapTriggersSubVersion.v4;  // ❌ BREAKS 1.27!
}
```

**WTGMerger127 (FIXED):**
```csharp
// NEVER change SubVersion for old format
if (triggers.SubVersion == null)
{
    Console.WriteLine("✓ SubVersion=null preserved (WC3 1.27 old format)");
    // ✅ DO NOT CHANGE IT!
}
```

**Why this matters:**
- Changing `SubVersion` from `null` to `v4` converts the file from old format to enhanced format
- World Editor 1.27 cannot properly read enhanced format files
- This causes triggers to appear in wrong categories or fail to load

---

### 2. Category IDs Don't Match Positions

**Original WTGMerger (BROKEN):**
```csharp
// Lines 708, 804 - uses GetNextId()
int newCategoryId = GetNextId(target);  // ❌ WRONG for old format!

// GetNextId returns MAX(existing IDs) + 1
// This causes category ID != position
```

**Example of the problem:**
```
Existing categories: ID=0, ID=1, ID=5 (IDs don't match positions)
GetNextId() returns: 6
New category created at position 3 with ID=6
Result: Position=3, ID=6  ❌ BROKEN!

When trigger has ParentId=6:
  WC3 looks at position 6 → doesn't exist!
  WC3 defaults to position 0 → trigger in wrong category!
```

**WTGMerger127 (FIXED):**
```csharp
// CRITICAL: For old format, ID MUST equal position
var existingCategoryCount = target.TriggerItems
    .OfType<TriggerCategoryDefinition>()
    .Count();

int newCategoryId = existingCategoryCount;  // ✅ ID = Position!
```

**Example of the fix:**
```
Existing categories at positions 0, 1, 2
New category created at position 3 with ID=3
Result: Position=3, ID=3  ✅ CORRECT!

When trigger has ParentId=3:
  WC3 looks at position 3 → finds category!
  Trigger appears in correct category!
```

---

### 3. Category ParentId Set to -1 Instead of 0

**Original WTGMerger (WRONG for 1.27):**
```csharp
// Line 709, 805
ParentId = -1  // ❌ Uses -1 for root-level
```

**WTGMerger127 (FIXED):**
```csharp
// CRITICAL: Old format uses 0, not -1
int newParentId = (target.SubVersion == null) ? 0 : -1;
destCategory.ParentId = newParentId;  // ✅ 0 for old format
```

**Why this matters:**
- Old format (WC3 1.27) doesn't save category `ParentId` to file
- When read back, all category `ParentId` values become 0
- Using -1 in memory is fine, but we should use 0 for consistency
- The file format forces it to 0 anyway

---

### 4. Missing FixCategoryIdsForOldFormat() Function

**Original WTGMerger:**
- ❌ No function to fix category IDs after loading
- ❌ Assumes loaded IDs are correct
- ❌ Doesn't handle malformed old format files

**WTGMerger127:**
- ✅ `FixCategoryIdsForOldFormat()` called immediately after loading
- ✅ Detects and fixes category ID mismatches
- ✅ Updates trigger ParentIds to match new category IDs
- ✅ Sets all category ParentIds to 0

**What this function does:**
```csharp
static void FixCategoryIdsForOldFormat(MapTriggers triggers)
{
    if (triggers.SubVersion != null)
        return;  // Enhanced format doesn't need this

    var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
    var idMapping = new Dictionary<int, int>();

    // Fix category IDs to match positions
    for (int position = 0; position < categories.Count; position++)
    {
        int oldId = categories[position].Id;
        int newId = position;  // CRITICAL: ID = Position
        idMapping[oldId] = newId;
        categories[position].Id = newId;
    }

    // Update trigger ParentIds
    var allTriggers = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
    foreach (var trigger in allTriggers)
    {
        if (trigger.ParentId >= 0 && idMapping.ContainsKey(trigger.ParentId))
        {
            trigger.ParentId = idMapping[trigger.ParentId];
        }
    }

    // Set all category ParentIds to 0
    foreach (var category in categories)
    {
        category.ParentId = 0;
    }
}
```

---

### 5. Missing AutoFixCategoriesForFormat() Function

**Original WTGMerger:**
- ❌ No final validation before save
- ❌ Can save files with invalid category structure

**WTGMerger127:**
- ✅ `AutoFixCategoriesForFormat()` called after every merge operation
- ✅ Validates category ParentIds are 0 (old format)
- ✅ Validates trigger ParentIds reference valid positions
- ✅ Fixes orphaned triggers

**What this function does:**
```csharp
static void AutoFixCategoriesForFormat(MapTriggers triggers)
{
    if (triggers.SubVersion != null)
        return;  // Enhanced format doesn't need this

    var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();

    // Ensure all category ParentIds are 0
    foreach (var category in categories)
    {
        if (category.ParentId != 0)
            category.ParentId = 0;
    }

    // Validate trigger ParentIds
    var allTriggers = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
    foreach (var trigger in allTriggers)
    {
        if (trigger.ParentId >= categories.Count)
        {
            trigger.ParentId = 0;  // Fix invalid ParentId
        }
    }
}
```

---

### 6. Pre-Save Validation Missing

**Original WTGMerger:**
- ❌ Minimal validation before save
- ❌ Can save corrupted files

**WTGMerger127:**
- ✅ Comprehensive pre-save validation
- ✅ Checks category ID = position for all categories
- ✅ Checks category ParentIds are 0 (old format)
- ✅ Verifies SubVersion hasn't changed
- ✅ Runs final fix if validation fails

**Validation code:**
```csharp
// Final category validation
var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
bool allCorrect = true;

for (int i = 0; i < categories.Count; i++)
{
    if (categories[i].Id != i)
    {
        Console.WriteLine($"❌ ERROR: Category at position {i} has ID={categories[i].Id}");
        allCorrect = false;
    }

    if (triggers.SubVersion == null && categories[i].ParentId != 0)
    {
        Console.WriteLine($"❌ ERROR: Category has ParentId={categories[i].ParentId}");
        allCorrect = false;
    }
}

if (!allCorrect)
{
    Console.WriteLine("⚠ Running final fix...");
    FixCategoryIdsForOldFormat(triggers);
    AutoFixCategoriesForFormat(triggers);
}
```

---

### 7. Post-Save Verification

**Original WTGMerger:**
- ✅ Has post-save verification (good!)
- ⚠️ Checks for variable loss
- ⚠️ Checks for ParentId changes
- ❌ Doesn't check if SubVersion was preserved

**WTGMerger127:**
- ✅ Has comprehensive post-save verification
- ✅ Checks variables count
- ✅ Checks categories count
- ✅ Checks triggers count
- ✅ **CRITICAL: Checks if SubVersion=null was preserved**

**SubVersion verification:**
```csharp
MapTriggers verified = ReadMapTriggersAuto(outputPath);

if (triggers.SubVersion == null && verified.SubVersion != null)
{
    Console.WriteLine("❌ ERROR: SubVersion changed from null!");
    success = false;
}
```

---

## 📊 Comparison Table

| Feature | Original WTGMerger | WTGMerger127 | Impact |
|---------|-------------------|--------------|--------|
| **SubVersion preservation** | ❌ Changes null→v4 | ✅ Preserves null | **CRITICAL** |
| **Category ID = Position** | ❌ Uses GetNextId() | ✅ Uses position | **CRITICAL** |
| **Category ParentId** | ⚠️ Uses -1 | ✅ Uses 0 for old format | **IMPORTANT** |
| **FixCategoryIdsForOldFormat** | ❌ Missing | ✅ Implemented | **CRITICAL** |
| **AutoFixCategoriesForFormat** | ❌ Missing | ✅ Implemented | **IMPORTANT** |
| **Pre-save validation** | ⚠️ Minimal | ✅ Comprehensive | **IMPORTANT** |
| **Post-save SubVersion check** | ❌ Missing | ✅ Implemented | **CRITICAL** |
| **Debug mode** | ✅ Yes | ✅ Yes (enhanced) | Nice |
| **Variable copying** | ✅ Yes | ✅ Yes (same) | Good |
| **MPQ support** | ✅ Yes | ✅ Yes (same) | Good |

---

## 🎯 Summary: Why WTGMerger127 Works for 1.27

1. **Never changes SubVersion** - Preserves old format
2. **Position-based category IDs** - Required by WC3 1.27
3. **Category ParentId = 0** - Old format requirement
4. **FixCategoryIdsForOldFormat()** - Fixes loaded maps
5. **AutoFixCategoriesForFormat()** - Final validation
6. **Comprehensive validation** - Prevents corruption

---

## 🔑 The Core Fix

**Original WTGMerger approach:**
```
Load map → Merge triggers → Change SubVersion to v4 → Save
Result: File format changed, incompatible with WC3 1.27 ❌
```

**WTGMerger127 approach:**
```
Load map → Fix category IDs → Merge triggers → Validate → Save
Result: Old format preserved, fully compatible with WC3 1.27 ✅
```

---

## 📝 When to Use Each Tool

### Use Original WTGMerger When:
- You're working with enhanced format maps (SubVersion=v4)
- You're using WC3 Reforged
- You don't need 1.27 compatibility

### Use WTGMerger127 When:
- You're working with WC3 1.27 maps (SubVersion=null)
- You need old format compatibility
- Triggers appear in wrong categories with other tools
- You need position-based category ID support

---

## 🧪 Test Results

### Original WTGMerger on 1.27 Maps:
❌ Changes SubVersion from null to v4
❌ Triggers appear in wrong categories
❌ World Editor 1.27 shows nesting issues
❌ May fail to load triggers correctly

### WTGMerger127 on 1.27 Maps:
✅ Preserves SubVersion=null
✅ Triggers appear in correct categories
✅ World Editor 1.27 loads correctly
✅ Position-based category lookup works

---

## 📚 Technical Deep Dive

See [README.md](README.md) for:
- Detailed explanation of old format structure
- How WC3 1.27 loads triggers
- Why category ID must equal position
- File format specifications

See [QUICKSTART.md](QUICKSTART.md) for:
- Step-by-step usage guide
- Common workflows
- Troubleshooting
- Example session
