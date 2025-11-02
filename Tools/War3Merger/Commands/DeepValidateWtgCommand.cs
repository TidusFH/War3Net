using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Extensions;
using War3Net.Build.Script;
using War3Net.IO.Mpq;

namespace War3Net.Tools.TriggerMerger.Commands;

public class DeepValidateWtgCommand : Command
{
    public DeepValidateWtgCommand() : base("deep-validate-wtg", "Deep validation of .wtg file structure - checks for all potential issues")
    {
        var fileOption = new Option<string>(
            "--file",
            description: "Path to the .wtg file to validate")
        {
            IsRequired = true
        };
        fileOption.AddAlias("-f");

        AddOption(fileOption);

        this.SetHandler(Execute, fileOption);
    }

    private static void Execute(string file)
    {
        Console.WriteLine($"Deep validating: {file}");
        Console.WriteLine(new string('=', 80));

        if (!File.Exists(file))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ File not found: {file}");
            Console.ResetColor();
            return;
        }

        // Read the .wtg file
        MapTriggers triggers;
        using (var stream = File.OpenRead(file))
        {
            triggers = new BinaryReader(stream).ReadMapTriggers();
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        Console.WriteLine("\n1. BASIC STRUCTURE");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"  Format Version: {triggers.FormatVersion}");
        Console.WriteLine($"  SubVersion: {triggers.SubVersion}");
        Console.WriteLine($"  Total Items: {triggers.TriggerItems?.Count ?? 0}");
        Console.WriteLine($"  Categories: {triggers.TriggerItems?.OfType<TriggerCategoryDefinition>().Count() ?? 0}");
        Console.WriteLine($"  Triggers: {triggers.TriggerItems?.OfType<TriggerDefinition>().Count() ?? 0}");
        Console.WriteLine($"  Variables: {triggers.Variables?.Count ?? 0}");

        if (triggers.TriggerItems == null || !triggers.TriggerItems.Any())
        {
            errors.Add("TriggerItems is null or empty");
        }

        Console.WriteLine("\n2. ID VALIDATION");
        Console.WriteLine(new string('-', 80));

        var allIds = new Dictionary<int, List<string>>();
        if (triggers.TriggerItems != null)
        {
            foreach (var item in triggers.TriggerItems)
            {
                if (!allIds.ContainsKey(item.Id))
                {
                    allIds[item.Id] = new List<string>();
                }

                string itemName = item is TriggerCategoryDefinition cat ? $"Category: {cat.Name}" :
                                 item is TriggerDefinition trig ? $"Trigger: {trig.Name}" : "Unknown";
                allIds[item.Id].Add(itemName);
            }
        }

        var duplicateIds = allIds.Where(kvp => kvp.Value.Count > 1).ToList();
        if (duplicateIds.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ FOUND {duplicateIds.Count} DUPLICATE IDs:");
            foreach (var dup in duplicateIds)
            {
                Console.WriteLine($"    ID {dup.Key} used by:");
                foreach (var name in dup.Value)
                {
                    Console.WriteLine($"      - {name}");
                }
                errors.Add($"Duplicate ID: {dup.Key}");
            }
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ All IDs are unique ({allIds.Count} items)");
            Console.ResetColor();
        }

        var zeroIds = allIds.Where(kvp => kvp.Key == 0).ToList();
        if (zeroIds.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ FOUND ITEMS WITH ID=0:");
            foreach (var name in zeroIds.First().Value)
            {
                Console.WriteLine($"      - {name}");
            }
            errors.Add("Items with ID=0 found");
            Console.ResetColor();
        }

        Console.WriteLine("\n3. NAME VALIDATION");
        Console.WriteLine(new string('-', 80));

        var categoryNames = new Dictionary<string, int>();
        var triggerNames = new Dictionary<string, int>();

        if (triggers.TriggerItems != null)
        {
            foreach (var item in triggers.TriggerItems)
            {
                if (item is TriggerCategoryDefinition cat)
                {
                    if (!categoryNames.ContainsKey(cat.Name))
                        categoryNames[cat.Name] = 0;
                    categoryNames[cat.Name]++;
                }
                else if (item is TriggerDefinition trig)
                {
                    if (!triggerNames.ContainsKey(trig.Name))
                        triggerNames[trig.Name] = 0;
                    triggerNames[trig.Name]++;
                }
            }
        }

        var dupCategoryNames = categoryNames.Where(kvp => kvp.Value > 1).ToList();
        var dupTriggerNames = triggerNames.Where(kvp => kvp.Value > 1).ToList();

        if (dupCategoryNames.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ DUPLICATE CATEGORY NAMES ({dupCategoryNames.Count}):");
            foreach (var dup in dupCategoryNames)
            {
                Console.WriteLine($"    - '{dup.Key}' appears {dup.Value} times");
            }
            warnings.Add($"{dupCategoryNames.Count} duplicate category names");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ All category names are unique ({categoryNames.Count} categories)");
            Console.ResetColor();
        }

        if (dupTriggerNames.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ DUPLICATE TRIGGER NAMES ({dupTriggerNames.Count}):");
            foreach (var dup in dupTriggerNames)
            {
                Console.WriteLine($"    - '{dup.Key}' appears {dup.Value} times");
            }
            warnings.Add($"{dupTriggerNames.Count} duplicate trigger names");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ All trigger names are unique ({triggerNames.Count} triggers)");
            Console.ResetColor();
        }

        Console.WriteLine("\n4. PARENT-CHILD RELATIONSHIP VALIDATION");
        Console.WriteLine(new string('-', 80));

        var validCategoryIds = new HashSet<int> { 0 }; // 0 is root
        if (triggers.TriggerItems != null)
        {
            foreach (var cat in triggers.TriggerItems.OfType<TriggerCategoryDefinition>())
            {
                validCategoryIds.Add(cat.Id);
            }
        }

        Console.WriteLine($"  Valid category IDs: {string.Join(", ", validCategoryIds.OrderBy(x => x))}");

        var invalidParentRefs = new List<string>();
        if (triggers.TriggerItems != null)
        {
            foreach (var item in triggers.TriggerItems)
            {
                if (!validCategoryIds.Contains(item.ParentId))
                {
                    string itemName = item is TriggerCategoryDefinition cat ? $"Category '{cat.Name}'" :
                                     item is TriggerDefinition trig ? $"Trigger '{trig.Name}'" : "Unknown";
                    invalidParentRefs.Add($"{itemName} (ID: {item.Id}, ParentId: {item.ParentId})");
                }
            }
        }

        if (invalidParentRefs.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ INVALID PARENT REFERENCES ({invalidParentRefs.Count}):");
            foreach (var item in invalidParentRefs)
            {
                Console.WriteLine($"    - {item}");
                errors.Add($"Invalid ParentId: {item}");
            }
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ All ParentId references are valid");
            Console.ResetColor();
        }

        Console.WriteLine("\n5. VARIABLE VALIDATION");
        Console.WriteLine(new string('-', 80));

        var definedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (triggers.Variables != null)
        {
            foreach (var variable in triggers.Variables)
            {
                definedVariables.Add(variable.Name);
            }
        }

        Console.WriteLine($"  Defined variables: {definedVariables.Count}");
        if (definedVariables.Count > 0)
        {
            Console.WriteLine($"    {string.Join(", ", definedVariables.Take(10))}");
            if (definedVariables.Count > 10)
            {
                Console.WriteLine($"    ... and {definedVariables.Count - 10} more");
            }
        }

        var referencedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (triggers.TriggerItems != null)
        {
            foreach (var trig in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                if (trig.Functions != null)
                {
                    ScanFunctionsForVariables(trig.Functions, referencedVariables);
                }
            }
        }

        Console.WriteLine($"  Referenced variables: {referencedVariables.Count}");

        var missingVariables = referencedVariables.Except(definedVariables, StringComparer.OrdinalIgnoreCase).ToList();
        if (missingVariables.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ MISSING VARIABLE DEFINITIONS ({missingVariables.Count}):");
            foreach (var varName in missingVariables.Take(20))
            {
                Console.WriteLine($"    - {varName}");
            }
            if (missingVariables.Count > 20)
            {
                Console.WriteLine($"    ... and {missingVariables.Count - 20} more");
            }
            errors.Add($"{missingVariables.Count} missing variable definitions");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ All referenced variables are defined");
            Console.ResetColor();
        }

        var unusedVariables = definedVariables.Except(referencedVariables, StringComparer.OrdinalIgnoreCase).ToList();
        if (unusedVariables.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ UNUSED VARIABLES ({unusedVariables.Count}):");
            foreach (var varName in unusedVariables.Take(10))
            {
                Console.WriteLine($"    - {varName}");
            }
            if (unusedVariables.Count > 10)
            {
                Console.WriteLine($"    ... and {unusedVariables.Count - 10} more");
            }
            warnings.Add($"{unusedVariables.Count} unused variables");
            Console.ResetColor();
        }

        Console.WriteLine("\n6. TRIGGER PROPERTY VALIDATION");
        Console.WriteLine(new string('-', 80));

        var triggerIssues = new List<string>();
        if (triggers.TriggerItems != null)
        {
            foreach (var trig in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                if (string.IsNullOrWhiteSpace(trig.Name))
                {
                    triggerIssues.Add($"Trigger ID {trig.Id} has empty name");
                }

                if (trig.Functions == null || !trig.Functions.Any())
                {
                    triggerIssues.Add($"Trigger '{trig.Name}' (ID: {trig.Id}) has no functions");
                }
            }
        }

        if (triggerIssues.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ TRIGGER PROPERTY ISSUES ({triggerIssues.Count}):");
            foreach (var issue in triggerIssues.Take(10))
            {
                Console.WriteLine($"    - {issue}");
            }
            if (triggerIssues.Count > 10)
            {
                Console.WriteLine($"    ... and {triggerIssues.Count - 10} more");
            }
            warnings.Add($"{triggerIssues.Count} trigger property issues");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ All triggers have valid properties");
            Console.ResetColor();
        }

        Console.WriteLine("\n7. CATEGORY HIERARCHY");
        Console.WriteLine(new string('-', 80));

        var categories = triggers.TriggerItems?.OfType<TriggerCategoryDefinition>().ToList() ?? new List<TriggerCategoryDefinition>();
        var categoryById = categories.ToDictionary(c => c.Id);

        // Check for circular references
        var circularRefs = new List<string>();
        foreach (var cat in categories)
        {
            var visited = new HashSet<int> { cat.Id };
            var currentId = cat.ParentId;
            var depth = 0;

            while (currentId != 0 && depth < 100)
            {
                if (visited.Contains(currentId))
                {
                    circularRefs.Add($"Category '{cat.Name}' (ID: {cat.Id}) has circular parent reference");
                    break;
                }

                visited.Add(currentId);
                if (categoryById.TryGetValue(currentId, out var parent))
                {
                    currentId = parent.ParentId;
                }
                else
                {
                    break;
                }
                depth++;
            }

            if (depth >= 100)
            {
                circularRefs.Add($"Category '{cat.Name}' (ID: {cat.Id}) has parent chain > 100 levels (possible circular ref)");
            }
        }

        if (circularRefs.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ CIRCULAR REFERENCES ({circularRefs.Count}):");
            foreach (var issue in circularRefs)
            {
                Console.WriteLine($"    - {issue}");
                errors.Add(issue);
            }
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ No circular references found");
            Console.ResetColor();
        }

        // Show hierarchy
        Console.WriteLine("\n  Category Hierarchy:");
        ShowHierarchy(categories, 0, "  ", categoryById);

        Console.WriteLine("\n8. TRIGGER DISTRIBUTION");
        Console.WriteLine(new string('-', 80));

        var triggersByCategory = new Dictionary<int, List<string>>();
        if (triggers.TriggerItems != null)
        {
            foreach (var trig in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                if (!triggersByCategory.ContainsKey(trig.ParentId))
                {
                    triggersByCategory[trig.ParentId] = new List<string>();
                }
                triggersByCategory[trig.ParentId].Add(trig.Name);
            }
        }

        foreach (var kvp in triggersByCategory.OrderBy(kvp => kvp.Key))
        {
            string categoryName = "Root";
            if (kvp.Key != 0 && categoryById.TryGetValue(kvp.Key, out var cat))
            {
                categoryName = cat.Name;
            }

            Console.WriteLine($"  {categoryName} (ID: {kvp.Key}): {kvp.Value.Count} triggers");
            foreach (var trigName in kvp.Value.Take(5))
            {
                Console.WriteLine($"    - {trigName}");
            }
            if (kvp.Value.Count > 5)
            {
                Console.WriteLine($"    ... and {kvp.Value.Count - 5} more");
            }
        }

        // SUMMARY
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("VALIDATION SUMMARY");
        Console.WriteLine(new string('=', 80));

        if (errors.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ CRITICAL ERRORS: {errors.Count}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ No critical errors found");
            Console.ResetColor();
        }

        if (warnings.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ WARNINGS: {warnings.Count}");
            foreach (var warning in warnings)
            {
                Console.WriteLine($"  - {warning}");
            }
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"No warnings");
        }

        if (errors.Any())
        {
            Console.WriteLine("\nCRITICAL ERRORS THAT MUST BE FIXED:");
            foreach (var error in errors.Take(20))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ {error}");
                Console.ResetColor();
            }
            if (errors.Count > 20)
            {
                Console.WriteLine($"  ... and {errors.Count - 20} more errors");
            }
        }
    }

    private static void ShowHierarchy(List<TriggerCategoryDefinition> categories, int parentId, string indent, Dictionary<int, TriggerCategoryDefinition> categoryById)
    {
        var children = categories.Where(c => c.ParentId == parentId).OrderBy(c => c.Name).ToList();
        foreach (var cat in children)
        {
            Console.WriteLine($"{indent}├─ {cat.Name} (ID: {cat.Id})");
            ShowHierarchy(categories, cat.Id, indent + "│  ", categoryById);
        }
    }

    private static void ScanFunctionsForVariables(IEnumerable<TriggerFunction> functions, HashSet<string> variables)
    {
        foreach (var function in functions)
        {
            if (function.Name != null && function.Name.StartsWith("udg_", StringComparison.OrdinalIgnoreCase))
            {
                var varName = function.Name.Substring(4);
                variables.Add(varName);
            }

            if (function.Parameters != null)
            {
                foreach (var param in function.Parameters)
                {
                    if (param.Value != null && param.Value.StartsWith("udg_", StringComparison.OrdinalIgnoreCase))
                    {
                        var varName = param.Value.Substring(4);
                        variables.Add(varName);
                    }

                    if (param.Function != null)
                    {
                        ScanFunctionsForVariables(new[] { param.Function }, variables);
                    }

                    if (param.ArrayIndexer != null)
                    {
                        ScanFunctionsForVariables(new[] { param.ArrayIndexer }, variables);
                    }
                }
            }

            if (function.ChildFunctions != null)
            {
                ScanFunctionsForVariables(function.ChildFunctions, variables);
            }
        }
    }
}
