using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using War3Net.Build.Script;

namespace WTGMerger
{
    /// <summary>
    /// Comprehensive health check for war3map.wtg files
    /// Detects corruption, invalid references, and structural issues
    /// </summary>
    public static class WTGHealthCheck
    {
        public class HealthCheckResult
        {
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> Info { get; set; } = new List<string>();
            public Dictionary<string, int> Statistics { get; set; } = new Dictionary<string, int>();

            public bool IsHealthy => Errors.Count == 0;
        }

        /// <summary>
        /// Performs comprehensive health check on a MapTriggers file
        /// </summary>
        public static HealthCheckResult PerformHealthCheck(MapTriggers triggers, string filePath)
        {
            var result = new HealthCheckResult();

            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║       COMPREHENSIVE WTG HEALTH CHECK                    ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine($"\nAnalyzing: {filePath}");
            Console.WriteLine($"Format: {triggers.FormatVersion}, SubVersion: {triggers.SubVersion?.ToString() ?? "null (1.27 format)"}");
            Console.WriteLine();

            // Run all diagnostic checks
            CheckBasicStatistics(triggers, result);
            CheckIDCorruption(triggers, result);
            CheckDuplicateIDs(triggers, result);
            CheckOrphanedItems(triggers, result);
            CheckVariables(triggers, result);
            CheckFileOrder(triggers, result);
            CheckTriggerIntegrity(triggers, result);
            CheckBinaryCorruption(triggers, result);
            CheckCircularReferences(triggers, result);
            CheckIDRanges(triggers, result);

            // Print summary
            PrintHealthCheckSummary(result);

            return result;
        }

        private static void CheckBasicStatistics(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ BASIC STATISTICS ═══");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            result.Statistics["TotalItems"] = triggers.TriggerItems.Count;
            result.Statistics["Categories"] = categories.Count;
            result.Statistics["Triggers"] = triggerDefs.Count;
            result.Statistics["Variables"] = triggers.Variables.Count;

            Console.WriteLine($"  Total Items: {triggers.TriggerItems.Count}");
            Console.WriteLine($"  Categories: {categories.Count}");
            Console.WriteLine($"  Triggers: {triggerDefs.Count}");
            Console.WriteLine($"  Variables: {triggers.Variables.Count}");

            // Function counts
            int totalFunctions = 0;
            int events = 0, conditions = 0, actions = 0;
            foreach (var trigger in triggerDefs)
            {
                totalFunctions += trigger.Functions.Count;
                events += trigger.Functions.Count(f => f.Type == TriggerFunctionType.Event);
                conditions += trigger.Functions.Count(f => f.Type == TriggerFunctionType.Condition);
                actions += trigger.Functions.Count(f => f.Type == TriggerFunctionType.Action);
            }

            result.Statistics["TotalFunctions"] = totalFunctions;
            result.Statistics["Events"] = events;
            result.Statistics["Conditions"] = conditions;
            result.Statistics["Actions"] = actions;

            Console.WriteLine($"  Total Functions: {totalFunctions}");
            Console.WriteLine($"    Events: {events}");
            Console.WriteLine($"    Conditions: {conditions}");
            Console.WriteLine($"    Actions: {actions}");
            Console.WriteLine();
        }

        private static void CheckIDCorruption(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ ID CORRUPTION CHECK ═══");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            bool foundCorruption = false;
            int corruptCount = 0;

            // Check category IDs for corruption (typical range: 0-100, corrupted: > 1000)
            foreach (var cat in categories)
            {
                if (cat.Id > 10000)
                {
                    result.Errors.Add($"CORRUPTED CATEGORY ID: '{cat.Name}' has ID={cat.Id} (0x{cat.Id:X}) - SEVERE CORRUPTION");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ CORRUPT: Category '{cat.Name}' ID={cat.Id} (0x{cat.Id:X})");
                    Console.ResetColor();
                    foundCorruption = true;
                    corruptCount++;
                }
                else if (cat.Id > 1000)
                {
                    result.Warnings.Add($"SUSPICIOUS CATEGORY ID: '{cat.Name}' has ID={cat.Id} (0x{cat.Id:X}) - might be corrupted");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ SUSPICIOUS: Category '{cat.Name}' ID={cat.Id} (0x{cat.Id:X})");
                    Console.ResetColor();
                    foundCorruption = true;
                    corruptCount++;
                }

                // Check ParentId
                if (cat.ParentId > 10000)
                {
                    result.Errors.Add($"CORRUPTED PARENT ID: Category '{cat.Name}' has ParentId={cat.ParentId} (0x{cat.ParentId:X})");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ CORRUPT: Category '{cat.Name}' ParentId={cat.ParentId} (0x{cat.ParentId:X})");
                    Console.ResetColor();
                    foundCorruption = true;
                    corruptCount++;
                }
                else if (cat.ParentId > 1000 && cat.ParentId != -1)
                {
                    result.Warnings.Add($"SUSPICIOUS PARENT ID: Category '{cat.Name}' has ParentId={cat.ParentId} (0x{cat.ParentId:X})");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ SUSPICIOUS: Category '{cat.Name}' ParentId={cat.ParentId} (0x{cat.ParentId:X})");
                    Console.ResetColor();
                    foundCorruption = true;
                    corruptCount++;
                }
            }

            // Check trigger IDs and ParentIds
            foreach (var trigger in triggerDefs)
            {
                if (trigger.Id > 10000)
                {
                    result.Errors.Add($"CORRUPTED TRIGGER ID: '{trigger.Name}' has ID={trigger.Id} (0x{trigger.Id:X})");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ CORRUPT: Trigger '{trigger.Name}' ID={trigger.Id} (0x{trigger.Id:X})");
                    Console.ResetColor();
                    foundCorruption = true;
                    corruptCount++;
                }

                if (trigger.ParentId > 10000)
                {
                    result.Errors.Add($"CORRUPTED PARENT ID: Trigger '{trigger.Name}' has ParentId={trigger.ParentId} (0x{trigger.ParentId:X})");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ CORRUPT: Trigger '{trigger.Name}' ParentId={trigger.ParentId} (0x{trigger.ParentId:X})");
                    Console.ResetColor();
                    foundCorruption = true;
                    corruptCount++;
                }
                else if (trigger.ParentId > 1000 && trigger.ParentId != -1)
                {
                    result.Warnings.Add($"SUSPICIOUS PARENT ID: Trigger '{trigger.Name}' has ParentId={trigger.ParentId} (0x{trigger.ParentId:X})");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ SUSPICIOUS: Trigger '{trigger.Name}' ParentId={trigger.ParentId} (0x{trigger.ParentId:X})");
                    Console.ResetColor();
                    foundCorruption = true;
                    corruptCount++;
                }
            }

            if (!foundCorruption)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No ID corruption detected");
                Console.ResetColor();
            }
            else
            {
                result.Statistics["CorruptedIDs"] = corruptCount;
            }

            Console.WriteLine();
        }

        private static void CheckDuplicateIDs(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ DUPLICATE ID CHECK ═══");

            var categoryIds = new Dictionary<int, List<string>>();
            var triggerIds = new Dictionary<int, List<string>>();

            // Collect category IDs
            foreach (var cat in triggers.TriggerItems.OfType<TriggerCategoryDefinition>())
            {
                if (!categoryIds.ContainsKey(cat.Id))
                {
                    categoryIds[cat.Id] = new List<string>();
                }
                categoryIds[cat.Id].Add(cat.Name);
            }

            // Collect trigger IDs
            foreach (var trigger in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                if (!triggerIds.ContainsKey(trigger.Id))
                {
                    triggerIds[trigger.Id] = new List<string>();
                }
                triggerIds[trigger.Id].Add(trigger.Name);
            }

            bool foundDuplicates = false;

            // Check for duplicate category IDs
            foreach (var kvp in categoryIds.Where(kvp => kvp.Value.Count > 1))
            {
                result.Errors.Add($"DUPLICATE CATEGORY ID: {kvp.Value.Count} categories share ID={kvp.Key}: {string.Join(", ", kvp.Value.Select(n => $"'{n}'"))}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ DUPLICATE: {kvp.Value.Count} categories with ID={kvp.Key}:");
                foreach (var name in kvp.Value)
                {
                    Console.WriteLine($"      - '{name}'");
                }
                Console.ResetColor();
                foundDuplicates = true;
            }

            // Check for duplicate trigger IDs
            foreach (var kvp in triggerIds.Where(kvp => kvp.Value.Count > 1))
            {
                result.Errors.Add($"DUPLICATE TRIGGER ID: {kvp.Value.Count} triggers share ID={kvp.Key}: {string.Join(", ", kvp.Value.Select(n => $"'{n}'"))}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ DUPLICATE: {kvp.Value.Count} triggers with ID={kvp.Key}:");
                foreach (var name in kvp.Value)
                {
                    Console.WriteLine($"      - '{name}'");
                }
                Console.ResetColor();
                foundDuplicates = true;
            }

            if (!foundDuplicates)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No duplicate IDs found");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static void CheckOrphanedItems(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ ORPHANED ITEMS CHECK ═══");

            var categoryIds = new HashSet<int>(triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Select(c => c.Id));
            var orphanedTriggers = new List<string>();
            var orphanedCategories = new List<string>();

            bool is127Format = triggers.SubVersion == null;
            int rootParentId = is127Format ? 0 : -1;

            // Check for orphaned triggers
            foreach (var trigger in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                if (trigger.ParentId != rootParentId && !categoryIds.Contains(trigger.ParentId))
                {
                    orphanedTriggers.Add($"'{trigger.Name}' (ParentId={trigger.ParentId})");
                    result.Errors.Add($"ORPHANED TRIGGER: '{trigger.Name}' references non-existent category ID={trigger.ParentId}");
                }
            }

            // Check for orphaned categories (categories whose parent doesn't exist)
            foreach (var category in triggers.TriggerItems.OfType<TriggerCategoryDefinition>())
            {
                if (category.ParentId != rootParentId && !categoryIds.Contains(category.ParentId))
                {
                    orphanedCategories.Add($"'{category.Name}' (ParentId={category.ParentId})");
                    result.Warnings.Add($"ORPHANED CATEGORY: '{category.Name}' references non-existent parent category ID={category.ParentId}");
                }
            }

            if (orphanedTriggers.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ {orphanedTriggers.Count} orphaned trigger(s):");
                foreach (var orphan in orphanedTriggers)
                {
                    Console.WriteLine($"      - {orphan}");
                }
                Console.ResetColor();
                result.Statistics["OrphanedTriggers"] = orphanedTriggers.Count;
            }

            if (orphanedCategories.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠ {orphanedCategories.Count} orphaned category(ies):");
                foreach (var orphan in orphanedCategories)
                {
                    Console.WriteLine($"      - {orphan}");
                }
                Console.ResetColor();
                result.Statistics["OrphanedCategories"] = orphanedCategories.Count;
            }

            if (orphanedTriggers.Count == 0 && orphanedCategories.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No orphaned items found");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static void CheckVariables(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ VARIABLE VALIDATION ═══");

            var variableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicateVars = new List<string>();

            // Check for duplicate variable names
            foreach (var variable in triggers.Variables)
            {
                if (variableNames.Contains(variable.Name))
                {
                    duplicateVars.Add(variable.Name);
                    result.Errors.Add($"DUPLICATE VARIABLE: '{variable.Name}'");
                }
                else
                {
                    variableNames.Add(variable.Name);
                }
            }

            if (duplicateVars.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ {duplicateVars.Count} duplicate variable name(s):");
                foreach (var varName in duplicateVars)
                {
                    Console.WriteLine($"      - '{varName}'");
                }
                Console.ResetColor();
            }

            // Check all trigger variable references
            var allReferencedVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var missingVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var trigger in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                var referencedVars = TriggerExporter.DetectCorruption(trigger, triggers)
                    .Where(issue => issue.StartsWith("MISSING VARIABLE:"))
                    .Select(issue => issue.Replace("MISSING VARIABLE: '", "").Replace("' referenced but not in map", ""))
                    .ToList();

                foreach (var varName in referencedVars)
                {
                    allReferencedVars.Add(varName);
                    if (!variableNames.Contains(varName))
                    {
                        missingVars.Add(varName);
                    }
                }
            }

            if (missingVars.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ {missingVars.Count} missing variable(s) referenced by triggers:");
                foreach (var varName in missingVars.OrderBy(v => v))
                {
                    Console.WriteLine($"      - '{varName}'");
                }
                Console.ResetColor();
                result.Statistics["MissingVariables"] = missingVars.Count;
            }

            // Check for unused variables (info only, not an error)
            var unusedVars = variableNames.Except(allReferencedVars, StringComparer.OrdinalIgnoreCase).ToList();
            if (unusedVars.Count > 0)
            {
                result.Info.Add($"{unusedVars.Count} unused variable(s): {string.Join(", ", unusedVars.Select(v => $"'{v}'"))}");
                result.Statistics["UnusedVariables"] = unusedVars.Count;
            }

            if (duplicateVars.Count == 0 && missingVars.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ All variables valid");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static void CheckFileOrder(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ FILE ORDER CHECK (1.27 FORMAT) ═══");

            if (triggers.SubVersion != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  ℹ Not 1.27 format - file order not critical");
                Console.ResetColor();
                Console.WriteLine();
                return;
            }

            int firstTriggerIndex = -1;
            int lastCategoryIndex = -1;

            for (int i = 0; i < triggers.TriggerItems.Count; i++)
            {
                if (triggers.TriggerItems[i] is TriggerDefinition && firstTriggerIndex == -1)
                {
                    firstTriggerIndex = i;
                }
                if (triggers.TriggerItems[i] is TriggerCategoryDefinition cat && cat.Type != TriggerItemType.RootCategory)
                {
                    lastCategoryIndex = i;
                }
            }

            if (firstTriggerIndex != -1 && lastCategoryIndex > firstTriggerIndex)
            {
                result.Warnings.Add($"FILE ORDER ISSUE: Categories appear after triggers (last category at {lastCategoryIndex}, first trigger at {firstTriggerIndex})");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠ NESTING ISSUE: Categories after triggers");
                Console.WriteLine($"      Last category at index: {lastCategoryIndex}");
                Console.WriteLine($"      First trigger at index: {firstTriggerIndex}");
                Console.WriteLine($"      This causes incorrect visual nesting in World Editor 1.27");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ File order correct (all categories before triggers)");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static void CheckTriggerIntegrity(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ TRIGGER INTEGRITY CHECK ═══");

            int corruptedTriggers = 0;

            foreach (var trigger in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                var issues = TriggerExporter.DetectCorruption(trigger, triggers);

                var criticalIssues = issues.Where(i => i.StartsWith("CORRUPT")).ToList();

                if (criticalIssues.Count > 0)
                {
                    corruptedTriggers++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ '{trigger.Name}': {criticalIssues.Count} critical issue(s)");
                    foreach (var issue in criticalIssues.Take(3))
                    {
                        Console.WriteLine($"      - {issue}");
                    }
                    if (criticalIssues.Count > 3)
                    {
                        Console.WriteLine($"      ... and {criticalIssues.Count - 3} more");
                    }
                    Console.ResetColor();

                    foreach (var issue in criticalIssues)
                    {
                        result.Errors.Add($"Trigger '{trigger.Name}': {issue}");
                    }
                }
            }

            if (corruptedTriggers == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ All triggers structurally valid");
                Console.ResetColor();
            }
            else
            {
                result.Statistics["CorruptedTriggers"] = corruptedTriggers;
            }

            Console.WriteLine();
        }

        private static void CheckBinaryCorruption(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ BINARY CORRUPTION CHECK ═══");

            bool foundCorruption = false;

            // Check for null bytes in trigger/category names
            foreach (var cat in triggers.TriggerItems.OfType<TriggerCategoryDefinition>())
            {
                if (cat.Name != null && cat.Name.Contains('\0'))
                {
                    result.Errors.Add($"BINARY CORRUPTION: Category '{cat.Name.Replace("\0", "\\0")}' contains null bytes");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Category '{cat.Name.Replace("\0", "\\0")}' has null bytes");
                    Console.ResetColor();
                    foundCorruption = true;
                }
            }

            foreach (var trigger in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                if (trigger.Name != null && trigger.Name.Contains('\0'))
                {
                    result.Errors.Add($"BINARY CORRUPTION: Trigger '{trigger.Name.Replace("\0", "\\0")}' contains null bytes");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Trigger '{trigger.Name.Replace("\0", "\\0")}' has null bytes");
                    Console.ResetColor();
                    foundCorruption = true;
                }
            }

            if (!foundCorruption)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No binary corruption detected");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static void CheckCircularReferences(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ CIRCULAR REFERENCE CHECK ═══");

            int circularRefs = 0;

            foreach (var trigger in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                try
                {
                    // This will throw if circular references exist
                    TriggerExporter.DetectCorruption(trigger, triggers);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("circular"))
                {
                    result.Errors.Add($"CIRCULAR REFERENCE: Trigger '{trigger.Name}' has circular function nesting");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ '{trigger.Name}': Circular reference detected");
                    Console.ResetColor();
                    circularRefs++;
                }
            }

            if (circularRefs == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No circular references found");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static void CheckIDRanges(MapTriggers triggers, HealthCheckResult result)
        {
            Console.WriteLine("═══ ID RANGE ANALYSIS ═══");

            var categoryIds = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Select(c => c.Id).ToList();
            var triggerIds = triggers.TriggerItems.OfType<TriggerDefinition>().Select(t => t.Id).ToList();

            if (categoryIds.Count > 0)
            {
                Console.WriteLine($"  Category IDs:");
                Console.WriteLine($"    Min: {categoryIds.Min()}");
                Console.WriteLine($"    Max: {categoryIds.Max()}");
                Console.WriteLine($"    Range: {categoryIds.Max() - categoryIds.Min()}");

                result.Statistics["MinCategoryID"] = categoryIds.Min();
                result.Statistics["MaxCategoryID"] = categoryIds.Max();
            }

            if (triggerIds.Count > 0)
            {
                Console.WriteLine($"  Trigger IDs:");
                Console.WriteLine($"    Min: {triggerIds.Min()}");
                Console.WriteLine($"    Max: {triggerIds.Max()}");
                Console.WriteLine($"    Range: {triggerIds.Max() - triggerIds.Min()}");

                result.Statistics["MinTriggerID"] = triggerIds.Min();
                result.Statistics["MaxTriggerID"] = triggerIds.Max();
            }

            // Check if IDs are sequential
            var allCategoryIds = categoryIds.OrderBy(id => id).ToList();
            bool sequential = true;
            for (int i = 1; i < allCategoryIds.Count; i++)
            {
                if (allCategoryIds[i] != allCategoryIds[i - 1] + 1 && allCategoryIds[i] != allCategoryIds[i - 1])
                {
                    sequential = false;
                    break;
                }
            }

            if (categoryIds.Count > 0)
            {
                if (sequential && categoryIds.Min() == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Category IDs are sequential (0, 1, 2, ...)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ Category IDs are NOT sequential");
                    Console.ResetColor();
                    result.Warnings.Add("Category IDs are not sequential - might indicate ID corruption or manual editing");
                }
            }

            Console.WriteLine();
        }

        private static void PrintHealthCheckSummary(HealthCheckResult result)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  HEALTH CHECK SUMMARY                    ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓✓✓ FILE IS HEALTHY! ✓✓✓");
                Console.WriteLine($"\n  No errors or warnings detected.");
                Console.WriteLine($"  This file should load correctly in World Editor.");
                Console.ResetColor();
            }
            else
            {
                if (result.Errors.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ ERRORS: {result.Errors.Count}");
                    Console.ResetColor();
                    Console.WriteLine("  Critical issues that WILL cause problems:");
                    foreach (var error in result.Errors.Take(10))
                    {
                        Console.WriteLine($"    • {error}");
                    }
                    if (result.Errors.Count > 10)
                    {
                        Console.WriteLine($"    ... and {result.Errors.Count - 10} more errors");
                    }
                    Console.WriteLine();
                }

                if (result.Warnings.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ WARNINGS: {result.Warnings.Count}");
                    Console.ResetColor();
                    Console.WriteLine("  Issues that MIGHT cause problems:");
                    foreach (var warning in result.Warnings.Take(10))
                    {
                        Console.WriteLine($"    • {warning}");
                    }
                    if (result.Warnings.Count > 10)
                    {
                        Console.WriteLine($"    ... and {result.Warnings.Count - 10} more warnings");
                    }
                    Console.WriteLine();
                }
            }

            if (result.Info.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ℹ INFO: {result.Info.Count}");
                Console.ResetColor();
                foreach (var info in result.Info.Take(5))
                {
                    Console.WriteLine($"    • {info}");
                }
                if (result.Info.Count > 5)
                {
                    Console.WriteLine($"    ... and {result.Info.Count - 5} more");
                }
                Console.WriteLine();
            }

            // Recommendations
            if (!result.IsHealthy)
            {
                Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    RECOMMENDATIONS                       ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

                if (result.Errors.Any(e => e.Contains("CORRUPT")))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n  ✗ FILE HAS CORRUPTION");
                    Console.WriteLine("\n  This file cannot be safely used in its current state.");
                    Console.WriteLine("\n  Options:");
                    Console.WriteLine("    1. Restore from backup if available");
                    Console.WriteLine("    2. Use WTGMerger's repair functions (Option 6)");
                    Console.WriteLine("    3. Recreate corrupted items manually in World Editor");
                    Console.ResetColor();
                }

                if (result.Errors.Any(e => e.Contains("ORPHANED TRIGGER")))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n  ⚠ ORPHANED TRIGGERS DETECTED");
                    Console.WriteLine("\n  Use WTGMerger Option 6 to repair orphaned triggers automatically.");
                    Console.ResetColor();
                }

                if (result.Errors.Any(e => e.Contains("MISSING VARIABLE")))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n  ⚠ MISSING VARIABLES");
                    Console.WriteLine("\n  Add the missing variables to the map, or remove references from triggers.");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
        }
    }
}
