// ------------------------------------------------------------------------------
// <copyright file="DiffWtgCommand.cs" company="Drake53">
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

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to show differences between two .wtg files.
    /// </summary>
    internal static class DiffWtgCommand
    {
        public static async Task ExecuteAsync(FileInfo wtg1File, FileInfo wtg2File)
        {
            await Task.Run(() =>
            {
                Console.WriteLine(".wtg File Diff Tool");
                Console.WriteLine("===================");
                Console.WriteLine();
                Console.WriteLine($"File 1: {wtg1File.FullName}");
                Console.WriteLine($"File 2: {wtg2File.FullName}");
                Console.WriteLine();

                try
                {
                    // Read both .wtg files
                    MapTriggers wtg1, wtg2;

                    using (var stream1 = wtg1File.OpenRead())
                    using (var reader1 = new BinaryReader(stream1))
                    {
                        wtg1 = reader1.ReadMapTriggers();
                    }

                    using (var stream2 = wtg2File.OpenRead())
                    using (var reader2 = new BinaryReader(stream2))
                    {
                        wtg2 = reader2.ReadMapTriggers();
                    }

                    Console.WriteLine("SUMMARY:");
                    Console.WriteLine("========");
                    Console.WriteLine();
                    Console.WriteLine($"File 1: v{wtg1.FormatVersion} - {wtg1.TriggerItems?.Count ?? 0} items, {wtg1.Variables?.Count ?? 0} variables");
                    Console.WriteLine($"File 2: v{wtg2.FormatVersion} - {wtg2.TriggerItems?.Count ?? 0} items, {wtg2.Variables?.Count ?? 0} variables");
                    Console.WriteLine();

                    // Compare categories
                    var cats1 = wtg1.TriggerItems?.OfType<TriggerCategoryDefinition>().ToList() ?? new();
                    var cats2 = wtg2.TriggerItems?.OfType<TriggerCategoryDefinition>().ToList() ?? new();

                    Console.WriteLine("CATEGORIES:");
                    Console.WriteLine($"  File 1: {cats1.Count}");
                    Console.WriteLine($"  File 2: {cats2.Count}");
                    Console.WriteLine($"  Difference: {cats2.Count - cats1.Count:+#;-#;0}");

                    var catNames1 = new HashSet<string>(cats1.Select(c => c.Name));
                    var catNames2 = new HashSet<string>(cats2.Select(c => c.Name));

                    var newCats = catNames2.Except(catNames1).ToList();
                    if (newCats.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  + Added categories ({newCats.Count}):");
                        foreach (var cat in newCats)
                        {
                            var category = cats2.First(c => c.Name == cat);
                            Console.WriteLine($"    + {cat} (ID: {category.Id}, ParentId: {category.ParentId})");
                        }
                        Console.ResetColor();
                    }

                    var removedCats = catNames1.Except(catNames2).ToList();
                    if (removedCats.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  - Removed categories ({removedCats.Count}):");
                        foreach (var cat in removedCats)
                        {
                            Console.WriteLine($"    - {cat}");
                        }
                        Console.ResetColor();
                    }
                    Console.WriteLine();

                    // Compare triggers
                    var trigs1 = wtg1.TriggerItems?.OfType<TriggerDefinition>().ToList() ?? new();
                    var trigs2 = wtg2.TriggerItems?.OfType<TriggerDefinition>().ToList() ?? new();

                    Console.WriteLine("TRIGGERS:");
                    Console.WriteLine($"  File 1: {trigs1.Count}");
                    Console.WriteLine($"  File 2: {trigs2.Count}");
                    Console.WriteLine($"  Difference: {trigs2.Count - trigs1.Count:+#;-#;0}");

                    var trigNames1 = new HashSet<string>(trigs1.Select(t => t.Name));
                    var trigNames2 = new HashSet<string>(trigs2.Select(t => t.Name));

                    var newTrigs = trigNames2.Except(trigNames1).ToList();
                    if (newTrigs.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  + Added triggers ({newTrigs.Count}):");

                        // Build category lookup for File 2
                        var catById2 = cats2.ToDictionary(c => c.Id, c => c.Name);
                        catById2[0] = "(root)";

                        foreach (var trigName in newTrigs)
                        {
                            var trigger = trigs2.First(t => t.Name == trigName);
                            var parentCatName = catById2.TryGetValue(trigger.ParentId, out var name) ? name : $"INVALID ID {trigger.ParentId}";

                            Console.Write($"    + {trigName} (ID: {trigger.Id}, ParentId: {trigger.ParentId} → {parentCatName})");

                            // Check if ParentId is valid
                            if (!catById2.ContainsKey(trigger.ParentId))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write($" ✗ INVALID PARENT!");
                                Console.ResetColor();
                            }
                            Console.WriteLine();
                        }
                        Console.ResetColor();
                    }

                    var removedTrigs = trigNames1.Except(trigNames2).ToList();
                    if (removedTrigs.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  - Removed triggers ({removedTrigs.Count}):");
                        foreach (var trig in removedTrigs)
                        {
                            Console.WriteLine($"    - {trig}");
                        }
                        Console.ResetColor();
                    }
                    Console.WriteLine();

                    // Compare variables
                    var vars1 = wtg1.Variables?.ToList() ?? new();
                    var vars2 = wtg2.Variables?.ToList() ?? new();

                    Console.WriteLine("VARIABLES:");
                    Console.WriteLine($"  File 1: {vars1.Count}");
                    Console.WriteLine($"  File 2: {vars2.Count}");
                    Console.WriteLine($"  Difference: {vars2.Count - vars1.Count:+#;-#;0}");

                    var varNames1 = new HashSet<string>(vars1.Select(v => v.Name));
                    var varNames2 = new HashSet<string>(vars2.Select(v => v.Name));

                    var newVars = varNames2.Except(varNames1).ToList();
                    if (newVars.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  + Added variables ({newVars.Count}):");
                        foreach (var varName in newVars.Take(10))
                        {
                            var variable = vars2.First(v => v.Name == varName);
                            Console.WriteLine($"    + {varName} ({variable.Type})");
                        }
                        if (newVars.Count > 10)
                        {
                            Console.WriteLine($"    ... and {newVars.Count - 10} more");
                        }
                        Console.ResetColor();
                    }
                    Console.WriteLine();

                    // CRITICAL: Check for invalid references
                    Console.WriteLine("VALIDATION:");
                    Console.WriteLine("===========");
                    Console.WriteLine();

                    var errors = new List<string>();

                    // Check 1: All category IDs are unique
                    var catIds2 = cats2.Select(c => c.Id).ToList();
                    var duplicateCatIds = catIds2.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                    if (duplicateCatIds.Any())
                    {
                        errors.Add($"Duplicate category IDs: {string.Join(", ", duplicateCatIds)}");
                    }

                    // Check 2: All trigger IDs are unique
                    var trigIds2 = trigs2.Select(t => t.Id).ToList();
                    var duplicateTrigIds = trigIds2.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                    if (duplicateTrigIds.Any())
                    {
                        errors.Add($"Duplicate trigger IDs: {string.Join(", ", duplicateTrigIds)}");
                    }

                    // Check 3: All item IDs are unique (categories + triggers)
                    var allIds2 = wtg2.TriggerItems?.Select(item => item.Id).ToList() ?? new();
                    var duplicateIds = allIds2.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                    if (duplicateIds.Any())
                    {
                        errors.Add($"Duplicate item IDs: {string.Join(", ", duplicateIds)}");
                    }

                    // Check 4: All ParentIds are valid
                    var validCatIds = new HashSet<int>(cats2.Select(c => c.Id));
                    validCatIds.Add(0); // Root

                    var invalidParents = new List<(string name, int parentId)>();
                    foreach (var cat in cats2)
                    {
                        if (cat.ParentId != 0 && !validCatIds.Contains(cat.ParentId))
                        {
                            invalidParents.Add((cat.Name, cat.ParentId));
                        }
                    }
                    foreach (var trig in trigs2)
                    {
                        if (!validCatIds.Contains(trig.ParentId))
                        {
                            invalidParents.Add((trig.Name, trig.ParentId));
                        }
                    }

                    if (invalidParents.Any())
                    {
                        errors.Add($"Invalid ParentId references ({invalidParents.Count}):");
                        foreach (var (name, parentId) in invalidParents.Take(5))
                        {
                            errors.Add($"  '{name}' has ParentId={parentId} (category doesn't exist!)");
                        }
                    }

                    if (errors.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ FOUND {errors.Count} ERRORS IN FILE 2:");
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"  {error}");
                        }
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.WriteLine("These errors will cause 'trigger data invalid' in World Editor!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ No structural errors found");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine();
                    Console.WriteLine("Stack trace:");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            });
        }
    }
}
