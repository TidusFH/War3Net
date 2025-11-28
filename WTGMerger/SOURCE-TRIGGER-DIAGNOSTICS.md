# Source Trigger Diagnostics - Is YOUR Trigger Already Broken?

## 🎯 The Critical Question

**Before copying triggers, ask: Is the SOURCE trigger already corrupted?**

If BetterTriggers (or any other tool) corrupted the trigger when saving, then:
- ✗ The SOURCE trigger is already invalid
- ✗ Copying it will just copy the corruption
- ✗ "trigger data invalid" would happen regardless of our tool
- ✓ You need to fix/recreate the SOURCE trigger first

This document explains the new diagnostic tools I've added to check if your SOURCE triggers are already broken.

---

## 🆕 New Source Trigger Diagnostic Options

### **Option 15: Export Trigger to Detailed Text File**

**What it does:**
- Exports complete trigger structure to human-readable text
- Shows ALL functions, parameters, variable references
- Optional hex dumps to detect binary corruption
- Generates pseudo-code representation

**When to use:**
- Want to see exactly what's inside a trigger
- Suspect trigger has been corrupted
- Need to share trigger data for debugging
- Comparing source vs target versions

**How to use:**
```
1. Run WTGMerger
2. Load maps
3. Choose option 15
4. Select (s)ource map
5. Enter category name
6. Enter trigger name
7. Choose whether to include hex dumps (y/n)
8. File saved as: TRIGGER_EXPORT_[name]_[timestamp].txt
```

**Example output:**
```
╔══════════════════════════════════════════════════════════╗
║  TRIGGER EXPORT: Absorb the Soul                        ║
╚══════════════════════════════════════════════════════════╝

=== BASIC PROPERTIES ===
Name: Absorb the Soul
ID: 15
ParentId: 3
IsEnabled: True
IsCustomTextTrigger: False

=== PARENT CATEGORY ===
✓ Category found: 'Ability Pack 1' (ID=3)

=== FUNCTIONS ===
Total: 12

EVENTS (1):
  [0] Unit - A unit Starts the effect of an ability
      Type: Event
      Enabled: True
      Parameters (2):
        [0] Type=Unit, Value="udg_AP1_Caster" ✓
        [1] Type=AbilityId, Value="A001"

CONDITIONS (1):
  [0] (Ability being cast) Equal to Absorb Soul
      ...

ACTIONS (10):
  [0] Set Variable
      Parameters:
        [0] Type=Variable, Value="AP1_Player" ✗MISSING
        [1] Type=Function
           → Nested Function: (Owner of (Triggering unit))

=== VARIABLE REFERENCES ===
✓ AP1_Caster                     [EXISTS]
   Type: unit, Array: False, Init: False
✗ AP1_Player                     [MISSING]
✓ AP1_Unit                       [EXISTS]
   Type: unit, Array: False, Init: False
✓ AP1_Point                      [EXISTS]
   Type: location, Array: False, Init: False
```

**This immediately shows you:** Variable `AP1_Player` is missing!

---

### **Option 16: Check Source Trigger for Corruption**

**What it does:**
- Scans trigger for common corruption patterns
- Detects: null bytes, empty names, circular references, missing variables
- Provides specific recommendations for each issue
- Optional detailed corruption report export

**When to use:**
- Before copying ANY trigger from source
- Suspect BetterTriggers corrupted the save
- Getting "trigger invalid" even after proper merge
- Want to verify source data integrity

**How to use:**
```
1. Run WTGMerger
2. Load maps
3. Choose option 16
4. Enter category name
5. Enter trigger name
6. Review corruption report
7. Optionally export detailed report
```

**What it checks:**
- ✓ Empty or null trigger name
- ✓ Invalid ParentId values (< -1)
- ✓ Suspiciously high ParentIds
- ✓ Empty function names
- ✓ Null bytes in strings (binary corruption)
- ✓ Missing variable references
- ✓ Empty variable parameters
- ✓ Circular references (infinite loop detection)
- ✓ Excessive function nesting (> 100 levels)

**Example output (Clean trigger):**
```
╔══════════════════════════════════════════════════════════╗
║    CHECK SOURCE TRIGGER FOR CORRUPTION                  ║
╚══════════════════════════════════════════════════════════╝

Running corruption detection...

✓ No corruption detected in source trigger!
✓ Trigger structure appears valid
```

**Example output (Corrupted trigger):**
```
Running corruption detection...

✗ FOUND 3 ISSUE(S):

  ✗ CORRUPT: Function name contains null bytes: 'SetVariable\0\0'
  ⚠ MISSING VARIABLE: 'AP1_Player' referenced but not in map
  ⚠ SUSPICIOUS: Very high ParentId=1024 (might be corrupted)

╔══════════════════════════════════════════════════════════╗
║  RECOMMENDATION                                          ║
╚══════════════════════════════════════════════════════════╝

✗ SOURCE TRIGGER IS CORRUPTED!

This trigger cannot be safely copied in its current state.

Options:
  1. Fix the source map in World Editor
  2. Recreate the trigger manually
  3. Try restoring from backup

Export detailed report? (y/n):
```

---

### **Option 17: Compare Two Triggers Side-by-Side**

**What it does:**
- Compares source trigger vs target trigger
- Shows property differences
- Runs corruption check on both
- Identifies what changed during copy

**When to use:**
- After copying trigger to target
- Want to see what's different between source and target
- Debugging why copied trigger doesn't work
- Verifying copy was successful

**How to use:**
```
1. Run WTGMerger
2. Load maps
3. Choose option 5 → Copy trigger to target
4. Choose option 17
5. Enter source category and trigger name
6. Enter target category and trigger name (or press Enter for same)
7. Review comparison
```

**Example output:**
```
╔══════════════════════════════════════════════════════════╗
║    COMPARE TWO TRIGGERS SIDE-BY-SIDE                    ║
╚══════════════════════════════════════════════════════════╝

SOURCE: Absorb the Soul
TARGET: Absorb the Soul

=== Property Comparison ===
  Name: Absorb the Soul ✓
  IsEnabled: True ✓
  IsCustomTextTrigger: False ✓
  Functions.Count: 12 ✓

=== CORRUPTION CHECK ===

SOURCE:
  ⚠ MISSING VARIABLE: 'AP1_Player' referenced but not in map

TARGET:
  ✓ No issues
```

This shows: The missing variable was in SOURCE, not introduced during copy!

---

### **Option 18: Extract Trigger + Variables to Standalone .wtg**

**What it does:**
- Creates minimal .wtg with just one trigger
- Includes only that trigger and its variables
- Perfect for sharing or isolated testing
- Also works as trigger backup

**When to use:**
- Want to share a single trigger with someone
- Need to test trigger in isolation
- Creating trigger library/collection
- Backing up important triggers
- Submitting bug report with minimal reproduction

**How to use:**
```
1. Run WTGMerger
2. Load source map
3. Choose option 18
4. Enter category name
5. Enter trigger name
6. File saved as: EXTRACTED_[name].wtg
7. Open EXTRACTED file in World Editor to test
```

**What gets extracted:**
- ✓ The trigger itself
- ✓ The category it belongs to
- ✓ ALL variables it references
- ✓ Root category item (if needed)

**Example output:**
```
✓ Extracted trigger saved to: EXTRACTED_Absorb_the_Soul.wtg

Contents:
  • 1 category: 'Ability Pack 1'
  • 1 trigger: 'Absorb the Soul'
  • 4 variable(s)

You can:
  1. Share this file with others
  2. Open in World Editor to test
  3. Use as backup of this trigger
```

**Why this is useful:**

If you open `EXTRACTED_Absorb_the_Soul.wtg` in World Editor and it shows "trigger invalid", then you KNOW:
- ✓ The source trigger is fundamentally broken
- ✓ Not a merge/copy issue
- ✓ Not a target map conflict
- ✗ The trigger needs to be fixed/recreated in source

---

## 📋 Recommended Workflow for Source Validation

### **Before ANY Merge Operation:**

```
Step 1: Check for corruption (Option 16)
  → Category: Ability Pack 1
  → Trigger: Absorb the Soul
  → Result: Identified missing variable 'AP1_Player'

Step 2: Export detailed structure (Option 15)
  → Review variable references
  → Check all functions are valid
  → Look for suspicious patterns

Step 3: Extract to standalone (Option 18)
  → Creates: EXTRACTED_Absorb_the_Soul.wtg
  → Test in World Editor
  → If invalid → SOURCE is broken, fix source first
  → If valid → Safe to copy

Step 4: Only AFTER source is validated, proceed with merge
  → Option 4 or 5: Copy trigger
  → Option 17: Compare source vs target
  → Option 13: Validate all target triggers
  → Save if everything checks out
```

---

## 🐛 Common Corruption Patterns & Causes

### **Pattern 1: Missing Variables**

**What you see:**
```
⚠ MISSING VARIABLE: 'AP1_Player' referenced but not in map
```

**Cause:**
- Variable was deleted but trigger still references it
- BetterTriggers didn't save variables correctly
- Map corruption during save
- Variable is in war3map.wct (custom text) but not .wtg

**Fix:**
1. Open source map in World Editor
2. Check if variable actually exists
3. If missing: Create it with correct type
4. If exists: Might be case mismatch (check exact spelling)

---

### **Pattern 2: Null Bytes in Strings**

**What you see:**
```
✗ CORRUPT: Function name contains null bytes: 'SetVariable\0\0'
```

**Cause:**
- Binary corruption (file damage)
- Bug in BetterTriggers string handling
- Incomplete write operation

**Fix:**
1. Source map is corrupted at binary level
2. Restore from backup if possible
3. Recreate trigger manually
4. Cannot be fixed by tools

---

### **Pattern 3: Invalid ParentId**

**What you see:**
```
⚠ SUSPICIOUS: Very high ParentId=1024 (might be corrupted)
```

**Cause:**
- Category was deleted but ID reference not updated
- ID corruption during save
- BetterTriggers ID reassignment bug

**Fix:**
1. Use Option 6: Repair orphaned triggers
2. Will reassign to valid category or root
3. Or manually fix ParentId in World Editor

---

### **Pattern 4: Circular References**

**What you see:**
```
✗ CORRUPT: Possible circular reference in nested functions
```

**Cause:**
- Function references itself (infinite loop)
- Parameter points back to parent function
- Critical data structure corruption

**Fix:**
1. This is SEVERE corruption
2. Trigger is fundamentally broken
3. MUST recreate manually
4. Cannot be salvaged

---

## 💡 Key Insights

### **If Corruption Check Shows Issues:**

**CORRUPT issues (red ✗):**
- Trigger is fundamentally broken
- Cannot be safely copied
- MUST fix source first
- Often requires manual recreation

**SUSPICIOUS issues (yellow ⚠):**
- Might be OK, might be corrupted
- Worth investigating
- May copy successfully but behave oddly
- Use Isolation Test (Option 14) to verify

**MISSING issues (yellow ⚠):**
- Variable references don't resolve
- Will definitely cause "trigger invalid"
- Easy to fix: Add missing variables
- Or might be intentional (custom text triggers)

---

### **BetterTriggers Specific Issues:**

Based on your report of BetterTriggers error (`"attempts to use the same type of variable _/AP1_Player"`), this suggests:

1. **BetterTriggers detected the same problem we would**
   - Variable issue exists in the data
   - Not a tool-specific bug
   - Real problem with trigger/variables

2. **Possible causes:**
   - Variable name has special characters BT doesn't handle
   - Type mismatch between variable definition and usage
   - Array vs non-array mismatch
   - Variable in .wct but not .wtg

3. **Our diagnostic advantage:**
   - Option 15: See EXACT variable references
   - Option 16: Detect WHAT is corrupted
   - Option 18: Test in isolation
   - Can pinpoint the exact issue

---

## 🎯 Action Plan

### **What to Do Right Now:**

1. **Run Option 16** on "Absorb the Soul" from SOURCE
   ```
   This will tell you if source trigger is already broken
   ```

2. **If corruption found:**
   ```
   → Fix source map first before attempting merge
   → Use Option 15 to see detailed structure
   → Use Option 18 to test extracted version
   ```

3. **If no corruption found:**
   ```
   → Copy to target (Option 5)
   → Use Option 17 to compare
   → Use Option 13 to validate result
   → Problem is with copy process, not source data
   ```

4. **Share findings:**
   ```
   → Output of Option 16 (corruption check)
   → Output of Option 15 (detailed export)
   → Does EXTRACTED_.wtg open in World Editor?
   → This pinpoints exact problem
   ```

---

## 📂 Generated Files

These diagnostic options create the following files:

| File Pattern | Option | Purpose |
|-------------|--------|---------|
| `TRIGGER_EXPORT_[name]_[time].txt` | 15 | Detailed trigger structure export |
| `CORRUPTION_REPORT_[name]_[time].txt` | 16 | Full corruption analysis report |
| `EXTRACTED_[name].wtg` | 18 | Standalone trigger + variables |
| `ISOLATION_TEST.wtg` | 14 | Minimal test environment |

**All files saved to same directory as output file.**

---

## 🎉 Summary

**The NEW diagnostic options answer the critical question:**

❓ **"Is my SOURCE trigger already broken?"**

**Before (without diagnostics):**
- Copy trigger → "trigger invalid"
- No idea if problem is source, target, or copy process
- Can't tell what's actually wrong
- Trial and error approach

**After (with these diagnostics):**
- Option 16: Check SOURCE for corruption → ✗ FOUND ISSUES
- Option 15: Export details → See exact problem
- Option 18: Extract and test → Confirm source is broken
- ✅ **Now you know: Fix SOURCE first, then merge**

**This is exactly what you suspected!**

> "before i also want options for the trigger itself because i think that might have been a issue with bettertriggers saving, so the trigger data invalid would still happen either way"

**You were right!** Let's verify that hypothesis with these tools.

Run Option 16 on your source trigger and share the results! 🔍
