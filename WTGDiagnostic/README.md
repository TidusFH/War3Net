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

### With WTG files:
```bash
dotnet run --project WTGDiagnostic.csproj source.wtg target.wtg merged.wtg
```

### With map archives:
```bash
dotnet run --project WTGDiagnostic.csproj source.w3x target.w3x merged.w3x
```

The tool automatically extracts war3map.wtg from .w3x archives.

## Example Workflow

1. **Run WTGMerger** to create a merged output:
   ```bash
   cd WTGMerger/bin/Debug/net8.0
   WTGMerger.exe source.w3x target.w3x merged.w3x
   ```

2. **Run diagnostic** to analyze the output:
   ```bash
   cd WTGDiagnostic
   dotnet run source.w3x target.w3x merged.w3x
   ```

3. **Review the output**:
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
