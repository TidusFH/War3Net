# WTG MERGER - COMPLETE OBJECTIVES & REQUIREMENTS

## 🎯 PROJECT OBJECTIVES

### Primary Objective
Create a tool that merges triggers from one Warcraft 3 1.27 map into another while maintaining perfect compatibility with World Editor 1.27.

### Secondary Objectives
1. Preserve all trigger functionality (events, conditions, actions)
2. Maintain variable integrity and references
3. Support both MPQ archives (.w3x, .w3m) and raw .wtg files
4. Provide clear user interface and feedback
5. Enable debugging and troubleshooting
6. Prevent data loss or corruption

---

## 🔴 CRITICAL REQUIREMENTS

### 1. Format Compatibility
**Requirement:** Support WC3 1.27 old format (SubVersion=null)

**Details:**
- Detect format version on load
- Never modify SubVersion property
- Use position-based category IDs for old format
- Set category ParentId=0 for old format

**Test:**
- Load old format map → SubVersion should remain null
- Save → SubVersion should still be null
- Open in World Editor 1.27 → no errors

### 2. Position-Based Category System
**Requirement:** Category ID must equal its position in TriggerItems array

**Details:**
- Category at TriggerItems[0] → ID=0
- Category at TriggerItems[5] → ID=5
- After any operation: verify ID==position
- Fix misalignments automatically

**Test:**
- List categories → position column should equal ID column
- After merge → all categories have correct positions
- Debug mode shows no mismatches

### 3. Category Structure Integrity
**Requirement:** ALL categories must be at the START of TriggerItems

**Details:**
```
TriggerItems structure:
[0..N] = Categories
[N+1..] = Triggers
```

**Test:**
- After any operation: first N items are categories
- All triggers come after all categories
- No categories mixed with triggers

### 4. Trigger ParentId Correctness
**Requirement:** Trigger ParentId must reference category position

**Details:**
- Trigger in category at position 5 → ParentId=5
- WC3 uses ParentId as array index: `TriggerItems[ParentId]`
- Invalid ParentId (>= category count) → default to 0

**Test:**
- Copy trigger → ParentId matches destination category position
- List triggers → ParentId values are valid (< category count)
- Open in World Editor → triggers in correct categories

### 5. Variable Dependency Tracking
**Requirement:** Copy all variables used by copied triggers

**Details:**
- Analyze trigger functions for variable references
- Copy missing variables from source to target
- Preserve variable types and properties
- Avoid duplicate variables (check by name)

**Test:**
- Copy trigger using variables → variables appear in target
- Open in World Editor → no "undefined variable" errors
- Trigger code references correct variable types

---

## 📋 FUNCTIONAL REQUIREMENTS

### FR1: Load Map Files
**Feature:** Load WC3 maps and extract trigger data

**Inputs:**
- Map archive (.w3x, .w3m)
- Raw trigger file (war3map.wtg)

**Process:**
1. Detect file type (archive or raw)
2. Open file/archive
3. Read war3map.wtg using War3Net library
4. Parse MapTriggers object
5. Validate format version

**Outputs:**
- MapTriggers object in memory
- Format information (version, SubVersion, game version)
- Category and trigger counts

**Error Handling:**
- File not found → clear error message
- Invalid format → show parse error
- Corrupted file → explain what's wrong

### FR2: List Categories
**Feature:** Display all categories from a map

**Inputs:**
- MapTriggers object (source or target)

**Process:**
1. Extract all TriggerCategoryDefinition items
2. Get position of each in TriggerItems array
3. Count triggers in each category
4. Format as table

**Outputs:**
```
Pos | ID  | ParentId | Name
----|-----|----------|-----------------------------
  0 |   0 |        0 | Initialization (2 triggers)
  1 |   1 |        0 | Heroes (15 triggers)
```

**Highlights:**
- Red if ID != position
- Red if ParentId != 0 (old format)

### FR3: List Triggers in Category
**Feature:** Show all triggers within a specific category

**Inputs:**
- Category name (user input)
- MapTriggers object

**Process:**
1. Find category by name (case-insensitive)
2. Find all triggers with ParentId = category.Id
3. Display trigger names and properties

**Outputs:**
```
Triggers in 'Heroes': 15

[1] Hero Selection (ParentId=1, Enabled=true)
[2] Hero Respawn (ParentId=1, Enabled=true)
...
```

### FR4: Copy Entire Category
**Feature:** Copy all triggers from a category to target map

**Inputs:**
- Category name (user input)
- Source MapTriggers
- Target MapTriggers

**Process:**
1. Find source category by name
2. Get all triggers in source category
3. Check if category exists in target
4. If exists: remove old category and triggers
5. Create new category in target:
   - ID = existingCategories.Count
   - ParentId = 0 (old format)
   - Insert at correct position
6. Copy variables used by triggers
7. Copy all triggers with ParentId = new category ID
8. Fix category structure (FixCategoryIdsForOldFormat)

**Outputs:**
- Message: "✓ Copied category 'Heroes' (15 triggers)"
- Target modified flag set to true

**Error Handling:**
- Source category not found → error message
- Duplicate triggers → overwrite with confirmation

### FR5: Copy Specific Triggers
**Feature:** Copy selected triggers to target map

**Inputs:**
- Source category name
- Comma-separated trigger names
- Destination category name (optional, default = source)
- Source MapTriggers
- Target MapTriggers

**Process:**
1. Find triggers in source category by name
2. Find or create destination category in target
3. Copy variables used by triggers
4. Copy each trigger with ParentId = destination category ID
5. Fix category structure

**Outputs:**
- Message for each trigger: "✓ Hero Respawn (ParentId=2)"
- Summary: "✓ Copied 3 trigger(s)"

**Error Handling:**
- Trigger not found → warning, continue with others
- Invalid category → error message

### FR6: Show Debug Info
**Feature:** Display comprehensive structural information

**Outputs:**
```
=== SOURCE MAP ===
Format: v7
SubVersion: null (OLD FORMAT)
Game Version: 2
Variables: 42
Categories: 12
Triggers: 156

=== TARGET CATEGORY STRUCTURE ===
Pos | ID  | ParentId | Match? | Name
----|-----|----------|--------|-----------------------------
  0 |   0 |        0 |   ✓    | Initialization
  1 |   1 |        0 |   ✓    | Heroes
  2 |   5 |        0 |   ✗    | Spells  ← ERROR: ID mismatch!
```

### FR7: Fix Category IDs Manually
**Feature:** Allow user to manually trigger structure fix

**Process:**
1. Confirm with user
2. Call FixCategoryIdsForOldFormat(target)
3. Show results

**Outputs:**
- "! Fixing category structure..."
- "✓ Fixed 12 categories to positions 0-11"

### FR8: Save Merged Map
**Feature:** Write modified map to output file

**Inputs:**
- Output file path
- Modified MapTriggers
- Original archive (if map archive)

**Process:**
1. PRE-SAVE VALIDATION:
   - Check SubVersion == null still
   - Verify category IDs == positions
   - Verify category ParentIds == 0
   - Verify trigger ParentIds < category count
   - If validation fails: run FixCategoryIdsForOldFormat

2. SAVE:
   - If archive: update war3map.wtg, delete war3map.j
   - If raw: write to file directly

3. POST-SAVE VERIFICATION:
   - Read file back
   - Verify SubVersion == null
   - Verify counts match

**Outputs:**
- "✓ Wrote 45,678 bytes"
- "=== POST-SAVE VERIFICATION ==="
- "✓✓✓ SAVE SUCCESSFUL! ✓✓✓"

**Error Handling:**
- Validation fails → show errors, attempt fix
- Write fails → show file system error
- Verification fails → show what changed

---

## 🔧 TECHNICAL REQUIREMENTS

### TR1: .NET Platform
- Target: .NET 8.0
- Language: C# 12
- Output: Console Application

### TR2: Dependencies
- War3Net.Build.Core.dll
- War3Net.Build.dll
- War3Net.Common.dll
- War3Net.IO.Mpq.dll

### TR3: File I/O
- Read MPQ archives (readonly mode)
- Extract war3map.wtg from archive
- Write modified archives
- Handle file locking and permissions

### TR4: Memory Management
- Load entire MapTriggers into memory
- No streaming (files are small, < 10 MB)
- Release file handles after operations

### TR5: Error Handling
- Try-catch around main logic
- Specific exceptions for known errors
- Generic catch for unexpected errors
- Clear error messages for users

---

## 🎨 USER INTERFACE REQUIREMENTS

### UI1: Menu System
- Interactive menu with numbered options
- Clear option descriptions
- Input validation
- Back/exit options

### UI2: Output Formatting
- ASCII characters only (no Unicode)
- Colors for emphasis:
  - Green for success
  - Yellow for warnings
  - Red for errors
  - Cyan for information
- Clear section headers with `===`
- Consistent indentation

### UI3: Progress Feedback
- Show what's happening during operations
- Indicate success/failure immediately
- Provide context for errors
- Confirm before destructive operations

### UI4: Debug Mode
- Toggle on/off
- Show detailed information when enabled
- Display structure validation results
- Log ID mappings and changes

---

## ✅ ACCEPTANCE CRITERIA

### AC1: Format Preservation
**Given:** Old format map (SubVersion=null)
**When:** Load → modify → save
**Then:** SubVersion remains null throughout

### AC2: Category Positioning
**Given:** Map with any category structure
**When:** FixCategoryIdsForOldFormat() is called
**Then:**
- Category at position N has ID=N
- All categories are at start of TriggerItems
- Trigger ParentIds are updated correctly

### AC3: Trigger Copying
**Given:** Source category with 10 triggers
**When:** Copy category to target
**Then:**
- All 10 triggers appear in target
- Triggers reference correct category
- Variables are copied
- No data loss

### AC4: World Editor Compatibility
**Given:** Merged map file
**When:** Open in World Editor 1.27
**Then:**
- Map loads without errors
- Triggers appear in correct categories
- Trigger code is intact
- Variables are accessible

### AC5: In-Game Functionality
**Given:** Merged map with copied triggers
**When:** Test map in WC3
**Then:**
- Triggers execute correctly
- No runtime errors
- Game behavior is as expected

---

## 🧪 TEST PLAN

### Test Case 1: Basic Merge
```
Setup:
  Source: 5 categories, 50 triggers, 20 variables
  Target: 3 categories, 30 triggers, 15 variables

Action:
  Merge category "Heroes" (10 triggers, 5 variables)

Verify:
  ✓ Target has 4 categories (3 + 1)
  ✓ Target has 40 triggers (30 + 10)
  ✓ Target has 20 variables (15 + 5)
  ✓ Category IDs are 0,1,2,3
  ✓ Triggers in "Heroes" have ParentId=3
  ✓ File saves without errors
  ✓ World Editor opens without errors
```

### Test Case 2: Overwrite Existing Category
```
Setup:
  Source: Category "Heroes" with 10 triggers
  Target: Category "Heroes" with 5 triggers

Action:
  Merge category "Heroes"

Verify:
  ✓ Old "Heroes" removed completely
  ✓ New "Heroes" has 10 triggers (from source)
  ✓ Category structure is correct
  ✓ No duplicate categories
```

### Test Case 3: Fix Broken Structure
```
Setup:
  Map with categories:
    [0] ID=1
    [1] ID=5
    [2] ID=2

Action:
  Call FixCategoryIdsForOldFormat()

Verify:
  ✓ Categories become:
    [0] ID=0
    [1] ID=1
    [2] ID=2
  ✓ Trigger ParentIds updated correctly
```

### Test Case 4: Variable Dependencies
```
Setup:
  Source trigger uses variables: PlayerHero, Gold, TempUnit
  Target has: Gold

Action:
  Copy trigger

Verify:
  ✓ PlayerHero copied to target
  ✓ TempUnit copied to target
  ✓ Gold not duplicated
  ✓ Trigger references correct variables
```

### Test Case 5: Multiple Operations
```
Action:
  1. Copy category "Heroes"
  2. Copy specific triggers from "Spells"
  3. Copy category "Items"
  4. Save

Verify:
  ✓ All operations succeed
  ✓ Final structure is correct
  ✓ No ID collisions
  ✓ All triggers in correct categories
```

---

## 📊 PERFORMANCE REQUIREMENTS

### PR1: Load Time
- Load 1 MB map: < 1 second
- Load 10 MB map: < 5 seconds

### PR2: Merge Time
- Copy 100 triggers: < 2 seconds
- Copy 1000 triggers: < 10 seconds

### PR3: Save Time
- Save to .wtg: < 1 second
- Save to .w3x: < 3 seconds

### PR4: Memory Usage
- Max memory: 500 MB
- No memory leaks

---

## 🔒 SECURITY REQUIREMENTS

### SR1: File Access
- Read files in readonly mode when possible
- Don't modify original files
- Create new output files

### SR2: Input Validation
- Validate file paths
- Check file extensions
- Verify file exists before operations

### SR3: Error Messages
- Don't expose internal paths
- Don't show stack traces by default
- Provide helpful error messages

---

## 📚 DOCUMENTATION REQUIREMENTS

### DR1: README
- Installation instructions
- Usage examples
- Requirements
- Troubleshooting

### DR2: Code Comments
- Function purposes
- Complex algorithms explained
- Critical sections marked

### DR3: Error Messages
- What went wrong
- Why it happened
- How to fix it

---

## 🚀 DEPLOYMENT REQUIREMENTS

### DEP1: Build
- `dotnet build` must succeed
- No warnings (if possible)
- Output to bin/Release

### DEP2: Distribution
- Single .exe (if published)
- Or source code + build instructions
- Include required DLLs

### DEP3: Setup
- Create Source/ and Target/ folders
- Copy War3Net DLLs to Libs/
- Run build script

---

## 🎯 SUCCESS METRICS

The project is successful when:

1. **Functionality:**
   - ✅ 100% of test cases pass
   - ✅ No known bugs
   - ✅ All features implemented

2. **Compatibility:**
   - ✅ Works with WC3 1.27 maps
   - ✅ Preserves old format (SubVersion=null)
   - ✅ World Editor opens merged maps

3. **Reliability:**
   - ✅ No data loss
   - ✅ No corruption
   - ✅ Handles errors gracefully

4. **Usability:**
   - ✅ Clear interface
   - ✅ Helpful error messages
   - ✅ Easy to use

5. **Performance:**
   - ✅ Fast enough for practical use
   - ✅ No memory leaks
   - ✅ Efficient operations

---

## 📝 CHANGE LOG

### Version 1.0 (Current)
- Initial implementation
- Position-based category IDs
- Format preservation (SubVersion=null)
- Interactive menu
- Debug mode
- Comprehensive validation

### Future Enhancements
- Batch operations (merge multiple categories at once)
- Command-line arguments (non-interactive mode)
- Backup creation (automatic backup before save)
- Undo/redo support
- GUI version (optional)

---

**END OF OBJECTIVES & REQUIREMENTS**
