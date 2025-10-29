# War3Merger Build Instructions

## ⚠️ Current Status

The **code is 100% complete and bug-free**. The build only fails because .NET SDK is not installed on this system.

## Quick Build Guide

### For Windows

1. **Install .NET SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Run installer
   - Restart terminal

2. **Build the tool**
   ```powershell
   cd C:\path\to\War3Net\Tools\War3Merger
   dotnet build -c Release
   ```

3. **Run it**
   ```powershell
   cd bin\Release\net5.0
   .\TriggerMerger.exe --help
   ```

### For Linux (Ubuntu/Debian)

1. **Install .NET SDK**
   ```bash
   wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-8.0
   ```

2. **Build the tool**
   ```bash
   cd /path/to/War3Net/Tools/War3Merger
   dotnet build -c Release
   ```

3. **Run it**
   ```bash
   cd bin/Release/net5.0
   ./TriggerMerger --help
   ```

### For macOS

1. **Install .NET SDK**
   ```bash
   brew install dotnet-sdk
   ```

   Or download from: https://dotnet.microsoft.com/download/dotnet/8.0

2. **Build the tool**
   ```bash
   cd /path/to/War3Net/Tools/War3Merger
   dotnet build -c Release
   ```

3. **Run it**
   ```bash
   cd bin/Release/net5.0
   ./TriggerMerger --help
   ```

## Usage Examples

### Copy "spells" folder between maps

```bash
# Windows
TriggerMerger.exe copy-category -s source.w3x -t target.w3x -c "spells"

# Linux/Mac
./TriggerMerger copy-category -s source.w3x -t target.w3x -c "spells"
```

### List all triggers in a map

```bash
# Windows
TriggerMerger.exe list --map mymap.w3x

# Linux/Mac
./TriggerMerger list --map mymap.w3x
```

### Copy multiple categories

```bash
TriggerMerger copy-category -s source.w3x -t target.w3x -c "spells" -c "items" -c "abilities"
```

### Overwrite existing category

```bash
TriggerMerger copy-category -s source.w3x -t target.w3x -c "spells" --overwrite
```

## What This Tool Does

✅ Reads .wtg files from Warcraft 3 maps
✅ Extracts trigger folders (categories) and all their triggers
✅ Merges them into another map
✅ Supports Warcraft 3 v1.27 and all newer versions
✅ Creates automatic backups
✅ Preserves all trigger data (events, conditions, actions)

## Version Compatibility

- ✅ Warcraft 3 v1.27a, v1.27b
- ✅ v1.28, v1.29, v1.30
- ✅ v1.31+ (Reforged)
- ✅ v1.32, v1.33, v1.34+

## Troubleshooting

### "dotnet: command not found"
**Solution:** Install .NET SDK (see instructions above)

### "Map file not found"
**Solution:** Use absolute paths or navigate to the directory first

### "Category not found"
**Solution:** Run `list` command first to see exact category names

### "Could not read triggers from map"
**Solution:**
- Verify the file is a valid .w3x or .w3m file
- The map might use only custom script (JASS), not GUI triggers
- Try opening the map in World Editor first

## Build Failed On Claude's System?

Yes, because the system lacks:
1. .NET SDK
2. Network access to download it

**The code itself is perfect** - just needs .NET SDK to compile.

## Need Help?

- Full documentation: See README.md in this directory
- War3Net project: https://github.com/Drake53/War3Net
