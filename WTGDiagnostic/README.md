# WTG Binary Diagnostic Tool

A diagnostic tool to identify exactly what War3Net is doing wrong when writing WTG files.

## Purpose

This tool compares three WTG files to diagnose why War3Net produces empty/corrupted output:
1. **Source WTG** (1.31 format) - Known good input
2. **Target WTG** (1.27 format) - Known good input
3. **Merged WTG** - War3Net's output (suspected broken)

## What It Does

1. **Parses all three files** with War3Net to show:
   - Format version, SubVersion, game version
   - Variable counts
   - Trigger/category counts
   - Sample variable names

2. **Dumps binary hex** for inspection:
   - First 512 bytes of each file
   - Shows file headers and structure

3. **Compares byte-by-byte**:
   - Highlights differences between merged and target
   - Shows exact offsets where data differs
   - Calculates percentage of different bytes

4. **Analyzes WTG structure**:
   - Validates file ID ('WTG!')
   - Checks format version
   - Verifies variable count in header
   - Detects corrupted/empty files

## Building

```bash
dotnet build WTGDiagnostic.csproj
```

## Usage

### Default Mode (Recommended):
The tool looks for files in these default locations:
- `Source/war3map.wtg`
- `Target/war3map.wtg`
- `Target/war3map_merged.wtg`

Just run:
```bash
dotnet run
```

Or on Windows:
```batch
run.bat
```

### Custom Paths:
You can also specify custom file paths:
```bash
dotnet run source.wtg target.wtg merged.wtg
```

Or with map archives (auto-extracts war3map.wtg):
```bash
dotnet run source.w3x target.w3x merged.w3x
```

## Example Workflow

1. **Prepare folders**:
   ```
   WTGDiagnostic/
     Source/
       war3map.wtg      (extract from source 1.31 map)
     Target/
       war3map.wtg      (extract from target 1.27 map)
       war3map_merged.wtg   (output from WTGMerger)
   ```

2. **Extract WTG files from maps** (using MPQ Editor or similar):
   - Extract from source.w3x → `Source/war3map.wtg`
   - Extract from target.w3x → `Target/war3map.wtg`

3. **Run WTGMerger** to create merged output:
   ```bash
   cd WTGMerger/bin/Debug/net8.0
   WTGMerger.exe source.w3x target.w3x output.w3x
   ```

4. **Extract merged WTG**:
   - Extract from output.w3x → `WTGDiagnostic/Target/war3map_merged.wtg`

5. **Run diagnostic**:
   ```bash
   cd WTGDiagnostic
   dotnet run
   ```

6. **Review the output**:
   - Check if MERGED has correct variable/trigger counts
   - Look for "[ERROR]" or "[WARNING]" messages
   - Review hex dumps to see if data looks valid
   - Check byte-by-byte differences

## What to Look For

### If MERGED file is truly empty:
```
[MERGED] Statistics:
  Variables: 0
  Trigger Items: 0
```
→ War3Net isn't writing any data at all

### If MERGED has correct counts but World Editor shows empty:
```
[MERGED] Statistics:
  Variables: 24
  Trigger Items: 15
```
→ Data is in memory, but binary output is corrupted

### If binary comparison shows 100% different:
```
Total differences: 12,847 bytes (100.00%)
```
→ War3Net is writing completely wrong format

### If WTG header is invalid:
```
[ERROR] Invalid file ID (expected 'WTG!', got '????')
```
→ War3Net corrupted the file header

## Output Files

The diagnostic tool doesn't create any files - it only reads and analyzes.
All output goes to the console.

## Next Steps Based on Results

Once we identify the problem:
- **Missing data**: Fix the serialization to actually write variables/triggers
- **Corrupted header**: Fix the format version / file ID writing
- **Wrong offsets**: Fix the binary layout calculation
- **Stream disposal**: Fix how memory streams are handled

This will point us to the exact bug in War3Net's writer.
