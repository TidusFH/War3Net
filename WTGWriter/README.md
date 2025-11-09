# Custom WTG Writer

A custom binary writer for Warcraft 3 WTG (trigger) files that produces correct output.

## Why This Exists

War3Net's `BinaryWriter.Write(MapTriggers)` extension is fundamentally broken:
- Writes wrong format version (`-2147483644` instead of `7`)
- Writes zeros instead of category/trigger data
- Produces files that World Editor can't read

## What This Does

This custom writer:
1. Reads `MapTriggers` data structures from War3Net (which works)
2. Writes correct WTG binary format (which War3Net fails at)
3. Based on analysis of working WTG files via hex dumps

## Binary Format (Reverse Engineered)

Based on diagnostic analysis of working files:

```
Offset | Size | Description
-------|------|-------------
0x00   | 4    | File ID: "WTG!"
0x04   | 4    | Format Version (int32, usually 7)
0x08   | 4    | Category count (int32)
0x0C   | 4    | SubVersion (int32, 0 if null)
...    | var  | Category definitions
...    | 4    | Variable count (int32)
...    | var  | Variable definitions
...    | 4    | Trigger count (int32)
...    | var  | Trigger definitions
```

### Category Definition
```
- Name (null-terminated string)
- Unknown field 1 (int32)
- Unknown field 2 (int32)
- Category ID (int32)
- Parent ID (int32, -1 for root)
```

### Variable Definition
```
- Name (null-terminated string)
- Type (null-terminated string)
- Unknown (int32, seems to be 1)
- IsArray (int32, 0 or 1)
- ArraySize (int32)
- IsInitialized (int32, 0 or 1)
- InitialValue (null-terminated string)
- Variable ID (int32)
- Parent ID (int32)
```

### Trigger Definition
```
- Name (null-terminated string)
- Description (null-terminated string)
- IsEnabled (int32, 0 or 1)
- IsCustomScript (int32, 0 or 1)
- IsInitiallyOn (int32, 0 or 1)
- RunOnMapInit (int32, 0 or 1)
- Trigger ID (int32)
- Category ID/Parent ID (int32)
- Event count (int32)
  - Event functions (recursive structure)
- Condition count (int32)
  - Condition functions (recursive structure)
- Action count (int32)
  - Action functions (recursive structure)
```

## Usage

```csharp
using WTGWriter;

// Read triggers with War3Net (works fine)
MapTriggers triggers = ReadMapTriggersFromSomewhere();

// Write with custom writer (works correctly!)
using var stream = File.Create("war3map.wtg");
CustomWTGWriter.Write(stream, triggers);
```

## Integration with WTGMerger

The WTGMerger tool will be updated to use this custom writer instead of War3Net's broken one.

## Notes

Some fields are marked "Unknown" because the exact purpose isn't documented. The values are based on observation of working WTG files. They appear to work correctly when set to the values shown above.

## Testing

Compare output with diagnostic tool:
```bash
# Before (War3Net writer)
Format Version: -2147483644  ← WRONG!

# After (Custom writer)
Format Version: 7  ← CORRECT!
```

The output should match the format of working WTG files byte-for-byte.
