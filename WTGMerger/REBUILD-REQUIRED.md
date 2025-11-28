# 🔨 Rebuild Required After Updates

## Why Option 7 is Missing

You're seeing an **old compiled version** of WTGMerger. The new features (Options 7 & 8 for orphan repair) were added to the source code but need to be **rebuilt** to work.

## Quick Fix

### Windows:
```batch
rebuild.bat
```

Then run normally:
```batch
run.bat
```

### Linux/Mac:
```bash
./rebuild.sh
```

Then run:
```bash
dotnet run
```

## What Gets Added After Rebuild

You'll see these new options:

```
7. Repair orphaned triggers (fix invalid ParentIds)
8. Diagnose orphans (show orphaned triggers/categories)
9. DEBUG: Show comprehensive debug information
d. DEBUG: Toggle debug mode (currently: OFF)
s. Save and exit
```

## Why This Happens

When source code changes, .NET caches compiled binaries in `bin/` and `obj/` folders. The rebuild script:

1. **Deletes old compiled files** (`bin/`, `obj/`)
2. **Rebuilds from source** with latest changes
3. **Ensures you get new features**

## Alternative Manual Rebuild

If the scripts don't work, manually run:

```bash
# Clean
dotnet clean

# Rebuild
dotnet build --configuration Release

# Run
dotnet run --configuration Release
```

## Verify Rebuild Worked

After rebuilding, you should see:
- Options 1-9, d, s, 0 in the menu
- "Repair orphaned triggers" as option 7
- "Diagnose orphans" as option 8

If you still don't see them, try:
1. Delete `bin/` and `obj/` folders manually
2. Run `dotnet restore`
3. Run `dotnet build --no-incremental`
4. Run `dotnet run`

## What Changed

**Commit ac5dd87** added:
- `OrphanRepair.cs` - New file for orphan detection/repair
- Updated `Program.cs` - New menu options 7 & 8
- Documentation for orphan repair feature

These changes are in the source code but weren't compiled yet in your binary.
