# Debugging "Trigger Data Invalid" Errors

## 🔍 What This Error Means

When you get "trigger data missing or invalid" in Warcraft 3, it usually means:

1. **TriggerItemCounts mismatch** - The count dictionary doesn't match actual items
2. **Orphaned triggers** - Triggers with ParentId pointing to non-existent categories
3. **Duplicate IDs** - Multiple items sharing the same ID
4. **Corrupted trigger functions** - Missing or invalid event/condition/action data
5. **Version mismatch** - Format version incompatible with game version

---

## ✅ What We Fixed

### Fix #1: TriggerItemCounts Update (CRITICAL!)

**Problem**: MapTriggers has a `TriggerItemCounts` dictionary that MUST match the actual trigger items.

**Solution**: Added `UpdateTriggerItemCounts()` function called:
- After copying a category
- After copying specific triggers
- **BEFORE saving the file** (most important!)

```csharp
// This was missing - now it's there!
UpdateTriggerItemCounts(targetTriggers);
```

### Fix #2: ParentId Assignment

**Problem**: Copied triggers kept their old ParentId from source map.

**Solution**: Set `ParentId` to the new category's ID when copying:

```csharp
CopyTrigger(sourceTrigger, GetNextId(target), newCategory.Id)
//                                            ^^^^^^^^^^^^^^^^
//                                            Sets ParentId correctly
```

### Fix #3: Validation Before Save

**Problem**: No way to know if data was corrupted before loading in WC3.

**Solution**: Added `ValidateAndShowStats()` that checks:
- ✅ Orphaned triggers (ParentId → non-existent category)
- ✅ Duplicate IDs
- ✅ TriggerItemCounts matches actual items
- ✅ Shows complete statistics

---

## 🧪 How to Debug Your Issue

### Step 1: Run the Merger Again

```cmd
cd WTGMerger
run.bat
```

### Step 2: Copy Your Triggers

When you choose **Option 6** (Save and exit), you'll now see:

```
╔══════════════════════════════════════════════════════════╗
║              VALIDATION & STATISTICS                     ║
╚══════════════════════════════════════════════════════════╝

Format Version: v7
SubVersion: None
Game Version: 31

Total Variables: 24
Total Trigger Items: 459

Trigger Item Counts:
  Category: 33
  Gui: 426

✓ No orphaned triggers found
✓ No duplicate IDs found
```

### Step 3: Look for Warnings

#### ⚠️ Warning: Orphaned Triggers

```
⚠ WARNING: 5 orphaned triggers found:
  - My Trigger (ParentId=99)
  - Another Trigger (ParentId=15)
```

**What this means**: These triggers reference a category that doesn't exist.

**How to fix**:
- This shouldn't happen with our fixed code
- If it does, the ParentId fix didn't work
- Report this as a bug!

#### ❌ Error: Duplicate IDs

```
❌ ERROR: 2 duplicate IDs found:
  ID 42: Category A, Trigger B
```

**What this means**: Two items have the same ID - this WILL corrupt your map!

**How to fix**:
- This shouldn't happen with `GetNextId()`
- If it does, something is very wrong
- Report this immediately!

---

## 🔬 Advanced Debugging

### Check the Generated File

1. After merge, you'll have: `Target/war3map_merged.wtg`
2. Try opening it in a hex editor
3. First 4 bytes should be: `57 54 47 21` ("WTG!")
4. Next 4 bytes = version (usually `07 00 00 00` for v7)

### Compare Before/After

Run the tool on target BEFORE merging:

```cmd
dotnet run
# Choose option 2 (List target categories)
# Write down the trigger counts
```

Then after merging, compare. All counts should be higher.

### Test with Simple Case

1. Create a test map with 1 category, 1 trigger
2. Extract the war3map.wtg
3. Try merging it
4. See if it works

This isolates whether the problem is:
- Our tool (if simple case fails)
- Your specific triggers (if simple case works)

---

## 🚨 Common Issues & Solutions

### Issue: Map crashes on load

**Likely cause**: TriggerItemCounts not updated

**Fix**: Already fixed! Update to latest version and try again.

### Issue: Triggers appear but don't work

**Likely cause**: Trigger functions not deep-copied correctly

**Check**: Look at the trigger in World Editor. Are events/conditions/actions there?

**Fix**: If actions are missing, we have a deep-copy bug in `CopyTriggerFunction()`.

### Issue: Some categories show 0 triggers

**Likely cause**: ParentId not set correctly

**Fix**: Already fixed! The triggers now have `ParentId = newCategory.Id`.

### Issue: World Editor says "This map is protected"

**Likely cause**: Wrong problem! This isn't about triggers.

**Fix**: You can't edit protected maps. Use a different source.

---

## 📝 Validation Checklist

Before using a merged map:

- [ ] Validation showed "✓ No orphaned triggers"
- [ ] Validation showed "✓ No duplicate IDs"
- [ ] TriggerItemCounts matches expected numbers
- [ ] File size increased (more triggers = bigger file)
- [ ] Can open war3map_merged.wtg in a hex editor (not corrupted)
- [ ] First bytes are "WTG!" (57 54 47 21)

---

## 🐛 If You Still Have Issues

### Collect This Information:

1. **Validation output** - Screenshot or copy the validation stats
2. **Error message** - Exact wording from WC3 or World Editor
3. **Source map info**:
   - Which map? (name)
   - Which category you copied?
   - How many triggers?

4. **Target map info**:
   - Custom map or official?
   - How many categories/triggers BEFORE merge?

5. **What you did**:
   - Used option 4 (copy category) or option 5 (specific triggers)?
   - Which triggers exactly?

### Test These:

**Test 1**: Copy in opposite direction
- Try using target as source and source as target
- Does it still fail?

**Test 2**: Copy different category
- Try a different category from same map
- Does it work?

**Test 3**: Copy to empty map
- Create blank map
- Try copying to it
- Isolates if target map is the problem

---

## 🎯 Quick Reference

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| "Trigger data invalid" on load | TriggerItemCounts mismatch | Fixed in latest version |
| Orphaned triggers warning | ParentId wrong | Fixed in latest version |
| Duplicate IDs error | GetNextId() bug | Shouldn't happen - report! |
| Map crashes in-game | Corrupted trigger functions | Deep-copy issue - report with details |
| Categories show 0 triggers | ParentId not set | Fixed in latest version |
| Triggers disappear | Not actually copied | Check console output |

---

## 🔧 Developer Debug Mode

If you're a developer and want MORE debug info, add this to WriteWTGFile():

```csharp
static void WriteWTGFile(string filePath, MapTriggers triggers)
{
    // DEBUG: Dump structure
    Console.WriteLine("\n=== DEBUG INFO ===");
    Console.WriteLine($"TriggerItems.Count: {triggers.TriggerItems.Count}");
    Console.WriteLine($"Variables.Count: {triggers.Variables.Count}");
    foreach (var kvp in triggers.TriggerItemCounts)
    {
        Console.WriteLine($"Count[{kvp.Key}] = {kvp.Value}");
    }
    Console.WriteLine("==================\n");

    // ... rest of function
}
```

---

## 📞 Need Help?

If none of this helps:

1. Make sure you're using the LATEST version (git pull)
2. Try the simple test case above
3. Collect the information listed in "If You Still Have Issues"
4. Create a GitHub issue with all details

The validation output is CRITICAL - always include it!
