// ------------------------------------------------------------------------------
// <copyright file="MergeWtgFilesCommand.cs" company="Drake53">
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
using War3Net.Tools.TriggerMerger.Services;

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to merge .wtg files directly (no MPQ manipulation).
    /// </summary>
    internal static class MergeWtgFilesCommand
    {
        public static async Task ExecuteAsync(
            FileInfo sourceWtgFile,
            FileInfo targetWtgFile,
            FileInfo outputWtgFile,
            string? categoryName,
            string[]? triggerNames,
            bool validate)
        {
            await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("Direct .wtg File Merger");
                    Console.WriteLine("=======================");
                    Console.WriteLine();
                    Console.WriteLine($"Source .wtg: {sourceWtgFile.FullName}");
                    Console.WriteLine($"Target .wtg: {targetWtgFile.FullName}");
                    Console.WriteLine($"Output .wtg: {outputWtgFile.FullName}");
                    Console.WriteLine();

                    // Read source .wtg
                    Console.WriteLine("Reading source .wtg file...");
                    MapTriggers sourceTriggers;
                    using (var sourceStream = sourceWtgFile.OpenRead())
                    using (var sourceReader = new BinaryReader(sourceStream))
                    {
                        sourceTriggers = sourceReader.ReadMapTriggers();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Loaded source .wtg (v{sourceTriggers.FormatVersion})");
                    Console.WriteLine($"  Categories: {sourceTriggers.TriggerItems?.OfType<TriggerCategoryDefinition>().Count() ?? 0}");
                    Console.WriteLine($"  Triggers: {sourceTriggers.TriggerItems?.OfType<TriggerDefinition>().Count() ?? 0}");
                    Console.WriteLine($"  Variables: {sourceTriggers.Variables?.Count ?? 0}");
                    Console.ResetColor();
                    Console.WriteLine();

                    // Read target .wtg
                    Console.WriteLine("Reading target .wtg file...");
                    MapTriggers targetTriggers;
                    using (var targetStream = targetWtgFile.OpenRead())
                    using (var targetReader = new BinaryReader(targetStream))
                    {
                        targetTriggers = targetReader.ReadMapTriggers();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Loaded target .wtg (v{targetTriggers.FormatVersion})");
                    Console.WriteLine($"  Categories: {targetTriggers.TriggerItems?.OfType<TriggerCategoryDefinition>().Count() ?? 0}");
                    Console.WriteLine($"  Triggers: {targetTriggers.TriggerItems?.OfType<TriggerDefinition>().Count() ?? 0}");
                    Console.WriteLine($"  Variables: {targetTriggers.Variables?.Count ?? 0}");
                    Console.ResetColor();
                    Console.WriteLine();

                    // Check version compatibility
                    if (sourceTriggers.FormatVersion != targetTriggers.FormatVersion)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ WARNING: Format version mismatch!");
                        Console.WriteLine($"  Source: v{sourceTriggers.FormatVersion}");
                        Console.WriteLine($"  Target: v{targetTriggers.FormatVersion}");
                        Console.WriteLine($"  Continuing anyway, but this might cause issues...");
                        Console.ResetColor();
                        Console.WriteLine();
                    }

                    // Perform merge
                    if (triggerNames != null && triggerNames.Length > 0)
                    {
                        // Merge specific triggers
                        Console.WriteLine($"Merging specific triggers: {string.Join(", ", triggerNames)}");
                        MergeSpecificTriggers(sourceTriggers, targetTriggers, categoryName, triggerNames);
                    }
                    else if (!string.IsNullOrEmpty(categoryName))
                    {
                        // Merge entire category
                        Console.WriteLine($"Merging category: {categoryName}");
                        MergeCategory(sourceTriggers, targetTriggers, categoryName);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: Must specify either --category or --triggers");
                        Console.ResetColor();
                        return;
                    }

                    Console.WriteLine();

                    // Write output .wtg
                    Console.WriteLine($"Writing merged .wtg to: {outputWtgFile.FullName}");
                    using (var outputStream = outputWtgFile.Create())
                    using (var outputWriter = new BinaryWriter(outputStream))
                    {
                        outputWriter.Write(targetTriggers);
                    }

                    var outputSize = new FileInfo(outputWtgFile.FullName).Length;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Wrote merged .wtg ({outputSize} bytes)");
                    Console.ResetColor();
                    Console.WriteLine();

                    // Validate output if requested
                    if (validate)
                    {
                        Console.WriteLine("Validating merged .wtg file...");
                        ValidateMergedWtg(outputWtgFile.FullName);
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine();
                    Console.WriteLine("✓ SUCCESS!");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Next steps:");
                    Console.WriteLine($"  1. Use an MPQ editor (e.g., MPQ Editor, Ladik's MPQ Editor)");
                    Console.WriteLine($"  2. Open your target map (.w3x file)");
                    Console.WriteLine($"  3. Replace war3map.wtg with: {outputWtgFile.FullName}");
                    Console.WriteLine($"  4. Save and test in World Editor");
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

        private static void MergeCategory(MapTriggers source, MapTriggers target, string categoryName)
        {
            var merger = new TriggerCategoryMerger();
            var result = merger.CopyCategories(source, target, new List<string> { categoryName }, overwrite: false);

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {result.ErrorMessage}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"✓ Merged category '{categoryName}'");
            Console.WriteLine($"  Triggers copied: {result.CopiedCategories.Sum(c => c.TriggerCount)}");

            // Merge variables
            var variableService = new VariableMergeService();
            var allTriggers = result.CopiedCategories
                .SelectMany(c => target.TriggerItems?.OfType<TriggerDefinition>()
                    .Where(t => t.Name == c.CategoryName) ?? Enumerable.Empty<TriggerDefinition>())
                .ToList();

            var referencedVars = variableService.GetReferencedVariables(
                allTriggers,
                source.Variables?.ToList() ?? new List<VariableDefinition>());

            if (referencedVars.Any())
            {
                var addedVars = variableService.MergeVariables(source, target, referencedVars);
                Console.WriteLine($"✓ Added {addedVars} required variables");
            }
        }

        private static void MergeSpecificTriggers(
            MapTriggers source,
            MapTriggers target,
            string? categoryName,
            string[] triggerNames)
        {
            var maxId = target.TriggerItems?.Any() == true
                ? target.TriggerItems.Max(item => item.Id)
                : 0;

            // Find or create target category if specified
            TriggerCategoryDefinition? targetCategory = null;
            if (!string.IsNullOrEmpty(categoryName))
            {
                var sourceCategory = source.TriggerItems?
                    .OfType<TriggerCategoryDefinition>()
                    .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                if (sourceCategory != null)
                {
                    // Try to find existing category in target
                    targetCategory = target.TriggerItems?
                        .OfType<TriggerCategoryDefinition>()
                        .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                    // Create category if it doesn't exist
                    if (targetCategory == null)
                    {
                        var newCategoryId = ++maxId;
                        targetCategory = new TriggerCategoryDefinition
                        {
                            Id = newCategoryId,
                            Name = sourceCategory.Name,
                            IsComment = sourceCategory.IsComment,
                            ParentId = 0, // Put in root
                        };
                        target.TriggerItems?.Add(targetCategory);
                        Console.WriteLine($"✓ Created category: {categoryName} (ID: {newCategoryId})");
                    }
                }
            }

            var copiedCount = 0;

            foreach (var triggerName in triggerNames)
            {
                var sourceTrigger = source.TriggerItems?
                    .OfType<TriggerDefinition>()
                    .FirstOrDefault(t => t.Name.Equals(triggerName, StringComparison.OrdinalIgnoreCase));

                if (sourceTrigger == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Trigger '{triggerName}' not found in source");
                    Console.ResetColor();
                    continue;
                }

                // Create new trigger with proper ID and ParentId
                var newTriggerId = ++maxId;

                // CRITICAL: Cannot use object initializer with init-only properties!
                // Must set properties after construction
                var newTrigger = new TriggerDefinition();
                newTrigger.Id = newTriggerId;
                newTrigger.Name = sourceTrigger.Name;
                newTrigger.Description = sourceTrigger.Description;
                newTrigger.IsComment = sourceTrigger.IsComment;
                newTrigger.IsEnabled = sourceTrigger.IsEnabled;
                newTrigger.IsCustomTextTrigger = sourceTrigger.IsCustomTextTrigger;
                newTrigger.IsInitiallyOn = sourceTrigger.IsInitiallyOn;
                newTrigger.RunOnMapInit = sourceTrigger.RunOnMapInit;

                // Use target category ID, or root if not found
                newTrigger.ParentId = targetCategory?.Id ?? 0;

                // Copy functions (can't assign directly due to init-only)
                if (sourceTrigger.Functions != null)
                {
                    foreach (var function in sourceTrigger.Functions)
                    {
                        newTrigger.Functions.Add(function);
                    }
                }

                target.TriggerItems?.Add(newTrigger);
                copiedCount++;
                Console.WriteLine($"✓ Copied trigger: {triggerName} (ID: {newTriggerId}, ParentId: {newTrigger.ParentId})");
            }

            Console.WriteLine($"✓ Copied {copiedCount} triggers");
        }

        private static void ValidateMergedWtg(string wtgFilePath)
        {
            try
            {
                using var stream = File.OpenRead(wtgFilePath);
                using var reader = new BinaryReader(stream);
                var triggers = reader.ReadMapTriggers();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Merged .wtg is parseable");
                Console.ResetColor();

                var categories = triggers.TriggerItems?.OfType<TriggerCategoryDefinition>().ToList() ?? new List<TriggerCategoryDefinition>();
                var triggerDefs = triggers.TriggerItems?.OfType<TriggerDefinition>().ToList() ?? new List<TriggerDefinition>();

                Console.WriteLine($"  Final counts:");
                Console.WriteLine($"    Categories: {categories.Count}");
                Console.WriteLine($"    Triggers: {triggerDefs.Count}");
                Console.WriteLine($"    Variables: {triggers.Variables?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Validation failed: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
