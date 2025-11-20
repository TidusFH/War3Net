# Warcraft 3 Trigger Format Research Documentation

This folder contains comprehensive documentation about the Warcraft 3 trigger file format (.wtg), based on research conducted during WTGMerger development and debugging.

## Documents

### 📘 [Quick-Reference-Guide.md](Quick-Reference-Guide.md)
**Start here!** TL;DR version with critical do's and don'ts, common operations, and debugging checklists.

**Best for:**
- Quick lookups during development
- "What was that rule again?" moments
- Emergency troubleshooting
- New developers getting started

### 📗 [WC3-1.27-Format-Specification.md](WC3-1.27-Format-Specification.md)
Detailed technical specification of the WC3 1.27 trigger file format.

**Covers:**
- File structure and layout
- What data is saved vs. what defaults
- Category and trigger properties
- ParentId relationships
- Non-sequential ID behavior
- Reading and writing process

**Best for:**
- Understanding how the format works
- Reference during implementation
- Learning format internals

### 📕 [War3Net-Implementation-Details.md](War3Net-Implementation-Details.md)
Deep dive into how War3Net library reads and writes trigger files.

**Covers:**
- War3Net source code analysis
- Reading/writing implementation details
- Round-trip behavior (read → write → read)
- Type hierarchy and class structure
- Extension methods
- Practical code examples

**Best for:**
- Working with War3Net library
- Understanding War3Net behavior
- Code integration examples
- Advanced implementation details

### 📙 [Bug-Investigation-Log.md](Bug-Investigation-Log.md)
Complete chronological log of bugs found and fixed in WTGMerger.

**Covers:**
- Timeline of investigation
- Each bug's root cause and fix
- Test evidence and validation
- Lessons learned
- What went wrong and why

**Best for:**
- Understanding what bugs were fixed
- Learning from mistakes
- Troubleshooting similar issues
- Historical context

## Key Discoveries

### 🎯 Most Important Findings

1. **Non-Sequential Category IDs Are Normal**
   - World Editor creates maps with non-sequential IDs (0,1,2,4,3,6,8,25...)
   - War3Net handles them perfectly
   - Never renumber category IDs!

2. **Duplicate Trigger IDs Are Normal (1.27)**
   - All triggers have `Id = 0` after loading
   - This is expected behavior
   - War3Net handles it correctly
   - Don't try to "fix" it!

3. **ParentIds Are Sacred**
   - Category IDs and trigger ParentIds are read from file
   - They match correctly as-is
   - Preserve them exactly - don't recalculate!

4. **File Order Matters**
   - Categories must appear before triggers in TriggerItems list
   - Required for 1.27 format writing
   - But position doesn't determine category membership - ParentId does!

5. **Process Source and Target Identically**
   - If Source displays correctly, use same processing for Target
   - Don't add Target-only "fixes" that Source doesn't need
   - Differential processing causes corruption

## Quick Start

### For New Developers

1. Read [Quick-Reference-Guide.md](Quick-Reference-Guide.md) first
2. Review the "DO/DON'T" section carefully
3. Check out code examples
4. Refer to other docs as needed

### For Troubleshooting

1. Check symptoms in [Quick-Reference-Guide.md](Quick-Reference-Guide.md) "Debugging" section
2. Review [Bug-Investigation-Log.md](Bug-Investigation-Log.md) for similar issues
3. Verify format behavior in [WC3-1.27-Format-Specification.md](WC3-1.27-Format-Specification.md)
4. Check War3Net behavior in [War3Net-Implementation-Details.md](War3Net-Implementation-Details.md)

### For Implementation

1. Review DO/DON'T rules in [Quick-Reference-Guide.md](Quick-Reference-Guide.md)
2. Study code examples in [War3Net-Implementation-Details.md](War3Net-Implementation-Details.md)
3. Understand format constraints in [WC3-1.27-Format-Specification.md](WC3-1.27-Format-Specification.md)
4. Learn from past mistakes in [Bug-Investigation-Log.md](Bug-Investigation-Log.md)

## Common Questions

### Q: Why are category IDs non-sequential?
**A:** World Editor assigns IDs incrementally but doesn't renumber when categories are deleted. This creates gaps. It's normal and works perfectly fine.

### Q: Should I renumber category IDs to be sequential?
**A:** **NO!** This breaks trigger ParentId relationships. Keep original IDs unchanged.

### Q: Why do all triggers have the same ID (0)?
**A:** In 1.27 format, trigger IDs aren't stored in the file. They all default to 0 when loaded. This is normal.

### Q: Should I fix duplicate trigger IDs?
**A:** **NO!** Duplicate trigger IDs are normal in 1.27 format. War3Net handles them correctly.

### Q: How do triggers know which category they belong to?
**A:** `trigger.ParentId` must match `category.Id`. If ParentId = 17, trigger appears under category with Id = 17.

### Q: Why isn't my Target file displaying correctly?
**A:** Most likely you're calling a "fix" function that modifies IDs or ParentIds. Check if processing differs from Source.

### Q: Can I use file position to determine category membership?
**A:** **NO!** In 1.27 format, all categories come before all triggers. Position doesn't determine membership - ParentId matching does.

### Q: What's the difference between 1.27 and Reforged formats?
**A:** See [WC3-1.27-Format-Specification.md](WC3-1.27-Format-Specification.md) "What Gets Saved" section for detailed comparison.

## Testing Guidelines

After making changes to WTGMerger:

1. ✅ Load same file as Source (Option 1) and Target (Option 2)
2. ✅ Verify both display identical category structures
3. ✅ Compare trigger counts with World Editor
4. ✅ Test with BetterTriggers for validation
5. ✅ Check problematic categories (e.g., "Obelisks Arthas")
6. ✅ Verify file order: categories before triggers

## Key Rules (Summary)

### ✅ Always Do
- Keep category IDs non-sequential (they work fine!)
- Preserve trigger ParentIds exactly as read
- Keep categories before triggers in TriggerItems list
- Process Source and Target identically
- Match trigger.ParentId to category.Id for category assignment

### ❌ Never Do
- Renumber category IDs to sequential
- Try to "fix" duplicate trigger IDs
- Recalculate ParentIds based on position
- Add Target-only processing that Source doesn't need
- Rely on file position for category membership

## Related Resources

- **War3Net GitHub**: https://github.com/Drake53/War3Net
- **HiveWE Format Spec**: https://github.com/stijnherfst/HiveWE/wiki/war3map.wtg-Triggers
- **WC3C Specs**: http://www.wc3c.net/tools/specs/index.html
- **BetterTriggers**: Uses ParentId-based recursive traversal

## Contributing

If you discover new format behaviors or fix additional bugs:

1. Document findings in appropriate file
2. Update Quick Reference Guide with new rules
3. Add examples to War3Net Implementation Details
4. Log bugs in Bug Investigation Log

## Version History

- **2024-01**: Initial documentation created during WTGMerger debugging
- Major bugs fixed:
  - Category ID renumbering corruption
  - FixDuplicateIds destroying ParentId relationships
  - Variable index remapping issues
  - Comment category trigger assignment

## License

This documentation is part of the War3Net project. See main LICENSE file for details.
