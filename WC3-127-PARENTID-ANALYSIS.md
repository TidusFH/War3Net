# WC3 1.27 Format ParentId Behavior - Complete Analysis

## 🎯 Root Cause Found

After analyzing War3Net's source code and WTGMerger's implementation, I found the **critical difference** in how ParentIds are handled:

## War3Net Source Code Analysis

### TriggerCategoryDefinition (Categories)

**Reading:**
```csharp
// Line 22-35 in TriggerCategoryDefinition.cs
internal void ReadFrom(BinaryReader reader, ...)
{
    Id = reader.ReadInt32();
    Name = reader.ReadChars();
    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        IsComment = reader.ReadBool();
    }

    if (subVersion is not null)  // ← CRITICAL!
    {
        IsExpanded = reader.ReadBool();
        ParentId = reader.ReadInt32();  // ← Only read if SubVersion != null
    }
    // If SubVersion is null, ParentId defaults to 0!
}
```

**Writing:**
```csharp
// Line 38-52 in TriggerCategoryDefinition.cs
internal override void WriteTo(BinaryWriter writer, ...)
{
    writer.Write(Id);
    writer.WriteString(Name);
    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        writer.WriteBool(IsComment);
    }

    if (subVersion is not null)  // ← CRITICAL!
    {
        writer.WriteBool(IsExpanded);
        writer.Write(ParentId);  // ← Only write if SubVersion != null
    }
    // If SubVersion is null, ParentId is NOT written!
}
```

### TriggerDefinition (Triggers)

**Reading:**
```csharp
// Line 23-52 in TriggerDefinition.cs
internal void ReadFrom(BinaryReader reader, ...)
{
    Name = reader.ReadChars();
    Description = reader.ReadChars();
    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        IsComment = reader.ReadBool();
    }

    if (subVersion is not null)
    {
        Id = reader.ReadInt32();
    }

    IsEnabled = reader.ReadBool();
    IsCustomTextTrigger = reader.ReadBool();
    IsInitiallyOn = !reader.ReadBool();
    RunOnMapInit = reader.ReadBool();
    ParentId = reader.ReadInt32();  // ← ALWAYS reads, even in 1.27!

    // ... read functions
}
```

**Writing:**
```csharp
// Line 55-80 in TriggerDefinition.cs
internal override void WriteTo(BinaryWriter writer, ...)
{
    writer.WriteString(Name);
    writer.WriteString(Description);
    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        writer.WriteBool(IsComment);
    }

    if (subVersion is not null)
    {
        writer.Write(Id);
    }

    writer.WriteBool(IsEnabled);
    writer.WriteBool(IsCustomTextTrigger);
    writer.WriteBool(!IsInitiallyOn);
    writer.WriteBool(RunOnMapInit);
    writer.Write(ParentId);  // ← ALWAYS writes, even in 1.27!

    // ... write functions
}
```

## 🔍 Critical Difference

| Item | SubVersion=null (WC3 1.27) | SubVersion!=null (WC3 1.31+) |
|------|---------------------------|------------------------------|
| **Category ParentId** | ❌ NOT read/written | ✅ Read and written |
| **Trigger ParentId** | ✅ Read and written | ✅ Read and written |
| **Variable Id** | ❌ NOT read/written | ✅ Read and written |

## 🐛 Why All Categories Have ParentId=0

### The Cycle

1. **File on disk (WC3 1.27 format):**
   - Categories listed in order
   - No ParentId field in binary data
   - Triggers have ParentId field pointing to categories

2. **War3Net reads file:**
   - SubVersion=null detected
   - Categories: ParentId NOT read → **defaults to 0**
   - Triggers: ParentId IS read → gets actual values

3. **In memory:**
   - All categories: ParentId=0
   - Triggers: ParentId=actual values from file

4. **War3Net writes file:**
   - SubVersion=null (maintaining 1.27 format)
   - Categories: ParentId NOT written
   - Triggers: ParentId IS written

5. **File on disk again:**
   - Same as step 1 - no category ParentId data

6. **World Editor reads file:**
   - Categories appear in file order
   - World Editor interprets ordering as hierarchy
   - In your case, categories are listed after triggers
   - This creates visual nesting in World Editor

## 📊 Your Diagnostic Output Explained

```
TARGET map:
📁 Initialization (ID=0, ParentId=0) - 11 trigger(s)
  📁 Load Heroes (ID=1, ParentId=0) ← All have ParentId=0!
  📁 DEBUG (ID=2, ParentId=0)
  📁 Intro Cinematic (ID=3, ParentId=0)
  ... 29 more categories, all ParentId=0
```

**What's happening:**
1. All categories read from file with ParentId=0 (not in file)
2. Categories listed sequentially in TriggerItems list
3. World Editor interprets first category as "root" of file order
4. Visual nesting is based on **file position**, not ParentId
5. Since triggers come after categories in the list, they appear nested

## 🔧 WTGMerger Implementation Analysis

### Good: No Hardcoded ParentId=0

**WTGMerger does NOT hardcode ParentId=0!**

Looking at the code:

```csharp
// Line 746-749: When creating new category
destCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
{
    Id = GetNextId(target),
    ParentId = -1,  // ← Sets to -1, not 0!
    Name = destCategoryName,
    //...
};
```

```csharp
// Line 842-845: When merging category
var newCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
{
    Id = GetNextId(target),
    ParentId = -1,  // ← Sets to -1, not 0!
    Name = sourceCategory.Name,
    //...
};
```

**WTGMerger correctly sets ParentId=-1 for root-level categories!**

### The Real Issue: War3Net's Read Behavior

When War3Net reads the file:
1. Categories get ParentId=0 (not read from 1.27 file)
2. WTGMerger doesn't modify ParentIds unless explicitly asked
3. On write, ParentId values in memory are ignored (not written in 1.27)
4. On next read, back to ParentId=0 again

## 🎭 Why World Editor Shows Nesting

World Editor doesn't use ParentId for 1.27 format maps. It uses **file position order**.

### File Structure (Simplified)

```
Categories:
  [0] Initialization
  [1] Load Heroes
  [2] DEBUG
  [3] Intro Cinematic
  ... more categories

Triggers:
  [0] Trigger belonging to Initialization
  [1] Another trigger
  ... more triggers
```

**World Editor logic:**
- First category = root of that section
- Categories listed together = siblings or nested based on position
- If triggers appear AFTER all categories, they may appear nested under last category
- This is based on ordering, NOT ParentId

## 🔄 How GetTriggersInCategory Works

```csharp
// Line 800-804
var triggersInCategory = triggers.TriggerItems
    .OfType<TriggerDefinition>()
    .Where(t => t.ParentId == category.Id)  // ← Uses ParentId!
    .ToList();
```

**This works because:**
- Triggers DO have ParentId in 1.27 format ✅
- Trigger ParentIds are read/written correctly ✅
- Categories in 1.27 maps still have sequential IDs ✅

**Example:**
- Category "Spels Heroes" has Id=33554453
- Its triggers have ParentId=33554453
- `GetTriggersInCategory` finds them by matching ParentId ✅

## ⚠️ The Orphaned Triggers Issue

```
[WARNING] 11 orphaned trigger(s) found:
  - Initialization (ParentId=234)
  - Init 01 Players (ParentId=234)
  ...
```

These triggers have ParentId=234 which **was written to the file** (because trigger ParentIds are always written), but:
1. Category ID 234 doesn't exist anymore
2. This is from previous map corruption/editing
3. ParentId=234 persists in file because it's always written for triggers

## 🎯 Why Copying a Single Trigger Worked

```
MERGED: Spels Heroes (ID=235) - 1 trigger
```

If you copied ONE specific trigger:
1. WTGMerger created category "Spels Heroes" with ParentId=-1
2. Copied the trigger with new ParentId=235
3. That trigger is correctly linked to the new category ✅

The diagnostic showing "54 triggers" in SOURCE is correct - that's how many are in the source "Spels Heroes" category. But you only asked to copy 1, so only 1 was copied.

## 📋 Summary of Interactions

### No Conflicts in WTGMerger!

WTGMerger and War3Net work correctly together:

1. **Reading:**
   - War3Net reads file with SubVersion=null
   - Categories get ParentId=0 (not in file)
   - Triggers get correct ParentIds (from file)
   - ✅ No conflict

2. **Manipulation:**
   - WTGMerger sets new category ParentId=-1
   - WTGMerger sets new trigger ParentId=newCategory.Id
   - ✅ No conflict

3. **Writing:**
   - War3Net writes with SubVersion=null
   - Category ParentIds ignored (not written)
   - Trigger ParentIds written correctly
   - ✅ No conflict

4. **Next Read:**
   - Categories ParentId=0 again (not in file)
   - Triggers ParentId preserved from file
   - ✅ Expected behavior

## 🛠️ Solutions

### Option 1: Accept 1.27 Behavior (RECOMMENDED)

**Reality:**
- Category ParentIds don't exist in 1.27 format
- They will always be 0 when read
- World Editor uses file order, not ParentId
- This is NORMAL for WC3 1.27 maps

**What to do:**
1. Don't worry about category ParentId=0
2. Fix orphaned triggers with ParentId=234:
   ```
   Option 7: Repair orphaned triggers
   Mode: Smart
   ```
3. Verify triggers are correctly linked to categories
4. Accept that visual nesting in World Editor is based on file order

**Advantages:**
- ✅ Maximum compatibility (1.27, 1.31+, Reforged)
- ✅ Triggers work correctly (ParentIds preserved)
- ✅ No data corruption
- ✅ Map file smaller

### Option 2: Convert to SubVersion=v4

**If you NEED category ParentId persistence:**

```csharp
// In ReadMapTriggersAuto, uncomment/modify:
if (triggers.SubVersion == null)
{
    triggers.SubVersion = MapTriggersSubVersion.v4;

    // Assign category ParentIds
    var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>();
    foreach (var cat in categories)
    {
        cat.ParentId = -1;  // All root level
    }
}
```

**Advantages:**
- ✅ Category ParentIds preserved
- ✅ Variable IDs preserved
- ✅ More structured format

**Disadvantages:**
- ❌ NOT compatible with WC3 1.27
- ❌ Orphan repair becomes CRITICAL
- ❌ Larger file size

## 🎯 Recommendation

**Keep SubVersion=null** because:

1. ✅ Your TARGET map is already in 1.27 format
2. ✅ Triggers work correctly (ParentIds preserved)
3. ✅ Maximum compatibility
4. ⚠️ Category nesting is visual only in World Editor
5. ⚠️ Fix orphaned triggers (ParentId=234) to prevent confusion

**Just fix the orphaned triggers:**
```
Option 7: Repair orphaned triggers
Mode 1: Smart
This will fix the 11 triggers with ParentId=234
```

The category "nesting" in World Editor is cosmetic and based on file order, not actual ParentId relationships.

## 🔍 Verification

To verify this is all working correctly:

```bash
# 1. Fix orphaned triggers
Select option: 7
Mode: 1 (Smart)

# 2. Copy your trigger(s)
Select option: 5 (Copy specific)

# 3. Check result
Select option: 9 (DEBUG info)
Look for: Trigger ParentIds match category IDs

# 4. Save
Select option: s

# 5. Test in World Editor
- Triggers should work in-game
- Visual nesting is cosmetic only
```

The most important thing: **Trigger ParentIds** must be correct, and they ARE being preserved correctly by War3Net!
