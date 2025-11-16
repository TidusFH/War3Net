# Variable Copy Fix - November 2025

## Problem Identified

The variable detection system in `CopyMissingVariables()` was only copying variables that were **explicitly referenced** in trigger function parameters with type `TriggerFunctionParameterType.Variable`.

This caused global variables to not be copied when:
- Variables were defined in source map but not directly referenced as function parameters
- Variables were referenced through other means (preset values, strings, custom script)
- Global variables were needed for map initialization even if not used in copied triggers

## Root Cause

**File**: `Program.cs`, lines 2739-2752

```csharp
if (param.Type == TriggerFunctionParameterType.Variable)
{
    var varName = GetVariableNameFromParameter(param, mapTriggers);
    if (!string.IsNullOrEmpty(varName))
    {
        usedVariables.Add(varName);
    }
}
```

This only detects variables that are:
- Direct function parameters
- Of type `TriggerFunctionParameterType.Variable` (type 1)

**Missing detection for:**
- Global variables not referenced in parameters
- Variables referenced in preset values (type 0)
- Variables referenced in string values (type 3)

## Solution Implemented

Modified `CopyMissingVariables()` to accept a `bool copyAllVariables` parameter:

```csharp
static void CopyMissingVariables(MapTriggers source, MapTriggers target,
    List<TriggerDefinition> triggers, bool copyAllVariables = true)
```

**Default behavior (`copyAllVariables = true`):**
- Copies **ALL** variables from source map to target map
- Safer for maps with global variables
- Prevents "trigger data missing or invalid" errors
- Handles type conflicts by renaming variables

**Optional behavior (`copyAllVariables = false`):**
- Only copies variables explicitly referenced in trigger parameters
- More selective, but may miss global variables

## Changes Made

### 1. Modified `CopyMissingVariables()` (line 2510)

**Before:**
```csharp
static void CopyMissingVariables(MapTriggers source, MapTriggers target, List<TriggerDefinition> triggers)
{
    // Only scanned trigger parameters for variables
}
```

**After:**
```csharp
static void CopyMissingVariables(MapTriggers source, MapTriggers target,
    List<TriggerDefinition> triggers, bool copyAllVariables = true)
{
    if (copyAllVariables)
    {
        // Copy ALL variables from source map
        foreach (var sourceVar in source.Variables)
        {
            usedVariables.Add(sourceVar.Name);
        }
    }
    else
    {
        // Only copy variables referenced in trigger parameters (old behavior)
    }
}
```

### 2. Impact on Existing Calls

Both existing calls now use `copyAllVariables = true` by default:

**Line 1903** - `CopySpecificTriggers()`:
```csharp
CopyMissingVariables(source, target, triggersToCopy);
// Now copies ALL variables from source
```

**Line 2084** - (other merge operation):
```csharp
CopyMissingVariables(source, target, sourceCategoryTriggers);
// Now copies ALL variables from source
```

## Benefits

1. **Prevents Missing Variable Errors**: Global variables are always copied
2. **Handles Type Conflicts**: Automatic variable renaming if types differ
3. **Maintains Backward Compatibility**: Old behavior available via parameter
4. **Better Diagnostics**: Enhanced console output shows what's being copied

## Testing Recommendations

1. Test with maps that have global variables
2. Verify "Trigger data missing or invalid" error is resolved
3. Test with maps that have variable name conflicts
4. Verify automatic renaming works correctly
5. Test with DEBUG_MODE enabled to see full variable copy log

## Console Output Changes

**Before fix:**
```
  ℹ No variables used by these triggers
```

**After fix (with global variables):**
```
  Copying ALL 15 variable(s) from source map:
    + Copied: 'udg_HeroLevel' (integer)
    + Copied: 'udg_PlayerGold' (integer array)
    + Copied: 'udg_GameMode' (string)
    ...
  ✓ Copied 15 variable(s), renamed 0 variable(s)
```

## Related Files

- **Program.cs** - Main variable copy logic
- **WTG-FORMAT-SPECIFICATION.md** - Variable format documentation
- **WTG-DEBUGGING-GUIDE.md** - Troubleshooting guide for variable issues

## Future Enhancements

Potential improvements for better variable detection:

1. **Smart Detection**: Scan preset and string parameters for variable names
2. **Dependency Analysis**: Detect variables used by other variables
3. **User Option**: Add menu option to toggle copy behavior
4. **Validation**: Verify all copied variables are valid before writing

## Date

**Created**: November 16, 2025
**Issue**: Global variables not being copied during trigger merge
**Status**: ✅ Fixed
