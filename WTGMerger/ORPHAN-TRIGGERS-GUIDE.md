# Orphaned Triggers - Diagnostic and Repair Guide

## What Are Orphaned Triggers?

**Orphaned triggers** are triggers that reference a non-existent parent category via their `ParentId` field.

### Example Problem

Your diagnostic output showed:
```
[WARNING] 11 orphaned trigger(s) found:
  - Initialization (ID=0, ParentId=234)
  - Init 01 Players (ID=0, ParentId=234)
  - Init 02 Units (ID=0, ParentId=234)
  ...
```

**Problem:** These triggers have `ParentId=234`, but there's no category with ID=234 in the map!

### Why This Happens

1. **Previous merges/edits** - The map was edited before and category IDs changed
2. **Manual editing** - Someone manually edited the WTG file incorrectly
3. **Tool bugs** - Previous versions of merge tools had bugs
4. **Corrupted files** - File corruption during save/transfer

### Impact

**In WC3 1.27 format (SubVersion=null):**
- ParentIds are **NOT saved** to the file
- Orphaned triggers appear at root level in World Editor
- ✓ Map still works, but structure is messy

**In WC3 1.31+ format (SubVersion=v4/v7):**
- ParentIds ARE saved to the file
- ✗ World Editor may show errors
- ✗ Triggers may not appear correctly
- ✗ Map may fail to load

## How to Diagnose Orphans

### Step 1: Run Diagnostic

In WTGMerger menu, select option **8: Diagnose orphans**

```
Select option: 8

╔══════════════════════════════════════════════════════════╗
║           ORPHAN DIAGNOSTIC REPORT                       ║
╚══════════════════════════════════════════════════════════╝

=== ORPHANED TRIGGERS ===
⚠ Found 11 orphaned trigger(s):

  ParentId=234 (non-existent): 11 trigger(s)
    - Initialization
    - Init 01 Players
    - Init 02 Units
    - Init 03 Music
    - Init 04 Environment
    ... and 6 more
```

### Step 2: Understand the Report

**Key Information:**
- **ParentId=234** - The invalid category ID being referenced
- **11 trigger(s)** - Number of triggers affected
- **Trigger names** - Shows which triggers are orphaned

**Look for patterns:**
- All triggers start with "Init" → Should belong to "Initialization" category
- Names contain category name → Easy to match

## How to Repair Orphans

### Option 7: Repair Orphaned Triggers

WTGMerger offers two repair modes:

#### Mode 1: Smart Repair (Recommended)

Tries to match triggers to categories based on naming patterns:

```
Select option: 7

╔══════════════════════════════════════════════════════════╗
║          REPAIR ORPHANED TRIGGERS                        ║
╚══════════════════════════════════════════════════════════╝

Modes:
  1. Smart - Try to match triggers to categories by name
  2. Root - Move all orphaned triggers to root level

Select mode (1-2): 1

[ORPHAN REPAIR] Found 11 orphaned trigger(s)
  Repaired: 'Initialization' (was ParentId=234) → 'Initialization' (ParentId=0)
  Repaired: 'Init 01 Players' (was ParentId=234) → 'Initialization' (ParentId=0)
  Repaired: 'Init 02 Units' (was ParentId=234) → 'Initialization' (ParentId=0)
  ...

✓ Repaired 11 orphaned trigger(s)
```

**Smart Matching Rules:**

1. **Pattern: "Init XX"** → Matches to "Initialization" category
   - "Init 01 Players" → "Initialization"
   - "Initialization" (trigger) → "Initialization" (category)

2. **Pattern: Trigger name contains category name**
   - "Obelisk Setup" → "Obelisks" category
   - "Arthas Special Effect" → "Arthas Special Effect" category

3. **Pattern: Category name contains trigger name prefix**
   - "Arthas Builds..." → "Arthas ..." category
   - "Illidan First..." → "Illidan ..." category

4. **No match** → Moves to root level (ParentId=-1)

#### Mode 2: Root Repair

Moves ALL orphaned triggers to root level:

```
Select mode (1-2): 2

[ORPHAN REPAIR] Found 11 orphaned trigger(s)
  Repaired: 'Initialization' (was ParentId=234) → '<Root>' (ParentId=-1)
  Repaired: 'Init 01 Players' (was ParentId=234) → '<Root>' (ParentId=-1)
  ...

✓ Repaired 11 orphaned trigger(s)
```

**Use this when:**
- You want to manually organize triggers later in World Editor
- Smart matching isn't working correctly
- You prefer clean root-level organization

## When to Use Orphan Repair

### Before Merging (Recommended)

Run orphan repair on your **TARGET** map before copying triggers:

```
1. Load source and target maps
2. Select option 8 - Diagnose orphans
3. If orphans found, select option 7 - Repair orphans
4. Select mode 1 (Smart) or 2 (Root)
5. Now proceed with copying categories (option 4)
6. Save (option s)
```

**Why?** This ensures your target map is clean before adding more content.

### After Merging

If you forgot to repair before merging:

```
1. Complete your merge operations
2. Before saving, select option 8 - Diagnose orphans
3. If orphans found, select option 7 - Repair orphans
4. Save (option s)
```

### Periodically

Check for orphans periodically, especially if:
- You've done multiple merges
- You've edited triggers in World Editor
- Map was created with older tools
- You're experiencing trigger editor issues

## WC3 1.27 vs 1.31+ Compatibility

### WC3 1.27 Format (SubVersion=null)

**Current Status:** Your maps are in this format

**Behavior:**
- ParentIds are **NOT saved** to file
- All triggers default to ParentId=0 when read
- Orphaned triggers don't cause errors
- But structure is lost on save/reload

**Should you repair orphans?**
- ✓ **YES** - Even though ParentIds aren't saved, fixing them improves:
  - Internal tool consistency
  - Easier debugging
  - Cleaner structure if you upgrade to 1.31+ later
- ✓ No harm in repairing
- ✓ Makes diagnostic output cleaner

### WC3 1.31+ Format (SubVersion=v4)

**If you upgrade to this format:**

**Behavior:**
- ParentIds ARE saved to file
- Orphaned triggers WILL cause problems
- World Editor may show errors
- Triggers may not appear in correct categories

**You MUST repair orphans before saving in this format!**

## Verification

### After Repair - Check Results

1. **Run diagnostic again:**
   ```
   Select option: 8

   ✓ No orphaned triggers found
   ✓ No orphaned categories found
   ```

2. **Check trigger distribution:**
   ```
   Select option: 9 (Debug info)

   Look for the "TRIGGER DISTRIBUTION" section
   Should show triggers properly distributed
   ```

3. **Test in World Editor:**
   - Save the merged map (option s)
   - Open in World Editor
   - Check Trigger Editor (F6)
   - Verify all triggers are in correct folders

## Common Issues

### Issue 1: Smart Repair Puts Triggers in Wrong Category

**Solution:** Use Mode 2 (Root), then manually organize in World Editor

### Issue 2: Orphans Keep Appearing After Repair

**Cause:** New orphans are being created during merge

**Solution:**
1. Check source map for orphans before copying
2. Repair both source AND target before merge
3. Report as bug if it persists

### Issue 3: Some Triggers Appear at Root Despite Repair

**Expected in 1.27 format:**
- ParentIds aren't saved
- Triggers default to root on reload
- This is normal behavior

**Workaround:**
- Keep the source .wtg file for future merges
- Or upgrade to 1.31+ format (loses 1.27 compatibility)

## Example Workflow

### Complete Merge with Orphan Repair

```
Step 1: Load maps
  $ ./WTGMerger
  Reading source: ../Source/map.w3x
  Reading target: ../Target/map.w3x

Step 2: Diagnose orphans
  Select option: 8
  ⚠ Found 11 orphaned trigger(s)

Step 3: Repair orphans
  Select option: 7
  Select mode (1-2): 1
  ✓ Repaired 11 orphaned trigger(s)

Step 4: Copy category
  Select option: 4
  Enter category name: Spels Heroes
  ✓ Category copied!

Step 5: Final check
  Select option: 8
  ✓ No orphaned triggers found

Step 6: Save
  Select option: s
  ✓ Merge complete!
```

## Technical Details

### ParentId Field

**In VariableDefinition:**
```csharp
public int ParentId { get; set; }
```

**In TriggerDefinition:**
```csharp
public int ParentId { get; set; }
```

**Valid Values:**
- `-1` = Root level (no parent)
- `0+` = ID of parent category

**Invalid Values:**
- Any ID that doesn't match an existing category → **ORPHAN**

### How Orphans Are Detected

```csharp
var validCategoryIds = new HashSet<int>(
    categories.Select(c => c.Id)
);
validCategoryIds.Add(-1); // Root is always valid

var orphanedTriggers = triggers
    .Where(t => t.ParentId >= 0 &&
                !validCategoryIds.Contains(t.ParentId))
    .ToList();
```

### Smart Matching Algorithm

```
1. Check if trigger name starts with "Init"
   → Match to "Initialization" category

2. Check if trigger name contains category name
   → Match to that category

3. Check if category name contains first word of trigger name
   → Match to that category

4. No match found
   → Set ParentId = -1 (root level)
```

## FAQ

**Q: Will repairing orphans break my map?**
A: No. It fixes broken references. Your map can only get better.

**Q: Do I need to repair orphans if I'm using 1.27 format?**
A: Not strictly necessary, but recommended for cleaner structure and easier debugging.

**Q: Can I undo a repair?**
A: No, but you can exit without saving (option 0) and reload.

**Q: Why do orphans appear after saving in 1.27 format?**
A: Because ParentIds aren't saved in 1.27 format. All triggers default to ParentId=0 (root) when reloaded.

**Q: Should I use Smart or Root repair mode?**
A: Start with Smart (mode 1). If results aren't good, use Root (mode 2) and organize manually.

**Q: Can orphaned categories be repaired?**
A: The tool detects orphaned categories but doesn't auto-repair them yet. Manually set their ParentId to -1 in World Editor.

## See Also

- **VARIABLE-ID-BUG-ANALYSIS.md** - Variable ID issues
- **WTG-VARIABLE-ID-DIAGNOSTIC.md** - Diagnostic procedures
- **QUICK-START-GUIDE.md** - Basic usage guide
