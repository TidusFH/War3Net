# TriggerMerger - Quick Start Guide

Get started with TriggerMerger in 5 minutes.

## Installation

### Step 1: Install .NET 5.0

**Windows:**
Download from: https://dotnet.microsoft.com/download/dotnet/5.0

**Linux (Ubuntu/Debian):**
```bash
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-5.0
```

**Linux (Other) / macOS:**
Visit: https://dotnet.microsoft.com/download/dotnet/5.0

### Step 2: Build TriggerMerger

```bash
cd /home/user/War3Net/tools/War3Net.Tools.TriggerMerger
dotnet build -c Release
```

### Step 3: Run It

```bash
# Linux/Mac
cd bin/Release/net5.0
./TriggerMerger --help

# Windows
cd bin\Release\net5.0
TriggerMerger.exe --help
```

### Optional: Add to PATH

**Linux/Mac:**
```bash
# Add to ~/.bashrc or ~/.zshrc
export PATH="$PATH:/home/user/War3Net/tools/War3Net.Tools.TriggerMerger/bin/Release/net5.0"
```

**Windows:**
Add `C:\path\to\War3Net\tools\War3Net.Tools.TriggerMerger\bin\Release\net5.0` to your PATH environment variable.

## Basic Usage

### See What's in a Map

```bash
TriggerMerger list --map "mymap.w3x"
```

### Copy a Trigger Folder

```bash
TriggerMerger copy-category \
  --source "map_with_spells.w3x" \
  --target "my_empty_map.w3x" \
  --category "spells"
```

That's it! Your map now has the spells category with all its triggers.

## Common Scenarios

### Scenario 1: "I want to copy spells from one map to another"

```bash
# Preview first (optional)
TriggerMerger copy-category -s source.w3x -t target.w3x -c "spells" --dry-run

# Do the actual copy
TriggerMerger copy-category -s source.w3x -t target.w3x -c "spells"

# Result: target_merged.w3x created with spells
```

### Scenario 2: "The category already exists, I want to update it"

```bash
TriggerMerger copy-category \
  -s new_version.w3x \
  -t old_version.w3x \
  -c "spells" \
  --overwrite
```

### Scenario 3: "I want to copy multiple folders at once"

```bash
TriggerMerger copy-category \
  -s library.w3x \
  -t mymap.w3x \
  -c "spells" -c "items" -c "systems"
```

### Scenario 4: "I want to see exactly what triggers are in each folder"

```bash
TriggerMerger list --map "mymap.w3x" --detailed
```

## Tips

💡 **Always use --dry-run first** to preview changes
```bash
TriggerMerger copy-category -s source.w3x -t target.w3x -c "spells" --dry-run
```

💡 **Backups are automatic** - Your original file is safe. Look for `.backup_*` files.

💡 **Category names are case-insensitive** - "Spells", "spells", "SPELLS" all work

💡 **Use quotes for names with spaces**
```bash
TriggerMerger copy-category -s a.w3x -t b.w3x -c "Hero Abilities"
```

## Troubleshooting

### Problem: "dotnet: command not found"
**Solution:** Install .NET SDK 5.0 (see Step 1 above)

### Problem: "Map file not found"
**Solution:** Use absolute paths or check your current directory
```bash
# Use full path
TriggerMerger list --map "/full/path/to/map.w3x"

# Or navigate to the directory first
cd /path/to/maps
TriggerMerger list --map "map.w3x"
```

### Problem: "Category not found"
**Solution:** List categories first to see exact names
```bash
TriggerMerger list --map "source.w3x"
```

### Problem: "Could not read triggers from map"
**Solution:**
- Verify the file is a valid .w3x or .w3m file
- Try opening it in World Editor first
- The map might use only custom script (JASS), not GUI triggers

## Need Help?

- Full documentation: See `README.md` in this directory
- War3Net project: https://github.com/Drake53/War3Net
- Report issues: https://github.com/Drake53/War3Net/issues

## Version Compatibility

✅ Works with **Warcraft 3 v1.27** and all newer versions (including Reforged)

## What's Next?

Read the full [README.md](README.md) for:
- All command options
- Advanced usage examples
- Technical details
- Version compatibility info

Happy mapping! 🎮
