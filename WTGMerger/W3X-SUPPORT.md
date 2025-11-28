# Working with .w3x Map Archives Directly

## 🎉 NEW FEATURE: Direct Map Archive Support

WTGMerger now supports working directly with `.w3x` and `.w3m` map files! No more manual MPQ extraction!

---

## 🚀 Quick Start

### Option 1: Using Default Folders (EASIEST)

```
War3Net/
├── Source/
│   └── SourceMap.w3x         ← Your source map
└── Target/
    └── TargetMap.w3x         ← Your target map
    └── TargetMap_merged.w3x  ← Output will be saved here
```

Just place your `.w3x` files in the folders and run:
```cmd
WTGMerger\run.bat
```

### Option 2: Custom Paths

```cmd
cd WTGMerger
dotnet run -- "C:\Maps\Source.w3x" "C:\Maps\Target.w3x" "C:\Output\Merged.w3x"
```

---

## ✨ Key Features

### 1. **Auto-Detection**
The tool automatically detects whether you're using:
- Raw `.wtg` files
- Complete `.w3x` map archives
- Complete `.w3m` map archives

### 2. **Automatic .j Synchronization**
When saving to a `.w3x` file, the tool asks:

```
╔══════════════════════════════════════════════════════════╗
║           JASS CODE SYNCHRONIZATION                      ║
╚══════════════════════════════════════════════════════════╝

IMPORTANT: The war3map.j file (JASS code) must be synchronized
with the war3map.wtg file (trigger structure).

Do you want to DELETE war3map.j from the output map?
(World Editor will regenerate it when you open the map)

1. YES - Delete war3map.j (RECOMMENDED)
2. NO  - Keep war3map.j (may cause 'trigger data invalid' error)

Choice (1-2):
```

**Choose option 1 (RECOMMENDED)** to automatically remove `war3map.j` and let World Editor regenerate it correctly.

### 3. **No Manual MPQ Editing Required**
The tool handles everything internally:
- Extracts `war3map.wtg` from the archive
- Processes your trigger operations
- Saves the modified triggers back
- Optionally removes `war3map.j`
- All in one step!

---

## 📋 Complete Workflow Examples

### Example 1: Copy Triggers Between Maps

**Files:**
- `DefenseMap.w3x` - Has AI triggers you want
- `MyMap.w3x` - Your map

**Steps:**
1. Place files:
   ```
   Source/DefenseMap.w3x
   Target/MyMap.w3x
   ```

2. Run `WTGMerger\run.bat`

3. Menu:
   ```
   Select option: 5 (Copy SPECIFIC trigger(s))
   Enter category: AI
   Enter trigger name: AI Player 1
   Enter destination category: Custom AI
   ```

4. Save:
   ```
   Select option: 6 (Save and exit)
   Delete war3map.j? Choose 1 (YES)
   ```

5. Result: `Target/MyMap_merged.w3x`

6. Open `MyMap_merged.w3x` in World Editor
   - You'll see: "Generating trigger data..."
   - Wait for it to finish
   - Save the map (Ctrl+S)
   - Done! ✓

### Example 2: Mix .wtg and .w3x Files

You can also mix formats:
```cmd
dotnet run -- "Source.wtg" "Target.w3x" "Output.w3x"
```

The tool adapts automatically!

---

## 🔧 Technical Details

### What Happens Internally

1. **On Load:**
   ```
   Reading source: DefenseMap.w3x
     Opening MPQ archive...
     Extracting war3map.wtg...
   ✓ Source loaded: 150 items, 12 variables
   ```

2. **On Save (with .j deletion):**
   ```
   Preparing to save merged WTG to: Target_merged.w3x
     Copying map archive...
     Opening output archive...
     Removing old war3map.wtg...
     Adding updated war3map.wtg...
     Removing war3map.j for sync...
     Archive updated successfully!

   ✓ war3map.j has been removed from the output map
   ✓ World Editor will regenerate it when you open the map
   ```

### Files the Tool Handles

- `war3map.wtg` - Trigger structure (modified)
- `war3map.j` - JASS code (optionally removed)
- `scripts/war3map.j` - Alternative JASS location (also removed if present)
- All other map files - Copied unchanged

---

## 💡 Best Practices

### ✅ DO:
- Use `.w3x` files directly when possible
- Choose "YES" to delete war3map.j when prompted
- Always open the output map in World Editor after merging
- Wait for "Generating trigger data..." to complete
- Save the map in World Editor after opening

### ❌ DON'T:
- Skip opening the output map in World Editor
- Try to use the output map directly in game without World Editor processing
- Choose "NO" for .j deletion unless you know what you're doing

---

## 🐛 Troubleshooting

### Issue: "trigger data invalid" error
**Cause:** war3map.j was not deleted
**Solution:**
1. Run the tool again
2. This time choose "YES" when asked to delete war3map.j
3. Or manually remove war3map.j using MPQ Editor

### Issue: Map won't load in World Editor
**Cause:** Possible corruption or incompatible version
**Solution:**
1. Check validation output (should be all green ✓)
2. Try with a backup of your original map
3. Ensure you're using WC3 version 1.27+ or Reforged

### Issue: Tool says "war3map.wtg not found in map archive"
**Cause:** The map file is corrupted or not a valid WC3 map
**Solution:**
1. Try opening the map in World Editor first
2. Save it, then use it with the tool
3. Check if the .w3x file is actually a renamed folder

---

## 🔄 Comparison: Old vs New Workflow

### OLD Workflow (Manual):
1. Open map in MPQ Editor
2. Extract war3map.wtg
3. Run WTGMerger on .wtg files
4. Get war3map_merged.wtg
5. Open MPQ Editor again
6. Delete war3map.j from map
7. Replace war3map.wtg with merged version
8. Save map
9. Open in World Editor
10. Wait for regeneration
11. Save again

**Total: 11 steps** 😓

### NEW Workflow (Automatic):
1. Run WTGMerger with .w3x files
2. Choose "YES" to delete .j
3. Open output in World Editor
4. Save

**Total: 4 steps** 🎉

---

## 📖 Additional Resources

- **SYNCING-WTG-WITH-J.md** - Detailed explanation of .wtg/.j synchronization
- **README.md** - Complete feature list and documentation
- **WHERE-ARE-MY-FILES.md** - File organization guide
- **DEBUGGING-GUIDE.md** - Troubleshooting validation errors

---

## 🎯 Summary

**The New Way:**
```cmd
# Just use .w3x files directly!
run.bat

# Tool detects map archives automatically
# Asks if you want to sync .j file
# Handles everything internally
# Output is ready to open in World Editor
```

**No more:**
- ❌ MPQ Editor manual extraction
- ❌ Manual .j file deletion
- ❌ Manual repacking
- ❌ Complex multi-step workflow

**Result:**
✅ Simple, automated, error-free trigger merging!

---

**Questions? Issues?**
- Check the validation output (always shown before save)
- Read SYNCING-WTG-WITH-J.md for .j sync details
- Ensure you choose "YES" to delete .j when prompted
