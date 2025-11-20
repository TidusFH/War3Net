# Using w3x2lni and WC3MapTranslator for Object Data

## Overview

Great idea! Using an external converter to handle object data could simplify implementation. Let's analyze the two main options: **w3x2lni** and **WC3MapTranslator**.

## Option 1: w3x2lni (Lua-based)

### What It Is
- **Project**: https://github.com/sumneko/w3x2lni
- **Language**: Lua (97%) + C++ (3%)
- **License**: GPL-3.0
- **Purpose**: Convert WC3 maps between multiple formats

### Supported Formats
```
.w3x ↔ LNI ↔ OBJ ↔ SLK
```

- **LNI**: Human-readable Lua-based format (VCS-friendly)
- **OBJ**: World Editor format
- **SLK**: Distribution format (WC3 readable only)

### What It Can Convert
- ✅ Triggers (war3map.wtg) - **KEY ADVANTAGE**
- ✅ Object data (units, items, abilities, etc.)
- ✅ Terrain
- ✅ Doodads, units, regions
- ✅ Sounds, cameras
- ✅ Map info
- ✅ Custom scripts
- ⚠️ Some features still in development (TODO list)

### Pros
✅ **Comprehensive** - handles triggers AND object data
✅ **Human-readable** - LNI format is text-based
✅ **Version control friendly** - can diff LNI files
✅ **Popular** - 7,900+ downloads on Hiveworkshop
✅ **Open source** - GPL-3.0

### Cons
❌ **External dependency** - need to bundle or download
❌ **Lua/C++ based** - harder to integrate with C#
❌ **Process overhead** - need to shell out to external tool
❌ **Chinese-focused** - documentation mostly Chinese
❌ **Learning curve** - need to understand LNI format
❌ **Another failure point** - external tool could crash/fail

### Integration Approach

```csharp
// Workflow:
// 1. Convert .w3x to LNI using w3x2lni
// 2. Parse LNI files (simple text format)
// 3. Modify object data in LNI
// 4. Convert back to .w3x using w3x2lni

public class W3x2lniIntegration
{
    private string w3x2lniPath = "path/to/w3x2lni.exe";

    public void ConvertToLNI(string mapPath, string outputDir)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = w3x2lniPath,
                Arguments = $"--input \"{mapPath}\" --output \"{outputDir}\" --format lni",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"w3x2lni conversion failed: {process.StandardError.ReadToEnd()}");
        }
    }

    public void ConvertToW3x(string lniDir, string outputPath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = w3x2lniPath,
                Arguments = $"--input \"{lniDir}\" --output \"{outputPath}\" --format obj",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"w3x2lni conversion failed: {process.StandardError.ReadToEnd()}");
        }
    }

    public Dictionary<string, UnitData> ParseUnitLNI(string lniDir)
    {
        var units = new Dictionary<string, UnitData>();
        var unitFile = Path.Combine(lniDir, "table", "unit.ini"); // Example path

        // LNI is INI-like format, can use simple text parsing
        var lines = File.ReadAllLines(unitFile);
        // Parse LNI format...

        return units;
    }
}
```

### Example LNI Format

```ini
[h001]
name = "Custom Footman"
base = "hfoo"
uhpm = 500  # Hit points max
unam = "My Custom Unit"

[h002]
name = "Hero Knight"
base = "Hpal"
uhpm = 1000
```

---

## Option 2: WC3MapTranslator (JavaScript/TypeScript)

### What It Is
- **Project**: https://github.com/ChiefOfGxBxL/WC3MapTranslator
- **Language**: TypeScript/JavaScript (Node.js)
- **License**: MIT
- **Purpose**: Convert war3map files ↔ JSON

### Supported Formats

**✅ Fully Supported (Bidirectional)**:
- Units Objects (war3map.w3u)
- Items Objects (war3map.w3t)
- Abilities Objects (war3map.w3a)
- Destructables Objects (war3map.w3b)
- Doodads Objects (war3map.w3d)
- Upgrades Objects (war3map.w3q)
- Buffs Objects (war3map.w3h)
- Units placement (war3mapUnits.doo)
- Doodads placement (war3map.doo)
- Regions (war3map.w3r)
- Cameras (war3map.w3c)
- Sounds (war3map.w3s)
- Info (war3map.w3i)
- Imports (war3map.imp)
- Strings (war3map.wts)

**❌ NOT Supported**:
- **Triggers (war3map.wtg)** - **CRITICAL LIMITATION**
- JASS/LUA scripts
- Pathing
- Shadow maps

### Pros
✅ **JSON format** - extremely easy to work with
✅ **Object data support** - all object types (w3u, w3t, w3a, etc.)
✅ **MIT license** - permissive
✅ **TypeScript** - well-typed, modern
✅ **Active development** - maintained

### Cons
❌ **No trigger support** - DEALBREAKER for WTGMerger!
❌ **Node.js dependency** - need to bundle Node.js
❌ **No CLI** - library only (would need wrapper)
❌ **External dependency** - another tool to manage

### Integration Approach

```csharp
// Would need to create Node.js wrapper script
// Then call it from C#

public class WC3MapTranslatorIntegration
{
    private string nodePath = "node";
    private string translatorScript = "path/to/translator-wrapper.js";

    public void ConvertObjectsToJSON(string mapPath, string outputDir)
    {
        // Extract .w3x to temp folder
        using var archive = MpqArchive.Open(mapPath);

        // Convert each object file using Node.js
        ConvertFile(archive, "war3map.w3u", outputDir);
        ConvertFile(archive, "war3map.w3t", outputDir);
        ConvertFile(archive, "war3map.w3a", outputDir);
        // etc.
    }

    private void ConvertFile(MpqArchive archive, string fileName, string outputDir)
    {
        // Extract binary file
        var tempFile = Path.GetTempFileName();
        using (var fileStream = MpqFile.OpenRead(archive, fileName))
        using (var output = File.Create(tempFile))
        {
            fileStream.CopyTo(output);
        }

        // Call Node.js translator
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = nodePath,
                Arguments = $"\"{translatorScript}\" \"{tempFile}\" \"{outputDir}/{fileName}.json\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };

        process.Start();
        process.WaitForExit();

        File.Delete(tempFile);
    }

    public UnitObjectData ParseUnitsJSON(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var units = JsonSerializer.Deserialize<UnitObjectData>(json);
        return units;
    }
}
```

### Example JSON Format

```json
{
  "units": [
    {
      "oldId": "hfoo",
      "newId": "h001",
      "modifications": [
        {
          "id": "uhpm",
          "type": "int",
          "value": 500
        },
        {
          "id": "unam",
          "type": "string",
          "value": "Custom Footman"
        }
      ]
    }
  ]
}
```

---

## Option 3: War3Net (Current Approach)

### What It Is
- **Built-in to your project**
- **Language**: C# (.NET)
- **License**: MIT
- **Already using it** for triggers

### Pros
✅ **Already integrated** - no new dependencies
✅ **Native C#** - no external processes
✅ **Complete API** - read/write all formats
✅ **Trigger support** - war3map.wtg fully supported
✅ **Object data support** - all object types
✅ **Type-safe** - compile-time checking
✅ **No conversion overhead** - direct binary manipulation
✅ **Well-documented** - extensive tests and examples

### Cons
❌ **Binary format** - not human-readable
❌ **No version control** - can't diff binary files
❌ **More complex API** - need to understand object model

### Current Status
Already using War3Net for:
- ✅ Reading/writing triggers
- ✅ Reading/writing variables
- ✅ MPQ archive operations
- ⚠️ Not yet using for object data (but available!)

---

## Comparison Matrix

| Feature | w3x2lni | WC3MapTranslator | War3Net |
|---------|---------|------------------|---------|
| **Trigger support** | ✅ Yes | ❌ No | ✅ Yes |
| **Object data** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Language** | Lua/C++ | TypeScript | C# |
| **Integration** | Shell exec | Node.js | Native |
| **Format** | LNI (text) | JSON | Binary |
| **Version control** | ✅ Friendly | ✅ Friendly | ❌ Binary |
| **Performance** | ⚠️ Slower (conversion) | ⚠️ Slower (conversion) | ✅ Fast (direct) |
| **Dependencies** | External tool | Node.js | None |
| **Learning curve** | Medium (LNI format) | Low (JSON) | Medium (API) |
| **License** | GPL-3.0 | MIT | MIT |
| **Already integrated** | ❌ No | ❌ No | ✅ Yes |
| **Maintenance** | ⚠️ Active | ✅ Active | ✅ Very active |

---

## Recommendation

### For WTGMerger: **Stick with War3Net** ✅

**Reasons**:

1. **Trigger support is critical** - WC3MapTranslator doesn't support triggers, so it's a non-starter
2. **Already integrated** - War3Net is already working perfectly for triggers
3. **No new dependencies** - adding external tools increases complexity
4. **Performance** - direct binary manipulation is faster than convert → modify → convert
5. **Type safety** - C# compile-time checking prevents errors
6. **Single codebase** - everything in one language

### When External Tools Make Sense

External converters (w3x2lni, WC3MapTranslator) are better for:
- **Version control workflows** - if you want to store maps as text in git
- **Manual editing** - if humans need to edit map data directly
- **Diff inspection** - if you need to review changes in pull requests
- **Multi-tool pipelines** - if integrating with other tools

But for **WTGMerger's use case** (programmatic merging), War3Net is superior.

---

## Implementation Path with War3Net

Here's how easy object data handling is with War3Net (which you already have):

```csharp
// Already in your libraries!
using War3Net.Build;
using War3Net.Build.Object;
using War3Net.IO.Mpq;

public class ObjectDataHandler
{
    public ObjectRegistry LoadObjects(string mapPath)
    {
        var registry = new ObjectRegistry();

        // Open the map
        var map = Map.Open(mapPath);

        // Load units
        if (map.UnitObjectData != null)
        {
            foreach (var unit in map.UnitObjectData.NewUnits)
            {
                string code = unit.NewId.ToRawcode();
                registry.Units[code] = new UnitInfo
                {
                    Code = code,
                    BaseCode = unit.OldId.ToRawcode(),
                    Name = GetUnitName(unit),
                    Modifications = unit.Modifications
                };
            }
        }

        // Load items
        if (map.ItemObjectData != null)
        {
            foreach (var item in map.ItemObjectData.NewItems)
            {
                string code = item.NewId.ToRawcode();
                registry.Items[code] = new ItemInfo
                {
                    Code = code,
                    BaseCode = item.OldId.ToRawcode(),
                    Name = GetItemName(item)
                };
            }
        }

        // Similar for abilities, destructables, etc.

        return registry;
    }

    public void CopyObject(Map sourceMap, Map targetMap, string objectCode, ObjectType type)
    {
        switch (type)
        {
            case ObjectType.Unit:
                var unit = sourceMap.UnitObjectData.NewUnits
                    .FirstOrDefault(u => u.NewId.ToRawcode() == objectCode);

                if (unit != null)
                {
                    targetMap.UnitObjectData.NewUnits.Add(CloneUnit(unit));
                }
                break;

            case ObjectType.Item:
                // Similar...
                break;
        }

        targetMap.Save("output.w3x");
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
                Value = mod.Value
            });
        }

        return clone;
    }

    private string GetUnitName(SimpleObjectModification unit)
    {
        // Find the "unam" (name) modification
        var nameMod = unit.Modifications.FirstOrDefault(m => m.Id == "unam".FromRawcode());
        return nameMod?.Value?.ToString() ?? "Unknown";
    }
}
```

**That's it!** No external tools, no conversion, just direct manipulation with the libraries you already have.

---

## Hybrid Approach (Advanced)

If you want the **best of both worlds**:

1. **Use War3Net for core functionality** (triggers, object data)
2. **Optionally export to JSON/LNI** for user inspection or version control
3. **User can manually edit JSON** if they want
4. **Import back** for final merge

```csharp
public class HybridApproach
{
    public void MergeWithPreview(string source, string target, string output)
    {
        // 1. Use War3Net to load and analyze
        var sourceMap = Map.Open(source);
        var targetMap = Map.Open(target);

        var detector = new ObjectReferenceDetector();
        var missing = detector.FindMissing(sourceMap, targetMap);

        // 2. Export missing objects to JSON for user review
        var jsonExport = JsonSerializer.Serialize(missing, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText("missing_objects.json", jsonExport);

        Console.WriteLine("⚠ Missing objects exported to missing_objects.json");
        Console.WriteLine("Review and edit if needed, then press Enter to continue...");
        Console.ReadLine();

        // 3. Import user's decisions from JSON
        var decisions = JsonSerializer.Deserialize<MergeDecisions>("missing_objects.json");

        // 4. Use War3Net to perform the actual merge
        foreach (var decision in decisions.ObjectsToCopy)
        {
            CopyObject(sourceMap, targetMap, decision.Code, decision.Type);
        }

        targetMap.Save(output);
    }
}
```

---

## Conclusion

**Recommendation: Use War3Net directly** ✅

**Why**:
- ✅ Already integrated
- ✅ Supports triggers (critical!)
- ✅ Native C# (no external dependencies)
- ✅ Better performance
- ✅ Single codebase

**External converters** (w3x2lni, WC3MapTranslator) are great tools, but they solve a different problem (version control, human editing) than what WTGMerger needs (programmatic merging).

**What I recommend**:
Implement Phase 1 object detection using War3Net directly. It will be:
- Faster to implement (2-3 days)
- More reliable (no external dependencies)
- Easier to maintain (all C#)
- More performant (no conversion overhead)

Want me to implement object detection using War3Net?
