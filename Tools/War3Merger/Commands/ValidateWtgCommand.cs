// ------------------------------------------------------------------------------
// <copyright file="ValidateWtgCommand.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using War3Net.Build.Extensions;
using War3Net.Build.Script;
using War3Net.IO.Mpq;

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to validate .wtg file structure and find errors.
    /// </summary>
    internal static class ValidateWtgCommand
    {
        public static async Task ExecuteAsync(FileInfo mapFile)
        {
            await Task.Run(() =>
            {
                Console.WriteLine(".wtg Structure Validator");
                Console.WriteLine("========================");
                Console.WriteLine();
                Console.WriteLine($"Validating: {mapFile.FullName}");
                Console.WriteLine();

                if (!mapFile.Exists)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: File not found!");
                    Console.ResetColor();
                    return;
                }

                try
                {
                    MapTriggers triggers;

                    // Read .wtg file
                    Console.WriteLine("Reading .wtg file...");
                    using (var archive = MpqArchive.Open(mapFile.FullName, loadListFile: true))
                    {
                        if (!archive.FileExists("war3map.wtg"))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error: Map has no war3map.wtg file!");
                            Console.ResetColor();
                            return;
                        }

                        using var wtgStream = archive.OpenFile("war3map.wtg");
                        using var reader = new BinaryReader(wtgStream);
                        triggers = reader.ReadMapTriggers();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Successfully parsed .wtg file");
                    Console.ResetColor();
                    Console.WriteLine();

                    var errors = new List<string>();
                    var warnings = new List<string>();

                    // Validate structure
                    Console.WriteLine("VALIDATION CHECKS:");
                    Console.WriteLine("==================");
                    Console.WriteLine();

                    // Check 1: TriggerItems collection
                    Console.WriteLine("1. Checking TriggerItems collection...");
                    if (triggers.TriggerItems == null)
                    {
                        errors.Add("TriggerItems collection is null!");
                    }
                    else
                    {
                        Console.WriteLine($"   Total items: {triggers.TriggerItems.Count}");

                        var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                        var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

                        Console.WriteLine($"   Categories: {categories.Count}");
                        Console.WriteLine($"   Triggers: {triggerDefs.Count}");

                        // Check 2: Validate IDs are unique
                        Console.WriteLine();
                        Console.WriteLine("2. Checking ID uniqueness...");
                        var allIds = triggers.TriggerItems.Select(item => item.Id).ToList();
                        var duplicateIds = allIds.GroupBy(id => id)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key)
                            .ToList();

                        if (duplicateIds.Any())
                        {
                            errors.Add($"Found {duplicateIds.Count} duplicate IDs: {string.Join(", ", duplicateIds)}");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("   ✓ All IDs are unique");
                            Console.ResetColor();
                        }

                        // Check 3: Validate ParentId references
                        Console.WriteLine();
                        Console.WriteLine("3. Checking ParentId references...");
                        var categoryIds = new HashSet<int>(categories.Select(c => c.Id));
                        categoryIds.Add(0); // Root parent

                        var invalidParentIds = new List<(string name, int parentId)>();

                        foreach (var item in triggers.TriggerItems)
                        {
                            if (item is TriggerCategoryDefinition cat)
                            {
                                if (cat.ParentId != 0 && !categoryIds.Contains(cat.ParentId))
                                {
                                    invalidParentIds.Add((cat.Name, cat.ParentId));
                                }
                            }
                            else if (item is TriggerDefinition trig)
                            {
                                if (!categoryIds.Contains(trig.ParentId))
                                {
                                    invalidParentIds.Add((trig.Name, trig.ParentId));
                                }
                            }
                        }

                        if (invalidParentIds.Any())
                        {
                            errors.Add($"Found {invalidParentIds.Count} items with invalid ParentId:");
                            foreach (var (name, parentId) in invalidParentIds.Take(10))
                            {
                                errors.Add($"  - '{name}' has ParentId={parentId} (category doesn't exist)");
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("   ✓ All ParentId references are valid");
                            Console.ResetColor();
                        }

                        // Check 4: Validate trigger functions
                        Console.WriteLine();
                        Console.WriteLine("4. Checking trigger functions...");
                        var functionErrors = 0;

                        foreach (var trigger in triggerDefs)
                        {
                            if (trigger.Functions != null)
                            {
                                try
                                {
                                    ValidateFunctionList(trigger.Functions, trigger.Name, ref functionErrors);
                                }
                                catch (Exception ex)
                                {
                                    errors.Add($"Error validating functions in trigger '{trigger.Name}': {ex.Message}");
                                }
                            }
                        }

                        if (functionErrors > 0)
                        {
                            warnings.Add($"Found {functionErrors} potential issues in trigger functions");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("   ✓ Trigger functions structure looks valid");
                            Console.ResetColor();
                        }
                    }

                    // Check 5: Validate variables
                    Console.WriteLine();
                    Console.WriteLine("5. Checking variables...");
                    if (triggers.Variables == null)
                    {
                        warnings.Add("Variables collection is null");
                    }
                    else
                    {
                        Console.WriteLine($"   Total variables: {triggers.Variables.Count}");

                        var varNames = triggers.Variables.Select(v => v.Name).ToList();
                        var duplicateVars = varNames.GroupBy(n => n)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key)
                            .ToList();

                        if (duplicateVars.Any())
                        {
                            errors.Add($"Found {duplicateVars.Count} duplicate variable names: {string.Join(", ", duplicateVars)}");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("   ✓ All variable names are unique");
                            Console.ResetColor();
                        }
                    }

                    // Print results
                    Console.WriteLine();
                    Console.WriteLine("VALIDATION RESULTS:");
                    Console.WriteLine("===================");
                    Console.WriteLine();

                    if (errors.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ FOUND {errors.Count} ERRORS:");
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"  - {error}");
                        }
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.WriteLine("These errors will cause World Editor and other tools to crash!");
                    }

                    if (warnings.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ FOUND {warnings.Count} WARNINGS:");
                        foreach (var warning in warnings)
                        {
                            Console.WriteLine($"  - {warning}");
                        }
                        Console.ResetColor();
                        Console.WriteLine();
                    }

                    if (!errors.Any() && !warnings.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ ALL CHECKS PASSED!");
                        Console.WriteLine("The .wtg file structure is valid.");
                        Console.ResetColor();
                    }
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
            });
        }

        private static void ValidateFunctionList(IEnumerable<TriggerFunction> functions, string triggerName, ref int errorCount)
        {
            foreach (var function in functions)
            {
                if (function.Parameters != null)
                {
                    foreach (var param in function.Parameters)
                    {
                        // Check if parameter has nested function
                        if (param.Function != null)
                        {
                            ValidateFunctionList(new[] { param.Function }, triggerName, ref errorCount);
                        }

                        // Check array indexer
                        if (param.ArrayIndexer != null)
                        {
                            // Array indexer can have nested functions
                            if (param.ArrayIndexer.Function != null)
                            {
                                ValidateFunctionList(new[] { param.ArrayIndexer.Function }, triggerName, ref errorCount);
                            }
                        }
                    }
                }

                // Check child functions
                if (function.ChildFunctions != null)
                {
                    ValidateFunctionList(function.ChildFunctions, triggerName, ref errorCount);
                }
            }
        }
    }
}
