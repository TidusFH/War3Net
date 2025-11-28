# WTG Format Specification & Implementation Notes

**Last Updated:** 2025-11-16
**War3Net Version:** Latest (analyzed from source)
**Format Version:** 7 (Frozen Throne 1.27+)

## Overview

This document contains verified information about the Warcraft III Trigger (.wtg) file format, based on:
- War3Net library source code analysis
- Community specifications (HiveWorkshop, wc3c.net)
- Actual implementation testing

---

## File Structure (Format Version 7, SubVersion = null)

### Header
```
char[4]:  File signature (0x21475457 = "WTG!")
int32:    Format version (7 for Frozen Throne)
```

### Categories Section
```
int32:    Category count
Category[]: Array of TriggerCategoryDefinition
```

### Game Version
```
int32:    Game version (usually 2 for 1.27+)
```

### Variables Section
```
int32:    Variable count
Variable[]: Array of VariableDefinition
```

### Triggers Section
```
int32:    Trigger count
Trigger[]: Array of TriggerDefinition
```

---

## Data Structures

### TriggerCategoryDefinition (Format 7, SubVersion = null)
```
int32:    Id
string:   Name (null-terminated)
int32:    IsComment (0 = no, non-zero = yes) (Format 7 only)
```

**IMPORTANT:**
- ParentId is **NOT** written in 1.27 format (SubVersion = null)
- ParentId **IS** written in 1.31+ format (SubVersion = v4 or v7)

### VariableDefinition (Format 7, SubVersion = null)
```
string:   Name (null-terminated)
string:   Type (null-terminated)
int32:    Unk (always 1)
int32:    IsArray (0 = no, 1 = yes)
int32:    ArraySize (Format 7 only)
int32:    IsInitialized (0 = no, 1 = yes)
string:   InitialValue (null-terminated)
```

**IMPORTANT:**
- Id and ParentId are **NOT** written in 1.27 format
- Id and ParentId **ARE** written in 1.31+ format

### TriggerDefinition (Format 7, SubVersion = null)
```
string:   Name (null-terminated)
string:   Description (null-terminated)
int32:    IsComment (0 = no, non-zero = yes) (Format 7 only)
int32:    IsEnabled (0 = no, 1 = yes)
int32:    IsCustomTextTrigger (0 = no, 1 = yes)
int32:    IsInitiallyOff (0 = on, 1 = off)
int32:    RunOnMapInit (0 = no, 1 = yes)
int32:    ParentId (category ID)
int32:    Function count
TriggerFunction[]: Array of functions
```

**IMPORTANT:**
- Trigger Id is **NOT** written in 1.27 format
- ParentId **IS** written in 1.27 format (unlike categories!)

### TriggerFunction
```
int32:    Type (0 = event, 1 = condition, 2 = action, 3 = call)
int32:    Branch (only if this is a child function AND Branch.HasValue)
string:   Name (null-terminated)
int32:    IsEnabled (0 = no, 1 = yes)
TriggerFunctionParameter[]: Parameters (count determined by TriggerData.txt)
int32:    ChildFunctions count (Format 7 only)
TriggerFunction[]: Child functions (Format 7 only)
```

**IMPORTANT:**
- Parameter count is **NOT** written
- Parameter count is determined by looking up function name in TriggerData.txt
- Branch field is only written if Branch.HasValue (only set for child functions)

### TriggerFunctionParameter
```
int32:    Type (0 = preset, 1 = variable, 2 = function, 3 = string, -1 = invalid)
string:   Value (null-terminated)
int32:    HasFunction (0 = no, 1 = yes)
TriggerFunction: Function (only if HasFunction = 1)
int32:    HasArrayIndexer (0 = no, 1 = yes)
TriggerFunctionParameter: ArrayIndexer (only if HasArrayIndexer = 1)
```

**IMPORTANT:**
- Both HasFunction and HasArrayIndexer flags are **ALWAYS** written
- They are **NOT** mutually exclusive
- No "Unknown" field exists after Function (older specs were wrong!)

---

## String Encoding

### War3Net's WriteString Implementation

War3Net uses `writer.WriteString()` for ALL string fields, which:
1. Writes each character using `writer.Write(char)`
2. Handles surrogate pairs correctly
3. Adds null terminator (`\0`) at the end if not already present

**CRITICAL:** Do NOT use `writer.Write(byte[])` for strings!
Using byte arrays adds an implicit length prefix in some .NET implementations.

```csharp
// CORRECT - War3Net's method
writer.WriteString("Test Category");
// Output: 54 65 73 74 20 43 61 74 65 67 6F 72 79 00
//         T  e  s  t     C  a  t  e  g  o  r  y \0

// WRONG - Don't use byte array
byte[] bytes = Encoding.UTF8.GetBytes("Test");
writer.Write(bytes);  // May add length prefix!
```

---

## Boolean Encoding

### Format: 32-bit Integers

**ALL booleans in WTG format are 4-byte integers (int32):**

```csharp
// CORRECT - War3Net's method
public static void WriteBool(this BinaryWriter writer, bool b)
{
    writer.Write(b ? 1 : 0);  // Writes int32 (4 bytes)
}

// When reading:
public static bool ReadBool(this BinaryReader reader)
{
    return reader.ReadInt32().ToBool();  // Reads int32 (4 bytes)
}
```

**CRITICAL:** Do NOT use single-byte bools!
```csharp
// WRONG - Do NOT do this
writer.Write((byte)(value ? 1 : 0));  // Only 1 byte!
```

**Why 4 bytes?**
- Warcraft III was designed for 32-bit alignment
- All boolean fields use int32 for compatibility
- Writing 1-byte bools causes parse errors (read shifts by 3 bytes)

---

## Common Mistakes & Fixes

### ❌ Mistake 1: Writing Unknown Field After Function
```csharp
// WRONG - Older specs incorrectly documented this
writer.WriteBool(param.Function is not null);
if (param.Function is not null)
{
    WriteTriggerFunction(writer, param.Function, ...);
    writer.Write(0);  // ❌ This doesn't exist!
}
```

```csharp
// CORRECT - No Unknown field
writer.WriteBool(param.Function is not null);
if (param.Function is not null)
{
    WriteTriggerFunction(writer, param.Function, ...);
    // No extra field here!
}
```

### ❌ Mistake 2: Using Byte Array for Strings
```csharp
// WRONG - May add length prefix
byte[] bytes = Encoding.UTF8.GetBytes(value);
writer.Write(bytes);  // ❌ Incorrect format
```

```csharp
// CORRECT - Use WriteString extension
writer.WriteString(value);  // ✅ Proper format
```

### ❌ Mistake 3: Using 1-Byte Booleans
```csharp
// WRONG - Only writes 1 byte
writer.Write((byte)(enabled ? 1 : 0));  // ❌ Parser expects 4 bytes
```

```csharp
// CORRECT - Write 4-byte int32
writer.WriteBool(enabled);  // ✅ Writes int32 (4 bytes)
```

### ❌ Mistake 4: Writing ParentId for Categories in 1.27 Format
```csharp
// WRONG - ParentId not in 1.27 format
writer.Write(category.Id);
writer.WriteString(category.Name);
writer.WriteBool(category.IsComment);
writer.Write(category.ParentId);  // ❌ Not in 1.27!
```

```csharp
// CORRECT - No ParentId for categories in 1.27
writer.Write(category.Id);
writer.WriteString(category.Name);
writer.WriteBool(category.IsComment);
// That's it!
```

---

## Format Differences: 1.27 vs 1.31+

| Feature | 1.27 (SubVersion = null) | 1.31+ (SubVersion = v4/v7) |
|---------|-------------------------|----------------------------|
| **Category ParentId** | ❌ Not written | ✅ Written |
| **Category IsExpanded** | ❌ Not written | ✅ Written |
| **Variable Id** | ❌ Not written | ✅ Written |
| **Variable ParentId** | ❌ Not written | ✅ Written |
| **Trigger Id** | ❌ Not written | ✅ Written |
| **Trigger ParentId** | ✅ Written | ✅ Written |
| **File structure** | Flat lists | Item type headers + counts |

---

## TriggerData.txt Usage

### Purpose
TriggerData.txt contains metadata about all trigger functions, including:
- Number of parameters for each function
- Parameter types
- Return types

### Parameter Count Lookup

**CRITICAL:** Parameter count is **NOT** stored in WTG file!
You must parse TriggerData.txt to determine how many parameters each function has.

```csharp
// Example from TriggerData.txt
[TriggerEvents]
TriggerRegisterAnyUnitEventBJ=triggeraction,unitevent

// This means TriggerRegisterAnyUnitEventBJ has 2 parameters:
// 1. triggeraction
// 2. unitevent

// When reading WTG:
for (int i = 0; i < parameterCountFromTriggerData; i++)
{
    ReadParameter(reader);
}
```

### Building the Lookup Table

```csharp
Dictionary<string, int> functionParamCounts = new();

// Parse [TriggerEvents], [TriggerConditions], [TriggerActions], [TriggerCalls]
foreach (var section in sections)
{
    foreach (var (functionName, argsString) in section)
    {
        var args = argsString.Split(',')
            .Where(arg => arg != "0" && arg != "1" && arg != "nothing" && arg.Trim() != "")
            .ToArray();

        int count = args.Length;
        if (section == "TriggerCalls")
            count--;  // First arg is return type

        functionParamCounts[functionName] = count;
    }
}
```

---

## Testing & Verification

### Verification Checklist

✅ **File reads without errors in War3Net**
- No "A 32-bit bool must be 0 or 1" errors
- No "Invalid data" exceptions

✅ **File opens in World Editor without crashes**
- No "trigger invalid" errors
- No variable conflict errors

✅ **Triggers display correctly in World Editor**
- All functions appear
- All parameters have correct values
- Nested calls work properly

✅ **File can be saved from World Editor**
- No corruption on re-save
- Re-saved file is valid

### Common Parse Errors & Causes

| Error Message | Likely Cause | Fix |
|---------------|--------------|-----|
| "A 32-bit bool must be 0 or 1, but got 'X'" | Used 1-byte bool instead of 4-byte | Use `writer.WriteBool()` |
| "trigger invalid" in World Editor | Missing/extra fields in structure | Match War3Net source exactly |
| File crashes World Editor | Shifted bytes from incorrect field sizes | Verify all int32/string formats |
| Variables conflict errors | Variable section corrupted | Check WriteVariableDefinition |

---

## Implementation Checklist

When implementing WTG writer:

- [ ] Use War3Net's `WriteString()` for ALL strings
- [ ] Use War3Net's `WriteBool()` for ALL booleans (4-byte int32)
- [ ] Do NOT write Unknown field after Function parameters
- [ ] Do NOT write ParentId for categories in 1.27 format
- [ ] DO write ParentId for triggers in 1.27 format
- [ ] Do NOT write parameter counts (use TriggerData.txt)
- [ ] Write Branch field only if Branch.HasValue
- [ ] Filter out RootCategory from category list
- [ ] Write file signature (0x21475457) first
- [ ] Test with War3Net parser after writing

---

## References

- **War3Net Source:** https://github.com/Drake53/War3Net
- **HiveWorkshop Spec:** https://www.hiveworkshop.com/threads/warcraft-3-trigger-format-specification-wtg.279006/
- **wc3c.net Spec:** http://www.wc3c.net/tools/specs/

---

## Revision History

| Date | Changes |
|------|---------|
| 2025-11-16 | Initial documentation based on War3Net source analysis |
| 2025-11-16 | Confirmed 4-byte bools, removed Unknown field myth |
| 2025-11-16 | Verified string encoding (WriteString, not byte arrays) |

---

## Contact

For questions or corrections, see:
- War3Net GitHub Issues
- WTGMerger repository
