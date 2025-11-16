# Variable Reference Debugging Guide

## Problem: Red Gear and Blank Variable Sections

When triggers show a **red gear icon** in World Editor and variable sections are **blank**, this means the trigger's function parameters have **broken variable references**.

### Why This Happens

In WC3 trigger files, variable references are stored in function parameter `Value` fields as either:
1. **Variable NAME** (string): `"Caster"`, `"udg_HeroLevel"`, etc.
2. **Variable ID** (string number): `"0"`, `"15"`, `"204"`, etc.

When copying triggers between maps:
- ❌ If parameters store **variable IDs**, those IDs become invalid in the target map
- ✅ If parameters store **variable NAMEs**, they should work after copying variables

## Diagnostic Steps

### Step 1: Enable DEBUG Mode

1. Run WTGMerger
2. Choose option `d` to toggle DEBUG mode
3. Verify you see: `DEBUG mode: ON`

### Step 2: Re-Run the Merge

1. Choose option `5` (Copy Specific Triggers)
2. Enter source category, trigger name, and destination category
3. **Watch the console output carefully**

### Step 3: Check Variable Detection Output

Look for these DEBUG messages:

```
[DEBUG] ═══ CopyMissingVariables START ═══
[DEBUG] Analyzing 1 trigger(s)
[DEBUG] Copy ALL variables: False
[DEBUG] Scanning trigger: Absorb the Soul
[DEBUG]   GetVariablesUsedByTrigger: Absorb the Soul (X functions)
[DEBUG]     Function: Action - SetVariable
[DEBUG]       Param: Type=Variable, Value='Caster', HasFunction=False, HasArrayIndexer=False
[DEBUG]       >>> VARIABLE DETECTED: 'Caster' (from param.Value='Caster')
```

### Step 4: Identify the Issue

#### ✅ **Good Output** (Variables Detected):
```
[DEBUG]       >>> VARIABLE DETECTED: 'Caster' (from param.Value='Caster')
[DEBUG]       >>> VARIABLE DETECTED: 'SpellPoint' (from param.Value='SpellPoint')
[DEBUG]   Total variables collected: 2

  Analyzing 2 variable(s) used by triggers:
    + Copied: 'Caster' (unit)
    + Copied: 'SpellPoint' (location)

  ✓ Copied 2 variable(s), renamed 0 variable(s)
```
**Result**: Variables should work in World Editor

---

#### ❌ **Bad Output** (No Variables Detected):
```
[DEBUG]   GetVariablesUsedByTrigger: Absorb the Soul (3 functions)
[DEBUG]     Function: Action - SetUnitPosition
[DEBUG]       Param: Type=Preset, Value='...', HasFunction=True, HasArrayIndexer=False
[DEBUG]       Param: Type=String, Value='...', HasFunction=False, HasArrayIndexer=False
[DEBUG]   Total variables collected: 0

  ℹ No variables used by these triggers
```
**Result**: Trigger uses NO variables (might be okay, or detection is broken)

---

#### ⚠️ **Warning Output** (Unresolved Variables):
```
[DEBUG]       Param: Type=Variable, Value='0', HasFunction=False, HasArrayIndexer=False
[DEBUG]       !!! UNRESOLVED VARIABLE: Type=Variable but couldn't extract name from Value='0'
```
**Result**: **THIS IS THE BUG!** Parameter has Type=Variable but Value is an ID number that doesn't match any variable in the source map.

## Common Issues and Solutions

### Issue 1: Parameters Store Variable IDs Instead of Names

**Symptoms:**
```
[DEBUG]       !!! UNRESOLVED VARIABLE: Type=Variable but couldn't extract name from Value='0'
```

**Cause**: The source map was created with BetterTriggers or another tool that stores variable references by ID instead of name.

**Solution**:
1. Open source map in World Editor
2. Re-save the trigger
3. Try the merge again
4. OR: Manually create the trigger in the target map

---

### Issue 2: Variables Not in Source Map

**Symptoms:**
```
[DEBUG]       >>> VARIABLE DETECTED: 'SomeVar'
    ⚠ Warning: Variable 'SomeVar' not found in source map
```

**Cause**: Trigger references a variable that doesn't exist in the source map (corrupted trigger).

**Solution**:
1. Open source map in World Editor
2. Check the trigger for broken references
3. Fix the trigger and re-save
4. Try the merge again

---

### Issue 3: Wrong Parameter Type

**Symptoms**:
```
[DEBUG]       Param: Type=Preset, Value='Caster', ...
```

**Cause**: Parameter should be Type=Variable but is marked as Type=Preset.

**Solution**: The trigger is corrupted. Re-create it in World Editor.

---

### Issue 4: copyAllVariables=True Copying Too Many

**Symptoms**:
```
  Copying ALL 89 variable(s) from source map:
    + Copied: 'all_point' (location)
    + Copied: 'RandomizeSpels' (integer)
    ...
```
But trigger only uses 2-3 variables.

**Cause**: `copyAllVariables=True` copies every variable from the entire source map.

**Solution**: Now fixed - default is `copyAllVariables=False` (selective copying).

---

## Manual Workaround

If variable detection fails completely:

1. **Option A**: Copy variables manually in World Editor
   - Open target map in WE
   - Create the needed variables manually
   - Then merge the trigger

2. **Option B**: Use copyAllVariables=True
   - Modify code to set `copyAllVariables: true` in CopySpecificTriggers call
   - Copies all variables from source (might include extras)

3. **Option C**: Export to standalone .wtg and import in WE
   - Use extraction feature to create standalone trigger file
   - Import the .wtg in World Editor directly
   - WE will handle variable references

---

## Understanding Parameter Types

From `TriggerFunctionParameterType.cs`:

| Type | Value | Description |
|------|-------|-------------|
| Preset | 0 | Constant value or function call |
| Variable | 1 | Reference to a variable |
| Function | 2 | Nested function call |
| String | 3 | String literal |
| Undefined | -1 | Invalid/corrupted |

**For variable references**:
- `Type` MUST be `1` (Variable)
- `Value` should contain variable name (best) or valid ID (risky)

---

## DEBUG Output Example

Here's what complete DEBUG output looks like for a trigger with 2 variables:

```
[DEBUG] ═══ CopyMissingVariables START ═══
[DEBUG] Analyzing 1 trigger(s)
[DEBUG] Copy ALL variables: False
[DEBUG] Scanning trigger: Absorb the Soul
[DEBUG]   GetVariablesUsedByTrigger: Absorb the Soul (3 functions)
[DEBUG]     Function: Event - UnitSpellEffect
[DEBUG]       Param: Type=Preset, Value='', HasFunction=False, HasArrayIndexer=False
[DEBUG]     Function: Condition - CompareSpellBeingCast
[DEBUG]       Param: Type=Function, Value='', HasFunction=True, HasArrayIndexer=False
[DEBUG]       Param: Type=Preset, Value='A001', HasFunction=False, HasArrayIndexer=False
[DEBUG]     Function: Action - SetVariable
[DEBUG]       Param: Type=Variable, Value='Caster', HasFunction=False, HasArrayIndexer=False
[DEBUG]       >>> VARIABLE DETECTED: 'Caster' (from param.Value='Caster')
[DEBUG]       Param: Type=Function, Value='', HasFunction=True, HasArrayIndexer=False
[DEBUG]     Function: Action - SetVariable
[DEBUG]       Param: Type=Variable, Value='SpellPoint', HasFunction=False, HasArrayIndexer=False
[DEBUG]       >>> VARIABLE DETECTED: 'SpellPoint' (from param.Value='SpellPoint')
[DEBUG]       Param: Type=Function, Value='', HasFunction=True, HasArrayIndexer=False
[DEBUG]   Total variables collected: 2

  Analyzing 2 variable(s) used by triggers:
    + Copied: 'Caster' (unit)
    + Copied: 'SpellPoint' (location)

  ✓ Copied 2 variable(s), renamed 0 variable(s)
[DEBUG] ═══ CopyMissingVariables END ═══
```

---

## Next Steps

1. **Pull latest code** from repository
2. **Enable DEBUG mode**
3. **Re-run the merge**
4. **Copy ALL console output** and share it
5. **We'll diagnose** the exact issue from the DEBUG logs

The DEBUG output will show us:
- Which variables the trigger is trying to use
- Whether parameter Values are names or IDs
- Whether variables exist in the source map
- Why variable references are breaking

This will allow us to create a targeted fix for your specific issue.
