using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Script;
using War3Net.IO.Mpq;
using War3Net.Build.Extensions;

namespace WTGMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string sourcePath, targetPath, outputPath;

                if (args.Length > 0)
                {
                    // Use command line arguments
                    sourcePath = args[0];
                    targetPath = args.Length > 1 ? args[1] : "../Target/war3map.wtg";
                    outputPath = args.Length > 2 ? args[2] : "../Target/war3map_merged.wtg";
                }
                else
                {
                    // Auto-detect .w3x or .wtg files in default folders
                    sourcePath = AutoDetectMapFile("../Source");
                    targetPath = AutoDetectMapFile("../Target");

                    // Generate output filename based on target
                    if (IsMapArchive(targetPath))
                    {
                        var targetFileName = Path.GetFileNameWithoutExtension(targetPath);
                        var targetExt = Path.GetExtension(targetPath);
                        outputPath = Path.Combine(Path.GetDirectoryName(targetPath) ?? "../Target",
                                                  $"{targetFileName}_merged{targetExt}");
                    }
                    else
                    {
                        outputPath = "../Target/war3map_merged.wtg";
                    }
                }

                Console.WriteLine("=== War3Net WTG Trigger Merger ===\n");

                if (args.Length > 0)
                {
                    Console.WriteLine("Using command line arguments:");
                    Console.WriteLine($"  Source: {sourcePath}");
                    Console.WriteLine($"  Target: {targetPath}");
                    Console.WriteLine($"  Output: {outputPath}");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Using default paths:");
                    Console.WriteLine($"  Source: {Path.GetFullPath(sourcePath)}");
                    Console.WriteLine($"  Target: {Path.GetFullPath(targetPath)}");
                    Console.WriteLine($"  Output: {Path.GetFullPath(outputPath)}");
                    Console.WriteLine();
                }

                // Read source (auto-detect .wtg or .w3x/.w3m)
                Console.WriteLine($"Reading source: {sourcePath}");
                MapTriggers sourceTriggers = ReadMapTriggersAuto(sourcePath);
                Console.WriteLine($"✓ Source loaded: {sourceTriggers.TriggerItems.Count} items, {sourceTriggers.Variables.Count} variables");

                // Read target (auto-detect .wtg or .w3x/.w3m)
                Console.WriteLine($"\nReading target: {targetPath}");
                MapTriggers targetTriggers = ReadMapTriggersAuto(targetPath);
                Console.WriteLine($"✓ Target loaded: {targetTriggers.TriggerItems.Count} items, {targetTriggers.Variables.Count} variables");

                // Auto-adjust output path based on target type
                if (IsMapArchive(targetPath) && !IsMapArchive(outputPath))
                {
                    // If target is .w3x but output is .wtg, change output to .w3x
                    outputPath = Path.ChangeExtension(outputPath, Path.GetExtension(targetPath));
                    Console.WriteLine($"\n⚠ Output adjusted to match target type: {outputPath}");
                }

                // Fix duplicate IDs if they exist
                FixDuplicateIds(targetTriggers);

                // Check and report on category structure
                CheckCategoryStructure(targetTriggers);

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
                    Console.WriteLine("6. Fix all TARGET categories to root-level (ParentId = -1)");
                    Console.WriteLine("7. Save and exit");
                    Console.WriteLine("8. Exit without saving");
                    Console.WriteLine();
                    Console.Write("Select option (1-8): ");

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
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║          FIX CATEGORY NESTING                            ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nThis will set ALL categories in TARGET to root-level (ParentId = -1).");
                            Console.WriteLine("Use this if your categories are incorrectly nested.");
                            Console.Write("\nProceed? (y/n): ");
                            string? confirmFix = Console.ReadLine();
                            if (confirmFix?.ToLower() == "y")
                            {
                                int fixedCount = FixAllCategoriesToRoot(targetTriggers);
                                Console.WriteLine($"\n✓ Fixed {fixedCount} categories to root-level");

                                // Verify the fix worked
                                Console.WriteLine("\n=== Verification ===");
                                var categories = targetTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                                var rootCount = categories.Count(c => c.ParentId == -1);
                                var nestedCount = categories.Count(c => c.ParentId >= 0);
                                Console.WriteLine($"Categories with ParentId=-1: {rootCount}");
                                Console.WriteLine($"Categories with ParentId>=0: {nestedCount}");

                                if (nestedCount > 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("❌ WARNING: Some categories still have ParentId >= 0!");
                                    Console.ResetColor();
                                }

                                modified = true;
                            }
                            break;

                        case "7":
                            if (modified)
                            {
                                Console.WriteLine($"\nPreparing to save merged WTG to: {outputPath}");

                                // CRITICAL: Update trigger item counts before saving
                                UpdateTriggerItemCounts(targetTriggers);

                                // Validate and show statistics
                                ValidateAndShowStats(targetTriggers);

                                // Show ParentId values before saving for debugging
                                Console.WriteLine("\n=== DEBUG: Category ParentIds Before Save ===");
                                var debugCategories = targetTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().Take(5).ToList();
                                foreach (var cat in debugCategories)
                                {
                                    Console.WriteLine($"  '{cat.Name}': ParentId={cat.ParentId}");
                                }
                                if (targetTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count() > 5)
                                {
                                    Console.WriteLine($"  ... and {targetTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count() - 5} more");
                                }

                                Console.WriteLine($"\nWriting file...");

                                // Check if OUTPUT is a map archive (.w3x/.w3m)
                                if (IsMapArchive(outputPath))
                                {
                                    Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                                    Console.WriteLine("║           JASS CODE SYNCHRONIZATION                      ║");
                                    Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                                    Console.WriteLine("\nIMPORTANT: The war3map.j file (JASS code) must be synchronized");
                                    Console.WriteLine("with the war3map.wtg file (trigger structure).");
                                    Console.WriteLine("\nDo you want to DELETE war3map.j from the output map?");
                                    Console.WriteLine("(World Editor will regenerate it when you open the map)");
                                    Console.WriteLine();
                                    Console.WriteLine("1. YES - Delete war3map.j (RECOMMENDED)");
                                    Console.WriteLine("2. NO  - Keep war3map.j (may cause 'trigger data invalid' error)");
                                    Console.Write("\nChoice (1-2): ");

                                    string? syncChoice = Console.ReadLine();
                                    bool deleteJassFile = syncChoice == "1";

                                    WriteMapArchive(targetPath, outputPath, targetTriggers, deleteJassFile);

                                    if (deleteJassFile)
                                    {
                                        Console.WriteLine("\n✓ war3map.j has been removed from the output map");
                                        Console.WriteLine("✓ World Editor will regenerate it when you open the map");
                                    }
                                }
                                else
                                {
                                    WriteWTGFile(outputPath, targetTriggers);
                                    Console.WriteLine("\n⚠ NOTE: You're working with raw .wtg files.");
                                    Console.WriteLine("   Remember to delete war3map.j from your map archive");
                                    Console.WriteLine("   so World Editor can regenerate it!");
                                    Console.WriteLine("\n   See SYNCING-WTG-WITH-J.md for details.");
                                }

                                Console.WriteLine("\n✓ Merge complete!");
                                Console.WriteLine("\n=== Final Target Categories ===");
                                ListCategoriesDetailed(targetTriggers);
                            }
                            else
                            {
                                Console.WriteLine("\nNo changes made.");
                            }
                            return;

                        case "8":
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

            // Use reflection to call internal WriteTo method with specific parameter type
            var writeToMethod = typeof(MapTriggers).GetMethod(
                "WriteTo",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new[] { typeof(BinaryWriter) },  // Specify the exact parameter type
                null);

            if (writeToMethod == null)
            {
                throw new InvalidOperationException("Could not find internal WriteTo(BinaryWriter) method");
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
                // Create new category at root level
                destCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
                {
                    Id = GetNextId(target),
                    ParentId = -1,  // CRITICAL: Root-level category
                    Name = destCategoryName,
                    IsComment = false,
                    IsExpanded = true
                };
                target.TriggerItems.Add(destCategory);
                Console.WriteLine($"\n  ✓ Created new category '{destCategoryName}' (ID={destCategory.Id}, ParentId={destCategory.ParentId})");
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
                var copiedTrigger = CopyTrigger(sourceTrigger, GetNextId(target), destCategory.Id);
                target.TriggerItems.Insert(insertIndex, copiedTrigger);
                insertIndex++;
                Console.WriteLine($"    ✓ {copiedTrigger.Name}");
            }

            // Update trigger item counts
            UpdateTriggerItemCounts(target);
        }

        /// <summary>
        /// Gets all triggers that belong to a specific category (using ParentId)
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

            // Get all triggers that have this category as their parent (using ParentId)
            var triggersInCategory = triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId == category.Id)
                .ToList();

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
            // ALWAYS set ParentId = -1 for root-level when copying between files
            // (source ParentId might point to non-existent category in target)
            var newCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
            {
                Id = GetNextId(target),
                ParentId = -1,  // CRITICAL: Always root-level for copied categories
                Name = sourceCategory.Name,
                IsComment = sourceCategory.IsComment,
                IsExpanded = sourceCategory.IsExpanded
            };

            // Add category at the end
            target.TriggerItems.Add(newCategory);
            Console.WriteLine($"  Added category '{categoryName}' to target (ID={newCategory.Id}, ParentId={newCategory.ParentId})");

            // Copy all triggers
            foreach (var sourceTrigger in sourceCategoryTriggers)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, GetNextId(target), newCategory.Id);
                target.TriggerItems.Add(copiedTrigger);
                Console.WriteLine($"    + Copied trigger: {copiedTrigger.Name}");
            }

            // Update trigger item counts
            UpdateTriggerItemCounts(target);
        }

        /// <summary>
        /// Updates the TriggerItemCounts dictionary based on actual items
        /// </summary>
        static void UpdateTriggerItemCounts(MapTriggers triggers)
        {
            triggers.TriggerItemCounts.Clear();

            foreach (var item in triggers.TriggerItems)
            {
                if (triggers.TriggerItemCounts.ContainsKey(item.Type))
                {
                    triggers.TriggerItemCounts[item.Type]++;
                }
                else
                {
                    triggers.TriggerItemCounts[item.Type] = 1;
                }
            }
        }

        /// <summary>
        /// Validates trigger structure and shows statistics before saving
        /// </summary>
        static void ValidateAndShowStats(MapTriggers triggers)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              VALIDATION & STATISTICS                     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            Console.WriteLine($"\nFormat Version: {triggers.FormatVersion}");
            Console.WriteLine($"SubVersion: {triggers.SubVersion?.ToString() ?? "None"}");
            Console.WriteLine($"Game Version: {triggers.GameVersion}");

            Console.WriteLine($"\nTotal Variables: {triggers.Variables.Count}");
            Console.WriteLine($"Total Trigger Items: {triggers.TriggerItems.Count}");

            Console.WriteLine($"\nTrigger Item Counts:");
            foreach (var kvp in triggers.TriggerItemCounts.OrderBy(x => x.Key))
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }

            // Validate ParentIds
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var categoryIds = new HashSet<int>(categories.Select(c => c.Id));

            // Check category hierarchy
            Console.WriteLine($"\nCategory Hierarchy:");
            var rootCategories = categories.Where(c => c.ParentId < 0).ToList();
            var nestedCategories = categories.Where(c => c.ParentId >= 0).ToList();
            Console.WriteLine($"  Root-level categories: {rootCategories.Count}");
            Console.WriteLine($"  Nested categories: {nestedCategories.Count}");

            foreach (var cat in rootCategories.Take(5))
            {
                Console.WriteLine($"    - {cat.Name} (ID={cat.Id}, ParentId={cat.ParentId})");
            }
            if (rootCategories.Count > 5)
            {
                Console.WriteLine($"    ... and {rootCategories.Count - 5} more");
            }

            // Check for orphaned categories
            var orphanedCategories = nestedCategories
                .Where(c => !categoryIds.Contains(c.ParentId))
                .ToList();

            if (orphanedCategories.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ WARNING: {orphanedCategories.Count} orphaned categories (ParentId points to non-existent category):");
                foreach (var cat in orphanedCategories.Take(5))
                {
                    Console.WriteLine($"  - {cat.Name} (ID={cat.Id}, ParentId={cat.ParentId})");
                }
                if (orphanedCategories.Count > 5)
                {
                    Console.WriteLine($"  ... and {orphanedCategories.Count - 5} more");
                }
                Console.ResetColor();
            }

            // Check for orphaned triggers
            var orphanedTriggers = triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId >= 0 && !categoryIds.Contains(t.ParentId))
                .ToList();

            if (orphanedTriggers.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ WARNING: {orphanedTriggers.Count} orphaned triggers (ParentId points to non-existent category):");
                foreach (var trigger in orphanedTriggers.Take(5))
                {
                    Console.WriteLine($"  - {trigger.Name} (ParentId={trigger.ParentId})");
                }
                if (orphanedTriggers.Count > 5)
                {
                    Console.WriteLine($"  ... and {orphanedTriggers.Count - 5} more");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ No orphaned triggers found");
                Console.ResetColor();
            }

            // Check for duplicate IDs
            var allIds = triggers.TriggerItems.Select(item => item.Id).ToList();
            var duplicateIds = allIds.GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ ERROR: {duplicateIds.Count} duplicate IDs found:");
                foreach (var id in duplicateIds.Take(5))
                {
                    var items = triggers.TriggerItems.Where(item => item.Id == id).ToList();
                    Console.WriteLine($"  ID {id}: {string.Join(", ", items.Select(i => i.Name))}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ No duplicate IDs found");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Fixes all categories to root-level by setting ParentId = -1
        /// </summary>
        static int FixAllCategoriesToRoot(MapTriggers triggers)
        {
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            int fixedCount = 0;

            foreach (var category in categories)
            {
                if (category.ParentId != -1)
                {
                    Console.WriteLine($"  Fixing '{category.Name}' (was ParentId={category.ParentId})");
                    category.ParentId = -1;
                    fixedCount++;
                }
            }

            return fixedCount;
        }

        /// <summary>
        /// Checks and reports on category structure, showing any suspicious ParentId values
        /// </summary>
        static void CheckCategoryStructure(MapTriggers triggers)
        {
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();

            if (categories.Count == 0)
            {
                return;
            }

            Console.WriteLine("\n=== Category Structure Analysis ===");

            var rootCategories = categories.Where(c => c.ParentId < 0).ToList();
            var potentiallyNested = categories.Where(c => c.ParentId >= 0).ToList();

            Console.WriteLine($"Root-level categories (ParentId < 0): {rootCategories.Count}");
            Console.WriteLine($"Potentially nested (ParentId >= 0): {potentiallyNested.Count}");

            if (potentiallyNested.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ WARNING: Found {potentiallyNested.Count} categories with ParentId >= 0:");
                foreach (var cat in potentiallyNested.Take(10))
                {
                    var parentCat = categories.FirstOrDefault(c => c.Id == cat.ParentId);
                    if (parentCat != null)
                    {
                        Console.WriteLine($"  - '{cat.Name}' (ID={cat.Id}, ParentId={cat.ParentId} → nested under '{parentCat.Name}')");
                    }
                    else
                    {
                        Console.WriteLine($"  - '{cat.Name}' (ID={cat.Id}, ParentId={cat.ParentId} → ORPHANED!)");
                    }
                }
                if (potentiallyNested.Count > 10)
                {
                    Console.WriteLine($"  ... and {potentiallyNested.Count - 10} more");
                }
                Console.WriteLine("\nℹ If these should be root-level, they will be fixed when you add new categories.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All categories are at root level");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Fixes duplicate IDs by reassigning unique IDs to all trigger items
        /// </summary>
        static void FixDuplicateIds(MapTriggers triggers)
        {
            // Check if there are any duplicate IDs
            var allIds = triggers.TriggerItems.Select(item => item.Id).ToList();
            var duplicateIds = allIds.GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Count == 0)
            {
                return; // No duplicates, nothing to fix
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠ WARNING: Found {duplicateIds.Count} duplicate ID(s) in target file!");
            Console.WriteLine("  Reassigning unique IDs to all trigger items...");
            Console.ResetColor();

            // Reassign IDs to ALL items (0, 1, 2, 3, ...)
            for (int i = 0; i < triggers.TriggerItems.Count; i++)
            {
                var oldId = triggers.TriggerItems[i].Id;
                triggers.TriggerItems[i].Id = i;
            }

            // Build a mapping of old ID -> new ID
            var oldIdToNewId = new Dictionary<int, int>();
            for (int i = 0; i < triggers.TriggerItems.Count; i++)
            {
                var item = triggers.TriggerItems[i];
                // Store mapping: we just reassigned IDs sequentially
                // The item at index i now has ID = i
                oldIdToNewId[i] = i;
            }

            // Update ParentIds in ALL items (both categories and triggers)
            foreach (var item in triggers.TriggerItems)
            {
                // Skip root-level items (ParentId -1 or 0 should stay that way)
                if (item.ParentId < 0)
                {
                    continue;
                }

                if (item is TriggerDefinition trigger)
                {
                    // Find the category this trigger belongs to
                    var category = triggers.TriggerItems
                        .OfType<TriggerCategoryDefinition>()
                        .FirstOrDefault(c => c.Id == trigger.ParentId);

                    if (category != null)
                    {
                        // ParentId should be the category's NEW ID (which is its index)
                        trigger.ParentId = triggers.TriggerItems.IndexOf(category);
                    }
                    else
                    {
                        // No parent found, make it root-level
                        trigger.ParentId = -1;
                    }
                }
                else if (item is TriggerCategoryDefinition category)
                {
                    // Categories can also be nested - find parent category
                    var parentCategory = triggers.TriggerItems
                        .OfType<TriggerCategoryDefinition>()
                        .FirstOrDefault(c => c.Id == category.ParentId);

                    if (parentCategory != null)
                    {
                        // Update to parent's new ID
                        category.ParentId = triggers.TriggerItems.IndexOf(parentCategory);
                    }
                    else
                    {
                        // No parent found, make it root-level
                        category.ParentId = -1;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Reassigned IDs: 0 to {triggers.TriggerItems.Count - 1}");
            Console.WriteLine($"  ✓ Updated ParentIds for both categories and triggers");
            Console.ResetColor();
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
        /// Creates a deep copy of a trigger with a new ID and parent ID
        /// </summary>
        static TriggerDefinition CopyTrigger(TriggerDefinition source, int newId, int newParentId)
        {
            // Type must be set via constructor (it's read-only)
            var copy = new TriggerDefinition(source.Type)
            {
                Id = newId,
                ParentId = newParentId,  // Set the new parent category ID
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

        /// <summary>
        /// Checks if a file is a Warcraft 3 map archive (.w3x or .w3m)
        /// </summary>
        static bool IsMapArchive(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".w3x" || extension == ".w3m";
        }

        /// <summary>
        /// Auto-detects map files in a folder, prioritizing .w3x/.w3m over .wtg
        /// </summary>
        static string AutoDetectMapFile(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            // Priority 1: Look for .w3x files
            var w3xFiles = Directory.GetFiles(folderPath, "*.w3x");
            if (w3xFiles.Length > 0)
            {
                Console.WriteLine($"  Detected: {Path.GetFileName(w3xFiles[0])} (WC3 map archive)");
                return w3xFiles[0];
            }

            // Priority 2: Look for .w3m files
            var w3mFiles = Directory.GetFiles(folderPath, "*.w3m");
            if (w3mFiles.Length > 0)
            {
                Console.WriteLine($"  Detected: {Path.GetFileName(w3mFiles[0])} (WC3 campaign archive)");
                return w3mFiles[0];
            }

            // Priority 3: Look for war3map.wtg
            var wtgPath = Path.Combine(folderPath, "war3map.wtg");
            if (File.Exists(wtgPath))
            {
                Console.WriteLine($"  Detected: war3map.wtg (raw trigger file)");
                return wtgPath;
            }

            throw new FileNotFoundException($"No map files found in {folderPath}. Looking for: *.w3x, *.w3m, or war3map.wtg");
        }

        /// <summary>
        /// Reads MapTriggers from either a raw .wtg file or a map archive (.w3x/.w3m)
        /// </summary>
        static MapTriggers ReadMapTriggersAuto(string filePath)
        {
            if (IsMapArchive(filePath))
            {
                return ReadMapArchiveFile(filePath);
            }
            else
            {
                return ReadWTGFile(filePath);
            }
        }

        /// <summary>
        /// Reads MapTriggers from a map archive (.w3x/.w3m)
        /// </summary>
        static MapTriggers ReadMapArchiveFile(string archivePath)
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Map archive not found: {archivePath}");
            }

            Console.WriteLine($"  Opening MPQ archive...");
            using var archive = MpqArchive.Open(archivePath, true);
            archive.DiscoverFileNames();

            var triggerFileName = MapTriggers.FileName; // "war3map.wtg"

            if (!archive.FileExists(triggerFileName))
            {
                throw new FileNotFoundException($"Trigger file '{triggerFileName}' not found in map archive.");
            }

            Console.WriteLine($"  Extracting {triggerFileName}...");
            using var triggerStream = archive.OpenFile(triggerFileName);
            using var reader = new BinaryReader(triggerStream);

            // Use reflection to call internal constructor
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
        /// Writes MapTriggers to a map archive, optionally removing war3map.j
        /// </summary>
        static void WriteMapArchive(string originalArchivePath, string outputArchivePath, MapTriggers triggers, bool removeJassFile)
        {
            Console.WriteLine($"  Opening original archive...");
            using var originalArchive = MpqArchive.Open(originalArchivePath, true);
            originalArchive.DiscoverFileNames();

            Console.WriteLine($"  Creating archive builder...");
            var builder = new MpqArchiveBuilder(originalArchive);

            // Serialize triggers to memory
            using var triggerStream = new MemoryStream();
            using var writer = new BinaryWriter(triggerStream);

            // Use reflection to call internal WriteTo method
            var writeToMethod = typeof(MapTriggers).GetMethod(
                "WriteTo",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new[] { typeof(BinaryWriter) },
                null);

            if (writeToMethod == null)
            {
                throw new InvalidOperationException("Could not find internal WriteTo(BinaryWriter) method");
            }

            writeToMethod.Invoke(triggers, new object[] { writer });
            writer.Flush();

            triggerStream.Position = 0;

            // Remove old trigger file and add new one
            var triggerFileName = MapTriggers.FileName; // "war3map.wtg"
            Console.WriteLine($"  Replacing {triggerFileName}...");
            builder.RemoveFile(triggerFileName);
            builder.AddFile(MpqFile.New(triggerStream, triggerFileName));

            // Optionally remove war3map.j to force regeneration
            if (removeJassFile)
            {
                var jassFileName = "war3map.j";
                if (originalArchive.FileExists(jassFileName))
                {
                    Console.WriteLine($"  Removing {jassFileName} for sync...");
                    builder.RemoveFile(jassFileName);
                }

                // Also check for scripts/war3map.j
                var jassFileNameAlt = "scripts/war3map.j";
                if (originalArchive.FileExists(jassFileNameAlt))
                {
                    Console.WriteLine($"  Removing {jassFileNameAlt} for sync...");
                    builder.RemoveFile(jassFileNameAlt);
                }
            }

            // Save the modified archive
            Console.WriteLine($"  Saving to {outputArchivePath}...");
            builder.SaveTo(outputArchivePath);
            Console.WriteLine($"  Archive updated successfully!");
        }
    }
}
