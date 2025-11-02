// ------------------------------------------------------------------------------
// <copyright file="DeepValidateWtgCommand.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using War3Net.Build.Script;
using War3Net.IO.Mpq;

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to perform deep validation of trigger file structure.
    /// </summary>
    internal static class DeepValidateWtgCommand
    {
        public static async Task ExecuteAsync(FileInfo file)
        {
            try
            {
                Console.WriteLine($"Deep Validation: {file.FullName}");
                Console.WriteLine();

                MapTriggers triggers;

                // Read the WTG file
                if (file.Extension.Equals(".w3x", StringComparison.OrdinalIgnoreCase) ||
                    file.Extension.Equals(".w3m", StringComparison.OrdinalIgnoreCase))
                {
                    using var archive = MpqArchive.Open(file.FullName);
                    using var stream = archive.OpenFile("war3map.wtg");
                    using var reader = new BinaryReader(stream);
                    triggers = MapTriggers.Parse(reader, true);
                }
                else
                {
                    using var stream = File.OpenRead(file.FullName);
                    using var reader = new BinaryReader(stream);
                    triggers = MapTriggers.Parse(reader, true);
                }

                // Validation counters
                int totalErrors = 0;
                int totalWarnings = 0;

                // 1. ID Validation
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("1. ID VALIDATION");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                var (idErrors, idWarnings) = ValidateIds(triggers);
                totalErrors += idErrors;
                totalWarnings += idWarnings;
                Console.WriteLine();

                // 2. Name Validation
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("2. NAME VALIDATION");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                var (nameErrors, nameWarnings) = ValidateNames(triggers);
                totalErrors += nameErrors;
                totalWarnings += nameWarnings;
                Console.WriteLine();

                // 3. Parent-Child Relationships
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("3. PARENT-CHILD RELATIONSHIPS");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                var (parentErrors, parentWarnings) = ValidateParentChildRelationships(triggers);
                totalErrors += parentErrors;
                totalWarnings += parentWarnings;
                Console.WriteLine();

                // 4. Variable Validation
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("4. VARIABLE VALIDATION");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                var (varErrors, varWarnings) = ValidateVariables(triggers);
                totalErrors += varErrors;
                totalWarnings += varWarnings;
                Console.WriteLine();

                // 5. Trigger Properties
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("5. TRIGGER PROPERTIES");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                var (trigErrors, trigWarnings) = ValidateTriggerProperties(triggers);
                totalErrors += trigErrors;
                totalWarnings += trigWarnings;
                Console.WriteLine();

                // 6. Category Hierarchy
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("6. CATEGORY HIERARCHY");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                var (hierErrors, hierWarnings) = ValidateCategoryHierarchy(triggers);
                totalErrors += hierErrors;
                totalWarnings += hierWarnings;
                Console.WriteLine();

                // 7. Trigger Distribution
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("7. TRIGGER DISTRIBUTION");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                DisplayTriggerDistribution(triggers);
                Console.WriteLine();

                // 8. Function Validation
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("8. FUNCTION VALIDATION");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                var (funcErrors, funcWarnings) = ValidateFunctions(triggers);
                totalErrors += funcErrors;
                totalWarnings += funcWarnings;
                Console.WriteLine();

                // Summary
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("VALIDATION SUMMARY");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.ResetColor();

                if (totalErrors == 0 && totalWarnings == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ No issues found - file appears to be valid!");
                    Console.ResetColor();
                }
                else
                {
                    if (totalErrors > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ {totalErrors} critical error(s) found");
                        Console.ResetColor();
                    }

                    if (totalWarnings > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ {totalWarnings} warning(s) found");
                        Console.ResetColor();
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error during validation: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }

        private static (int errors, int warnings) ValidateIds(MapTriggers triggers)
        {
            int errors = 0;
            int warnings = 0;

            var seenIds = new HashSet<int>();
            var duplicateIds = new List<(int id, string name, string type)>();
            var zeroIds = new List<(string name, string type)>();

            if (triggers.TriggerItems != null)
            {
                foreach (var item in triggers.TriggerItems)
                {
                    int id;
                    string name;
                    string type;

                    if (item is TriggerCategoryDefinition category)
                    {
                        id = category.Id;
                        name = category.Name;
                        type = "Category";
                    }
                    else if (item is TriggerDefinition trigger)
                    {
                        id = trigger.Id;
                        name = trigger.Name;
                        type = "Trigger";
                    }
                    else
                    {
                        continue;
                    }

                    if (id == 0)
                    {
                        zeroIds.Add((name, type));
                        errors++;
                    }
                    else if (!seenIds.Add(id))
                    {
                        duplicateIds.Add((id, name, type));
                        errors++;
                    }
                }
            }

            if (zeroIds.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Found {zeroIds.Count} item(s) with ID = 0:");
                foreach (var (name, type) in zeroIds)
                {
                    Console.WriteLine($"  - {type}: {name}");
                }
                Console.ResetColor();
            }

            if (duplicateIds.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Found {duplicateIds.Count} duplicate ID(s):");
                foreach (var (id, name, type) in duplicateIds)
                {
                    Console.WriteLine($"  - ID {id}: {type} '{name}'");
                }
                Console.ResetColor();
            }

            if (errors == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ All IDs are unique and non-zero ({seenIds.Count} items checked)");
                Console.ResetColor();
            }

            return (errors, warnings);
        }

        private static (int errors, int warnings) ValidateNames(MapTriggers triggers)
        {
            int errors = 0;
            int warnings = 0;

            var categoryNames = new HashSet<string>();
            var triggerNames = new HashSet<string>();
            var duplicateCategories = new List<string>();
            var duplicateTriggers = new List<string>();

            if (triggers.TriggerItems != null)
            {
                foreach (var item in triggers.TriggerItems)
                {
                    if (item is TriggerCategoryDefinition category)
                    {
                        if (!categoryNames.Add(category.Name))
                        {
                            duplicateCategories.Add(category.Name);
                            warnings++;
                        }
                    }
                    else if (item is TriggerDefinition trigger)
                    {
                        if (!triggerNames.Add(trigger.Name))
                        {
                            duplicateTriggers.Add(trigger.Name);
                            warnings++;
                        }
                    }
                }
            }

            if (duplicateCategories.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Found {duplicateCategories.Count} duplicate category name(s):");
                foreach (var name in duplicateCategories)
                {
                    Console.WriteLine($"  - '{name}'");
                }
                Console.ResetColor();
            }

            if (duplicateTriggers.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Found {duplicateTriggers.Count} duplicate trigger name(s):");
                foreach (var name in duplicateTriggers)
                {
                    Console.WriteLine($"  - '{name}'");
                }
                Console.ResetColor();
            }

            if (warnings == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ All names are unique ({categoryNames.Count} categories, {triggerNames.Count} triggers)");
                Console.ResetColor();
            }

            return (errors, warnings);
        }

        private static (int errors, int warnings) ValidateParentChildRelationships(MapTriggers triggers)
        {
            int errors = 0;
            int warnings = 0;

            var validIds = new HashSet<int>();
            var invalidParents = new List<(string name, int parentId, string type)>();

            // First pass: collect all valid IDs
            if (triggers.TriggerItems != null)
            {
                foreach (var item in triggers.TriggerItems)
                {
                    if (item is TriggerCategoryDefinition category)
                    {
                        validIds.Add(category.Id);
                    }
                    else if (item is TriggerDefinition trigger)
                    {
                        validIds.Add(trigger.Id);
                    }
                }
            }

            // Second pass: validate parent references
            if (triggers.TriggerItems != null)
            {
                foreach (var item in triggers.TriggerItems)
                {
                    int parentId;
                    string name;
                    string type;

                    if (item is TriggerCategoryDefinition category)
                    {
                        parentId = category.ParentId;
                        name = category.Name;
                        type = "Category";
                    }
                    else if (item is TriggerDefinition trigger)
                    {
                        parentId = trigger.ParentId;
                        name = trigger.Name;
                        type = "Trigger";
                    }
                    else
                    {
                        continue;
                    }

                    // ParentId of 0 means root level, which is valid
                    if (parentId != 0 && !validIds.Contains(parentId))
                    {
                        invalidParents.Add((name, parentId, type));
                        errors++;
                    }
                }
            }

            if (invalidParents.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Found {invalidParents.Count} invalid parent reference(s):");
                foreach (var (name, parentId, type) in invalidParents)
                {
                    Console.WriteLine($"  - {type} '{name}' has ParentId={parentId} (does not exist)");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All parent references are valid");
                Console.ResetColor();
            }

            return (errors, warnings);
        }

        private static (int errors, int warnings) ValidateVariables(MapTriggers triggers)
        {
            int errors = 0;
            int warnings = 0;

            var definedVariables = new HashSet<string>();
            var usedVariables = new HashSet<string>();

            // Collect defined variables
            if (triggers.Variables != null)
            {
                foreach (var variable in triggers.Variables)
                {
                    definedVariables.Add(variable.Name);
                }
            }

            // Scan for variable usage in trigger functions
            if (triggers.TriggerItems != null)
            {
                foreach (var item in triggers.TriggerItems)
                {
                    if (item is TriggerDefinition trigger && trigger.Functions != null)
                    {
                        foreach (var function in trigger.Functions)
                        {
                            ScanFunctionForVariables(function, usedVariables);
                        }
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Defined variables: {definedVariables.Count}");
            Console.WriteLine($"Used variables (detected): {usedVariables.Count}");
            Console.ResetColor();

            // Note: We don't report undefined variables as errors because variable detection is heuristic
            if (usedVariables.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Variable usage detected in triggers");
                Console.ResetColor();
            }

            return (errors, warnings);
        }

        private static void ScanFunctionForVariables(TriggerFunction function, HashSet<string> usedVariables)
        {
            // Check if function name contains variable reference
            if (!string.IsNullOrEmpty(function.Name) && function.Name.StartsWith("gg_"))
            {
                usedVariables.Add(function.Name);
            }

            // Recursively scan parameters
            if (function.Parameters != null && function.Parameters.Count > 0)
            {
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    var param = function.Parameters[i];

                    if (param.Function != null)
                    {
                        ScanFunctionForVariables(param.Function, usedVariables);
                    }

                    if (!string.IsNullOrEmpty(param.Value) && param.Value.StartsWith("gg_"))
                    {
                        usedVariables.Add(param.Value);
                    }
                }
            }
        }

        private static (int errors, int warnings) ValidateTriggerProperties(MapTriggers triggers)
        {
            int errors = 0;
            int warnings = 0;

            var emptyNames = new List<string>();
            var noFunctions = new List<string>();

            if (triggers.TriggerItems != null)
            {
                foreach (var item in triggers.TriggerItems)
                {
                    if (item is TriggerDefinition trigger)
                    {
                        if (string.IsNullOrWhiteSpace(trigger.Name))
                        {
                            emptyNames.Add($"Trigger ID {trigger.Id}");
                            errors++;
                        }

                        if (trigger.Functions == null || trigger.Functions.Count == 0)
                        {
                            noFunctions.Add(trigger.Name);
                            warnings++;
                        }
                    }
                    else if (item is TriggerCategoryDefinition category)
                    {
                        if (string.IsNullOrWhiteSpace(category.Name))
                        {
                            emptyNames.Add($"Category ID {category.Id}");
                            errors++;
                        }
                    }
                }
            }

            if (emptyNames.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Found {emptyNames.Count} item(s) with empty names:");
                foreach (var name in emptyNames)
                {
                    Console.WriteLine($"  - {name}");
                }
                Console.ResetColor();
            }

            if (noFunctions.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Found {noFunctions.Count} trigger(s) with no functions:");
                foreach (var name in noFunctions.Take(10))
                {
                    Console.WriteLine($"  - {name}");
                }
                if (noFunctions.Count > 10)
                {
                    Console.WriteLine($"  ... and {noFunctions.Count - 10} more");
                }
                Console.ResetColor();
            }

            if (errors == 0 && warnings == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All trigger properties are valid");
                Console.ResetColor();
            }

            return (errors, warnings);
        }

        private static (int errors, int warnings) ValidateCategoryHierarchy(MapTriggers triggers)
        {
            int errors = 0;
            int warnings = 0;

            var categoryParents = new Dictionary<int, int>();
            var circularRefs = new List<(string name, List<int> chain)>();

            // Build parent map for categories
            if (triggers.TriggerItems != null)
            {
                foreach (var item in triggers.TriggerItems)
                {
                    if (item is TriggerCategoryDefinition category)
                    {
                        categoryParents[category.Id] = category.ParentId;
                    }
                }
            }

            // Check for circular references
            foreach (var categoryId in categoryParents.Keys)
            {
                var visited = new HashSet<int>();
                var chain = new List<int>();
                var currentId = categoryId;

                while (currentId != 0)
                {
                    if (visited.Contains(currentId))
                    {
                        // Found a circular reference
                        var category = triggers.TriggerItems?.OfType<TriggerCategoryDefinition>()
                            .FirstOrDefault(c => c.Id == categoryId);
                        circularRefs.Add((category?.Name ?? $"ID {categoryId}", chain));
                        errors++;
                        break;
                    }

                    visited.Add(currentId);
                    chain.Add(currentId);

                    if (!categoryParents.TryGetValue(currentId, out var parentId))
                    {
                        break;
                    }

                    currentId = parentId;
                }
            }

            if (circularRefs.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Found {circularRefs.Count} circular reference(s) in category hierarchy:");
                foreach (var (name, chain) in circularRefs)
                {
                    Console.WriteLine($"  - Category '{name}': {string.Join(" -> ", chain)}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ No circular references in category hierarchy");
                Console.ResetColor();
            }

            return (errors, warnings);
        }

        private static void DisplayTriggerDistribution(MapTriggers triggers)
        {
            var categories = triggers.TriggerItems?.OfType<TriggerCategoryDefinition>().ToList() ?? new List<TriggerCategoryDefinition>();
            var triggersInFile = triggers.TriggerItems?.OfType<TriggerDefinition>().ToList() ?? new List<TriggerDefinition>();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Total categories: {categories.Count}");
            Console.WriteLine($"Total triggers: {triggersInFile.Count}");
            Console.ResetColor();

            if (categories.Any())
            {
                var categoryTriggerCount = new Dictionary<int, (string name, int count)>();

                // Count triggers per category
                foreach (var category in categories)
                {
                    var count = triggersInFile.Count(t => t.ParentId == category.Id);
                    categoryTriggerCount[category.Id] = (category.Name, count);
                }

                // Show top categories by trigger count
                var topCategories = categoryTriggerCount
                    .OrderByDescending(kvp => kvp.Value.count)
                    .Take(10)
                    .ToList();

                if (topCategories.Any(c => c.Value.count > 0))
                {
                    Console.WriteLine();
                    Console.WriteLine("Top categories by trigger count:");
                    foreach (var (id, (name, count)) in topCategories)
                    {
                        if (count > 0)
                        {
                            Console.WriteLine($"  - {name}: {count} trigger(s)");
                        }
                    }
                }
            }

            // Count root-level triggers
            var rootTriggers = triggersInFile.Count(t => t.ParentId == 0);
            if (rootTriggers > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ {rootTriggers} trigger(s) at root level (not in any category)");
                Console.ResetColor();
            }
        }

        private static (int errors, int warnings) ValidateFunctions(MapTriggers triggers)
        {
            int errors = 0;
            int warnings = 0;

            var functionIssues = new List<(string triggerName, string issue)>();

            if (triggers.TriggerItems != null)
            {
                foreach (var item in triggers.TriggerItems)
                {
                    if (item is TriggerDefinition trigger && trigger.Functions != null)
                    {
                        foreach (var function in trigger.Functions)
                        {
                            ValidateFunction(function, trigger.Name, functionIssues, 0);
                        }
                    }
                }
            }

            if (functionIssues.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Found {functionIssues.Count} potential function issue(s):");
                foreach (var (triggerName, issue) in functionIssues.Take(20))
                {
                    Console.WriteLine($"  - Trigger '{triggerName}': {issue}");
                }
                if (functionIssues.Count > 20)
                {
                    Console.WriteLine($"  ... and {functionIssues.Count - 20} more");
                }
                Console.ResetColor();
                warnings += functionIssues.Count;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ No obvious function issues detected");
                Console.ResetColor();
            }

            return (errors, warnings);
        }

        private static void ValidateFunction(TriggerFunction function, string triggerName, List<(string, string)> issues, int depth)
        {
            if (depth > 50)
            {
                issues.Add((triggerName, $"Function nesting depth exceeds 50 levels"));
                return;
            }

            if (string.IsNullOrWhiteSpace(function.Name))
            {
                issues.Add((triggerName, "Function has empty name"));
            }

            // Recursively validate nested functions in parameters
            if (function.Parameters != null && function.Parameters.Count > 0)
            {
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    var param = function.Parameters[i];

                    if (param.Function != null)
                    {
                        ValidateFunction(param.Function, triggerName, issues, depth + 1);
                    }
                }
            }
        }
    }
}
