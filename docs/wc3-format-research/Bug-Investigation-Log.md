# WTGMerger Bug Investigation and Fixes

## Timeline of Investigation

### Issue #1: Extra Variables Being Added (RESOLVED)
**Reported**: "89 extra variables being added when merging triggers that use 0 variables"

**Root Cause**: `CopyMissingVariables` had default parameter `copyAllVariables = true`

**Fix**: Changed default to `false` - now only copies variables actually used by triggers

**Commit**: Initial variable fix

---

### Issue #2: Variable Reference Corruption (RESOLVED)
**Reported**: Variable references showing wrong values (e.g., `gg_unit_Eevi_0101` instead of `illidan(evil) 0101<gen>`)

**Root Cause**: When variables are copied from source to target, they get new indices in target's variable list. But trigger function parameters still referenced old source indices.

**Fix**: Implemented `RemapVariableIndices` system:
- Builds mapping: source variable index → target variable index
- Updates all function parameters in copied triggers
- Uses `TriggerFunctionParameter.ArrayIndexValues` for array references

**Commit**: Variable index remapping implementation

---

### Issue #3: Triggers in Comment Categories (RESOLVED)
**Reported**: Triggers appearing in comment categories (visual separators)

**Root Cause**: Some triggers had ParentIds pointing to comment categories

**Fix**:
- Added filtering to exclude comment categories from category listings
- Created Option 7: FixTriggersInCommentCategories
- Reassigns triggers to previous non-comment category (or next if no previous)

**Commit**: Comment category filtering and repair

---

### Issue #4: Category Display Corruption (MAJOR - RESOLVED)

**Reported**: "Obelisks Arthas shows 2 triggers instead of 14", "Victory Cinematic triggers appearing in VoiceOvers category"

This was the most complex issue with multiple contributing bugs:

#### Bug #4A: File Order Implementation (REVERTED)
**Attempted Fix**: Tried to use file order instead of ParentId matching for 1.27 format

**Result**: Made everything worse - all 203 triggers appeared in last category "Acolyte Death Safety"

**Reason**: In WC3 1.27, all categories are stored first, then all triggers. No interspersing possible. File order can't determine category membership - ParentIds must be used.

**Action**: Immediately reverted commit bdd0e92 with commit 73ed389

#### Bug #4B: RenumberCategoriesSequentially Corrupting IDs (FIXED)
**Discovery**: Source file displayed correctly with non-sequential IDs (0,1,2,4,3,6,8,25...) but Target displayed wrong after processing

**Root Cause**:
- `RenumberCategoriesSequentially` was renumbering category IDs from non-sequential to sequential (0,1,2,3...)
- But War3Net reads/writes category IDs directly from file
- Non-sequential IDs work perfectly fine!
- Renumbering broke trigger ParentId relationships because some trigger ParentId updates were missed

**Evidence**:
```
Source file (non-sequential IDs):
[13] Obelisks Arthas - ID: 17 - 14 triggers ✓ CORRECT

Target file (after renumbering to sequential):
[13] Obelisks Arthas - ID: 12 - 2 triggers ✗ WRONG
```

**Fix**: Modified `RenumberCategoriesSequentially` to:
- NOT renumber category IDs at all
- Only normalize category ParentIds for 1.27 format (-1 → 0)
- Preserve all original IDs and trigger ParentIds

**Commit**: 6db9221 - Fix category ID corruption by removing renumbering logic

#### Bug #4C: FixFileOrder Renumbering IDs (FIXED)
**Root Cause**: `FixFileOrder` was also renumbering category IDs while reordering items

**Fix**: Modified `FixFileOrder` to:
- Still reorder TriggerItems (categories before triggers) - required for 1.27 format
- NOT renumber any IDs - keep original non-sequential IDs unchanged
- NOT update trigger ParentIds - they already match correctly

**Commit**: Same commit as #4B (6db9221)

#### Bug #4D: FixDuplicateIds Destroying ParentIds (SMOKING GUN - FIXED)

**Discovery**: Source displays correctly, Target displays wrong. Only difference: `FixDuplicateIds` called on Target but NOT on Source!

**Root Cause**:
For WC3 1.27 format:
1. War3Net reads category IDs from file (can be non-sequential: 0,1,2,4,3,6,8,25...)
2. War3Net reads trigger ParentIds from file (pointing to those category IDs)
3. War3Net does NOT read trigger IDs from file → all triggers have `Id = 0` (duplicates!)
4. `FixDuplicateIds` runs to fix the duplicate trigger IDs
5. **CRITICAL BUG**: Old code changed all IDs first, THEN tried to match ParentIds:
   ```csharp
   // Step 1: Change all IDs
   triggers.TriggerItems[i].Id = i;

   // Step 2: Try to find parent category
   var category = triggers.TriggerItems
       .OfType<TriggerCategoryDefinition>()
       .FirstOrDefault(c => c.Id == trigger.ParentId); // FAILS!
   ```
6. By the time it tries to match, category IDs are already changed!
7. Lookups fail → all triggers become orphaned or assigned to wrong categories

**Evidence**:
```
Before FixDuplicateIds:
Category "Obelisks Arthas" - ID: 17
Trigger "Combat Detected" - ParentId: 17 ✓ Would match!

After FixDuplicateIds changes category ID:
Category "Obelisks Arthas" - ID: 0 (changed!)
Trigger "Combat Detected" - ParentId: 17 ✗ No match! Orphaned!
```

**Initial Fix Attempt**: Modified `FixDuplicateIds` to preserve ParentIds

**Final Fix**: Disabled `FixDuplicateIds` entirely!
- Duplicate trigger IDs (all = 0) are NORMAL for 1.27 format
- War3Net handles them correctly
- Source file with duplicate IDs displays perfectly
- We were trying to "fix" something that wasn't broken!

**Commits**:
- ed603e4 - Fix FixDuplicateIds corrupting ParentId relationships
- eccc811 - Disable FixDuplicateIds - it was corrupting Target display

---

## Key Lessons Learned

### 1. Non-Sequential IDs Are Normal and Correct
- World Editor creates maps with non-sequential category IDs
- War3Net, World Editor, and BetterTriggers all handle them correctly
- Never renumber category IDs!

### 2. Duplicate Trigger IDs Are Normal in 1.27
- Trigger IDs aren't stored in 1.27 format
- All triggers have `Id = 0` when loaded
- This is completely normal and expected
- War3Net's internal logic handles it correctly

### 3. ParentIds from File Are Sacred
- War3Net reads category IDs and trigger ParentIds from file
- They match correctly as-is
- Any attempt to recalculate or "fix" them breaks the relationship
- Preserve them exactly as loaded!

### 4. Source vs Target Processing Must Be Identical
- If Source displays correctly, use the same processing for Target
- Don't add "fixes" that Source doesn't need
- The smoking gun: FixDuplicateIds was only called on Target, not Source

### 5. Understanding File Format Is Critical
- Assumed War3Net assigned IDs sequentially when reading
- Actually reads IDs directly from file
- This assumption led to all the renumbering bugs
- Always verify assumptions against actual War3Net code!

## Test Evidence

### User's Test Results

**Test**: Put TARGET.wtg in Source folder (load same file as both source and target)

**Results**:
```
Loaded as SOURCE (no FixDuplicateIds):
[13] Obelisks Arthas - ID: 17 - 14 triggers ✓ CORRECT
  • Combat Detected
  • Combat Resolved
  • Arthas Damaged
  • [... 11 more triggers]

Loaded as TARGET (with FixDuplicateIds):
[13] Obelisks Arthas - ID: 12 - 2 triggers ✗ WRONG
  • Combat Detected
  • Combat Resolved
  (12 triggers missing!)
```

**Conclusion**: Same file, different results. The only difference was FixDuplicateIds. This proved it was the culprit.

## Files Modified

### WTGMerger/Program.cs

**Line 103**: Disabled `FixDuplicateIds(targetTriggers)` call

**Lines 2687-2735 - FixFileOrder**:
- Removed category ID renumbering
- Only reorders items (categories before triggers)
- Preserves all original IDs

**Lines 2742-2795 - RenumberCategoriesSequentially**:
- Removed category ID renumbering logic
- Only normalizes category ParentIds for 1.27 format
- Preserves trigger ParentIds

**Lines 3746-3820 - FixDuplicateIds**:
- Initially fixed to preserve ParentIds
- Ultimately disabled entirely (not called)
- Kept for reference but no longer used

## Validation

To verify fixes are working:

1. **Load target file** with Option 2
2. **Check category trigger counts** - should match World Editor
3. **Check specific categories**:
   - "Obelisks Arthas" should show 14 triggers
   - "Victory Cinematic" should contain its own triggers, not VoiceOvers triggers
4. **Compare with BetterTriggers** output - should match exactly

## Related Files

- War3Net source: `/src/War3Net.Build.Core/Serialization/Binary/Script/`
  - `MapTriggers.cs` - Reading/writing logic
  - `TriggerCategoryDefinition.cs` - Category serialization
  - `TriggerDefinition.cs` - Trigger serialization
  - `TriggerItem.cs` - Base class with Id and ParentId properties

## Future Considerations

1. **Never renumber category IDs** - they work fine as non-sequential
2. **Never "fix" duplicate trigger IDs** - they're normal in 1.27
3. **Process Source and Target identically** - don't add Target-only "fixes"
4. **Preserve ParentIds exactly as read** - don't recalculate
5. **Only modify what's necessary for merging** - variables, adding new triggers
6. **File order matters** - categories before triggers in 1.27 format
