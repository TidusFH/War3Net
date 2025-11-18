# WC3 Trigger Format - Quick Reference

## TL;DR - Critical Rules

### ✅ DO
- ✅ Keep category IDs non-sequential (0,1,2,4,3,6,8,25... is fine!)
- ✅ Leave trigger IDs as 0 (duplicate IDs are normal in 1.27)
- ✅ Preserve ParentIds exactly as read from file
- ✅ Keep categories before triggers in TriggerItems list
- ✅ Process Source and Target identically

### ❌ DON'T
- ❌ Renumber category IDs to sequential
- ❌ Try to "fix" duplicate trigger IDs
- ❌ Recalculate ParentIds
- ❌ Call FixDuplicateIds on Target
- ❌ Modify category IDs after reading from file

## Format Version Detection

```csharp
bool is127Format = triggers.SubVersion == null;
```

## What's Stored in File (1.27)

| Data | Stored? | Notes |
|------|---------|-------|
| Category ID | ✅ | Can be non-sequential |
| Category Name | ✅ | |
| Category ParentId | ❌ | Defaults to 0 |
| Trigger ID | ❌ | All default to 0 |
| Trigger Name | ✅ | |
| Trigger ParentId | ✅ | Must match category ID |
| Trigger Functions | ✅ | Events/Conditions/Actions |

## File Structure (1.27)

```
╔════════════════════╗
║ Header             ║
║ Format Version     ║
╠════════════════════╣
║ ALL CATEGORIES     ║ ← Must be first
╠════════════════════╣
║ Game Version       ║
╠════════════════════╣
║ ALL VARIABLES      ║
╠════════════════════╣
║ ALL TRIGGERS       ║ ← Must be last
╚════════════════════╝
```

Cannot intersperse categories and triggers!

## Category-Trigger Relationship

```
Category: "My Category"
├─ Id: 17 (from file)
├─ ParentId: 0 (not stored, defaults to 0)
└─ Visual position: determined by file order

Trigger: "My Trigger"
├─ Id: 0 (not stored, defaults to 0)
├─ ParentId: 17 (from file) ← MUST MATCH CATEGORY ID
└─ Appears under category with matching ID
```

## Common Operations

### Safe: Reordering Items
```csharp
// Safe - just changes order, keeps IDs
var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

triggers.TriggerItems.Clear();
triggers.TriggerItems.AddRange(categories);  // Categories first
triggers.TriggerItems.AddRange(triggerDefs); // Triggers second
```

### Unsafe: Changing IDs
```csharp
// DANGEROUS - breaks ParentId relationships!
for (int i = 0; i < categories.Count; i++)
{
    categories[i].Id = i;  // ❌ DON'T DO THIS!
}
```

### Safe: Adding New Triggers
```csharp
// Find a unique category ID
int newCategoryId = triggers.TriggerItems
    .OfType<TriggerCategoryDefinition>()
    .Select(c => c.Id)
    .DefaultIfEmpty(-1)
    .Max() + 1;

var newCategory = new TriggerCategoryDefinition
{
    Id = newCategoryId,  // ✅ Use unique non-sequential ID
    Name = "New Category",
    ParentId = 0,  // Root level
    IsComment = false
};

var newTrigger = new TriggerDefinition
{
    Id = 0,  // ✅ All triggers have Id=0 in 1.27
    ParentId = newCategoryId,  // ✅ Points to category
    Name = "New Trigger",
    IsEnabled = true
};
```

## Debugging Category Issues

### Symptom: Triggers in Wrong Categories

**Check:**
1. Are category IDs non-sequential? → This is normal! ✅
2. Do trigger ParentIds match category IDs? → They should match exactly
3. Was FixDuplicateIds called? → It shouldn't be! Disable it
4. Were category IDs renumbered? → They shouldn't be! Keep original IDs

### Symptom: Triggers Missing from Categories

**Check:**
1. Are triggers after categories in TriggerItems list? → They must be
2. Is file order: categories, variables, triggers? → Correct order
3. Was ParentId recalculated? → Should preserve original

### Symptom: Duplicate ID Warning

**For Triggers**: This is normal in 1.27! All triggers have Id=0. Ignore it.

**For Categories**: This is abnormal. Check if categories were read correctly.

## Version Differences

### WC3 1.27 (Original)
- `SubVersion == null`
- Category IDs: Stored in file
- Category ParentIds: NOT stored
- Trigger IDs: NOT stored
- Trigger ParentIds: Stored in file
- Hierarchy: File order + ParentId matching

### WC3 Reforged (Newer)
- `SubVersion != null`
- All IDs stored in file
- All ParentIds stored in file
- More metadata (IsExpanded, etc.)
- More flexible structure

## BetterTriggers Compatibility

BetterTriggers uses recursive ParentId-based traversal:
- Relies on correct ParentId matching
- No special fixing logic
- If WTGMerger displays correctly, BetterTriggers will too

## Testing Checklist

After making changes:

- [ ] Load Source with Option 1 - should show correct category structure
- [ ] Load Target with Option 2 - should show correct category structure
- [ ] Compare with World Editor - trigger counts should match
- [ ] Compare with BetterTriggers - structure should match
- [ ] Check specific problematic categories (e.g., "Obelisks Arthas")
- [ ] Verify no duplicate ID warnings (or acceptable ones for triggers)
- [ ] Ensure categories appear before triggers in TriggerItems

## Emergency Fix

If categories are corrupted in output file:

1. **Check WTGMerger code:**
   - Is `FixDuplicateIds` disabled? (Line ~103)
   - Does `FixFileOrder` NOT renumber IDs? (Line ~2687)
   - Does `RenumberCategoriesSequentially` NOT renumber IDs? (Line ~2742)

2. **Verify original file is good:**
   - Load it as Source (Option 1)
   - If Source displays correctly, file is good

3. **Check processing:**
   - Are Source and Target processed the same way?
   - Are any Target-only "fixes" being applied?

## Key War3Net Files

```
src/War3Net.Build.Core/
├── Script/
│   ├── MapTriggers.cs              (main structure)
│   ├── TriggerItem.cs              (base: Id, ParentId)
│   ├── TriggerCategoryDefinition.cs
│   └── TriggerDefinition.cs
├── Serialization/Binary/Script/
│   ├── MapTriggers.cs              (read/write logic)
│   ├── TriggerCategoryDefinition.cs (category read/write)
│   └── TriggerDefinition.cs        (trigger read/write)
└── Extensions/
    └── BinaryReaderExtensions.cs   (helper methods)
```

## References

- HiveWE Spec: https://github.com/stijnherfst/HiveWE/wiki/war3map.wtg-Triggers
- WC3C Specs: http://www.wc3c.net/tools/specs/index.html
- War3Net GitHub: https://github.com/Drake53/War3Net
- BetterTriggers: Uses ParentId-based recursive traversal
