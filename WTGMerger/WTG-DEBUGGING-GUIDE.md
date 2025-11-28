# WTG Debugging Guide - "Trigger Data Missing or Invalid"

## Error: "Unable to load file - Trigger data missing or invalid"

This error means World Editor cannot parse the war3map.wtg file. Here's how to diagnose and fix it.

---

## Step 1: Verify WTGMerger Can Parse the File

**After merging, immediately test if WTGMerger can read it back:**

1. After merge completes, check console for:
   ```
   === VERIFICATION: Reading saved file ===
   Variables written: X
   Variables in saved file: Y
   ✓ Merge complete!
   ```

2. If you see `❌ Could not verify saved file`, the file is corrupted.

3. If verification PASSED but World Editor still can't open it, proceed to Step 2.

---

## Step 2: Check for war3map.j Conflict

**CRITICAL:** When modifying war3map.wtg, you MUST delete war3map.j!

The war3map.j file contains compiled JASS code from triggers. If it's out of sync with war3map.wtg, World Editor will fail to load.

**Fix:**
1. Open your map archive (.w3x or .w3m) with an MPQ editor
2. Delete `war3map.j`
3. Save the archive
4. Try opening in World Editor again

World Editor will regenerate war3map.j from war3map.wtg when it opens.

---

## Step 3: Compare Original vs Merged File

**Test if the original target map opens:**

1. Before merging, make a backup of your target map
2. Try opening the backup in World Editor
3. If backup opens but merged doesn't, the merge corrupted something

**What to check:**
- File size difference (merged should be slightly larger)
- Number of triggers/categories/variables
- Format version (should be the same)

---

## Step 4: Test with Minimal Merge

**Create a minimal test case:**

1. Create a NEW empty map in World Editor
2. Add ONE simple trigger:
   ```
   Events: Map initialization
   Actions: Display Text Message "Test"
   ```
3. Save and close
4. Try merging ONE trigger from source into this empty map
5. Does the minimal merge work?

If yes: The issue is specific to your large target map
If no: The merge operation has a fundamental bug

---

## Step 5: Check Console Output for Warnings

**Look for these warning messages:**

```
⚠ Warning: X variable(s) were referenced but not found in source map
```

This means triggers reference variables that don't exist. World Editor will fail to load.

**Fix:** The source map must contain all variables the triggers need.

---

## Step 6: Verify Format Version Match

**Check if source and target use the same WTG format:**

In WTGMerger console output, look for:
```
[War3Writer] Format: v7, SubVersion: null
```

**Both source and target should have:**
- Same FormatVersion (v4 or v7)
- Same SubVersion (null for 1.27, v4/v7 for 1.31+)

If they differ, the merge might create incompatible files.

---

## Step 7: Enable Full Diagnostic Logging

**Get detailed logs:**

1. Run WTGMerger
2. Choose option `l` to enable diagnostic logging
3. Do your merge operation
4. Check the generated log file for errors

The log will show:
- Every trigger being copied
- Every variable being detected
- Every ID assignment
- File structure before/after merge

---

## Step 8: Test WTG File Directly

**Try opening just the war3map.wtg file:**

1. Extract war3map.wtg from your merged map
2. Use WTGMerger option 1 to load it
3. Does it parse correctly?
4. Check variable count, trigger count, etc.

If WTGMerger can parse it but World Editor can't, the issue is World Editor being stricter than our parser.

---

## Common Causes & Fixes

### Cause 1: Missing war3map.j Deletion

**Symptoms:**
- Merge completes successfully
- WTGMerger can read merged file
- World Editor shows "trigger data invalid"

**Fix:**
Delete war3map.j from the map archive before opening in World Editor.

---

### Cause 2: Variable References Without Definitions

**Symptoms:**
- Console shows: `⚠ Warning: variables were referenced but not found`
- World Editor can't load file

**Fix:**
Ensure source map has all variables that triggers reference.

---

### Cause 3: Corrupted Trigger Functions

**Symptoms:**
- Specific triggers cause crash
- Works with some triggers but not others

**Fix:**
Test triggers individually:
1. Extract problematic trigger to standalone file
2. Open standalone file in World Editor
3. If it fails, the trigger itself has issues
4. If it works, the merge process corrupted it

---

### Cause 4: ID Conflicts

**Symptoms:**
- Multiple items with same ID
- World Editor confused about which item is which

**Fix:**
Enable DEBUG mode and check for:
```
⚠ ID CORRUPTION DETECTED!
```

WTGMerger auto-repairs this, but verify in logs.

---

### Cause 5: Format Version Mismatch

**Symptoms:**
- Source is 1.31+ format (SubVersion = v4)
- Target is 1.27 format (SubVersion = null)
- Or vice versa

**Fix:**
Use maps with the same Warcraft 3 version (both 1.27 or both 1.31+).

---

## Emergency Recovery

If your map is broken:

1. **Restore from backup** (you made one, right?)
2. **Try extracting only specific triggers** instead of full merge
3. **Recreate triggers manually** in World Editor as last resort

---

## Reporting Bugs

If none of the above helps, provide:

1. Full console output from merge (with DEBUG mode on)
2. Diagnostic log file
3. Source map format version
4. Target map format version
5. Exact trigger being merged
6. Whether it works in isolation (extraction test)

This helps identify if it's a parser bug or data issue.
