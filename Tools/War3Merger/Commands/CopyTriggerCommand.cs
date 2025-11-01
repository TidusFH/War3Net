// ------------------------------------------------------------------------------
// <copyright file="CopyTriggerCommand.cs" company="Drake53">
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
using War3Net.Tools.TriggerMerger.Services;

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to copy specific individual triggers from one map to another.
    /// </summary>
    internal static class CopyTriggerCommand
    {
        public static async Task ExecuteAsync(
            FileInfo sourceFile,
            FileInfo targetFile,
            FileInfo? outputFile,
            string? categoryName,
            string[] triggerNames,
            bool dryRun,
            bool backup)
        {
            try
            {
                Console.WriteLine("TriggerMerger - Copy Specific Triggers");
                Console.WriteLine("======================================");
                Console.WriteLine();
                Console.WriteLine($"Source: {sourceFile.FullName}");
                Console.WriteLine($"Target: {targetFile.FullName}");
                Console.WriteLine($"Triggers to copy: {string.Join(", ", triggerNames)}");
                if (!string.IsNullOrEmpty(categoryName))
                {
                    Console.WriteLine($"From category: {categoryName}");
                }
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
                Console.WriteLine($"✓ Source triggers loaded");
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
                Console.WriteLine($"✓ Target triggers loaded");
                Console.ResetColor();
                Console.WriteLine();

                // Find source category if specified
                TriggerCategoryDefinition? sourceCategory = null;
                if (!string.IsNullOrEmpty(categoryName))
                {
                    sourceCategory = sourceTriggers.TriggerItems?
                        .OfType<TriggerCategoryDefinition>()
                        .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                    if (sourceCategory == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: Category '{categoryName}' not found in source map!");
                        Console.ResetColor();
                        return;
                    }
                }

                // Find source triggers
                var foundTriggers = new List<TriggerDefinition>();
                var notFoundTriggers = new List<string>();

                foreach (var triggerName in triggerNames)
                {
                    var trigger = sourceTriggers.TriggerItems?
                        .OfType<TriggerDefinition>()
                        .FirstOrDefault(t => t.Name.Equals(triggerName, StringComparison.OrdinalIgnoreCase));

                    if (trigger == null)
                    {
                        notFoundTriggers.Add(triggerName);
                        continue;
                    }

                    // If category specified, check if trigger belongs to it
                    if (sourceCategory != null && trigger.ParentId != sourceCategory.Id)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: Trigger '{triggerName}' is not in category '{categoryName}'");
                        Console.ResetColor();
                        continue;
                    }

                    foundTriggers.Add(trigger);
                }

                if (notFoundTriggers.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {notFoundTriggers.Count} triggers not found:");
                    foreach (var name in notFoundTriggers)
                    {
                        Console.WriteLine($"  - {name}");
                    }
                    Console.ResetColor();
                    return;
                }

                if (!foundTriggers.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: No triggers found to copy!");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine($"Found {foundTriggers.Count} triggers to copy:");
                foreach (var trigger in foundTriggers)
                {
                    var category = sourceTriggers.TriggerItems?
                        .OfType<TriggerCategoryDefinition>()
                        .FirstOrDefault(c => c.Id == trigger.ParentId);
                    Console.WriteLine($"  • {trigger.Name} (in category: {category?.Name ?? "Unknown"})");
                }
                Console.WriteLine();

                // Scan for required variables
                Console.WriteLine("Checking for required variables...");
                var variableService = new VariableMergeService();
                var referencedVars = variableService.GetReferencedVariables(
                    foundTriggers,
                    sourceTriggers.Variables?.ToList() ?? new List<VariableDefinition>());

                if (referencedVars.Any())
                {
                    Console.WriteLine($"  Found {referencedVars.Count} variable references");
                    var targetVarNames = new HashSet<string>(
                        targetTriggers.Variables?.Select(v => v.Name) ?? Enumerable.Empty<string>(),
                        StringComparer.OrdinalIgnoreCase);

                    var missingVars = referencedVars.Where(v => !targetVarNames.Contains(v)).ToList();
                    if (missingVars.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Need to add {missingVars.Count} missing variables:");
                        foreach (var varName in missingVars.Take(10))
                        {
                            Console.WriteLine($"    - {varName}");
                        }
                        if (missingVars.Count > 10)
                        {
                            Console.WriteLine($"    ... and {missingVars.Count - 10} more");
                        }
                        Console.ResetColor();
                    }
                }
                Console.WriteLine();

                if (dryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("DRY RUN: No files will be modified. Remove --dry-run to apply changes.");
                    Console.ResetColor();
                    return;
                }

                // Copy triggers to target
                Console.WriteLine("Copying triggers to target map...");

                // Get max ID for new items
                var maxId = targetTriggers.TriggerItems?.Any() == true
                    ? targetTriggers.TriggerItems.Max(item => item.Id)
                    : 0;

                // Find or create target category
                var targetCategory = sourceCategory != null
                    ? FindOrCreateCategory(targetTriggers, sourceCategory, ref maxId)
                    : null;

                // Copy each trigger
                var copiedCount = 0;
                foreach (var sourceTrigger in foundTriggers)
                {
                    var newTriggerId = ++maxId;
                    var newTrigger = new TriggerDefinition
                    {
                        Id = newTriggerId,
                        Name = sourceTrigger.Name,
                        Description = sourceTrigger.Description,
                        IsComment = sourceTrigger.IsComment,
                        IsEnabled = sourceTrigger.IsEnabled,
                        IsCustom = sourceTrigger.IsCustom,
                        IsInitiallyOn = sourceTrigger.IsInitiallyOn,
                        RunOnMapInit = sourceTrigger.RunOnMapInit,
                        ParentId = targetCategory?.Id ?? sourceTrigger.ParentId,
                        Functions = sourceTrigger.Functions,
                    };

                    targetTriggers.TriggerItems?.Add(newTrigger);
                    copiedCount++;
                    Console.WriteLine($"  ✓ Copied: {newTrigger.Name}");
                }

                // Add required variables
                if (referencedVars.Any())
                {
                    var addedVars = variableService.MergeVariables(sourceTriggers, targetTriggers, referencedVars);
                    if (addedVars > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Added {addedVars} variables");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine();

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
                await triggerService.WriteTriggersAsync(targetFile.FullName, outputPath, targetTriggers);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine($"✓ Successfully copied {copiedCount} triggers!");
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

        private static TriggerCategoryDefinition FindOrCreateCategory(
            MapTriggers target,
            TriggerCategoryDefinition sourceCategory,
            ref int maxId)
        {
            // Try to find existing category with same name
            var existing = target.TriggerItems?
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(sourceCategory.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                Console.WriteLine($"  Using existing category: {existing.Name}");
                return existing;
            }

            // Create new category
            var newCategoryId = ++maxId;
            var newCategory = new TriggerCategoryDefinition
            {
                Id = newCategoryId,
                Name = sourceCategory.Name,
                IsComment = sourceCategory.IsComment,
                ParentId = sourceCategory.ParentId,
            };

            target.TriggerItems?.Add(newCategory);
            Console.WriteLine($"  Created new category: {newCategory.Name}");
            return newCategory;
        }
    }
}
