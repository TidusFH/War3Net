# War3Diagnostic.cs Enhancements

## Current Implementation Review

War3Diagnostic.cs is well-structured but has room for improvement. Below are the enhancement opportunities identified.

---

## Enhancement #1: Add TriggerData Validation

**Current:** Diagnostic doesn't validate trigger function names against TriggerData

**Enhancement:** Add validation to check:
- Unknown trigger function names (Events/Conditions/Actions/Calls)
- Parameter count mismatches
- Invalid function types

**Benefits:** Detect corrupted trigger functions early

---

## Enhancement #2: Expand Hex Dump Coverage

**Current:** Only shows first 512 bytes
**Issue:** Important differences might occur after byte 512

**Enhancement:**
- Add configurable hex dump size
- Add hex dump of specific sections (variables, categories, triggers)
- Add hex dump around detected differences

**Benefits:** Better visibility into file structure

---

## Enhancement #3: Add File Order Analysis

**Current:** Shows hierarchy but not file order
**Issue:** In WC3 1.27 format, file order matters for World Editor display

**Enhancement:**
- Show exact order of items in TriggerItems list
- Highlight when categories appear after triggers (causes visual nesting in WE)
- Show index position of each item

**Benefits:** Understand visual nesting issues in World Editor

---

## Enhancement #4: Add ID Collision Detection

**Current:** Only checks for duplicate variable IDs
**Issue:** Could have category/trigger ID collisions too

**Enhancement:**
- Check for duplicate category IDs
- Check for duplicate trigger IDs
- Check for ID collisions across different item types
- Warn if IDs are sequential or have gaps

**Benefits:** Detect ID management issues

---

## Enhancement #5: Add ParentId Distribution Statistics

**Current:** Shows ParentId but no statistics
**Enhancement:**
- Show histogram of ParentId values
- Detect if all triggers have same ParentId (nesting issue)
- Show ParentId=-1 vs ParentId=0 distinction
- Flag unusual ParentId patterns

**Benefits:** Quickly identify nesting and orphan issues

---

## Enhancement #6: Add TriggerFunction Structure Validation

**Current:** Doesn't analyze trigger function structure
**Enhancement:**
- Validate function parameter counts
- Check for null/empty function names
- Validate ChildFunction structure (depth, count)
- Check for circular references in functions
- Validate parameter types match their usage

**Benefits:** Detect function corruption

---

## Enhancement #7: Add GameVersion Consistency Check

**Current:** Shows GameVersion but doesn't validate
**Enhancement:**
- Check GameVersion is within valid range
- Warn if GameVersion doesn't match format version
- Compare GameVersion across source/target/merged

**Benefits:** Detect version compatibility issues

---

## Enhancement #8: Add Category Tree Depth Analysis

**Current:** Shows tree but no depth analysis
**Enhancement:**
- Calculate maximum category nesting depth
- Warn if depth exceeds World Editor limits
- Show average depth
- Flag categories with no triggers

**Benefits:** Detect over-nested structures

---

## Enhancement #9: Add Binary Section Analysis

**Current:** Basic hex dump and byte-by-byte comparison
**Enhancement:**
- Parse WTG sections and show byte ranges
- Hex dump each section separately:
  - Header (signature, version)
  - Categories section
  - Variables section
  - Triggers section
- Show size of each section
- Compare section sizes across files

**Benefits:** Pinpoint exactly where corruption occurs

---

## Enhancement #10: Add Corruption Pattern Detection

**Current:** Generic error detection
**Enhancement:** Detect specific corruption patterns:
- All triggers having same ParentId (common bug)
- Categories appearing after triggers in file order (WE nesting)
- Missing category for common ParentId values
- Triggers with ParentId=234 (your specific issue)
- Variable names that are empty or invalid
- Duplicate trigger names (not IDs, but names)

**Benefits:** Quickly identify known issues

---

## Enhancement #11: Add TriggerItemCounts Validation

**Current:** Shows TriggerItemCounts but doesn't validate
**Enhancement:**
- Validate counts match actual items
- Compare expected vs actual for each type
- Flag if RootCategory count is wrong
- Check if counts are consistent with SubVersion

**Benefits:** Detect metadata corruption

---

## Enhancement #12: Add String Encoding Validation

**Current:** Assumes all strings are valid
**Enhancement:**
- Check for invalid UTF-8 sequences
- Detect null bytes in strings
- Warn about very long strings
- Check for control characters

**Benefits:** Detect string corruption

---

## Priority Recommendations

**High Priority (Implement First):**
1. Enhancement #3 - File Order Analysis (critical for 1.27 format)
2. Enhancement #5 - ParentId Distribution (critical for diagnosing your issues)
3. Enhancement #9 - Binary Section Analysis (critical for debugging)
4. Enhancement #10 - Corruption Pattern Detection (practical)

**Medium Priority:**
5. Enhancement #2 - Expanded Hex Dumps
6. Enhancement #4 - ID Collision Detection
7. Enhancement #11 - TriggerItemCounts Validation

**Low Priority (Nice to Have):**
8. Enhancement #1 - TriggerData Validation (requires TriggerData loading)
9. Enhancement #6 - Function Structure Validation (complex)
10. Enhancement #7 - GameVersion Consistency
11. Enhancement #8 - Tree Depth Analysis
12. Enhancement #12 - String Encoding Validation

---

## Implementation Notes

- All enhancements should be optional (controlled by flags)
- Don't make diagnostic slower - only run expensive checks if requested
- Keep existing functionality working
- Add clear headers for each new diagnostic section
