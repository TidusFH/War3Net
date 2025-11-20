# Example Outputs

## Example Map: Campaign.w3x

Suppose we have a campaign map with:
- 3 custom units
- 2 custom items
- 1 custom ability

## Command

```bash
ObjectDataExporter.exe Campaign.w3x output txt
```

## Output Files

### 1. summary.txt

```
╔══════════════════════════════════════════════════════════╗
║              OBJECT DATA EXPORT SUMMARY                  ║
╚══════════════════════════════════════════════════════════╝

Export Date: 2024-01-20 15:45:30

Units:             3 custom objects
Items:             2 custom objects
Abilities:         1 custom objects
Destructables:     0 custom objects
Doodads:           0 custom objects
Buffs:             0 custom objects
Upgrades:          0 custom objects

─────────────────────────────────────────────────────────
TOTAL:             6 custom objects
```

### 2. units.txt

```
═══════════════════════════════════════════════════════════
                      CUSTOM UNITS
═══════════════════════════════════════════════════════════

[h001] - Based on [hfoo]
Type: Unit
Modifications: 8

  unam = Elite Footman (String)
  utip = Powerful melee warrior with enhanced combat abilities (String)
  uhpm = 600 (Int)
  uatk = 20 (Int)
  udef = 5 (Int)
  umvs = 320 (Int)
  ugol = 150 (Int)
  ulum = 0 (Int)

───────────────────────────────────────────────────────────

[h002] - Based on [hrif]
Type: Unit
Modifications: 6

  unam = Sharpshooter (String)
  utip = Elite ranged unit with superior accuracy (String)
  uhpm = 450 (Int)
  uatk = 35 (Int)
  uran = 750 (Int)
  ugol = 200 (Int)

───────────────────────────────────────────────────────────

[h003] - Based on [hkni]
Type: Unit
Modifications: 7

  unam = Paladin Champion (String)
  utip = Heavy armored knight with healing abilities (String)
  uhpm = 1000 (Int)
  uatk = 25 (Int)
  udef = 8 (Int)
  umvs = 280 (Int)
  ugol = 300 (Int)

───────────────────────────────────────────────────────────
```

### 3. items.txt

```
═══════════════════════════════════════════════════════════
                      CUSTOM ITEMS
═══════════════════════════════════════════════════════════

[I001] - Based on [ratf]
Type: Item
Modifications: 5

  unam = Sword of Flames (String)
  utip = Legendary sword that deals fire damage (String)
  iabi = 1229801545 ('AIbf') (Int)
  ugol = 500 (Int)
  ilev = 5 (Int)

───────────────────────────────────────────────────────────

[I002] - Based on [phe1]
Type: Item
Modifications: 4

  unam = Greater Health Potion (String)
  utip = Restores 500 hit points (String)
  uhpm = 500 (Int)
  ugol = 150 (Int)

───────────────────────────────────────────────────────────
```

### 4. abilities.txt

```
═══════════════════════════════════════════════════════════
                    CUSTOM ABILITIES
═══════════════════════════════════════════════════════════

[A001] - Based on [AHtb]
Type: Ability
Modifications: 6

  anam = Thunder Strike (String)
  atp1 = Calls down a powerful lightning bolt (String)
  aut1 = Learn Thunder Strike (String)
  acdn = 8 (Real)
  aman = 100 (Int)
  ahdu = 2.5 (Real)

───────────────────────────────────────────────────────────
```

## INI Format Example

```bash
ObjectDataExporter.exe Campaign.w3x output ini
```

### units.ini

```ini
[h001]
base = hfoo
type = Unit
unam = Elite Footman
utip = Powerful melee warrior with enhanced combat abilities
uhpm = 600
uatk = 20
udef = 5
umvs = 320
ugol = 150
ulum = 0

[h002]
base = hrif
type = Unit
unam = Sharpshooter
utip = Elite ranged unit with superior accuracy
uhpm = 450
uatk = 35
uran = 750
ugol = 200

[h003]
base = hkni
type = Unit
unam = Paladin Champion
utip = Heavy armored knight with healing abilities
uhpm = 1000
uatk = 25
udef = 8
umvs = 280
ugol = 300
```

### items.ini

```ini
[I001]
base = ratf
type = Item
unam = Sword of Flames
utip = Legendary sword that deals fire damage
iabi = 1229801545
ugol = 500
ilev = 5

[I002]
base = phe1
type = Item
unam = Greater Health Potion
utip = Restores 500 hit points
uhpm = 500
ugol = 150
```

## CSV Format Example

```bash
ObjectDataExporter.exe Campaign.w3x output csv
```

### units.csv

```csv
ObjectCode,BaseCode,ModificationId,ModificationName,Value,Type
h001,hfoo,unam,,Elite Footman,String
h001,hfoo,utip,,Powerful melee warrior with enhanced combat abilities,String
h001,hfoo,uhpm,,600,Int
h001,hfoo,uatk,,20,Int
h001,hfoo,udef,,5,Int
h001,hfoo,umvs,,320,Int
h001,hfoo,ugol,,150,Int
h001,hfoo,ulum,,0,Int
h002,hrif,unam,,Sharpshooter,String
h002,hrif,utip,,Elite ranged unit with superior accuracy,String
h002,hrif,uhpm,,450,Int
h002,hrif,uatk,,35,Int
h002,hrif,uran,,750,Int
h002,hrif,ugol,,200,Int
h003,hkni,unam,,Paladin Champion,String
h003,hkni,utip,,Heavy armored knight with healing abilities,String
h003,hkni,uhpm,,1000,Int
h003,hkni,uatk,,25,Int
h003,hkni,udef,,8,Int
h003,hkni,umvs,,280,Int
h003,hkni,ugol,,300,Int
```

**Opened in spreadsheet:**

| ObjectCode | BaseCode | ModificationId | Value | Type |
|------------|----------|----------------|-------|------|
| h001 | hfoo | unam | Elite Footman | String |
| h001 | hfoo | utip | Powerful melee warrior... | String |
| h001 | hfoo | uhpm | 600 | Int |
| h001 | hfoo | uatk | 20 | Int |
| ... | ... | ... | ... | ... |

## Practical Examples

### Finding High-Cost Units

```bash
# Export to CSV
ObjectDataExporter.exe MyMap.w3x analysis csv

# Load in spreadsheet and filter
# Filter ModificationId = "ugol" AND Value > 300
```

### Comparing Map Versions

```bash
# Export both versions
ObjectDataExporter.exe MyMap_v1.w3x v1 ini
ObjectDataExporter.exe MyMap_v2.w3x v2 ini

# Compare with diff
diff -u v1/units.ini v2/units.ini

# Output shows what changed:
+ [h004]
+ base = hpal
+ type = Unit
+ unam = New Hero
+ uhpm = 1500
```

### Documenting a Campaign

```bash
# Export all maps
for map in Chapter*.w3x; do
    ObjectDataExporter.exe "$map" "docs/${map%.w3x}" txt
done

# Now you have documentation for each chapter
ls docs/
# Chapter1_objects/
# Chapter2_objects/
# Chapter3_objects/
```

### Searching for Specific Objects

```bash
# Export to TXT
ObjectDataExporter.exe BigMap.w3x search txt

# Find all objects with "Fire" in name
grep -r "Fire" search/

# Output:
# search/abilities.txt:  anam = Fire Nova (String)
# search/items.txt:  unam = Sword of Flames (String)
# search/units.txt:  unam = Fire Elemental (String)
```

### Extracting Object Codes

```bash
# Export to INI
ObjectDataExporter.exe MyMap.w3x codes ini

# Get all custom unit codes
grep "^\[" codes/units.ini | tr -d '[]'

# Output:
# h001
# h002
# h003

# Use in ObjectMerger
ObjectMerger.exe MyMap.w3x OtherMap.w3x merged.w3x
# Then copy codes: h001,h002,h003
```

## Large Map Example

For a map with hundreds of objects:

```
╔══════════════════════════════════════════════════════════╗
║              OBJECT DATA EXPORT SUMMARY                  ║
╚══════════════════════════════════════════════════════════╝

Export Date: 2024-01-20 16:22:45

Units:           245 custom objects
Items:            89 custom objects
Abilities:       156 custom objects
Destructables:    23 custom objects
Doodads:          45 custom objects
Buffs:            67 custom objects
Upgrades:         34 custom objects

─────────────────────────────────────────────────────────
TOTAL:           659 custom objects
```

Each file will contain all modifications for all objects of that type.

## Notes on Format Choice

**TXT**: Best for human reading and quick inspection
- ✅ Easy to read
- ✅ Clear structure
- ❌ Large file size

**INI**: Best for version control and diffing
- ✅ Compact
- ✅ Works well with git diff
- ✅ Easy to search
- ⚠️ Less visual separation

**CSV**: Best for analysis and spreadsheets
- ✅ Import to Excel/LibreOffice
- ✅ Sort, filter, analyze
- ✅ Create charts
- ❌ Not human-readable in text editor
