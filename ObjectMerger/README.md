# Warcraft 3 Object Merger

A command-line tool to copy custom objects (units, items, abilities, etc.) between Warcraft 3 maps.

## Features

**Phase 1 (Implemented):**
- ✅ Copy custom units, items, abilities, destructables, doodads, buffs, upgrades
- ✅ Interactive object selection
- ✅ Conflict detection (ID conflicts)
- ✅ Skip or overwrite conflict resolution
- ✅ Batch copy by object type
- ✅ Statistics and object listing

**Phase 2 (TODO):**
- ⏳ Automatic dependency detection
- ⏳ Dependency graph visualization
- ⏳ Auto-select dependent objects

**Phase 3 (TODO):**
- ⏳ Smart ID remapping for conflicts
- ⏳ Automatic reference updating
- ⏳ Cross-file reference tracking

## Quick Start

### Build

```bash
cd ObjectMerger
dotnet build -c Release
```

### Run

```bash
ObjectMerger.exe source.w3x target.w3x output.w3x
```

Or just run without arguments for interactive mode:

```bash
ObjectMerger.exe
```

## Usage Examples

### Example 1: Copy Specific Units

```
Source map path: CampaignMap.w3x
Target map path: MyMap.w3x
Output map path: MyMap_merged.w3x

Main Menu > 3 (Copy specific objects)

Object codes: h001,h002,h003

Found 3 object(s) to copy:
  h001 - Custom Footman (Unit)
  h002 - Hero Knight (Unit)
  h003 - Siege Tank (Unit)

⚠ Conflict: h001 (Unit)
  Source: Custom Footman
  Target: Different Custom Unit

How to resolve?
  [S] Skip - Don't copy this object
  [O] Overwrite - Replace target's version
  [R] Rename - Find new ID (Phase 3)
  [A] Skip All

Choice: O

✓ Copied: 3 object(s)

Main Menu > 6 (Save and exit)
✓ Map saved successfully!
```

### Example 2: Copy All Items

```
Main Menu > 4 (Copy all objects of a type)

Select object type:
1. Unit
2. Item
3. Ability
...

Choice: 2

Found 15 Item(s) in source map:
  I001 - Magic Sword
  I002 - Health Potion
  I003 - Mana Crystal
  ... and 12 more

Copy all 15 Item(s)? [Y/N]: Y

✓ Copied: 12 object(s)
⊘ Skipped: 3 object(s) (conflicts)
```

## Architecture

### Models (`Models/ObjectInfo.cs`)

Data structures:
- `ObjectInfo` - Represents a custom object
- `ObjectType` - Enum for object types (Unit, Item, etc.)
- `ObjectConflict` - ID conflict information
- `ConflictResolution` - How to handle conflicts
- `MergeOptions` - Configuration for merge operations
- `MergeResult` - Result of merge operation

### Services

**`ObjectRegistry.cs`**
- Loads all custom objects from a map
- Provides lookup by code and type
- Stores references to War3Net objects

**`DependencyResolver.cs`** (Phase 1: Basic)
- Finds dependencies between objects
- Currently: Basic check for custom base objects
- Phase 2: Will scan modifications for references

**`ConflictResolver.cs`**
- Detects ID conflicts
- Interactive or automatic resolution
- Phase 3: Will implement smart ID remapping

**`ObjectCopier.cs`**
- Copies objects from source to target
- Clones object data deeply
- Handles removal for overwrites

### Main Program (`Program.cs`)

- Interactive menu system
- Coordinates all services
- Saves output map

## Development Roadmap

### Phase 1 - Basic Copying ✅ (COMPLETE)

**What it does:**
- Copy objects with manual selection
- Detect ID conflicts
- Skip or overwrite conflicts

**Estimated time:** 1 week

### Phase 2 - Dependency Detection ⏳ (TODO)

**What to add:**

1. **Modify `DependencyResolver.cs`:**
```csharp
// Scan modifications for object references
private void ScanModifications(SimpleObjectModification obj)
{
    foreach (var mod in obj.Modifications)
    {
        // Check if modification value is an object code
        string? objCode = ExtractObjectCode(mod.Value);
        if (objCode != null && !IsBlizzardObject(objCode))
        {
            obj.Dependencies.Add(objCode);
        }
    }
}

// Parse modification values for object codes
private string? ExtractObjectCode(object? value)
{
    // Example: "h001" or list of codes
    // Implementation depends on modification type
}
```

2. **Add dependency visualization:**
```csharp
public void ShowDependencyGraph(List<ObjectInfo> objects)
{
    Console.WriteLine("\nDependency Graph:");
    foreach (var obj in objects)
    {
        Console.WriteLine($"{obj.Code} - {obj.Name}");
        foreach (var dep in obj.Dependencies)
        {
            Console.WriteLine($"  └─→ {dep}");
        }
    }
}
```

3. **Auto-select dependencies:**
```csharp
public List<ObjectInfo> AutoSelectDependencies(List<string> selectedCodes)
{
    var result = new List<ObjectInfo>();
    var toProcess = new Queue<string>(selectedCodes);
    var visited = new HashSet<string>();

    while (toProcess.Count > 0)
    {
        string code = toProcess.Dequeue();
        if (visited.Contains(code)) continue;

        var obj = FindObjectByCode(code);
        if (obj == null) continue;

        visited.Add(code);
        result.Add(obj);

        // Queue dependencies
        foreach (var dep in obj.Dependencies)
        {
            if (!visited.Contains(dep))
            {
                toProcess.Enqueue(dep);
            }
        }
    }

    return result;
}
```

**Estimated time:** 2-3 days

### Phase 3 - ID Remapping ⏳ (TODO)

**What to add:**

1. **Implement `FindUnusedId` properly:**
```csharp
public string FindUnusedId(ObjectType type)
{
    string prefix = GetPrefixForType(type);

    // Try sequential IDs
    for (int i = 0; i < 1000; i++)
    {
        string candidateId = $"{prefix}{i:D3}";
        if (!targetRegistry.HasObject(candidateId, type))
        {
            return candidateId;
        }
    }

    // Try with uppercase prefix
    prefix = prefix.ToUpper();
    for (int i = 0; i < 1000; i++)
    {
        string candidateId = $"{prefix}{i:D3}";
        if (!targetRegistry.HasObject(candidateId, type))
        {
            return candidateId;
        }
    }

    throw new InvalidOperationException("No unused IDs!");
}
```

2. **Create ID remapping service:**
```csharp
public class IdRemapper
{
    private Dictionary<string, string> remapping = new();

    public void AddRemapping(string oldId, string newId)
    {
        remapping[oldId] = newId;
    }

    public void ApplyRemapping(Map targetMap, List<ObjectInfo> objects)
    {
        foreach (var obj in objects)
        {
            if (remapping.ContainsKey(obj.Code))
            {
                // Rename the object
                RenameObject(obj, remapping[obj.Code]);

                // Update all references to this object
                UpdateReferences(targetMap, obj.Code, remapping[obj.Code]);
            }
        }
    }

    private void UpdateReferences(Map targetMap, string oldId, string newId)
    {
        // Scan all objects for references to oldId
        // Replace with newId
        // This is the complex part!
    }
}
```

3. **Enable rename in ConflictResolver:**
```csharp
case "R":
    string newId = conflictResolver.FindUnusedId(conflict.Type);
    conflict.Resolution = ConflictResolution.Rename;
    conflict.NewCode = newId;
    Console.WriteLine($"→ Will rename {conflict.ObjectCode} → {newId}");
    return;
```

**Estimated time:** 2-3 days

## Known Limitations

**Phase 1:**
- No dependency detection
- Cannot rename conflicting IDs
- No reference updating

**All Phases:**
- Does not copy trigger references to objects
- Does not copy preplaced units/destructables
- Does not validate object data integrity

## Tips

1. **Always backup your maps** before merging
2. **Test in World Editor** after merging
3. **Copy dependencies manually** in Phase 1
4. **Use 'Skip' for conflicts** unless you're sure about overwriting

## Comparison with WTGMerger

| Feature | WTGMerger | ObjectMerger |
|---------|-----------|--------------|
| Complexity | Very High | Moderate |
| Lines of code | ~4,200 | ~1,500 |
| Recursive structures | Yes | No |
| ParentId hell | Yes | None |
| File order issues | Yes | No |
| Main challenge | Category hierarchy | Dependencies |

ObjectMerger is **~60% simpler** than WTGMerger!

## Troubleshooting

**Error: "Source or target map not loaded"**
- Check file paths
- Ensure maps are valid .w3x files
- Try opening in World Editor first

**Objects not appearing in World Editor**
- Check object data in Object Editor
- Verify object wasn't skipped due to conflict
- Check for base object existence

**Map won't save**
- Check disk space
- Verify write permissions
- Try different output path

## Contributing

To extend this tool:

1. **Add new object types**: Follow pattern in `ObjectRegistry.cs`
2. **Improve dependency detection**: Modify `DependencyResolver.cs`
3. **Add ID remapping**: Implement Phase 3 features
4. **Add batch operations**: Extend menu in `Program.cs`

## License

Same as War3Net project (MIT).

## Credits

- Built on War3Net library by Drake53
- Inspired by WTGMerger development experience
- Template created for easier object management

## Support

For issues or questions:
1. Check this README
2. Review War3Net documentation
3. Test with simple maps first
4. Compare with WTGMerger patterns
