# COMPLETE SETUP & USAGE GUIDE - WTGMerger127

## 📋 PREREQUISITES

### Required Software
1. **.NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Version: 8.0 or higher
   - Verify: Open Command Prompt, run `dotnet --version`

2. **Windows OS**
   - Windows 7 or higher
   - Command Prompt or PowerShell

3. **Warcraft 3 1.27**
   - World Editor 1.27
   - Maps must be old format (SubVersion=null)

---

## 📁 PROJECT STRUCTURE

```
War3Net/
├── Libs/                              (War3Net DLL files)
│   ├── War3Net.Build.Core.dll
│   ├── War3Net.Build.dll
│   ├── War3Net.Common.dll
│   └── War3Net.IO.Mpq.dll
├── Source/                            (Put source map here)
│   └── your_source_map.w3x
├── Target/                            (Put target map here)
│   └── your_target_map.w3x
└── WTGMerger127/
    ├── Program.cs                     (Main program)
    ├── WTGMerger127.csproj           (Project config)
    ├── run.bat                        (Run script)
    ├── README.md                      (Documentation)
    ├── QUICKSTART.md                  (Quick guide)
    └── DIFFERENCES.md                 (Comparison)
```

---

## 🚀 STEP-BY-STEP SETUP

### Step 1: Verify .NET Installation
```cmd
cd War3Net\WTGMerger127
dotnet --version
```
**Expected output:** `8.0.x` or higher

### Step 2: Build the Project
```cmd
dotnet build --configuration Release
```
**Expected output:** `Build succeeded. 0 Warning(s), 0 Error(s)`

### Step 3: Prepare Your Maps
- Place **SOURCE map** in `War3Net\Source\` folder
- Place **TARGET map** in `War3Net\Target\` folder
- Supported formats: `.w3x`, `.w3m`, or raw `war3map.wtg`

### Step 4: Run the Program
```cmd
run.bat
```
OR
```cmd
dotnet run --configuration Release
```

---

## 🎮 USING THE PROGRAM

### Main Menu Options

```
===============================================================
                    MERGE OPTIONS
===============================================================
1. List all categories from SOURCE
2. List all categories from TARGET
3. List triggers in a specific category
4. Copy ENTIRE category
5. Copy SPECIFIC trigger(s)
6. Show format & structure debug info
7. Manually fix target category IDs (if needed)
8. Toggle debug mode (currently: ON)
9. Save and exit
0. Exit without saving
```

### Common Workflows

#### Workflow 1: Copy One Entire Category
```
1. Select option "1" → See source categories
2. Select option "4" → Copy entire category
   Enter: "Hero Abilities"
3. Select option "9" → Save and exit
   Choose: "y" to delete war3map.j
```

#### Workflow 2: Copy Multiple Specific Triggers
```
1. Select option "1" → See source categories
2. Select option "3" → List triggers in category
   Enter: "Spells"
3. Select option "5" → Copy specific triggers
   Source category: "Spells"
   Trigger names: "Fireball, Ice Nova, Lightning"
   Destination: "Spells"
4. Select option "9" → Save and exit
```

#### Workflow 3: Copy Multiple Categories
```
1. Select option "4" → Copy category "Heroes"
2. Select option "4" → Copy category "Spells"
3. Select option "4" → Copy category "Items"
4. Select option "9" → Save and exit
```

---

## 🔍 VERIFICATION CHECKLIST

### After Merging
1. ✅ Check output file created: `War3Net\Target\your_map_merged.w3x`
2. ✅ Verify console shows: `+++ SAVE SUCCESSFUL! +++`
3. ✅ Check no errors during save

### In World Editor 1.27
1. ✅ Open merged map: `File → Open → your_map_merged.w3x`
2. ✅ Press F4 to open Trigger Editor
3. ✅ Verify categories appear (not nested under "Initialization")
4. ✅ Click each copied category → verify triggers are inside
5. ✅ Open a trigger → verify code is intact (events, conditions, actions)
6. ✅ Check variables panel → verify variables exist

### In-Game Test
1. ✅ Test Map (Ctrl+F9)
2. ✅ Verify copied triggers execute correctly
3. ✅ Check no errors in game

---

## ⚠️ TROUBLESHOOTING

### Problem 1: "dotnet: command not found"
**Solution:** Install .NET 8.0 SDK, restart Command Prompt

### Problem 2: "No map files found"
**Solution:**
- Check `Source/` and `Target/` folders exist
- Put at least one `.w3x` or `.w3m` file in each folder

### Problem 3: "Build failed"
**Solution:**
- Check `Libs/` folder contains all 4 DLL files
- Verify `WTGMerger127.csproj` references correct paths

### Problem 4: Triggers appear in wrong category
**Solution:**
- Select option "7" → Manually fix category IDs
- Re-save with option "9"

### Problem 5: "trigger data invalid" error in World Editor
**Solution:**
- When saving, choose "y" to delete war3map.j
- World Editor will regenerate it automatically

---

## 📊 WHAT GETS SAVED

### Old Format (WC3 1.27)
| Item | ID Saved? | ParentId Saved? | Notes |
|------|-----------|-----------------|-------|
| Category | ✅ YES | ❌ NO | ParentId always reads as 0 |
| Trigger | ❌ NO | ✅ YES | ParentId is POSITION INDEX |
| Variable | ❌ NO | N/A | All IDs become 0 (normal) |

### Important Notes
- Category IDs MUST equal their position (0, 1, 2, 3...)
- Trigger ParentId references category POSITION
- SubVersion MUST stay null (never change to v4!)

---

## 🎯 SUCCESS CRITERIA

Your merge is successful when:
- ✅ Console shows `+++ SAVE SUCCESSFUL! +++`
- ✅ Merged map opens in World Editor 1.27 without errors
- ✅ Triggers appear in correct categories (not all in "Initialization")
- ✅ Trigger code is intact (events, conditions, actions)
- ✅ Variables are accessible and correct type
- ✅ Map runs in-game without errors
- ✅ Copied triggers function correctly

---

## 🐛 DEBUG MODE

Enable with option "8" to see:
- Category ID → position mappings
- Trigger ParentId updates
- File format details
- In-memory structure validation

---

## 📝 COMMAND LINE USAGE

### Basic Usage
```cmd
cd WTGMerger127
dotnet run
```

### With Custom Paths
```cmd
dotnet run -- "C:\Maps\source.w3x" "C:\Maps\target.w3x" "C:\Maps\merged.w3x"
```

### With .wtg Files
```cmd
dotnet run -- "..\Source\war3map.wtg" "..\Target\war3map.wtg" "..\Target\war3map_merged.wtg"
```

---

## 🔑 KEY TECHNICAL DETAILS

### TriggerItems Structure (MUST BE THIS WAY!)
```
[0] Category "Initialization" (ID=0, ParentId=0)
[1] Category "Heroes"         (ID=1, ParentId=0)
[2] Category "Spells"         (ID=2, ParentId=0)
[3] Trigger "Map Init"        (ParentId=0 → points to [0])
[4] Trigger "Hero Spawn"      (ParentId=1 → points to [1])
[5] Trigger "Fireball"        (ParentId=2 → points to [2])
```

### Why This Matters
- WC3 1.27 uses trigger ParentId as ARRAY INDEX
- `Trigger(ParentId=2)` → WC3 looks at `TriggerItems[2]`
- If position 2 is not a category → CRASH or wrong placement!

### The Golden Rules
1. Category ID = Category Position (always!)
2. Category ParentId = 0 (always for old format)
3. Trigger ParentId = Category Position (where it belongs)
4. SubVersion = null (NEVER change!)
5. ALL categories BEFORE ALL triggers in TriggerItems
6. Trigger IDs = 0 (normal, not saved in old format)
7. Variable IDs = 0 (normal, not saved in old format)

---

## 📞 SUPPORT

### If Something Goes Wrong
1. Enable debug mode (option 8)
2. Check comprehensive debug info (option 6)
3. Verify format: SubVersion should be "null"
4. Check category structure shows position = ID

### File Locations
- **Output:** `War3Net\Target\your_map_merged.w3x`
- **Logs:** Console output (copy if needed)

---

## ✅ FINAL CHECKLIST

Before using in production:
- [ ] .NET 8.0 SDK installed and working
- [ ] Source and Target folders created
- [ ] Maps placed in correct folders
- [ ] Build succeeded without errors
- [ ] Test merge completed successfully
- [ ] Merged map opened in World Editor 1.27
- [ ] Triggers appear in correct categories
- [ ] Map tested in-game
- [ ] All triggers function correctly

---

## 🎓 UNDERSTANDING THE CODE

### Core Functions
1. **FixCategoryIdsForOldFormat()** - Called after load, ensures position=ID
2. **CopySpecificTriggers()** - Copies triggers, maintains structure
3. **MergeCategory()** - Merges entire category with all triggers
4. **SaveMergedMap()** - Validates and saves with format preservation

### Safety Features
- Pre-save validation (checks category structure)
- Post-save verification (reads file back)
- Automatic structure fixing (repositions categories)
- SubVersion preservation (never changes)
- Comprehensive error messages

---

## 🚀 YOU'RE READY!

The program is designed to be **foolproof** for WC3 1.27 old format.

Just remember:
1. Put maps in Source/ and Target/ folders
2. Run `run.bat`
3. Follow the menu
4. Test in World Editor 1.27

**Good luck with your map merging!** 🎮
