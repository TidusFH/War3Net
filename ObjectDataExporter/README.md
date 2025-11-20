# Warcraft 3 Object Data Exporter

A simple command-line tool to export all custom object data from a WC3 map to human-readable text files.

## What It Does

Extracts **all custom objects** from a .w3x map and exports them to readable format:
- Units (war3map.w3u)
- Items (war3map.w3t)
- Abilities (war3map.w3a)
- Destructables (war3map.w3b)
- Doodads (war3map.w3d)
- Buffs (war3map.w3h)
- Upgrades (war3map.w3q)

## Supported Formats

### TXT Format (Human-Readable)
```
═══════════════════════════════════════════════════════════
                      CUSTOM UNITS
═══════════════════════════════════════════════════════════

[h001] - Based on [hfoo]
Type: Unit
Modifications: 5

  unam = Custom Footman (String)
  uhpm = 500 (Int)
  uatk = 15 (Int)
  udef = 3 (Int)
  umvs = 300 (Int)

───────────────────────────────────────────────────────────
```

### INI Format (Configuration-Style)
```
[h001]
base = hfoo
type = Unit
unam = Custom Footman
uhpm = 500
uatk = 15
udef = 3
umvs = 300

[h002]
base = hpea
type = Unit
unam = Peasant Worker
...
```

### CSV Format (Spreadsheet-Friendly)
```
ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type
h001,hfoo,unam,,Custom Footman,String
h001,hfoo,uhpm,,500,Int
h001,hfoo,uatk,,15,Int
h002,hpea,unam,,Peasant Worker,String
...
```

## Usage

### Build

```bash
cd ObjectDataExporter
dotnet build -c Release
```

### Run

**Basic (auto output, TXT format):**
```bash
ObjectDataExporter.exe MyMap.w3x
```
Creates: `MyMap_objects/` folder with all data

**Specify output folder:**
```bash
ObjectDataExporter.exe MyMap.w3x C:/Export/MyObjects
```

**Specify format:**
```bash
ObjectDataExporter.exe MyMap.w3x MyObjects txt
ObjectDataExporter.exe MyMap.w3x MyObjects ini
ObjectDataExporter.exe MyMap.w3x MyObjects csv
```

**Interactive mode (no arguments):**
```bash
ObjectDataExporter.exe

Map file path (.w3x): C:/Maps/Campaign.w3x
Output path (or press Enter for auto):
Format [txt/ini/csv] (default: txt): ini
```

## Output Structure

```
MyMap_objects/
├── summary.txt          # Overview of all objects
├── units.txt            # All custom units
├── items.txt            # All custom items
├── abilities.txt        # All custom abilities
├── destructables.txt    # All custom destructables
├── doodads.txt          # All custom doodads
├── buffs.txt            # All custom buffs
└── upgrades.txt         # All custom upgrades
```

## Example Output

### summary.txt
```
╔══════════════════════════════════════════════════════════╗
║              OBJECT DATA EXPORT SUMMARY                  ║
╚══════════════════════════════════════════════════════════╝

Export Date: 2024-01-20 14:32:15

Units:            15 custom objects
Items:             8 custom objects
Abilities:        12 custom objects
Destructables:     0 custom objects
Doodads:           0 custom objects
Buffs:             3 custom objects
Upgrades:          5 custom objects

─────────────────────────────────────────────────────────
TOTAL:            43 custom objects
```

### units.txt (snippet)
```
[h001] - Based on [hfoo]
Type: Unit
Modifications: 8

  unam = Elite Footman (String)
  uhpm = 600 (Int)
  uatk = 20 (Int)
  udef = 5 (Int)
  umvs = 320 (Int)
  ugol = 50 (Int)
  ulbd = 15 (Int)
  uico = ReplaceableTextures\CommandButtons\BTNFootman.blp (String)

───────────────────────────────────────────────────────────

[h002] - Based on [hpea]
Type: Unit
Modifications: 3

  unam = Master Peasant (String)
  uhpm = 400 (Int)
  ugol = 30 (Int)

───────────────────────────────────────────────────────────
```

## Use Cases

### 1. Debug Object Data
```bash
# Export your map to see what's actually in it
ObjectDataExporter.exe MyMap.w3x debug_export txt

# Check the files to understand object modifications
cat debug_export/units.txt
```

### 2. Compare Two Maps
```bash
# Export both maps
ObjectDataExporter.exe Map1.w3x export1 ini
ObjectDataExporter.exe Map2.w3x export2 ini

# Use diff tool to compare
diff -r export1/ export2/
```

### 3. Document Your Map
```bash
# Export to CSV for spreadsheet
ObjectDataExporter.exe Campaign.w3x documentation csv

# Open in Excel/LibreOffice
libreoffice documentation/units.csv
```

### 4. Find Specific Objects
```bash
# Export to TXT
ObjectDataExporter.exe MyMap.w3x search txt

# Search for specific modifications
grep "uhpm = 500" search/units.txt
grep "Custom" search/units.txt
```

### 5. Backup Object Data
```bash
# Export before making changes
ObjectDataExporter.exe MyMap.w3x backup_$(date +%Y%m%d) ini

# Now you have human-readable backup of all objects
```

## Understanding Modification Codes

Common modification codes:

**Units:**
- `unam` - Name
- `uhpm` - Hit points max
- `uatk` - Attack (base damage)
- `udef` - Defense
- `umvs` - Movement speed
- `ugol` - Gold cost
- `ulum` - Lumber cost

**Items:**
- `inam` - Name
- `icid` - Class
- `ilvo` - Level
- `ugol` - Gold cost

**Abilities:**
- `anam` - Name
- `atp1` - Tooltip
- `aut1` - Untrained tooltip
- `acdn` - Cooldown
- `aman` - Mana cost

For complete list, check WC3 Object Editor documentation.

## Tips

1. **Large maps**: TXT format is most readable, INI is most compact
2. **Version control**: INI or TXT formats work well with git
3. **Analysis**: CSV format is best for spreadsheet analysis
4. **Backup**: Always export before major object changes
5. **Comparison**: Use INI format + diff tools to compare maps

## Troubleshooting

**"Map file not found"**
- Check file path
- Use absolute paths if relative doesn't work
- Ensure .w3x extension

**"Error loading map"**
- Verify map isn't corrupted
- Try opening in World Editor first
- Check if it's a valid .w3x file

**"No objects exported"**
- Map might have no custom objects
- Check summary.txt for counts
- Verify map has object data in Object Editor

**Empty output files**
- Map uses only Blizzard standard objects
- No custom modifications exist
- This is normal for some maps

## Comparison with Other Tools

| Feature | ObjectDataExporter | w3x2lni | WC3MapTranslator |
|---------|-------------------|---------|------------------|
| Export object data | ✅ | ✅ | ✅ |
| Human-readable | ✅ TXT/INI | ✅ LNI | ✅ JSON |
| Spreadsheet format | ✅ CSV | ❌ | ❌ |
| No dependencies | ✅ | ❌ (Lua) | ❌ (Node.js) |
| Simple to use | ✅ | ⚠️ | ⚠️ |
| Read-only | ✅ | ❌ | ❌ |

**ObjectDataExporter** is purpose-built for quick inspection and documentation, not for round-trip editing.

## Technical Details

- **Built with**: War3Net library
- **Language**: C# .NET 8.0
- **Lines of code**: ~500
- **Performance**: Exports typical map in 1-2 seconds
- **Memory**: Low footprint, streams output

## Future Enhancements (Optional)

- [ ] Support for base object data (not just custom)
- [ ] Modification name lookup (show "Hit Points" instead of "uhpm")
- [ ] Filter by object code or modification
- [ ] JSON export format
- [ ] Diff mode (compare two maps directly)
- [ ] Single file output option

## License

Same as War3Net (MIT).

## Credits

- Built on War3Net library by Drake53
- Created as companion tool to WTGMerger and ObjectMerger
