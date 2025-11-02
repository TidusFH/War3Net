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
                var merger = new Services.TriggerMerger();
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
