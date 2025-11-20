# War3Net Implementation Details

## Overview

War3Net is a .NET library for reading and writing Warcraft 3 file formats. This document details specific implementation behaviors discovered during WTGMerger development.

## Reading WC3 1.27 Trigger Files

### MapTriggers.ReadFrom() Implementation

**File**: `src/War3Net.Build.Core/Serialization/Binary/Script/MapTriggers.cs`

```csharp
internal void ReadFrom(BinaryReader reader, TriggerData triggerData)
{
    var header = reader.ReadInt32();
    if (header != FileFormatSignature)
        throw new InvalidDataException("Invalid file header");

    var version = reader.ReadInt32();

    if (Enum.IsDefined(typeof(MapTriggersFormatVersion), version))
    {
        // This is 1.27 format
        FormatVersion = (MapTriggersFormatVersion)version;
        SubVersion = null;  // ← Indicates 1.27 format

        // Read all categories
        nint triggerCategoryDefinitionCount = reader.ReadInt32();
        for (nint i = 0; i < triggerCategoryDefinitionCount; i++)
        {
            TriggerItems.Add(reader.ReadTriggerCategoryDefinition(
                TriggerItemType.Category, triggerData, FormatVersion, SubVersion));
        }

        // Read game version
        GameVersion = reader.ReadInt32();

        // Read all variables
        nint variableDefinitionCount = reader.ReadInt32();
        for (nint i = 0; i < variableDefinitionCount; i++)
        {
            Variables.Add(reader.ReadVariableDefinition(
                triggerData, FormatVersion, SubVersion));
        }

        // Read all triggers
        nint triggerDefinitionCount = reader.ReadInt32();
        for (nint i = 0; i < triggerDefinitionCount; i++)
        {
            TriggerItems.Add(reader.ReadTriggerDefinition(
                TriggerItemType.Gui, triggerData, FormatVersion, SubVersion));
        }
    }
    else if (Enum.IsDefined(typeof(MapTriggersSubVersion), version))
    {
        // This is Reforged or newer format
        // ... different parsing logic ...
    }
}
```

### TriggerCategoryDefinition.ReadFrom() Implementation

**File**: `src/War3Net.Build.Core/Serialization/Binary/Script/TriggerCategoryDefinition.cs`

```csharp
internal void ReadFrom(BinaryReader reader, TriggerData triggerData,
                      MapTriggersFormatVersion formatVersion,
                      MapTriggersSubVersion? subVersion)
{
    Id = reader.ReadInt32();           // ← READ from file
    Name = reader.ReadChars();          // ← READ from file

    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        IsComment = reader.ReadBool();  // ← READ from file
    }

    if (subVersion is not null)
    {
        // Only for newer formats (Reforged)
        IsExpanded = reader.ReadBool();
        ParentId = reader.ReadInt32();
    }
    // For 1.27 (subVersion == null):
    // ParentId is NOT read - remains at default value (0)
    // IsExpanded is NOT read - remains at default value
}
```

**Key Points:**
- `Id` is READ from file (can be non-sequential!)
- `ParentId` is NOT read for 1.27 format (defaults to 0)
- `IsComment` is READ for v7+ formats

### TriggerDefinition.ReadFrom() Implementation

**File**: `src/War3Net.Build.Core/Serialization/Binary/Script/TriggerDefinition.cs`

```csharp
internal void ReadFrom(BinaryReader reader, TriggerData triggerData,
                      MapTriggersFormatVersion formatVersion,
                      MapTriggersSubVersion? subVersion)
{
    Name = reader.ReadChars();              // ← READ from file
    Description = reader.ReadChars();       // ← READ from file

    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        IsComment = reader.ReadBool();      // ← READ from file
    }

    if (subVersion is not null)
    {
        // Only for newer formats
        Id = reader.ReadInt32();
    }
    // For 1.27 (subVersion == null):
    // Id is NOT read - remains at default value (0)

    IsEnabled = reader.ReadBool();          // ← READ from file
    IsCustomTextTrigger = reader.ReadBool(); // ← READ from file
    IsInitiallyOn = !reader.ReadBool();     // ← READ from file (inverted!)
    RunOnMapInit = reader.ReadBool();       // ← READ from file
    ParentId = reader.ReadInt32();          // ← READ from file

    nint guiFunctionCount = reader.ReadInt32();
    for (nint i = 0; i < guiFunctionCount; i++)
    {
        Functions.Add(reader.ReadTriggerFunction(
            triggerData, formatVersion, subVersion, false));
    }
}
```

**Key Points:**
- `Id` is NOT read for 1.27 format (defaults to 0 for ALL triggers!)
- `ParentId` IS read from file (critical for category assignment)
- `IsInitiallyOn` is inverted when reading/writing

### TriggerItem Base Class

**File**: `src/War3Net.Build.Core/Script/TriggerItem.cs`

```csharp
public abstract partial class TriggerItem
{
    internal TriggerItem(TriggerItemType triggerItemType)
    {
        Type = triggerItemType;
    }

    public TriggerItemType Type { get; private init; }
    public string Name { get; set; }
    public int Id { get; set; }         // Default: 0
    public int ParentId { get; set; }   // Default: 0

    public override string ToString() => Name;
}
```

**Default Values:**
- `Id = 0`
- `ParentId = 0`

**Impact for 1.27 Format:**
- All triggers have `Id = 0` after reading (not stored in file)
- All categories have `ParentId = 0` after reading (not stored in file)
- Category `Id` values are read from file and can be any value

## Writing WC3 1.27 Trigger Files

### MapTriggers.WriteTo() Implementation

```csharp
internal void WriteTo(BinaryWriter writer)
{
    writer.Write(FileFormatSignature);

    if (SubVersion is null)
    {
        // 1.27 format
        writer.Write((int)FormatVersion);

        // Write all categories (excluding RootCategory)
        var triggerCategories = TriggerItems
            .Where(item => item is TriggerCategoryDefinition &&
                          item.Type != TriggerItemType.RootCategory)
            .ToArray();

        writer.Write(triggerCategories.Length);
        foreach (var triggerCategory in triggerCategories)
        {
            writer.Write(triggerCategory, FormatVersion, SubVersion);
        }

        writer.Write(GameVersion);

        // Write all variables
        writer.Write(Variables.Count);
        foreach (var variable in Variables)
        {
            writer.Write(variable, FormatVersion, SubVersion);
        }

        // Write all triggers
        var triggers = TriggerItems
            .Where(item => item is TriggerDefinition)
            .ToArray();

        writer.Write(triggers.Length);
        foreach (var trigger in triggers)
        {
            writer.Write(trigger, FormatVersion, SubVersion);
        }
    }
}
```

**Key Points:**
- Categories must be written before triggers
- Uses `.Where()` to filter - order in TriggerItems matters!
- RootCategory is excluded from output

### TriggerCategoryDefinition.WriteTo() Implementation

```csharp
internal override void WriteTo(BinaryWriter writer,
                               MapTriggersFormatVersion formatVersion,
                               MapTriggersSubVersion? subVersion)
{
    writer.Write(Id);               // ← WRITE to file
    writer.WriteString(Name);       // ← WRITE to file

    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        writer.WriteBool(IsComment); // ← WRITE to file
    }

    if (subVersion is not null)
    {
        // Only for newer formats
        writer.WriteBool(IsExpanded);
        writer.Write(ParentId);
    }
    // For 1.27 (subVersion == null):
    // ParentId is NOT written
    // IsExpanded is NOT written
}
```

**What Gets Written:**
- ✅ Id (whatever value it has)
- ✅ Name
- ✅ IsComment (if v7+)
- ❌ ParentId (NOT written in 1.27)

### TriggerDefinition.WriteTo() Implementation

```csharp
internal override void WriteTo(BinaryWriter writer,
                               MapTriggersFormatVersion formatVersion,
                               MapTriggersSubVersion? subVersion)
{
    writer.WriteString(Name);           // ← WRITE to file
    writer.WriteString(Description);    // ← WRITE to file

    if (formatVersion >= MapTriggersFormatVersion.v7)
    {
        writer.WriteBool(IsComment);    // ← WRITE to file
    }

    if (subVersion is not null)
    {
        // Only for newer formats
        writer.Write(Id);
    }
    // For 1.27 (subVersion == null):
    // Id is NOT written

    writer.WriteBool(IsEnabled);              // ← WRITE to file
    writer.WriteBool(IsCustomTextTrigger);    // ← WRITE to file
    writer.WriteBool(!IsInitiallyOn);         // ← WRITE to file (inverted!)
    writer.WriteBool(RunOnMapInit);           // ← WRITE to file
    writer.Write(ParentId);                   // ← WRITE to file

    writer.Write(Functions.Count);
    foreach (var function in Functions)
    {
        writer.Write(function, formatVersion, subVersion);
    }
}
```

**What Gets Written:**
- ❌ Id (NOT written in 1.27)
- ✅ Name
- ✅ Description
- ✅ ParentId (critical!)
- ✅ All flags and functions

## Round-Trip Behavior (Read → Write → Read)

### Category Round-Trip (1.27)

```
Original File:
  Category.Id = 17
  Category.ParentId = (not stored)

After Read:
  Category.Id = 17        ✅ Preserved
  Category.ParentId = 0   (default)

After Write:
  Category.Id = 17        ✅ Written
  Category.ParentId = (not written)

After Re-Read:
  Category.Id = 17        ✅ Preserved
  Category.ParentId = 0   (default again)
```

**Conclusion**: Category IDs survive round-trip perfectly, even if non-sequential!

### Trigger Round-Trip (1.27)

```
Original File:
  Trigger.Id = (not stored)
  Trigger.ParentId = 17

After Read:
  Trigger.Id = 0          (default)
  Trigger.ParentId = 17   ✅ Preserved

After Write:
  Trigger.Id = (not written)
  Trigger.ParentId = 17   ✅ Written

After Re-Read:
  Trigger.Id = 0          (default again)
  Trigger.ParentId = 17   ✅ Preserved
```

**Conclusion**: Trigger ParentIds survive round-trip perfectly!

## Implications for WTGMerger

### What War3Net Handles Automatically
- ✅ Reading non-sequential category IDs correctly
- ✅ Writing non-sequential category IDs correctly
- ✅ Preserving trigger ParentId relationships
- ✅ Handling duplicate trigger IDs (all = 0)

### What WTGMerger Must Do
- ✅ Keep categories before triggers in TriggerItems list
- ✅ Preserve category IDs (don't renumber!)
- ✅ Preserve trigger ParentIds (don't recalculate!)
- ✅ When adding new categories, use unique IDs (not necessarily sequential)
- ✅ When adding new triggers, set ParentId to match target category ID

### What WTGMerger Must NOT Do
- ❌ Renumber category IDs to sequential
- ❌ Recalculate trigger ParentIds based on category positions
- ❌ Try to "fix" duplicate trigger IDs
- ❌ Modify category ParentIds (they're not used in 1.27 anyway)
- ❌ Rely on file position/index to determine category membership

## Extension Methods

War3Net provides useful extension methods in `BinaryReaderExtensions.cs`:

```csharp
public static MapTriggers ReadMapTriggers(this BinaryReader reader)
    => reader.ReadMapTriggers(TriggerData.Default);

public static MapTriggers ReadMapTriggers(this BinaryReader reader,
                                          TriggerData triggerData)
    => new MapTriggers(reader, triggerData);

public static TriggerCategoryDefinition ReadTriggerCategoryDefinition(
    this BinaryReader reader,
    TriggerItemType triggerItemType,
    TriggerData triggerData,
    MapTriggersFormatVersion formatVersion,
    MapTriggersSubVersion? subVersion)
    => new TriggerCategoryDefinition(reader, triggerItemType, triggerData,
                                    formatVersion, subVersion);

// ... similar for triggers, variables, functions, etc.
```

## Type Hierarchy

```
TriggerItem (abstract base)
├─ Id: int
├─ ParentId: int
├─ Name: string
├─ Type: TriggerItemType
│
├─ TriggerCategoryDefinition
│  ├─ IsComment: bool
│  ├─ IsExpanded: bool (not used in 1.27)
│  └─ ... (metadata)
│
├─ TriggerDefinition
│  ├─ Description: string
│  ├─ IsEnabled: bool
│  ├─ IsCustomTextTrigger: bool
│  ├─ IsInitiallyOn: bool
│  ├─ RunOnMapInit: bool
│  ├─ Functions: List<TriggerFunction>
│  └─ ... (more properties)
│
├─ TriggerVariableDefinition
│  └─ ... (for newer formats)
│
└─ DeletedTriggerItem
   └─ ... (for tracking deleted items)
```

## TriggerItemType Enum

```csharp
public enum TriggerItemType
{
    RootCategory = 0,    // Special root container
    Category = 4,        // Regular category
    Gui = 8,             // GUI trigger
    Comment = 16,        // Comment trigger
    Script = 32,         // Custom script trigger
    Variable = 64,       // Variable (newer formats)
    UNK1 = 128,         // Unknown
    UNK7 = 2048         // Unknown
}
```

## MapTriggersFormatVersion Enum

```csharp
public enum MapTriggersFormatVersion
{
    v4 = 4,
    v7 = 7   // WC3 1.27+ (IsComment field added)
}
```

**WC3 1.27 uses v7 format**

## Practical Examples

### Reading a WTG File

```csharp
using var fileStream = File.OpenRead("war3map.wtg");
using var reader = new BinaryReader(fileStream);
var triggers = reader.ReadMapTriggers();

// Check format
bool is127 = triggers.SubVersion == null;

// Iterate categories
foreach (var item in triggers.TriggerItems)
{
    if (item is TriggerCategoryDefinition category)
    {
        Console.WriteLine($"Category: {category.Name} (ID: {category.Id})");
    }
}

// Find triggers in a category
int targetCategoryId = 17;
var triggersInCategory = triggers.TriggerItems
    .OfType<TriggerDefinition>()
    .Where(t => t.ParentId == targetCategoryId)
    .ToList();
```

### Creating a New Category

```csharp
// Find next available ID (can be non-sequential!)
int maxId = triggers.TriggerItems
    .OfType<TriggerCategoryDefinition>()
    .Select(c => c.Id)
    .DefaultIfEmpty(-1)
    .Max();

int newId = maxId + 1;  // e.g., if max is 25, new is 26

var newCategory = new TriggerCategoryDefinition
{
    Type = TriggerItemType.Category,
    Id = newId,
    Name = "My New Category",
    ParentId = 0,        // Root level (not saved anyway in 1.27)
    IsComment = false
};

// Must add before any triggers!
triggers.TriggerItems.Insert(0, newCategory);
```

### Writing a WTG File

```csharp
using var fileStream = File.Create("output.wtg");
using var writer = new BinaryWriter(fileStream);
triggers.WriteTo(writer);
```

## Performance Considerations

- War3Net reads/writes sequentially - efficient for large files
- No in-memory indexing by default - build your own if needed
- LINQ queries on TriggerItems are not cached - consider caching results

## Thread Safety

War3Net classes are **not thread-safe**. If processing multiple maps concurrently, use separate MapTriggers instances.

## References

- War3Net GitHub: https://github.com/Drake53/War3Net
- Source Location: `src/War3Net.Build.Core/`
- NuGet: War3Net.Build.Core package
