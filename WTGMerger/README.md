# WTG Trigger Merger

A simple console application to merge triggers between Warcraft 3 `.wtg` files using the War3Net libraries.

## What This Does

This tool allows you to:
- Read raw `.wtg` (trigger) files directly
- List all trigger categories and their contents
- Copy specific trigger categories from one `.wtg` file to another
- Properly handle trigger data using War3Net's built-in parsers

## Why Use War3Net DLLs?

The War3Net libraries provide:
- ✅ **Proper WTG parsing**: Handles all WTG format versions (v4, v7, and newer)
- ✅ **Data validation**: Validates file headers and structure
- ✅ **Complete trigger support**: Preserves all trigger properties, events, conditions, and actions
- ✅ **Type safety**: Strong typing for all trigger components
- ✅ **Deep copying**: Properly copies nested functions and parameters

## Setup

1. Make sure you have the .NET SDK installed (8.0 or later)
2. The War3Net DLLs are already referenced from the `../Libs/` folder

## Build

```bash
cd WTGMerger
dotnet build
```

## Usage

### Interactive Mode

```bash
dotnet run
```

The program will:
1. Read the source WTG file from `../Source/war3map.wtg`
2. Read the target WTG file from `../Target/war3map.wtg`
3. Display all categories from both files
4. Ask you which category to copy
5. Merge the selected category and save to `../Target/war3map_merged.wtg`

### Modify Source Code for Custom Paths

Edit `Program.cs` and change these lines:

```csharp
var sourcePath = "../Source/war3map.wtg";
var targetPath = "../Target/war3map.wtg";
var outputPath = "../Target/war3map_merged.wtg";
```

## How It Works

### 1. Reading WTG Files

```csharp
using var fileStream = File.OpenRead(filePath);
using var reader = new BinaryReader(fileStream);
return new MapTriggers(reader, TriggerData.Default);
```

The `MapTriggers` constructor from War3Net.Build.Core:
- Validates the file header signature (`WTG!`)
- Detects the format version (v4, v7, or newer)
- Parses all trigger items (categories and triggers)
- Validates trigger data structure

### 2. Merging Categories

The merge process:
1. Finds the source category by name
2. Gets all triggers belonging to that category
3. Removes the category from target if it already exists
4. Creates a new category in the target
5. Deep copies all triggers with new IDs
6. Preserves all trigger properties, functions, and parameters

### 3. Writing WTG Files

```csharp
using var fileStream = File.Create(filePath);
using var writer = new BinaryWriter(fileStream);
triggers.WriteTo(writer);
```

## Common Issues and Solutions

### "Trigger data invalid" Error

**Cause**: This usually happens when:
- The WTG file is corrupted
- The file format version is not supported
- The file is not actually a WTG file

**Solution**: The War3Net parser includes proper validation:
```csharp
// From War3Net.Build.Core
if (header != FileFormatSignature)
{
    throw new InvalidDataException("Expected file header signature at the start of .wtg file.");
}
```

### File Version Not Supported

**Supported versions**:
- Format v4 (Reign of Chaos)
- Format v7 (The Frozen Throne)
- Sub-versions for v1.31+, v1.32+, v1.33+, v1.36+

### Variables Not Copied

This tool only copies **trigger categories and their triggers**. It does NOT copy:
- Global variables (these remain unchanged in the target)
- Custom script code
- Trigger strings

If you need to copy variables, you can extend the code to merge the `triggers.Variables` list.

## Example Output

```
=== War3Net WTG Trigger Merger ===

Reading source WTG: ../Source/war3map.wtg
✓ Source loaded: 45 items, 12 variables

Reading target WTG: ../Target/war3map.wtg
✓ Target loaded: 32 items, 8 variables

=== Source Categories ===
  - Initialization (3 triggers)
  - Player Actions (7 triggers)
  - Game Logic (15 triggers)

=== Target Categories (Before Merge) ===
  - Setup (2 triggers)
  - Quests (5 triggers)

Enter category name to copy: Game Logic

Merging category 'Game Logic' from source to target...
  Found 15 triggers in source category
  Added category 'Game Logic' to target
    + Copied trigger: Initialize Variables
    + Copied trigger: Player Enters Region
    + Copied trigger: Unit Dies
    ... (12 more)

Saving merged WTG to: ../Target/war3map_merged.wtg
✓ Merge complete!

=== Target Categories (After Merge) ===
  - Setup (2 triggers)
  - Quests (5 triggers)
  - Game Logic (15 triggers)
```

## Advantages Over Manual Binary Manipulation

### ❌ Manual Approach (What Fails)

```csharp
// Reading raw bytes - NO validation
byte[] data = File.ReadAllBytes("war3map.wtg");

// Manually parsing - error prone
int version = BitConverter.ToInt32(data, 4);
// ... more manual parsing
// ⚠️ Easy to get offsets wrong
// ⚠️ No validation
// ⚠️ Doesn't handle different format versions
```

### ✅ War3Net Approach (What Works)

```csharp
// Proper parsing with validation
using var reader = new BinaryReader(fileStream);
var triggers = new MapTriggers(reader, TriggerData.Default);

// Strongly typed access
foreach (var category in triggers.TriggerItems.OfType<TriggerCategoryDefinition>())
{
    Console.WriteLine(category.Name);
}

// Proper serialization
triggers.WriteTo(writer);
```

## Extending the Tool

### Copy Multiple Categories

```csharp
var categoriesToCopy = new[] { "Initialization", "Game Logic", "Quests" };

foreach (var categoryName in categoriesToCopy)
{
    MergeCategory(sourceTriggers, targetTriggers, categoryName);
}
```

### Copy Variables Too

```csharp
// Merge variables from source to target
foreach (var sourceVar in sourceTriggers.Variables)
{
    // Check if variable already exists
    var exists = targetTriggers.Variables
        .Any(v => v.Name == sourceVar.Name);

    if (!exists)
    {
        // Create a copy with proper structure
        var newVar = new VariableDefinition
        {
            Name = sourceVar.Name,
            Type = sourceVar.Type,
            IsArray = sourceVar.IsArray,
            ArraySize = sourceVar.ArraySize,
            IsInitialized = sourceVar.IsInitialized,
            InitialValue = sourceVar.InitialValue
        };

        targetTriggers.Variables.Add(newVar);
    }
}
```

### Work with MPQ Archives

If you have `.w3x` or `.w3m` files instead of raw `.wtg`:

```csharp
using War3Net.IO.Mpq;

using var archive = MpqArchive.Open("map.w3x", true);
using var triggerStream = archive.OpenFile("war3map.wtg");
using var reader = new BinaryReader(triggerStream);

var triggers = new MapTriggers(reader, TriggerData.Default);
```

## References

- [War3Net GitHub](https://github.com/Drake53/War3Net)
- [War3Net.Build.Core Documentation](https://github.com/Drake53/War3Net/tree/master/src/War3Net.Build.Core)
- [WTG File Format Specification](https://github.com/stijnherfst/HiveWE/wiki/war3map.wtg-Trigger-File-Format)

## License

This tool uses the War3Net libraries which are licensed under the MIT License.
