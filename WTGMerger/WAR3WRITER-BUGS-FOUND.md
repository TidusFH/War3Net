# Critical Bugs Found in War3Writer.cs

## Analysis Date
2025-11-10

## Summary
War3Writer.cs has **6 critical bugs** that will cause severe WTG file corruption. These bugs were found by comparing against War3Net's source code.

---

## Bug #1: TriggerFunctionParameter - Missing bool flags (CRITICAL)

**Location:** Line 386-403 in War3Writer.cs

**War3Net's Correct Implementation:**
```csharp
// TriggerFunctionParameter.cs line 50-60
writer.Write((int)Type);
writer.WriteString(Value);

writer.WriteBool(Function is not null);  // ← Always writes bool flag!
Function?.WriteTo(writer, formatVersion, subVersion);

writer.WriteBool(ArrayIndexer is not null);  // ← Always writes bool flag!
ArrayIndexer?.WriteTo(writer, formatVersion, subVersion);
```

**War3Writer's WRONG Implementation:**
```csharp
writer.Write((int)param.Type);
writer.WriteString(param.Value ?? string.Empty);

// Subparameters
if (param.Type == TriggerFunctionParameterType.Function)  // ← Missing bool!
{
    if (param.Function != null)
    {
        WriteTriggerFunction(writer, param.Function, formatVersion, subVersion);
    }
}
else if (param.ArrayIndexer != null)  // ← Missing bool! And wrong else-if!
{
    WriteTriggerFunctionParameter(writer, param.ArrayIndexer, formatVersion, subVersion);
}
```

**Issues:**
1. ❌ Doesn't write bool flag before Function
2. ❌ Doesn't write bool flag before ArrayIndexer
3. ❌ Uses `else if` - should write BOTH flags separately (not mutually exclusive)
4. ❌ Doesn't handle null Function/ArrayIndexer properly

**Impact:** File corruption, World Editor will crash or fail to load triggers

---

## Bug #2: TriggerFunction - Missing Branch field (CRITICAL)

**Location:** Line 349-381 in War3Writer.cs

**War3Net's Correct Implementation:**
```csharp
// TriggerFunction.cs line 64-68
writer.Write((int)Type);
if (Branch.HasValue)
{
    writer.Write(Branch.Value);  // ← Writes Branch for child functions!
}
```

**War3Writer's WRONG Implementation:**
```csharp
writer.Write((int)function.Type);
// ← Missing Branch handling completely!
```

**Impact:** Child functions (nested if-then-else) will be corrupted

---

## Bug #3: TriggerFunction - Wrong Name format handling (CRITICAL)

**Location:** Line 354-356 in War3Writer.cs

**War3Net's Correct Implementation:**
```csharp
// TriggerFunction.cs line 70
writer.WriteString(Name);  // ← ALWAYS written, no format check!
```

**War3Writer's WRONG Implementation:**
```csharp
if (formatVersion >= MapTriggersFormatVersion.v7)
{
    writer.WriteString(function.Name ?? string.Empty);  // ← WRONG! Conditional!
}
```

**Impact:** In format version < 7, Name won't be written, causing corruption

---

## Bug #4: TriggerFunction - Parameter count should NOT be written (CRITICAL)

**Location:** Line 362-363 in War3Writer.cs

**War3Net's Correct Implementation:**
```csharp
// TriggerFunction.cs line 73-76
foreach (var parameter in Parameters)
{
    writer.Write(parameter, formatVersion, subVersion);
}
// ← NO count written! Uses TriggerData to determine count on read!
```

**War3Writer's WRONG Implementation:**
```csharp
writer.Write(function.Parameters.Count);  // ← WRONG! Extra data!
foreach (var param in function.Parameters)
{
    WriteTriggerFunctionParameter(writer, param, formatVersion, subVersion);
}
```

**Impact:** MAJOR corruption - extra int32 written, all subsequent data offset incorrectly

---

## Bug #5: TriggerFunction - Wrong child function condition (CRITICAL)

**Location:** Line 368-378 in War3Writer.cs

**War3Net's Correct Implementation:**
```csharp
// TriggerFunction.cs line 78-85
if (formatVersion >= MapTriggersFormatVersion.v7)  // ← Checks FORMAT VERSION!
{
    writer.Write(ChildFunctions.Count);
    foreach (var childFunction in ChildFunctions)
    {
        writer.Write(childFunction, formatVersion, subVersion);
    }
}
```

**War3Writer's WRONG Implementation:**
```csharp
if (function.Type == TriggerFunctionType.Event || function.Type == TriggerFunctionType.Condition)  // ← WRONG! Checks TYPE!
{
    writer.Write(0);
}
else
{
    writer.Write(function.ChildFunctions.Count);
    foreach (var child in function.ChildFunctions)
    {
        WriteTriggerFunction(writer, child, formatVersion, subVersion);
    }
}
```

**Impact:** In format < v7, child functions written incorrectly; Actions with no children write 0 incorrectly

---

## Bug #6: TriggerFunction - Recursive call doesn't handle Branch

**Location:** Line 378 in War3Writer.cs

**Issue:** When recursively calling WriteTriggerFunction for child functions, the method doesn't handle the Branch field that child functions require.

**Impact:** Child functions missing critical Branch data

---

## Severity Assessment

**Overall Severity: CRITICAL - DO NOT USE War3Writer.cs as-is**

| Bug | Severity | Data Loss Risk |
|-----|----------|----------------|
| #1 - Missing bool flags | CRITICAL | 100% - File unreadable |
| #2 - Missing Branch | CRITICAL | 100% - Child functions corrupt |
| #3 - Wrong Name handling | HIGH | 80% - Format < v7 corrupt |
| #4 - Parameter count | CRITICAL | 100% - All offsets wrong |
| #5 - Wrong child condition | CRITICAL | 100% - Format < v7 corrupt |
| #6 - Branch in recursion | CRITICAL | 100% - Child functions corrupt |

---

## Required Fixes

All bugs must be fixed before War3Writer can be used in production. See WAR3WRITER-FIXES.md for corrected implementation.

---

## How Bugs Were Found

1. Compared War3Writer.cs against War3Net source code
2. Line-by-line analysis of serialization logic
3. Identified mismatches in:
   - Bool flag writes
   - Format version checks
   - Data count writes
   - Conditional logic

## Recommendation

**DO NOT integrate War3Writer.cs into WTGMerger until ALL bugs are fixed.**

Continue using War3Net's internal WriteTo method via reflection until War3Writer is corrected and tested.
