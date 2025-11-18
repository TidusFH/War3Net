# Object Manager Integration Assessment

## Executive Summary

**Question**: How hard would it be to make WTGMerger handle proper variables inside the map, like variables attached to object manager (custom units, items, abilities, etc.)?

**Answer**: **Moderate difficulty** - 3-5 days for detection, 1-2 weeks for full merging.

**Good News**: War3Net already provides ALL the necessary libraries! ✅

## What War3Net Already Provides

### ✅ Complete Object Data Support (Built-in)

War3Net has **full read/write support** for all object data types:

| Object Type | File | War3Net Class | Status |
|-------------|------|---------------|--------|
| Units | war3map.w3u | `UnitObjectData` | ✅ Ready |
| Items | war3map.w3t | `ItemObjectData` | ✅ Ready |
| Abilities | war3map.w3a | `AbilityObjectData` | ✅ Ready |
| Destructables | war3map.w3b | `DestructableObjectData` | ✅ Ready |
| Doodads | war3map.w3d | `DoodadObjectData` | ✅ Ready |
| Buffs | war3map.w3h | `BuffObjectData` | ✅ Ready |
| Upgrades | war3map.w3q | `UpgradeObjectData` | ✅ Ready |

### ✅ Map Archive Support (Built-in)

The `Map` class can:
- Open .w3x archives
- Read all object data files
- Modify object data
- Save back to archive

### ✅ Libraries Already Available

**You already have these in `/Libs/`:**
- `War3Net.Build.Core.dll` - Object data readers/writers
- `War3Net.Build.dll` - Map class for full map handling
- `War3Net.IO.Mpq.dll` - Archive operations
- `War3Net.Common.dll` - Utilities (including rawcode conversion)

**Not currently used but available:**
- `War3Net.CodeAnalysis.Jass.dll` - JASS parsing (useful for detecting object references in code)
- `War3Net.IO.Slk.dll` - Game data parsing (for getting object names from Blizzard data)

## No External Libraries Needed!

**You don't need to add anything.** War3Net has everything required.

## Implementation Difficulty Breakdown

### Phase 1: Detection Only (EASY - 2-3 days)

**What it does**:
- Scans variables for object references
- Reports which custom objects are missing in target
- User manually fixes in World Editor

**Code Required**:
```csharp
// 1. Read object data from maps
public class ObjectRegistry
{
    public Dictionary<string, UnitInfo> Units { get; }
    public Dictionary<string, ItemInfo> Items { get; }
    // etc.

    public static ObjectRegistry LoadFromMap(string mapPath)
    {
        using var archive = MpqArchive.Open(mapPath);
        var map = Map.Open(mapPath);

        var registry = new ObjectRegistry();

        // Read units
        if (map.UnitObjectData != null)
        {
            foreach (var unit in map.UnitObjectData.BaseUnits)
            {
                string code = unit.OldId.ToRawcode();
                registry.Units[code] = new UnitInfo { Code = code, ... };
            }
            foreach (var unit in map.UnitObjectData.NewUnits)
            {
                string code = unit.NewId.ToRawcode();
                registry.Units[code] = new UnitInfo { Code = code, ... };
            }
        }

        // Similar for items, abilities, etc.
        return registry;
    }
}

// 2. Scan variables for object references
public class VariableAnalyzer
{
    public List<ObjectReference> FindObjectReferences(MapTriggers triggers)
    {
        var references = new List<ObjectReference>();

        foreach (var variable in triggers.Variables)
        {
            // Check variable type
            var refType = GetObjectReferenceType(variable);
            if (refType != ObjectReferenceType.None)
            {
                string objectCode = ExtractObjectCode(variable.InitialValue);
                references.Add(new ObjectReference
                {
                    VariableName = variable.Name,
                    ObjectCode = objectCode,
                    Type = refType
                });
            }
        }

        return references;
    }

    private ObjectReferenceType GetObjectReferenceType(VariableDefinition variable)
    {
        // Detect by variable type
        return variable.Type switch
        {
            "unitcode" => ObjectReferenceType.Unit,
            "itemcode" => ObjectReferenceType.Item,
            "abilitycode" => ObjectReferenceType.Ability,
            "destructablecode" => ObjectReferenceType.Destructable,
            _ => ObjectReferenceType.None
        };
    }
}

// 3. Compare and report
public class ObjectValidator
{
    public ValidationReport ValidateObjects(
        MapTriggers sourceTriggers,
        ObjectRegistry sourceObjects,
        ObjectRegistry targetObjects)
    {
        var analyzer = new VariableAnalyzer();
        var references = analyzer.FindObjectReferences(sourceTriggers);

        var report = new ValidationReport();

        foreach (var reference in references)
        {
            bool existsInTarget = CheckIfExists(
                reference.ObjectCode,
                reference.Type,
                targetObjects);

            if (!existsInTarget)
            {
                report.MissingObjects.Add(new MissingObjectInfo
                {
                    ObjectCode = reference.ObjectCode,
                    Type = reference.Type,
                    UsedByVariable = reference.VariableName
                });
            }
        }

        return report;
    }
}
```

**Effort**: 2-3 days
- Day 1: ObjectRegistry implementation (load all object types)
- Day 2: VariableAnalyzer implementation (scan for references)
- Day 3: Integration and testing

**Value**: High - immediately useful, prevents broken merges

---

### Phase 2: Interactive Copying (MODERATE - 4-5 days)

**What it does**:
- All of Phase 1
- Shows user which objects to copy
- Copies selected objects to target map
- Warns about conflicts

**Additional Code Required**:
```csharp
public class ObjectMerger
{
    public void CopyObjects(
        string sourcePath,
        string targetPath,
        List<string> unitCodes,
        List<string> itemCodes,
        ConflictResolution conflictMode)
    {
        var sourceMap = Map.Open(sourcePath);
        var targetMap = Map.Open(targetPath);

        // Copy units
        foreach (var unitCode in unitCodes)
        {
            var sourceUnit = FindUnit(sourceMap.UnitObjectData, unitCode);
            if (sourceUnit != null)
            {
                var existingUnit = FindUnit(targetMap.UnitObjectData, unitCode);

                if (existingUnit != null)
                {
                    // Conflict!
                    if (conflictMode == ConflictResolution.Skip)
                        continue;
                    else if (conflictMode == ConflictResolution.Overwrite)
                        targetMap.UnitObjectData.NewUnits.Remove(existingUnit);
                    else // Rename
                        sourceUnit = RenameUnit(sourceUnit);
                }

                targetMap.UnitObjectData.NewUnits.Add(CloneUnit(sourceUnit));
            }
        }

        // Similar for items, abilities, etc.

        // Save modified map
        targetMap.Save(targetPath);
    }

    private SimpleObjectModification CloneUnit(SimpleObjectModification source)
    {
        var clone = new SimpleObjectModification
        {
            OldId = source.OldId,
            NewId = source.NewId
        };

        foreach (var mod in source.Modifications)
        {
            clone.Modifications.Add(new SimpleObjectDataModification
            {
                Id = mod.Id,
                Type = mod.Type,
                Value = mod.Value,
                // Clone other properties
            });
        }

        return clone;
    }
}
```

**User Interface**:
```
⚠ MISSING OBJECTS DETECTED:

Units:
  ☐ h001 (Custom Footman) - used by variable 'MyUnit'
  ☐ h002 (Hero Knight) - used by variable 'HeroUnit'

Items:
  ☐ I001 (Magic Sword) - used by variable 'MagicItem'

Select objects to copy to target map:
  [A]ll  [N]one  [S]elect individually  [C]ancel
```

**Effort**: 4-5 days (includes Phase 1)
- Days 1-3: Phase 1
- Day 4: ObjectMerger implementation
- Day 5: UI and conflict handling

**Value**: Very High - solves 90% of use cases

---

### Phase 3: Automatic Dependency Resolution (HARD - 1-2 weeks)

**What it does**:
- All of Phase 2
- Automatically resolves dependency chains
- Remaps conflicting IDs
- Updates all trigger references

**Challenges**:

1. **Dependency Chains**
   ```
   Custom Unit 'h001'
     ├─ Uses Custom Ability 'A001'
     │   └─ Uses Custom Buff 'B001'
     ├─ Uses Custom Item 'I001'
     └─ Based on 'hfoo' (must exist)
   ```
   Need to recursively resolve all dependencies.

2. **ID Conflicts**
   ```
   Source: Unit 'h001' = Custom Footman
   Target: Unit 'h001' = Different Custom Unit

   Solution: Remap source 'h001' -> 'h002' (find unused ID)
   Then update ALL references in triggers!
   ```

3. **Trigger Code References**
   ```jass
   // Trigger might have direct code references
   call CreateUnit(Player(0), 'h001', x, y, facing)

   // If we remap h001 -> h002, need to update this!
   call CreateUnit(Player(0), 'h002', x, y, facing)
   ```
   Requires JASS parsing and modification.

**Additional Code Required**:
```csharp
public class DependencyResolver
{
    public List<ObjectDependency> ResolveDependencies(
        string objectCode,
        ObjectReferenceType type,
        ObjectRegistry registry)
    {
        var dependencies = new List<ObjectDependency>();
        var visited = new HashSet<string>();

        ResolveDependenciesRecursive(objectCode, type, registry, dependencies, visited);

        return dependencies;
    }

    private void ResolveDependenciesRecursive(
        string objectCode,
        ObjectReferenceType type,
        ObjectRegistry registry,
        List<ObjectDependency> dependencies,
        HashSet<string> visited)
    {
        if (visited.Contains(objectCode))
            return;

        visited.Add(objectCode);

        // Find the object
        var obj = FindObject(objectCode, type, registry);
        if (obj == null)
            return;

        // Check what this object depends on
        if (type == ObjectReferenceType.Unit)
        {
            var unit = (UnitInfo)obj;

            // Check base unit
            if (!IsBlizzardUnit(unit.BaseId))
            {
                dependencies.Add(new ObjectDependency
                {
                    Code = unit.BaseId,
                    Type = ObjectReferenceType.Unit,
                    Reason = $"Base unit for {objectCode}"
                });
            }

            // Check custom abilities
            foreach (var abilityCode in unit.Abilities)
            {
                if (!IsBlizzardAbility(abilityCode))
                {
                    dependencies.Add(new ObjectDependency
                    {
                        Code = abilityCode,
                        Type = ObjectReferenceType.Ability,
                        Reason = $"Ability used by {objectCode}"
                    });

                    // Recursively check ability dependencies
                    ResolveDependenciesRecursive(abilityCode, ObjectReferenceType.Ability, registry, dependencies, visited);
                }
            }
        }
        // Similar for other types...
    }
}

public class IDRemapper
{
    public Dictionary<string, string> CreateRemapping(
        List<string> sourceIds,
        ObjectRegistry targetRegistry,
        ObjectReferenceType type)
    {
        var remapping = new Dictionary<string, string>();

        foreach (var sourceId in sourceIds)
        {
            if (Exists(sourceId, type, targetRegistry))
            {
                // Conflict - find unused ID
                string newId = FindUnusedId(type, targetRegistry);
                remapping[sourceId] = newId;
            }
        }

        return remapping;
    }

    private string FindUnusedId(ObjectReferenceType type, ObjectRegistry registry)
    {
        // For units: 'h000' to 'h999' (custom)
        // Try incrementally until we find unused
        string prefix = type switch
        {
            ObjectReferenceType.Unit => "h",
            ObjectReferenceType.Item => "I",
            ObjectReferenceType.Ability => "A",
            _ => "X"
        };

        for (int i = 0; i < 1000; i++)
        {
            string id = $"{prefix}{i:D3}";
            if (!registry.Units.ContainsKey(id))
                return id;
        }

        throw new InvalidOperationException("No unused IDs available!");
    }
}

public class TriggerUpdater
{
    public void UpdateTriggerReferences(
        MapTriggers triggers,
        Dictionary<string, string> remapping)
    {
        foreach (var trigger in triggers.TriggerItems.OfType<TriggerDefinition>())
        {
            foreach (var function in trigger.Functions)
            {
                UpdateFunctionReferences(function, remapping);
            }
        }
    }

    private void UpdateFunctionReferences(
        TriggerFunction function,
        Dictionary<string, string> remapping)
    {
        foreach (var param in function.Parameters)
        {
            // Check if parameter is an object code
            if (remapping.ContainsKey(param.Value))
            {
                param.Value = remapping[param.Value];
            }

            // Recursively update nested functions
            if (param.Function != null)
            {
                UpdateFunctionReferences(param.Function, remapping);
            }
        }

        // Update child functions (for if-then-else blocks)
        foreach (var child in function.ChildFunctions)
        {
            UpdateFunctionReferences(child, remapping);
        }
    }
}
```

**Effort**: 1-2 weeks
- Days 1-5: Phase 2
- Days 6-7: DependencyResolver implementation
- Days 8-9: IDRemapper and conflict handling
- Days 10-12: TriggerUpdater and testing
- Days 13-14: Integration testing and bug fixes

**Value**: Ultimate solution, but complex

---

## Recommended Approach

### Start with Phase 1 (Detection Only)

**Why**:
1. **Immediate value** - prevents broken merges right away
2. **Low risk** - read-only, doesn't modify anything
3. **User stays in control** - they manually fix objects in World Editor
4. **Quick to implement** - 2-3 days vs 1-2 weeks
5. **Can add more later** - Phase 2 and 3 build on Phase 1

**What user sees**:
```
═══════════════════════════════════════════════════════════
                OBJECT REFERENCE ANALYSIS
═══════════════════════════════════════════════════════════

Scanning variables for object references...

⚠ MISSING OBJECTS IN TARGET MAP:

Units (war3map.w3u):
  • h001 (Custom Footman)
    - Referenced by variable: 'MyCustomUnit'
    - Type: unitcode

  • h002 (Hero Knight)
    - Referenced by variable: 'HeroUnit'
    - Type: unitcode

Items (war3map.w3t):
  • I001 (Magic Sword)
    - Referenced by variable: 'MagicWeapon'
    - Type: itemcode

Abilities (war3map.w3a):
  • A001 (Custom Bash)
    - Referenced by variable: 'BashAbility'
    - Type: abilitycode

═══════════════════════════════════════════════════════════
RECOMMENDATION:
Before merging triggers, open the target map in World Editor
and add these custom objects from the source map.

Alternatively, use the Object Editor to export/import:
1. Open source map in World Editor
2. Object Editor → Export All Objects
3. Open target map in World Editor
4. Object Editor → Import Objects → Select exported file
5. Select only the objects listed above
═══════════════════════════════════════════════════════════

Continue with merge? [Y/N]:
```

### Then Add Phase 2 If Needed

If users find manual copying tedious, add interactive copying.

## Example Implementation (Phase 1)

Here's what you'd add to WTGMerger:

```csharp
// Add new menu option:
Console.WriteLine("9. Analyze object references");

// When user selects option 9:
case "9":
{
    Console.WriteLine("\nAnalyzing object references...");

    var sourceObjects = ObjectRegistry.LoadFromMap(sourcePath);
    var targetObjects = ObjectRegistry.LoadFromMap(targetPath);

    var validator = new ObjectValidator();
    var report = validator.ValidateObjects(
        sourceTriggers,
        sourceObjects,
        targetObjects);

    report.Display();
    break;
}
```

## Comparison with Current WTGMerger

| Feature | Current | With Phase 1 | With Phase 2 | With Phase 3 |
|---------|---------|--------------|--------------|--------------|
| Copy triggers | ✅ | ✅ | ✅ | ✅ |
| Copy variables | ✅ | ✅ | ✅ | ✅ |
| Warn about missing objects | ⚠️ Basic | ✅ Complete | ✅ Complete | ✅ Complete |
| Copy objects | ❌ | ❌ | ✅ | ✅ |
| Resolve dependencies | ❌ | ❌ | ❌ | ✅ |
| Remap conflicting IDs | ❌ | ❌ | ⚠️ Manual | ✅ Auto |
| Update trigger code | ❌ | ❌ | ❌ | ✅ |

## Existing Tools Comparison

### WTGMerger (Your Tool)
- ✅ Trigger merging
- ✅ Variable handling with remapping
- ✅ Category organization
- ✅ ParentId preservation
- ❌ Object data handling (proposed addition)

### War3Net Tools/War3Merger (Official)
- ⚠️ Basic trigger copying
- ❌ No variable handling
- ❌ No object data handling
- ❌ No ParentId fixes
- Note: Much simpler than WTGMerger

### Conclusion
**No existing tool does object merging**. This would be a unique feature!

## API Usage Examples

### Reading Object Data
```csharp
var map = Map.Open("MyMap.w3x");

// Access units
if (map.UnitObjectData != null)
{
    foreach (var unit in map.UnitObjectData.NewUnits)
    {
        string code = unit.NewId.ToRawcode(); // e.g., "h001"
        string basedOn = unit.OldId.ToRawcode(); // e.g., "hfoo"

        // Access modifications
        foreach (var mod in unit.Modifications)
        {
            int modId = mod.Id; // What property (e.g., hit points)
            object value = mod.Value; // New value
        }
    }
}

// Access items
if (map.ItemObjectData != null)
{
    foreach (var item in map.ItemObjectData.NewItems)
    {
        string code = item.NewId.ToRawcode();
        // Similar structure
    }
}
```

### Writing Object Data
```csharp
var map = Map.Open("TargetMap.w3x");

// Add a new unit
var newUnit = new SimpleObjectModification
{
    OldId = "hfoo".FromRawcode(), // Based on Footman
    NewId = "h001".FromRawcode()  // Custom code
};

// Add modifications
newUnit.Modifications.Add(new SimpleObjectDataModification
{
    Id = "uhpm".FromRawcode(), // Hit points max
    Type = ObjectDataType.Int,
    Value = 500
});

map.UnitObjectData.NewUnits.Add(newUnit);
map.Save("TargetMap.w3x");
```

## References

- **War3Net Map Class**: `/src/War3Net.Build.Core/Map.cs`
- **Object Data Classes**: `/src/War3Net.Build.Core/Object/`
- **Official War3Merger**: `/Tools/War3Merger/` (for reference)
- **Tests**: `/tests/War3Net.Build.Core.Tests/Object/`

## Conclusion

**It's definitely doable!** War3Net provides all the necessary infrastructure.

**Recommendation**: Start with Phase 1 (detection only) - 2-3 days of work, immediate value, low risk.

**Want me to implement Phase 1?** I can add object reference detection to WTGMerger right now.
