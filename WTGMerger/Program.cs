using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Script;

namespace WTGMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Paths to your WTG files (can be overridden by command line arguments)
                var sourcePath = args.Length > 0 ? args[0] : "../Source/war3map.wtg";
                var targetPath = args.Length > 1 ? args[1] : "../Target/war3map.wtg";
                var outputPath = args.Length > 2 ? args[2] : "../Target/war3map_merged.wtg";

                Console.WriteLine("=== War3Net WTG Trigger Merger ===\n");

                if (args.Length > 0)
                {
                    Console.WriteLine("Using command line arguments:");
                    Console.WriteLine($"  Source: {sourcePath}");
                    Console.WriteLine($"  Target: {targetPath}");
                    Console.WriteLine($"  Output: {outputPath}");
                    Console.WriteLine();
                }

                // Read source WTG file
                Console.WriteLine($"Reading source WTG: {sourcePath}");
                MapTriggers sourceTriggers = ReadWTGFile(sourcePath);
                Console.WriteLine($"✓ Source loaded: {sourceTriggers.TriggerItems.Count} items, {sourceTriggers.Variables.Count} variables");

                // Read target WTG file
                Console.WriteLine($"\nReading target WTG: {targetPath}");
                MapTriggers targetTriggers = ReadWTGFile(targetPath);
                Console.WriteLine($"✓ Target loaded: {targetTriggers.TriggerItems.Count} items, {targetTriggers.Variables.Count} variables");

                // Interactive menu
                bool modified = false;
                while (true)
                {
                    Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║                    MERGE OPTIONS                         ║");
                    Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                    Console.WriteLine("1. List all categories from SOURCE");
                    Console.WriteLine("2. List all categories from TARGET");
                    Console.WriteLine("3. List triggers in a specific category");
                    Console.WriteLine("4. Copy ENTIRE category");
                    Console.WriteLine("5. Copy SPECIFIC trigger(s)");
                    Console.WriteLine("6. Save and exit");
                    Console.WriteLine("7. Exit without saving");
                    Console.WriteLine();
                    Console.Write("Select option (1-7): ");

                    string? choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            Console.WriteLine("\n=== Source Categories ===");
                            ListCategoriesDetailed(sourceTriggers);
                            break;

                        case "2":
                            Console.WriteLine("\n=== Target Categories ===");
                            ListCategoriesDetailed(targetTriggers);
                            break;

                        case "3":
                            Console.Write("\nEnter category name: ");
                            string? catName = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(catName))
                            {
                                ListTriggersInCategory(sourceTriggers, catName);
                            }
                            break;

                        case "4":
                            Console.Write("\nEnter category name to copy: ");
                            string? categoryName = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(categoryName))
                            {
                                Console.WriteLine($"\nMerging category '{categoryName}' from source to target...");
                                MergeCategory(sourceTriggers, targetTriggers, categoryName);
                                Console.WriteLine("✓ Category copied!");
                                modified = true;
                            }
                            break;

                        case "5":
                            Console.Write("\nEnter category name where the trigger is: ");
                            string? sourceCat = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(sourceCat))
                            {
                                ListTriggersInCategory(sourceTriggers, sourceCat);
                                Console.Write("\nEnter trigger name to copy (or multiple separated by comma): ");
                                string? triggerNames = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(triggerNames))
                                {
                                    Console.Write("Enter destination category name (leave empty to keep same): ");
                                    string? destCat = Console.ReadLine();
                                    if (string.IsNullOrWhiteSpace(destCat))
                                        destCat = sourceCat;

                                    var triggers = triggerNames.Split(',').Select(t => t.Trim()).ToArray();
                                    CopySpecificTriggers(sourceTriggers, targetTriggers, sourceCat, triggers, destCat);
                                    Console.WriteLine("✓ Trigger(s) copied!");
                                    modified = true;
                                }
                            }
                            break;

                        case "6":
                            if (modified)
                            {
                                Console.WriteLine($"\nSaving merged WTG to: {outputPath}");
                                WriteWTGFile(outputPath, targetTriggers);
                                Console.WriteLine("✓ Merge complete!");
                                Console.WriteLine("\n=== Final Target Categories ===");
                                ListCategoriesDetailed(targetTriggers);
                            }
                            else
                            {
                                Console.WriteLine("\nNo changes made.");
                            }
                            return;

                        case "7":
                            Console.WriteLine("\nExiting without saving changes.");
                            return;

                        default:
                            Console.WriteLine("\n⚠ Invalid option. Please try again.");
                            break;
                    }
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
        /// Uses reflection to access internal constructor
        /// </summary>
        static MapTriggers ReadWTGFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"WTG file not found: {filePath}");
            }

            using var fileStream = File.OpenRead(filePath);
            using var reader = new BinaryReader(fileStream);

            // Use reflection to call internal constructor: MapTriggers(BinaryReader, TriggerData)
            var constructorInfo = typeof(MapTriggers).GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new[] { typeof(BinaryReader), typeof(TriggerData) },
                null);

            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Could not find internal MapTriggers constructor");
            }

            var triggers = (MapTriggers)constructorInfo.Invoke(new object[] { reader, TriggerData.Default });
            return triggers;
        }

        /// <summary>
        /// Writes MapTriggers object to a WTG file
        /// Uses reflection to access internal WriteTo method
        /// </summary>
        static void WriteWTGFile(string filePath, MapTriggers triggers)
        {
            using var fileStream = File.Create(filePath);
            using var writer = new BinaryWriter(fileStream);

            // Use reflection to call internal WriteTo method
            var writeToMethod = typeof(MapTriggers).GetMethod(
                "WriteTo",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (writeToMethod == null)
            {
                throw new InvalidOperationException("Could not find internal WriteTo method");
            }

            writeToMethod.Invoke(triggers, new object[] { writer });
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
        /// Lists all categories with detailed information
        /// </summary>
        static void ListCategoriesDetailed(MapTriggers triggers)
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

            Console.WriteLine($"\n  Total: {categories.Count} categories\n");
            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var categoryTriggers = GetTriggersInCategory(triggers, category.Name);
                Console.WriteLine($"  [{i + 1}] {category.Name}");
                Console.WriteLine($"      Triggers: {categoryTriggers.Count}");
                Console.WriteLine($"      ID: {category.Id}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Lists all triggers in a specific category
        /// </summary>
        static void ListTriggersInCategory(MapTriggers mapTriggers, string categoryName)
        {
            var triggers = GetTriggersInCategory(mapTriggers, categoryName);

            if (triggers.Count == 0)
            {
                Console.WriteLine($"\n  No triggers found in category '{categoryName}'");
                return;
            }

            Console.WriteLine($"\n  Triggers in '{categoryName}': {triggers.Count}\n");
            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                Console.WriteLine($"  [{i + 1}] {trigger.Name}");
                Console.WriteLine($"      Enabled: {trigger.IsEnabled}");
                Console.WriteLine($"      Events: {trigger.Functions.Count(f => f.Type == TriggerFunctionType.Event)}");
                Console.WriteLine($"      Conditions: {trigger.Functions.Count(f => f.Type == TriggerFunctionType.Condition)}");
                Console.WriteLine($"      Actions: {trigger.Functions.Count(f => f.Type == TriggerFunctionType.Action)}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Copies specific triggers from source to target
        /// </summary>
        static void CopySpecificTriggers(MapTriggers source, MapTriggers target, string sourceCategoryName, string[] triggerNames, string destCategoryName)
        {
            // Get source triggers
            var sourceTriggers = GetTriggersInCategory(source, sourceCategoryName);
            var triggersToCopy = new List<TriggerDefinition>();

            foreach (var triggerName in triggerNames)
            {
                var trigger = sourceTriggers.FirstOrDefault(t => t.Name.Equals(triggerName, StringComparison.OrdinalIgnoreCase));
                if (trigger != null)
                {
                    triggersToCopy.Add(trigger);
                }
                else
                {
                    Console.WriteLine($"  ⚠ Warning: Trigger '{triggerName}' not found in category '{sourceCategoryName}'");
                }
            }

            if (triggersToCopy.Count == 0)
            {
                Console.WriteLine("\n  No triggers to copy.");
                return;
            }

            // Find or create destination category
            var destCategory = target.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(destCategoryName, StringComparison.OrdinalIgnoreCase));

            if (destCategory == null)
            {
                // Create new category
                destCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
                {
                    Id = GetNextId(target),
                    Name = destCategoryName,
                    IsComment = false,
                    IsExpanded = true
                };
                target.TriggerItems.Add(destCategory);
                Console.WriteLine($"\n  ✓ Created new category '{destCategoryName}'");
            }

            // Find insertion point (after the category)
            var categoryIndex = target.TriggerItems.IndexOf(destCategory);
            int insertIndex = categoryIndex + 1;

            // Skip existing triggers in this category to insert at the end
            while (insertIndex < target.TriggerItems.Count &&
                   target.TriggerItems[insertIndex] is not TriggerCategoryDefinition)
            {
                insertIndex++;
            }

            // Copy triggers
            Console.WriteLine($"\n  Copying {triggersToCopy.Count} trigger(s) to category '{destCategoryName}':");
            foreach (var sourceTrigger in triggersToCopy)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, GetNextId(target));
                target.TriggerItems.Insert(insertIndex, copiedTrigger);
                insertIndex++;
                Console.WriteLine($"    ✓ {copiedTrigger.Name}");
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
