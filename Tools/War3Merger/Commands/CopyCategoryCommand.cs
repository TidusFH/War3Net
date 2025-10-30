// ------------------------------------------------------------------------------
// <copyright file="CopyCategoryCommand.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using War3Net.Tools.TriggerMerger.Services;

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to copy trigger categories from one map to another.
    /// </summary>
    internal static class CopyCategoryCommand
    {
        public static async Task ExecuteAsync(
            FileInfo sourceFile,
            FileInfo targetFile,
            FileInfo? outputFile,
            string category,
            string[]? categories,
            bool dryRun,
            bool backup,
            bool overwrite)
        {
            try
            {
                // Determine which categories to copy
                var categoriesToCopy = new List<string>();
                if (categories != null && categories.Length > 0)
                {
                    categoriesToCopy.AddRange(categories);
                }

                if (!string.IsNullOrWhiteSpace(category) && !categoriesToCopy.Contains(category))
                {
                    categoriesToCopy.Add(category);
                }

                if (categoriesToCopy.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: No categories specified to copy.");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine("TriggerMerger - Copy Category Command");
                Console.WriteLine("=====================================");
                Console.WriteLine();
                Console.WriteLine($"Source: {sourceFile.FullName}");
                Console.WriteLine($"Target: {targetFile.FullName}");
                Console.WriteLine($"Categories to copy: {string.Join(", ", categoriesToCopy)}");
                Console.WriteLine($"Overwrite existing: {overwrite}");
                Console.WriteLine($"Dry run: {dryRun}");
                Console.WriteLine();

                var triggerService = new TriggerService();

                // Read source triggers
                Console.WriteLine("Reading source map triggers...");
                var sourceTriggers = await triggerService.ReadTriggersAsync(sourceFile.FullName);
                if (sourceTriggers == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Could not read triggers from source map.");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Source triggers loaded (Version: {sourceTriggers.FormatVersion}, Items: {sourceTriggers.TriggerItems?.Count ?? 0})");
                Console.ResetColor();

                // Read target triggers
                Console.WriteLine("Reading target map triggers...");
                var targetTriggers = await triggerService.ReadTriggersAsync(targetFile.FullName);
                if (targetTriggers == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Could not read triggers from target map.");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Target triggers loaded (Version: {targetTriggers.FormatVersion}, Items: {targetTriggers.TriggerItems?.Count ?? 0})");
                Console.ResetColor();
                Console.WriteLine();

                // Copy categories
                Console.WriteLine("Processing categories...");
                var merger = new TriggerCategoryMerger();
                var result = merger.CopyCategories(sourceTriggers, targetTriggers, categoriesToCopy, overwrite);

                if (!result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {result.ErrorMessage}");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("Changes to be applied:");
                Console.WriteLine("----------------------");

                foreach (var copiedCategory in result.CopiedCategories)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  + Category: {copiedCategory.CategoryName}");
                    Console.ResetColor();
                    Console.WriteLine($"    Triggers: {copiedCategory.TriggerCount}");
                    Console.WriteLine($"    Action: {(copiedCategory.WasOverwritten ? "Overwrite" : "Add New")}");
                }

                // CRITICAL DEBUG: Verify the modified triggers actually have the new items
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("DEBUG: Verifying merge result...");
                Console.WriteLine($"  - ModifiedTriggers TriggerItems count: {result.ModifiedTriggers.TriggerItems?.Count ?? 0}");
                Console.WriteLine($"  - Original target TriggerItems count: {targetTriggers.TriggerItems?.Count ?? 0}");
                Console.WriteLine($"  - Are they the same object? {ReferenceEquals(result.ModifiedTriggers, targetTriggers)}");

                // Count categories and triggers separately
                if (result.ModifiedTriggers.TriggerItems != null)
                {
                    var allCategories = result.ModifiedTriggers.TriggerItems.OfType<War3Net.Build.Script.TriggerCategoryDefinition>().ToList();
                    var allTriggers = result.ModifiedTriggers.TriggerItems.OfType<War3Net.Build.Script.TriggerDefinition>().ToList();
                    Console.WriteLine($"  - Categories in modified triggers: {allCategories.Count}");
                    Console.WriteLine($"  - Actual triggers in modified triggers: {allTriggers.Count}");

                    // Check if the specific category exists
                    var spelsHeroes = allCategories.FirstOrDefault(c => c.Name.Equals("Spels Heroes", StringComparison.OrdinalIgnoreCase));
                    if (spelsHeroes != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ 'Spels Heroes' category found with ID: {spelsHeroes.Id}");
                        Console.ResetColor();

                        // Count triggers that belong to this category
                        var spelsHeroesTriggers = allTriggers.Where(t => t.ParentId == spelsHeroes.Id).ToList();
                        Console.WriteLine($"  - Triggers with ParentId={spelsHeroes.Id}: {spelsHeroesTriggers.Count}");
                        if (spelsHeroesTriggers.Any())
                        {
                            Console.WriteLine($"  - First 3 trigger names: {string.Join(", ", spelsHeroesTriggers.Take(3).Select(t => t.Name))}");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ 'Spels Heroes' category NOT FOUND in modified triggers!");
                        Console.ResetColor();
                    }
                }
                Console.ResetColor();

                if (result.SkippedCategories.Any())
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Skipped categories (already exist, use --overwrite to replace):");
                    foreach (var skipped in result.SkippedCategories)
                    {
                        Console.WriteLine($"  - {skipped}");
                    }

                    Console.ResetColor();
                }

                if (result.NotFoundCategories.Any())
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Categories not found in source:");
                    foreach (var notFound in result.NotFoundCategories)
                    {
                        Console.WriteLine($"  - {notFound}");
                    }

                    Console.ResetColor();
                }

                Console.WriteLine();

                if (dryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("DRY RUN: No files were modified. Remove --dry-run to apply changes.");
                    Console.ResetColor();
                    return;
                }

                // Determine output path
                var outputPath = outputFile?.FullName;
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    var directory = Path.GetDirectoryName(targetFile.FullName);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(targetFile.FullName);
                    var extension = Path.GetExtension(targetFile.FullName);
                    outputPath = Path.Combine(directory!, $"{fileNameWithoutExt}_merged{extension}");
                }

                // Create backup if requested
                if (backup && File.Exists(targetFile.FullName))
                {
                    var backupPath = $"{targetFile.FullName}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                    Console.WriteLine($"Creating backup: {backupPath}");
                    File.Copy(targetFile.FullName, backupPath, true);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Backup created");
                    Console.ResetColor();
                }

                // Write the modified triggers
                Console.WriteLine($"Writing modified map to: {outputPath}");
                await triggerService.WriteTriggersAsync(targetFile.FullName, outputPath, result.ModifiedTriggers);

                // CRITICAL: Merge trigger strings (.wts) if needed
                Console.WriteLine();
                Console.WriteLine("Checking for trigger string references...");
                var stringService = new TriggerStringService();

                // Get all copied triggers
                var copiedTriggers = new List<War3Net.Build.Script.TriggerDefinition>();
                foreach (var copiedCategory in result.CopiedCategories)
                {
                    var foundCategory = result.ModifiedTriggers.TriggerItems?
                        .OfType<War3Net.Build.Script.TriggerCategoryDefinition>()
                        .FirstOrDefault(c => c.Name.Equals(copiedCategory.CategoryName, StringComparison.OrdinalIgnoreCase));

                    if (foundCategory != null && result.ModifiedTriggers.TriggerItems != null)
                    {
                        var categoryTriggers = result.ModifiedTriggers.TriggerItems
                            .OfType<War3Net.Build.Script.TriggerDefinition>()
                            .Where(t => t.ParentId == foundCategory.Id)
                            .ToList();
                        copiedTriggers.AddRange(categoryTriggers);
                    }
                }

                // Scan copied triggers for TRIGSTR_ references
                var requiredStringIds = stringService.GetRequiredStringIds(copiedTriggers);

                if (requiredStringIds.Any())
                {
                    Console.WriteLine($"  Found {requiredStringIds.Count} trigger string references in copied triggers");
                    Console.WriteLine($"  Reading trigger strings from maps...");

                    // Read trigger strings from both maps
                    var sourceTriggerStrings = stringService.ReadTriggerStrings(sourceFile.FullName);
                    var targetTriggerStrings = stringService.ReadTriggerStrings(outputPath); // Read from output

                    // Merge required strings
                    var mergedStrings = stringService.MergeTriggerStrings(
                        sourceTriggerStrings,
                        targetTriggerStrings,
                        requiredStringIds);

                    // Write back to output if we added any strings
                    var addedCount = mergedStrings.Strings.Count - (targetTriggerStrings?.Strings.Count ?? 0);
                    if (addedCount > 0)
                    {
                        Console.WriteLine($"  Adding {addedCount} trigger strings to output map...");
                        // Need to create a temporary file to avoid reading/writing same file
                        var tempPath = outputPath + ".tmp";
                        stringService.WriteTriggerStrings(outputPath, tempPath, mergedStrings);

                        // Replace original output with the one that has merged strings
                        File.Delete(outputPath);
                        File.Move(tempPath, outputPath);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Added {addedCount} trigger strings to output map");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"  All required trigger strings already exist in target map");
                    }
                }
                else
                {
                    Console.WriteLine($"  No trigger string references found (triggers use inline strings)");
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Successfully copied trigger categories!");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine($"Output saved to: {outputPath}");
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
        }
    }
}
