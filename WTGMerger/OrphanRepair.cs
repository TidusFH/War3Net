using System;
using System.Collections.Generic;
using System.Linq;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Utility for repairing orphaned triggers with invalid ParentIds
    /// </summary>
    public static class OrphanRepair
    {
        /// <summary>
        /// Finds and repairs orphaned triggers that reference non-existent categories
        /// </summary>
        /// <param name="triggers">The MapTriggers to repair</param>
        /// <param name="mode">Repair mode: "root" sets ParentId=-1, "smart" tries to match by name</param>
        /// <returns>Number of triggers repaired</returns>
        public static int RepairOrphanedTriggers(MapTriggers triggers, string mode = "smart")
        {
            var categories = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .ToList();

            var validCategoryIds = new HashSet<int>(categories.Select(c => c.Id));
            validCategoryIds.Add(-1); // Root level is always valid

            var triggerDefs = triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .ToList();

            var orphanedTriggers = triggerDefs
                .Where(t => t.ParentId >= 0 && !validCategoryIds.Contains(t.ParentId))
                .ToList();

            if (orphanedTriggers.Count == 0)
            {
                return 0;
            }

            Console.WriteLine($"\n[ORPHAN REPAIR] Found {orphanedTriggers.Count} orphaned trigger(s)");

            int repairedCount = 0;

            foreach (var trigger in orphanedTriggers)
            {
                int oldParentId = trigger.ParentId;
                int newParentId = -1; // Default to root

                if (mode == "smart")
                {
                    // Try to find matching category by name similarity
                    newParentId = FindBestMatchingCategory(trigger, categories);
                }

                trigger.ParentId = newParentId;
                repairedCount++;

                var categoryName = newParentId == -1
                    ? "<Root>"
                    : categories.FirstOrDefault(c => c.Id == newParentId)?.Name ?? "Unknown";

                Console.WriteLine($"  Repaired: '{trigger.Name}' (was ParentId={oldParentId}) → '{categoryName}' (ParentId={newParentId})");
            }

            return repairedCount;
        }

        /// <summary>
        /// Tries to find the best matching category for an orphaned trigger
        /// based on naming patterns
        /// </summary>
        private static int FindBestMatchingCategory(TriggerDefinition trigger, List<TriggerCategoryDefinition> categories)
        {
            // Common naming patterns
            var triggerName = trigger.Name.ToLower();

            // Pattern 1: "Init XX" triggers → "Initialization" category
            if (triggerName.StartsWith("init ") || triggerName == "initialization")
            {
                var initCategory = categories.FirstOrDefault(c =>
                    c.Name.Equals("Initialization", StringComparison.OrdinalIgnoreCase));
                if (initCategory != null)
                {
                    return initCategory.Id;
                }
            }

            // Pattern 2: Trigger name contains category name
            // E.g., "Obelisk Setup" → "Obelisks" category
            foreach (var category in categories)
            {
                if (triggerName.Contains(category.Name.ToLower()))
                {
                    return category.Id;
                }
            }

            // Pattern 3: Category name contains trigger name prefix
            // E.g., "Arthas Special Effect" → "Arthas ..." category
            var triggerPrefix = triggerName.Split(' ').FirstOrDefault();
            if (!string.IsNullOrEmpty(triggerPrefix) && triggerPrefix.Length > 3)
            {
                var matchingCategory = categories.FirstOrDefault(c =>
                    c.Name.ToLower().Contains(triggerPrefix));
                if (matchingCategory != null)
                {
                    return matchingCategory.Id;
                }
            }

            // No match found, return -1 for root level
            return -1;
        }

        /// <summary>
        /// Diagnostic report showing orphaned triggers and categories
        /// </summary>
        public static void DiagnoseOrphans(MapTriggers triggers)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           ORPHAN DIAGNOSTIC REPORT                       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            var categories = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .ToList();

            var validCategoryIds = new HashSet<int>(categories.Select(c => c.Id));
            validCategoryIds.Add(-1); // Root level

            // Find orphaned triggers
            var orphanedTriggers = triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId >= 0 && !validCategoryIds.Contains(t.ParentId))
                .ToList();

            // Find orphaned categories
            var orphanedCategories = categories
                .Where(c => c.ParentId >= 0 && !validCategoryIds.Contains(c.ParentId))
                .ToList();

            Console.WriteLine($"\n=== ORPHANED TRIGGERS ===");
            if (orphanedTriggers.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ No orphaned triggers found");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Found {orphanedTriggers.Count} orphaned trigger(s):");
                Console.ResetColor();

                var grouped = orphanedTriggers.GroupBy(t => t.ParentId);
                foreach (var group in grouped)
                {
                    Console.WriteLine($"\n  ParentId={group.Key} (non-existent): {group.Count()} trigger(s)");
                    foreach (var trigger in group.Take(5))
                    {
                        Console.WriteLine($"    - {trigger.Name}");
                    }
                    if (group.Count() > 5)
                    {
                        Console.WriteLine($"    ... and {group.Count() - 5} more");
                    }
                }
            }

            Console.WriteLine($"\n=== ORPHANED CATEGORIES ===");
            if (orphanedCategories.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ No orphaned categories found");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Found {orphanedCategories.Count} orphaned category(ies):");
                Console.ResetColor();

                foreach (var cat in orphanedCategories)
                {
                    Console.WriteLine($"  - '{cat.Name}' (ID={cat.Id}, ParentId={cat.ParentId})");
                }
            }

            Console.WriteLine($"\n=== VALID CATEGORY IDS ===");
            Console.WriteLine($"Total categories: {categories.Count}");
            Console.WriteLine($"Valid IDs: {string.Join(", ", validCategoryIds.OrderBy(id => id).Take(20))}");
            if (validCategoryIds.Count > 20)
            {
                Console.WriteLine($"... and {validCategoryIds.Count - 20} more");
            }

            Console.WriteLine();
        }
    }
}
