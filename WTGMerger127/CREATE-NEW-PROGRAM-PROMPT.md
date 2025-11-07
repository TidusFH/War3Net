# PROMPT TEMPLATE: Create WC3 1.27 Trigger Merger Program

Copy and paste this prompt to an AI assistant to create a new WTG merger program from scratch:

---

## TASK: Create a Warcraft 3 1.27 Trigger Merger Program

Create a complete, production-ready C# .NET 8.0 console application that merges triggers between Warcraft 3 maps while maintaining full compatibility with World Editor 1.27.

---

## 🎯 PRIMARY OBJECTIVE

Merge triggers from one WC3 map (source) into another map (target) such that:
1. Merged triggers appear in the correct categories in World Editor 1.27
2. Map loads without errors
3. Triggers function correctly in-game
4. Format compatibility is preserved (SubVersion=null)

---

## 🔴 CRITICAL TECHNICAL REQUIREMENTS

### Requirement 1: Position-Based Category IDs
**Rule:** Category ID MUST equal its position in the TriggerItems array

**Why:** WC3 1.27 uses trigger `ParentId` as a POSITION INDEX, not an ID lookup.

**Example:**
```
TriggerItems array:
[0] Category "Init" (ID=0)
[1] Category "Heroes" (ID=1)
[2] Category "Spells" (ID=2)
[3] Trigger "Map Init" (ParentId=0 → looks up position 0)
```

**Implementation:**
- When loading: Fix any mismatches between category ID and position
- When adding category: Set ID = current category count
- When inserting category: Use `TriggerItems.Insert(position, category)`, NOT `.Add()`

### Requirement 2: Category ParentId Must Be 0
**Rule:** ALL categories in old format MUST have `ParentId=0`

**Why:** Old format does NOT save category ParentId to file, always reads as 0

**Implementation:**
- After loading: Set all category `ParentId` to 0
- When creating category: Set `ParentId=0` (for SubVersion=null)

### Requirement 3: Trigger ParentId = Category Position
**Rule:** Trigger `ParentId` references the POSITION of its parent category

**Example:**
```
Category "Spells" at position 2 (ID=2)
Trigger in "Spells" → ParentId=2
```

**Why:** WC3 1.27 uses `ParentId` to index into TriggerItems array

**Implementation:**
- When copying trigger: Set `ParentId = category.Id` (which equals position)
- After structure changes: Update trigger ParentIds if category positions changed

### Requirement 4: SubVersion Must Stay null
**Rule:** NEVER change `SubVersion` from `null` to anything else

**Why:** Changing SubVersion converts format, breaks WC3 1.27 compatibility

**Implementation:**
- Never modify `SubVersion` property
- Check format before operations: `if (triggers.SubVersion == null) { /* old format */ }`

### Requirement 5: TriggerItems Array Structure
**Rule:** ALL categories MUST be at the BEGINNING of TriggerItems, before any triggers

**Structure:**
```
TriggerItems[0..N] = Categories (in order, ID = position)
TriggerItems[N+1..] = Triggers (ParentId references category position)
```

**Why:** WC3 1.27 expects categories first for position-based lookup

**Implementation:**
- When restructuring: Remove all categories, re-insert at positions 0,1,2,3...
- When adding category: Insert at position = category count, NOT at end

### Requirement 6: IDs Not Saved in Old Format
**Rule:** Trigger IDs and Variable IDs are NOT saved in old format (all become 0)

**Why:** Old format doesn't store these IDs, uses names for variables

**Implementation:**
- Set `trigger.Id = 0` (doesn't matter, not saved)
- Set `variable.Id = 0` (doesn't matter, not saved)
- Don't worry about ID collisions for triggers/variables

---

## 📊 FILE FORMAT SPECIFICATIONS

### Old Format (WC3 1.27) - What Gets Saved to File

| Item Type | ID Saved? | ParentId Saved? | Position Saved? | Notes |
|-----------|-----------|-----------------|-----------------|-------|
| Category | ✅ YES | ❌ NO | ❌ NO | ParentId always reads as 0 |
| Trigger | ❌ NO | ✅ YES | ❌ NO | ParentId is POSITION INDEX |
| Variable | ❌ NO | ❌ NO | ❌ NO | Name is unique identifier |

### Format Detection
```csharp
if (mapTriggers.SubVersion == null)
{
    // OLD FORMAT - WC3 1.27
    // Use position-based category IDs
    // Category ParentId = 0
}
else
{
    // ENHANCED FORMAT - WC3 Reforged
    // Use ID-based lookups
    // Category ParentId can be -1 or other category ID
}
```

---

## 🔧 REQUIRED FUNCTIONS

### Function 1: FixCategoryIdsForOldFormat()
**Purpose:** Ensure category IDs match positions immediately after loading

**Algorithm:**
```
1. Check if SubVersion == null (old format)
2. Get all categories from TriggerItems
3. Build mapping: oldId → newId (where newId = position index)
4. Remove ALL categories from TriggerItems
5. Re-insert categories at positions 0,1,2,3... with ID = position
6. Update all trigger ParentIds using the mapping
7. Set all category ParentIds to 0
```

**When to call:**
- Immediately after loading source map
- Immediately after loading target map
- After any merge operation
- Before save (validation)

### Function 2: CopySpecificTriggers()
**Purpose:** Copy specific triggers from source to target

**Algorithm:**
```
1. Find triggers in source category by name
2. Find or create destination category in target
3. If creating new category:
   a. Set ID = existingCategories.Count
   b. Set ParentId = 0 (for old format)
   c. Insert at position = existingCategories.Count (NOT .Add()!)
4. Copy variables used by triggers
5. For each trigger:
   a. Copy trigger with ParentId = destination category ID
   b. Add to TriggerItems (will be after categories)
6. Call FixCategoryIdsForOldFormat() to ensure consistency
```

### Function 3: MergeCategory()
**Purpose:** Merge entire category with all its triggers

**Algorithm:**
```
1. Find source category by name
2. Get all triggers in source category
3. If category exists in target:
   a. Remove old category and its triggers
   b. Call FixCategoryIdsForOldFormat() to fix positions
4. Create new category:
   a. Set ID = existingCategories.Count
   b. Set ParentId = 0 (for old format)
   c. Insert at position = existingCategories.Count
5. Copy variables used by triggers
6. Copy all triggers with ParentId = new category ID
7. Call FixCategoryIdsForOldFormat() to ensure consistency
```

### Function 4: SaveMergedMap()
**Purpose:** Validate and save with format preservation

**Algorithm:**
```
1. PRE-SAVE VALIDATION:
   a. Check SubVersion == null still (not changed)
   b. For each category: verify ID == position in TriggerItems
   c. For each category: verify ParentId == 0 (old format)
   d. For each trigger: verify ParentId < category count
   e. If any validation fails: call FixCategoryIdsForOldFormat()

2. SAVE:
   a. If map archive (.w3x/.w3m):
      - Update war3map.wtg in archive
      - Delete war3map.j (out of sync)
   b. If raw .wtg file:
      - Write directly to file

3. POST-SAVE VERIFICATION:
   a. Read file back
   b. Verify SubVersion still null
   c. Verify category count matches
   d. Verify trigger count matches
   e. Verify variable count matches
```

---

## 🎮 USER INTERFACE REQUIREMENTS

### Interactive Menu
```
1. List all categories from SOURCE
2. List all categories from TARGET
3. List triggers in a specific category
4. Copy ENTIRE category
5. Copy SPECIFIC trigger(s)
6. Show format & structure debug info
7. Manually fix target category IDs (if needed)
8. Toggle debug mode (ON/OFF)
9. Save and exit
0. Exit without saving
```

### Output Format
- Use `+` for success messages
- Use `!` for warnings
- Use `X` or `ERROR:` for errors
- Use ASCII characters only (NO Unicode box characters ╔═╗)
- Show category position, ID, ParentId in listings
- Highlight mismatches in RED color

### Debug Mode
When enabled, show:
- Category ID → position mappings
- Trigger ParentId updates
- Format version details
- Structure validation results

---

## 📁 PROJECT STRUCTURE

```
WTGMerger/
├── Program.cs                  (Main program)
├── WTGMerger.csproj           (Project configuration)
└── run.bat                     (Batch script for easy execution)
```

### Project Configuration (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="War3Net.Build.Core">
      <HintPath>../Libs/War3Net.Build.Core.dll</HintPath>
    </Reference>
    <Reference Include="War3Net.Build">
      <HintPath>../Libs/War3Net.Build.dll</HintPath>
    </Reference>
    <Reference Include="War3Net.Common">
      <HintPath>../Libs/War3Net.Common.dll</HintPath>
    </Reference>
    <Reference Include="War3Net.IO.Mpq">
      <HintPath>../Libs/War3Net.IO.Mpq.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

### Batch Script (run.bat)
```batch
@echo off
echo ===============================================================
echo    WTG MERGER FOR WARCRAFT 3 1.27 (OLD FORMAT)
echo ===============================================================
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
dotnet run --configuration Release
pause
```

---

## 🧪 TESTING & VERIFICATION

### Unit Test Cases

**Test 1: Load and Fix Categories**
```
Given: Map with categories [ID=5, ID=7, ID=2]
When: FixCategoryIdsForOldFormat() is called
Then: Categories should become [ID=0, ID=1, ID=2] at positions [0,1,2]
```

**Test 2: Add New Category**
```
Given: Target has 3 categories at positions 0,1,2
When: Add new category "Heroes"
Then: New category at position 3 with ID=3
      TriggerItems[3] == new category
```

**Test 3: Remove Category**
```
Given: Categories [0]=Init, [1]=Heroes, [2]=Spells
When: Remove "Heroes"
Then: Categories become [0]=Init(ID=0), [1]=Spells(ID=1)
      Positions match IDs
```

**Test 4: Copy Trigger**
```
Given: Trigger in source category at position 5
When: Copy to target category at position 2
Then: Copied trigger has ParentId=2
```

### Integration Test

**Full Merge Workflow:**
```
1. Load source.w3x (old format)
2. Load target.w3x (old format)
3. Merge category "Hero Abilities" (8 triggers)
4. Save to merged.w3x
5. Verify:
   - File loads without error
   - SubVersion == null
   - Category count increased by 1
   - Trigger count increased by 8
   - Open in World Editor 1.27
   - Triggers appear in "Hero Abilities" category
```

---

## 🎯 SUCCESS CRITERIA

The program is successful when:

1. **File Operations:**
   - ✅ Reads WC3 1.27 maps (.w3x, .w3m, .wtg)
   - ✅ Writes merged maps without corruption
   - ✅ Preserves SubVersion=null throughout

2. **Category Management:**
   - ✅ Category IDs always equal positions
   - ✅ Category ParentIds always 0 (old format)
   - ✅ Categories always at START of TriggerItems

3. **Trigger Management:**
   - ✅ Trigger ParentIds reference correct category positions
   - ✅ Triggers appear in correct categories in World Editor
   - ✅ Trigger code intact (events, conditions, actions)

4. **World Editor 1.27 Compatibility:**
   - ✅ Merged map opens without errors
   - ✅ Triggers appear in correct categories (not nested)
   - ✅ Variables are accessible
   - ✅ Map runs in-game without errors

5. **User Experience:**
   - ✅ Clear menu and instructions
   - ✅ Informative error messages
   - ✅ Debug mode for troubleshooting
   - ✅ Batch script for easy execution

---

## ⚠️ CRITICAL "DO NOT" LIST

### ❌ DO NOT change SubVersion
```csharp
// WRONG:
if (triggers.SubVersion == null)
{
    triggers.SubVersion = MapTriggersSubVersion.v4; // BREAKS 1.27!
}

// CORRECT:
// Leave SubVersion alone, never modify it
```

### ❌ DO NOT use .Add() for categories
```csharp
// WRONG:
target.TriggerItems.Add(newCategory); // Goes to end, breaks structure!

// CORRECT:
target.TriggerItems.Insert(existingCategories.Count, newCategory);
```

### ❌ DO NOT use GetNextId() for old format
```csharp
// WRONG:
int newCategoryId = GetNextId(target); // Returns MAX(IDs)+1, wrong!

// CORRECT:
int newCategoryId = existingCategories.Count; // Position-based!
```

### ❌ DO NOT forget to fix structure after changes
```csharp
// WRONG:
RemoveCategory(target, "Heroes");
AddNewCategory(target, "Spells");
// Categories now have misaligned IDs!

// CORRECT:
RemoveCategory(target, "Heroes");
AddNewCategory(target, "Spells");
FixCategoryIdsForOldFormat(target); // Realign everything!
```

---

## 📚 LIBRARY USAGE

### War3Net.Build.Script Namespace

**Key Classes:**
- `MapTriggers` - Container for all triggers and categories
- `TriggerCategoryDefinition` - Category object
- `TriggerDefinition` - Trigger object
- `VariableDefinition` - Variable object
- `TriggerFunction` - Function/action in trigger
- `TriggerFunctionParameter` - Parameter to function

**Properties:**
```csharp
// MapTriggers
.FormatVersion      // v7 for old format
.SubVersion         // null for old format, v4 for enhanced
.GameVersion        // Usually 2
.TriggerItems       // List<TriggerItem> - categories and triggers
.Variables          // List<VariableDefinition>

// TriggerCategoryDefinition
.Id                 // MUST equal position in TriggerItems
.ParentId           // MUST be 0 for old format
.Name               // Category name
.IsComment          // Is commented out?
.IsExpanded         // Is expanded in editor?
.Type               // TriggerItemType.Category

// TriggerDefinition
.Id                 // Always 0 for old format (not saved)
.ParentId           // Category position (POSITION INDEX!)
.Name               // Trigger name
.Description        // Comment
.IsEnabled          // Is trigger enabled?
.Functions          // List of trigger functions
```

### File I/O

**Read from MPQ archive:**
```csharp
using var archive = MpqArchive.Open(mapPath, readOnly: true);
using var stream = archive.OpenFile("war3map.wtg");
using var reader = new BinaryReader(stream);
// Use reflection to call internal constructor
```

**Write to MPQ archive:**
```csharp
var builder = new MpqArchiveBuilder(originalArchive);
builder.RemoveFile("war3map.wtg");
builder.AddFile(MpqFile.New(newWtgStream, "war3map.wtg"));
builder.SaveTo(outputPath);
```

---

## 🔑 THE GOLDEN RULES (SUMMARY)

For WC3 1.27 Old Format:
1. **Category ID = Category Position** (always)
2. **Category ParentId = 0** (always)
3. **Trigger ParentId = Category Position** (where it belongs)
4. **SubVersion = null** (never change)
5. **Categories BEFORE Triggers** (in TriggerItems array)
6. **Trigger IDs = 0** (normal, not saved)
7. **Variable IDs = 0** (normal, not saved)

**The Core Truth:**
In WC3 1.27 old format, trigger `ParentId` is a POSITION INDEX into the TriggerItems array. Category IDs MUST equal their positions for the lookup to work. This is not optional - this is how the format works.

---

## 📝 ADDITIONAL REQUIREMENTS

1. **Error Handling:**
   - Wrap main logic in try-catch
   - Show clear error messages to user
   - Exit gracefully on errors

2. **Validation:**
   - Validate file paths before operations
   - Check map format (old vs enhanced)
   - Verify category structure before save

3. **User Feedback:**
   - Show progress messages during operations
   - Indicate success/failure clearly
   - Provide actionable error messages

4. **Code Quality:**
   - Use meaningful variable names
   - Add comments for complex logic
   - Follow C# naming conventions
   - Keep functions focused (single responsibility)

---

## 🚀 DELIVERABLES

Please create:
1. **Program.cs** - Complete implementation with all functions
2. **WTGMerger.csproj** - Project configuration
3. **run.bat** - Batch script for Windows
4. **README.md** - Usage instructions and technical details

The code should be production-ready, fully functional, and tested for WC3 1.27 compatibility.

---

**END OF PROMPT**

---

# HOW TO USE THIS PROMPT

1. Copy everything from "TASK: Create a Warcraft 3 1.27 Trigger Merger Program" to "END OF PROMPT"
2. Paste into AI assistant (Claude, ChatGPT, etc.)
3. AI will generate complete program code
4. Save files and test with your WC3 maps

The generated program should work correctly for WC3 1.27 old format maps.
