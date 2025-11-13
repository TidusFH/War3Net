# Diagnostic & Alternative Approaches for "Trigger Invalid" Issues

## 🎯 Purpose

If even after deleting war3map.j you're still getting "trigger invalid" errors, the problem is likely **with the trigger content itself** (missing variables, invalid references, corrupted structure, etc.) rather than file synchronization.

This document explains the new diagnostic tools I've added to help you identify **exactly what's wrong**.

---

## 🆕 New Menu Options Added

### **Option 12: Deep Validation of Specific Trigger**

**What it does:**
- Validates a single trigger in extreme detail
- Checks all properties, functions, parameters, and references
- Identifies missing variables, invalid ParentIds, corrupt data

**When to use:**
- You suspect a specific trigger is causing problems
- You want to see exactly what's wrong with a copied trigger
- Comparing a working vs non-working trigger

**How to use:**
```
1. Run WTGMerger
2. Load source and target maps
3. Choose option 12
4. Select (s)ource or (t)arget to validate from
5. Enter category name
6. Enter trigger name
7. See detailed validation report
```

**What it checks:**
- ✓ Basic properties (ID, ParentId, IsEnabled, etc.)
- ✓ ParentId points to valid category
- ✓ All functions (events, conditions, actions)
- ✓ All parameters and nested functions
- ✓ All variable references exist in map
- ✓ No empty/null values where they shouldn't be

**Example output:**
```
╔══════════════════════════════════════════════════════════╗
║  VALIDATING TRIGGER: Absorb the Soul                    ║
╚══════════════════════════════════════════════════════════╝

=== Basic Properties ===
ID: 235
ParentId: 32
IsEnabled: True
Functions: 15

=== Parent Category Validation ===
✓ Parent category found: 'Ability Pack 1'

=== Variable Reference Validation ===
Variables referenced: 3
  ✗ 'AP1_Player' - NOT FOUND     ← THIS IS YOUR PROBLEM!
  ✓ 'AP1_Unit'
  ✓ 'AP1_Point'

╔══════════════════════════════════════════════════════════╗
║  VALIDATION SUMMARY                                      ║
╚══════════════════════════════════════════════════════════╝
✗ 1 CRITICAL ISSUE(S) FOUND:
  • Variable 'AP1_Player' referenced but not found in map
```

---

### **Option 13: Validate All Triggers in Target**

**What it does:**
- Scans EVERY trigger in your target map
- Quick validation of variable references and ParentIds
- Shows summary of valid vs invalid triggers
- Lets you drill down into specific invalid triggers

**When to use:**
- After merging, before saving
- To find ALL problematic triggers at once
- To ensure merged map is healthy

**How to use:**
```
1. Run WTGMerger
2. Load source and target maps
3. Perform your merge operation (option 4 or 5)
4. Choose option 13 before saving
5. Review validation summary
6. Fix any issues found
```

**Example output:**
```
Validating 47 triggers...

✓ Trigger Init
✓ Spawn Units
✗ Absorb the Soul: 1 issue(s)
    - Missing variable: 'AP1_Player'
✓ Lightning Strike
✗ Chain Lightning: 2 issue(s)
    - Missing variable: 'CL_Caster'
    - Missing variable: 'CL_Target'

═══ VALIDATION SUMMARY ═══
✓ Valid: 45
✗ Invalid: 2

Invalid triggers:
  • Absorb the Soul
  • Chain Lightning
```

---

### **Option 14: Isolation Test - Copy to Empty Map**

**What it does:**
- Creates a completely empty map structure
- Copies ONLY the trigger you're testing
- Saves to separate test file
- Validates the result

**When to use:**
- To determine if trigger is fundamentally broken vs conflicting with target
- Testing if the problem is the trigger itself or the merge process
- Creating minimal reproduction for debugging

**How to use:**
```
1. Run WTGMerger
2. Load source and target maps
3. Choose option 14
4. Enter category name
5. Enter trigger name to test
6. Test file saved as ISOLATION_TEST.wtg
7. Try opening ISOLATION_TEST.wtg in World Editor
```

**What this tells you:**
- ✓ If test file loads in WE: Problem is with merge/target map
- ✗ If test file fails in WE: Trigger itself is corrupted/invalid

---

## 📋 Recommended Diagnostic Workflow

### **Step 1: Validate the Source Trigger (Before Copying)**

```
Option 12 → Choose (s)ource → Enter category → Enter trigger name
```

**Goal:** Ensure the source trigger is valid before attempting to copy it.

**If source trigger is invalid:**
- The trigger is already corrupt in source map
- No amount of merging will fix it
- You need to fix it in source map first or recreate it

**If source trigger is valid:**
- Proceed to Step 2

---

### **Step 2: Copy and Validate the Result**

```
Option 5 → Copy trigger to target
Option 12 → Choose (t)arget → Enter category → Enter copied trigger name
```

**Goal:** See if the copying process introduced problems.

**Common issues found:**
- Missing variables (not copied from source)
- Wrong ParentId (pointing to non-existent category)
- Corrupted parameter values

---

### **Step 3: Identify Missing Variables**

If validation shows missing variables, you have options:

**Option A: Check if variables exist in source**
```
Option 8 → Show comprehensive debug information
```
Look for the missing variables in the source variable list.

**If variables exist in source but not in target after copy:**
- Bug in `CopyMissingVariables()` function
- Might be name mismatch (case sensitivity?)
- Might be reference not detected properly

**If variables don't exist in source either:**
- Trigger is referencing non-existent variables
- Trigger is broken in source map
- Need to fix source map first

---

### **Step 4: Isolation Test**

```
Option 14 → Enter category → Enter trigger name
```

**This creates ISOLATION_TEST.wtg with:**
- Only the root category
- Only the test category
- Only the one trigger
- Only the variables it references

**Try opening ISOLATION_TEST.wtg in World Editor:**

✓ **If it loads successfully:**
- The trigger itself is fine
- Problem is with how it interacts with target map
- Possible conflicts: duplicate variables, ID conflicts, etc.

✗ **If World Editor shows "trigger invalid":**
- The trigger has fundamental problems
- Check the validation output for specifics
- Might need to recreate trigger manually

---

### **Step 5: Full Validation Before Saving**

```
Option 13 → Validate all triggers in target
```

**Before pressing 's' to save, always validate:**
- Ensures no broken triggers slip through
- Catches problems early
- Safer than finding out after opening in WE

---

## 🔍 Alternative Approaches to Consider

Based on your specific situation, here are different strategies:

### **Approach 1: Intermediate Format Merge (Options 10/11)**

**What it is:**
- Disassembles triggers into BetterTriggers-style format
- Performs merge at intermediate level
- Rebuilds with predictable IDs

**Pros:**
- More robust handling of complex hierarchies
- Better conflict detection
- Predictable ID assignment

**Cons:**
- More complex process
- Takes longer
- Might have its own bugs

**When to use:**
- Standard copy (options 4/5) keeps failing
- Complex category hierarchies
- Multiple triggers with dependencies

---

### **Approach 2: Manual Variable Pre-Copy**

**What it is:**
1. Identify all variables used by trigger (Option 12)
2. Manually copy those variables to target FIRST
3. Then copy the trigger

**How:**
```csharp
// Add this workflow to the tool:
1. Option 12 → Validate source trigger → Note missing variables
2. Option 8 → Debug info → Find those variables in source
3. Add option to copy specific variables by name
4. Then Option 5 → Copy trigger
```

**When to use:**
- `CopyMissingVariables()` is not working correctly
- You want full control over what's copied
- Debugging variable reference issues

---

### **Approach 3: Custom Text Triggers (.wct)**

**What it is:**
- For custom JASS triggers, use war3map.wct instead
- These are plain text and easier to merge
- World Editor handles the sync automatically

**Limitation:**
- Only works for custom text triggers (IsCustomTextTrigger=true)
- Your trigger is probably GUI, not custom text

---

### **Approach 4: Recreate Trigger in Target**

**What it is:**
- Open both maps in World Editor
- Manually recreate the trigger in target map
- Copy-paste the logic function by function

**Pros:**
- 100% guaranteed to work
- No tool bugs
- You see exactly what you're doing

**Cons:**
- Time-consuming
- Manual work
- Not scalable for many triggers

**When to use:**
- Only 1-2 triggers to copy
- Trigger is small/simple
- All automated methods fail

---

## 🐛 Debugging: What the Validator Shows You

### Example 1: Missing Variable

```
✗ 'AP1_Player' referenced but not found in map
```

**What this means:**
- Trigger has a function parameter that references variable "AP1_Player"
- That variable doesn't exist in the target map's variable list
- World Editor can't resolve the reference → "trigger invalid"

**How to fix:**
1. Check if AP1_Player exists in SOURCE map (Option 8)
2. If yes: Bug in CopyMissingVariables() - need to debug why it wasn't copied
3. If no: Trigger is already broken in source - fix source map first

---

### Example 2: Invalid ParentId

```
ParentId 235 doesn't match any category
```

**What this means:**
- Trigger claims its parent category has ID=235
- No category with ID=235 exists
- Trigger appears as orphaned in World Editor → might show as "trigger invalid"

**How to fix:**
- Use Option 6 (Repair orphaned triggers)
- Will reassign trigger to correct category or root

---

### Example 3: Empty Function Name

```
Function [3] has empty name
```

**What this means:**
- One of the trigger's functions has a null/empty name
- This is corrupt data
- World Editor won't load it

**How to fix:**
- Trigger is fundamentally corrupted
- Either fix in source map or recreate manually

---

## 🎯 Next Steps

1. **Build the updated tool:**
   ```bash
   cd /home/user/War3Net/WTGMerger
   dotnet build
   cd bin/Debug/net6.0
   ./WTGMerger
   ```

2. **Run diagnostics:**
   - Option 12: Validate your "Absorb the Soul" trigger from SOURCE
   - See what it reports

3. **Copy and re-validate:**
   - Option 5: Copy the trigger
   - Option 12: Validate it in TARGET
   - Compare the two validation reports

4. **Report findings:**
   - Share the validation output
   - We can pinpoint the exact problem
   - Fix the specific issue found

---

## 💡 Key Insight

The fact that **BetterTriggers also shows the variable error** ("attempts to use the same type of variable _/AP1_Player") is a HUGE clue.

This means:
- ✗ It's NOT a War3Net bug
- ✗ It's NOT a file format issue
- ✓ It's a REAL problem with the trigger/variables
- ✓ Something about how variables are handled

**Most likely:**
- Variable 'AP1_Player' exists in both source AND target
- BUT they have different types (unit vs player group vs integer, etc.)
- OR: Variable exists in source with different casing (_AP1_Player vs AP1_Player)
- OR: Variable is an array in source but not in target (or vice versa)

The validator will show you EXACTLY what the mismatch is!

---

## 📞 What to Share

After running the diagnostics, please share:

1. **Output of Option 12 for source trigger:**
   ```
   Validate from: (s)ource
   Category: Ability Pack 1
   Trigger: Absorb the Soul
   ```

2. **Output of Option 13 for target (after merge):**
   ```
   Shows all invalid triggers
   ```

3. **Does ISOLATION_TEST.wtg load in World Editor?**
   - Yes/No
   - If no, what error?

This will let me see exactly what's wrong and provide a targeted fix!
