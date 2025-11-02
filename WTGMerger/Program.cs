using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Script;
using War3Net.Build.Core.Extensions;

namespace WTGMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Paths to your WTG files
                var sourcePath = "../Source/war3map.wtg";
                var targetPath = "../Target/war3map.wtg";
                var outputPath = "../Target/war3map_merged.wtg";

                Console.WriteLine("=== War3Net WTG Trigger Merger ===\n");

                // Read source WTG file
                Console.WriteLine($"Reading source WTG: {sourcePath}");
                MapTriggers sourceTriggers = ReadWTGFile(sourcePath);
                Console.WriteLine($"✓ Source loaded: {sourceTriggers.TriggerItems.Count} items, {sourceTriggers.Variables.Count} variables");

                // Read target WTG file
                Console.WriteLine($"\nReading target WTG: {targetPath}");
                MapTriggers targetTriggers = ReadWTGFile(targetPath);
                Console.WriteLine($"✓ Target loaded: {targetTriggers.TriggerItems.Count} items, {targetTriggers.Variables.Count} variables");

                // Display trigger categories from source
                Console.WriteLine("\n=== Source Categories ===");
                ListCategories(sourceTriggers);

                Console.WriteLine("\n=== Target Categories (Before Merge) ===");
                ListCategories(targetTriggers);

                // Example: Copy a specific category from source to target
                Console.Write("\nEnter category name to copy (or press Enter to skip): ");
                string? categoryName = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    Console.WriteLine($"\nMerging category '{categoryName}' from source to target...");
                    MergeCategory(sourceTriggers, targetTriggers, categoryName);

                    // Save the merged result
                    Console.WriteLine($"\nSaving merged WTG to: {outputPath}");
                    WriteWTGFile(outputPath, targetTriggers);
                    Console.WriteLine("✓ Merge complete!");

                    Console.WriteLine("\n=== Target Categories (After Merge) ===");
                    ListCategories(targetTriggers);
                }
                else
                {
                    Console.WriteLine("\nNo category specified. Exiting without merge.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Reads a raw WTG file and returns MapTriggers object
        /// </summary>
        static MapTriggers ReadWTGFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"WTG file not found: {filePath}");
            }

            using var fileStream = File.OpenRead(filePath);
            using var reader = new BinaryReader(fileStream);

            // Use War3Net's built-in WTG parser extension method
            return reader.ReadMapTriggers(TriggerData.Default);
        }

        /// <summary>
        /// Writes MapTriggers object to a WTG file
        /// </summary>
        static void WriteWTGFile(string filePath, MapTriggers triggers)
        {
            using var fileStream = File.Create(filePath);
            using var writer = new BinaryWriter(fileStream);

            // Use War3Net's built-in WTG serialization extension method
            writer.Write(triggers);
        }

        /// <summary>
        /// Lists all categories in a MapTriggers object
        /// </summary>
        static void ListCategories(MapTriggers triggers)
        {
            var categories = triggers.TriggerItems
                .Where(item => item is TriggerCategoryDefinition)
                .Cast<TriggerCategoryDefinition>()
                .ToList();

            if (categories.Count == 0)
            {
                Console.WriteLine("  (No categories found)");
                return;
            }

            foreach (var category in categories)
            {
                var triggerCount = GetTriggersInCategory(triggers, category.Name).Count;
                Console.WriteLine($"  - {category.Name} ({triggerCount} triggers)");
            }
        }

        /// <summary>
        /// Gets all triggers that belong to a specific category
        /// </summary>
        static List<TriggerDefinition> GetTriggersInCategory(MapTriggers triggers, string categoryName)
        {
            var category = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                return new List<TriggerDefinition>();
            }

            var categoryIndex = triggers.TriggerItems.IndexOf(category);
            var triggersInCategory = new List<TriggerDefinition>();

            // Get all triggers that come after this category (until next category or end)
            for (int i = categoryIndex + 1; i < triggers.TriggerItems.Count; i++)
            {
                var item = triggers.TriggerItems[i];

                // Stop when we hit another category
                if (item is TriggerCategoryDefinition)
                {
                    break;
                }

                // Add triggers
                if (item is TriggerDefinition trigger)
                {
                    triggersInCategory.Add(trigger);
                }
            }

            return triggersInCategory;
        }

        /// <summary>
        /// Merges a category from source to target
        /// </summary>
        static void MergeCategory(MapTriggers source, MapTriggers target, string categoryName)
        {
            // Find source category
            var sourceCategory = source.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (sourceCategory == null)
            {
                throw new InvalidOperationException($"Category '{categoryName}' not found in source");
            }

            // Get triggers from source category
            var sourceCategoryTriggers = GetTriggersInCategory(source, categoryName);
            Console.WriteLine($"  Found {sourceCategoryTriggers.Count} triggers in source category");

            // Check if category already exists in target
            var targetCategory = target.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (targetCategory != null)
            {
                Console.WriteLine($"  Category '{categoryName}' already exists in target - removing it");
                RemoveCategory(target, categoryName);
            }

            // Create new category in target (Type must be set via constructor)
            var newCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
            {
                Id = GetNextId(target),
                Name = sourceCategory.Name,
                IsComment = sourceCategory.IsComment,
                IsExpanded = sourceCategory.IsExpanded
            };

            // Add category at the end
            target.TriggerItems.Add(newCategory);
            Console.WriteLine($"  Added category '{categoryName}' to target");

            // Copy all triggers
            foreach (var sourceTrigger in sourceCategoryTriggers)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, GetNextId(target));
                target.TriggerItems.Add(copiedTrigger);
                Console.WriteLine($"    + Copied trigger: {copiedTrigger.Name}");
            }
        }

        /// <summary>
        /// Removes a category and all its triggers from MapTriggers
        /// </summary>
        static void RemoveCategory(MapTriggers triggers, string categoryName)
        {
            var category = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                return;
            }

            var categoryIndex = triggers.TriggerItems.IndexOf(category);
            var itemsToRemove = new List<TriggerItem> { category };

            // Find all triggers in this category
            for (int i = categoryIndex + 1; i < triggers.TriggerItems.Count; i++)
            {
                var item = triggers.TriggerItems[i];

                if (item is TriggerCategoryDefinition)
                {
                    break;
                }

                itemsToRemove.Add(item);
            }

            // Remove all items
            foreach (var item in itemsToRemove)
            {
                triggers.TriggerItems.Remove(item);
            }
        }

        /// <summary>
        /// Gets the next available ID for a trigger item
        /// </summary>
        static int GetNextId(MapTriggers triggers)
        {
            if (triggers.TriggerItems.Count == 0)
            {
                return 0;
            }

            return triggers.TriggerItems.Max(item => item.Id) + 1;
        }

        /// <summary>
        /// Creates a deep copy of a trigger with a new ID
        /// </summary>
        static TriggerDefinition CopyTrigger(TriggerDefinition source, int newId)
        {
            // Type must be set via constructor (it's read-only)
            var copy = new TriggerDefinition(source.Type)
            {
                Id = newId,
                Name = source.Name,
                Description = source.Description,
                IsComment = source.IsComment,
                IsEnabled = source.IsEnabled,
                IsCustomTextTrigger = source.IsCustomTextTrigger,
                IsInitiallyOn = source.IsInitiallyOn,
                RunOnMapInit = source.RunOnMapInit
            };

            // Deep copy all functions (events, conditions, actions)
            foreach (var function in source.Functions)
            {
                copy.Functions.Add(CopyTriggerFunction(function));
            }

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of a trigger function
        /// </summary>
        static TriggerFunction CopyTriggerFunction(TriggerFunction source)
        {
            var copy = new TriggerFunction
            {
                Type = source.Type,
                Branch = source.Branch,
                Name = source.Name,
                IsEnabled = source.IsEnabled
            };

            // Copy parameters
            foreach (var param in source.Parameters)
            {
                copy.Parameters.Add(CopyTriggerFunctionParameter(param));
            }

            // Copy child functions (for if-then-else blocks)
            foreach (var childFunc in source.ChildFunctions)
            {
                copy.ChildFunctions.Add(CopyTriggerFunction(childFunc));
            }

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of a trigger function parameter
        /// </summary>
        static TriggerFunctionParameter CopyTriggerFunctionParameter(TriggerFunctionParameter source)
        {
            var copy = new TriggerFunctionParameter
            {
                Type = source.Type,
                Value = source.Value
            };

            // Copy nested function if present
            if (source.Function != null)
            {
                copy.Function = CopyTriggerFunction(source.Function);
            }

            // Copy array indexer if present
            if (source.ArrayIndexer != null)
            {
                copy.ArrayIndexer = CopyTriggerFunctionParameter(source.ArrayIndexer);
            }

            return copy;
        }
    }
}
