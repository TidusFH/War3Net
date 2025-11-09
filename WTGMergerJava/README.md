# WTG Test Tool - wc3libs Proof of Concept

A simple tool to validate that **wc3libs** can correctly read and write WTG files (unlike War3Net which produces empty/corrupted output).

## The Problem

The C# version using War3Net has persistent bugs:
- ❌ Maps open but are completely empty (no triggers/variables/categories)
- ❌ Stream disposal issues
- ❌ Format corruption
- ❌ SubVersion handling bugs

## This Tool

This is a **proof-of-concept** to test if wc3libs works correctly:
- ✅ Reads a WTG file using wc3libs
- ✅ Writes it back using wc3libs
- ✅ You verify if it opens correctly in World Editor

**If this works**, we can confidently build a full merger in Java.

## Building

### Linux/Mac:
```bash
chmod +x build.sh run.sh
./build.sh
```

### Windows:
```batch
build.bat
```

## Usage

### Test a WTG file:
```bash
# Linux/Mac:
./run.sh input.wtg output.wtg

# Windows:
run.bat input.wtg output.wtg
```

### What to test:

1. **Extract war3map.wtg from your map:**
   - Use MPQ Editor to open your .w3x file
   - Extract `war3map.wtg` to a folder

2. **Run the tool:**
   ```bash
   ./run.sh war3map.wtg war3map_test.wtg
   ```

3. **Check if it worked:**
   - Copy `war3map_test.wtg` back into the map (replacing the original)
   - Open the map in World Editor
   - Check if all triggers, variables, and categories are present

### Expected Output:

```
╔═══════════════════════════════════════════════════════════╗
║         WTG Test Tool - wc3libs Proof of Concept         ║
╚═══════════════════════════════════════════════════════════╝

Reading: war3map.wtg
✓ WTG loaded successfully

Statistics:
  Variables:  24
  Triggers:   15
  Categories: 5

Sample variables:
  - udg_Hero (unit)
  - udg_MaxMana (real)
  - udg_SpellPower (integer)
  ... and 21 more

Writing: war3map_test.wtg
✓ WTG written successfully
  Output size: 12847 bytes

╔═══════════════════════════════════════════════════════════╗
║                       SUCCESS!                            ║
╚═══════════════════════════════════════════════════════════╝

Now test if the output WTG file opens correctly in World Editor.
If it does, wc3libs is working correctly.
```

## What's Next?

**If this test succeeds** (map opens with all data intact):
- ✅ wc3libs works correctly
- ✅ We can build a full WTG merger in Java
- ✅ Scrap the buggy C# version

**If this test fails** (map still empty):
- ❌ wc3libs also has issues
- ❌ Need to find another solution

## Troubleshooting

**Error: ClassNotFoundException**
- Make sure wc3libs is in `../wc3libs/`
- Run build script first

**Output file is empty/corrupted**
- This would mean wc3libs also has bugs
- Please report with your WTG file for investigation

## License

Same as the parent War3Net project.
