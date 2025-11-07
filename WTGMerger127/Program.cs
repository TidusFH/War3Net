using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Script;
using War3Net.IO.Mpq;
using War3Net.Build.Extensions;

namespace WTGMerger127
{
    /// <summary>
    /// WTG Merger for Warcraft 3 1.27 - PROPER OLD FORMAT SUPPORT
    /// Implements position-based category IDs as required by WC3 1.27
    /// </summary>
    class Program
    {
        static bool DEBUG_MODE = true;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║   WTG MERGER FOR WARCRAFT 3 1.27 (OLD FORMAT)            ║");
                Console.WriteLine("║   Position-Based Category IDs | SubVersion=null           ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
                Console.WriteLine();

                string sourcePath, targetPath, outputPath;

                if (args.Length > 0)
                {
                    sourcePath = args[0];
                    targetPath = args.Length > 1 ? args[1] : "../Target/war3map.wtg";
                    outputPath = args.Length > 2 ? args[2] : "../Target/war3map_merged.wtg";
                }
                else
                {
                    sourcePath = AutoDetectMapFile("../Source");
                    targetPath = AutoDetectMapFile("../Target");

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

                Console.WriteLine("Paths:");
                Console.WriteLine($"  Source: {Path.GetFullPath(sourcePath)}");
                Console.WriteLine($"  Target: {Path.GetFullPath(targetPath)}");
                Console.WriteLine($"  Output: {Path.GetFullPath(outputPath)}");
                Console.WriteLine();

                // Read source and target
                Console.WriteLine($"Reading source...");
                MapTriggers sourceTriggers = ReadMapTriggersAuto(sourcePath);
                Console.WriteLine($"✓ Loaded: {sourceTriggers.TriggerItems.Count} items, {sourceTriggers.Variables.Count} variables");
                ShowFormatInfo(sourceTriggers, "SOURCE");

                Console.WriteLine($"\nReading target...");
                MapTriggers targetTriggers = ReadMapTriggersAuto(targetPath);
                Console.WriteLine($"✓ Loaded: {targetTriggers.TriggerItems.Count} items, {targetTriggers.Variables.Count} variables");
                ShowFormatInfo(targetTriggers, "TARGET");

                // CRITICAL: Fix category IDs for old format IMMEDIATELY after loading
                FixCategoryIdsForOldFormat(sourceTriggers);
                FixCategoryIdsForOldFormat(targetTriggers);

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
                    Console.WriteLine("6. Show format & structure debug info");
                    Console.WriteLine("7. Manually fix target category IDs (if needed)");
                    Console.WriteLine("8. Toggle debug mode (currently: " + (DEBUG_MODE ? "ON" : "OFF") + ")");
                    Console.WriteLine("9. Save and exit");
                    Console.WriteLine("0. Exit without saving");
                    Console.WriteLine();
                    Console.Write("Select option (0-9): ");

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
                                Console.WriteLine($"\nMerging category '{categoryName}'...");
                                MergeCategory(sourceTriggers, targetTriggers, categoryName);
                                Console.WriteLine("✓ Category merged!");
                                modified = true;
                            }
                            break;

                        case "5":
                            Console.Write("\nEnter source category name: ");
                            string? sourceCat = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(sourceCat))
                            {
                                ListTriggersInCategory(sourceTriggers, sourceCat);
                                Console.Write("\nEnter trigger name(s) to copy (comma-separated): ");
                                string? triggerNames = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(triggerNames))
                                {
                                    Console.Write("Destination category (empty = same): ");
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
                            ShowComprehensiveDebugInfo(sourceTriggers, targetTriggers);
                            break;

                        case "7":
                            Console.WriteLine("\n=== Manual Category ID Fix ===");
                            Console.WriteLine("This will ensure category IDs match positions for old format.");
                            Console.Write("Proceed? (y/n): ");
                            if (Console.ReadLine()?.ToLower() == "y")
                            {
                                FixCategoryIdsForOldFormat(targetTriggers);
                                Console.WriteLine("✓ Category IDs fixed!");
                                modified = true;
                            }
                            break;

                        case "8":
                            DEBUG_MODE = !DEBUG_MODE;
                            Console.WriteLine($"\n✓ Debug mode: {(DEBUG_MODE ? "ON" : "OFF")}");
                            break;

                        case "9":
                            if (modified)
                            {
                                SaveMergedMap(targetPath, outputPath, targetTriggers);
                            }
                            else
                            {
                                Console.WriteLine("\nNo changes made.");
                            }
                            return;

                        case "0":
                            Console.WriteLine("\nExiting without saving.");
                            return;

                        default:
                            Console.WriteLine("\n⚠ Invalid option.");
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
        /// CRITICAL FUNCTION: Fix category IDs to match positions for old format
        /// This is THE CORE FIX for WC3 1.27 compatibility
        /// </summary>
        static void FixCategoryIdsForOldFormat(MapTriggers triggers)
        {
            if (triggers.SubVersion != null)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] FixCategoryIdsForOldFormat: SubVersion={triggers.SubVersion} (enhanced format, skipping)");
                }
                return; // Enhanced format doesn't need this fix
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] ═══ FixCategoryIdsForOldFormat START ═══");
                Console.WriteLine($"[DEBUG] SubVersion=null detected → OLD FORMAT (WC3 1.27)");
            }

            var categories = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .ToList();

            if (categories.Count == 0)
            {
                if (DEBUG_MODE)
                    Console.WriteLine($"[DEBUG] No categories found");
                return;
            }

            // Build mapping: old ID → new ID (position)
            var idMapping = new Dictionary<int, int>();
            bool needsFix = false;

            for (int position = 0; position < categories.Count; position++)
            {
                var category = categories[position];
                int oldId = category.Id;
                int newId = position; // CRITICAL: ID must equal position

                idMapping[oldId] = newId;

                if (oldId != newId)
                {
                    needsFix = true;
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[DEBUG] Category '{category.Name}': position={position}, oldID={oldId} → newID={newId}");
                    }
                }
            }

            if (!needsFix)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] ✓ Category IDs already match positions!");
                }
                // Still need to fix ParentIds
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Fixing category IDs to match positions (WC3 1.27 requirement)...");
                Console.ResetColor();

                // Update category IDs
                for (int position = 0; position < categories.Count; position++)
                {
                    var category = categories[position];
                    category.Id = position;
                }

                // Update trigger ParentIds to use new category IDs
                var allTriggers = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
                foreach (var trigger in allTriggers)
                {
                    if (trigger.ParentId >= 0 && idMapping.ContainsKey(trigger.ParentId))
                    {
                        int newParentId = idMapping[trigger.ParentId];
                        if (DEBUG_MODE && trigger.ParentId != newParentId)
                        {
                            Console.WriteLine($"[DEBUG]   Trigger '{trigger.Name}': ParentId {trigger.ParentId} → {newParentId}");
                        }
                        trigger.ParentId = newParentId;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Fixed {categories.Count} category IDs to match positions");
                Console.ResetColor();
            }

            // CRITICAL: Set all category ParentIds to 0 (old format requirement)
            int fixedParentIds = 0;
            foreach (var category in categories)
            {
                if (category.ParentId != 0)
                {
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[DEBUG] Category '{category.Name}': ParentId {category.ParentId} → 0");
                    }
                    category.ParentId = 0;
                    fixedParentIds++;
                }
            }

            if (fixedParentIds > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Set {fixedParentIds} category ParentIds to 0 (old format)");
                Console.ResetColor();
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] ═══ FixCategoryIdsForOldFormat END ═══");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Copy specific triggers with proper 1.27 support
        /// </summary>
        static void CopySpecificTriggers(MapTriggers source, MapTriggers target,
            string sourceCategoryName, string[] triggerNames, string destCategoryName)
        {
            var sourceTriggers = GetTriggersInCategory(source, sourceCategoryName);
            var triggersToCopy = new List<TriggerDefinition>();

            foreach (var triggerName in triggerNames)
            {
                var trigger = sourceTriggers.FirstOrDefault(t =>
                    t.Name.Equals(triggerName, StringComparison.OrdinalIgnoreCase));
                if (trigger != null)
                {
                    triggersToCopy.Add(trigger);
                }
                else
                {
                    Console.WriteLine($"  ⚠ Warning: Trigger '{triggerName}' not found");
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
                // CRITICAL: For old format, new category ID MUST equal its position
                var existingCategoryCount = target.TriggerItems
                    .OfType<TriggerCategoryDefinition>()
                    .Count();

                int newCategoryId = existingCategoryCount; // Position-based!
                int newParentId = (target.SubVersion == null) ? 0 : -1; // 0 for old format

                destCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
                {
                    Id = newCategoryId,
                    ParentId = newParentId,
                    Name = destCategoryName,
                    IsComment = false,
                    IsExpanded = true
                };
                target.TriggerItems.Add(destCategory);

                Console.WriteLine($"\n  ✓ Created category '{destCategoryName}' (ID={newCategoryId}, ParentId={newParentId}, Position={existingCategoryCount})");
            }

            // Copy variables used by triggers
            CopyMissingVariables(source, target, triggersToCopy);

            // Find insertion point
            var categoryIndex = target.TriggerItems.IndexOf(destCategory);
            int insertIndex = categoryIndex + 1;

            while (insertIndex < target.TriggerItems.Count &&
                   target.TriggerItems[insertIndex] is not TriggerCategoryDefinition)
            {
                insertIndex++;
            }

            // Copy triggers - ParentId = category position
            Console.WriteLine($"\n  Copying {triggersToCopy.Count} trigger(s):");
            foreach (var sourceTrigger in triggersToCopy)
            {
                // CRITICAL: ParentId must be category POSITION (which equals ID for old format)
                var copiedTrigger = CopyTrigger(sourceTrigger, destCategory.Id);
                target.TriggerItems.Insert(insertIndex, copiedTrigger);
                insertIndex++;
                Console.WriteLine($"    ✓ {copiedTrigger.Name} (ParentId={copiedTrigger.ParentId})");
            }

            // Final fix to ensure everything is correct
            AutoFixCategoriesForFormat(target);
        }

        /// <summary>
        /// Merge entire category with proper 1.27 support
        /// </summary>
        static void MergeCategory(MapTriggers source, MapTriggers target, string categoryName)
        {
            var sourceCategory = source.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (sourceCategory == null)
            {
                throw new InvalidOperationException($"Category '{categoryName}' not found in source");
            }

            var sourceTriggers = GetTriggersInCategory(source, categoryName);
            Console.WriteLine($"  Found {sourceTriggers.Count} triggers in source category");

            // Check if category exists in target
            var targetCategory = target.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (targetCategory != null)
            {
                Console.WriteLine($"  Category exists in target - removing it first");
                RemoveCategory(target, categoryName);
            }

            // CRITICAL: For old format, new category ID MUST equal position
            var existingCategoryCount = target.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .Count();

            int newCategoryId = existingCategoryCount;
            int newParentId = (target.SubVersion == null) ? 0 : -1; // 0 for old format

            var newCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
            {
                Id = newCategoryId,
                ParentId = newParentId,
                Name = sourceCategory.Name,
                IsComment = sourceCategory.IsComment,
                IsExpanded = sourceCategory.IsExpanded
            };

            target.TriggerItems.Add(newCategory);
            Console.WriteLine($"  ✓ Added category (ID={newCategoryId}, ParentId={newParentId}, Position={existingCategoryCount})");

            // Copy variables
            CopyMissingVariables(source, target, sourceTriggers);

            // Copy triggers
            foreach (var sourceTrigger in sourceTriggers)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, newCategory.Id);
                target.TriggerItems.Add(copiedTrigger);
                Console.WriteLine($"    + {copiedTrigger.Name}");
            }

            // Final fix
            AutoFixCategoriesForFormat(target);
        }

        /// <summary>
        /// Final validation before save - ensure old format rules are followed
        /// </summary>
        static void AutoFixCategoriesForFormat(MapTriggers triggers)
        {
            if (triggers.SubVersion != null)
            {
                return; // Enhanced format doesn't need these fixes
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] ═══ AutoFixCategoriesForFormat START ═══");
            }

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();

            // Verify all category ParentIds are 0
            foreach (var category in categories)
            {
                if (category.ParentId != 0)
                {
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[DEBUG] Fixing category '{category.Name}': ParentId {category.ParentId} → 0");
                    }
                    category.ParentId = 0;
                }
            }

            // Verify trigger ParentIds reference valid positions
            var allTriggers = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
            foreach (var trigger in allTriggers)
            {
                if (trigger.ParentId >= 0)
                {
                    if (trigger.ParentId >= categories.Count)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ Trigger '{trigger.Name}' has invalid ParentId={trigger.ParentId} (max={categories.Count - 1})");
                        Console.WriteLine($"  Setting to 0 (first category)");
                        Console.ResetColor();
                        trigger.ParentId = 0;
                    }
                }
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] ═══ AutoFixCategoriesForFormat END ═══");
            }
        }

        /// <summary>
        /// Save merged map with proper 1.27 format preservation
        /// </summary>
        static void SaveMergedMap(string targetPath, string outputPath, MapTriggers triggers)
        {
            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║                    SAVING MERGED MAP                     ║");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════╝");

            // PRE-SAVE VERIFICATION
            Console.WriteLine($"\nFormat: {triggers.FormatVersion}");
            Console.WriteLine($"SubVersion: {triggers.SubVersion?.ToString() ?? "null (OLD FORMAT)"}");
            Console.WriteLine($"Game Version: {triggers.GameVersion}");
            Console.WriteLine($"Variables: {triggers.Variables.Count}");
            Console.WriteLine($"Categories: {triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
            Console.WriteLine($"Triggers: {triggers.TriggerItems.OfType<TriggerDefinition>().Count()}");

            // CRITICAL: NEVER change SubVersion for old format
            if (triggers.SubVersion == null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n✓ SubVersion=null preserved (WC3 1.27 old format)");
                Console.ResetColor();
            }

            // Final category validation
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            bool allCorrect = true;
            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i].Id != i)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ ERROR: Category at position {i} has ID={categories[i].Id} (should be {i})");
                    Console.ResetColor();
                    allCorrect = false;
                }
                if (triggers.SubVersion == null && categories[i].ParentId != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ ERROR: Category '{categories[i].Name}' has ParentId={categories[i].ParentId} (should be 0 for old format)");
                    Console.ResetColor();
                    allCorrect = false;
                }
            }

            if (!allCorrect)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⚠ Validation failed! Running final fix...");
                Console.ResetColor();
                FixCategoryIdsForOldFormat(triggers);
                AutoFixCategoriesForFormat(triggers);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Pre-save validation passed!");
                Console.ResetColor();
            }

            Console.WriteLine($"\nWriting to: {outputPath}");

            // Write file
            if (IsMapArchive(outputPath))
            {
                Console.WriteLine("\nWARNING: war3map.j must be synced!");
                Console.Write("Delete war3map.j from output map? (y/n): ");
                bool deleteJass = Console.ReadLine()?.ToLower() == "y";
                WriteMapArchive(targetPath, outputPath, triggers, deleteJass);
            }
            else
            {
                WriteWTGFile(outputPath, triggers);
            }

            // VERIFICATION: Read back
            Console.WriteLine("\n=== POST-SAVE VERIFICATION ===");
            try
            {
                MapTriggers verified = ReadMapTriggersAuto(outputPath);
                Console.WriteLine($"SubVersion in file: {verified.SubVersion?.ToString() ?? "null"}");
                Console.WriteLine($"Variables: {verified.Variables.Count}");
                Console.WriteLine($"Categories: {verified.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
                Console.WriteLine($"Triggers: {verified.TriggerItems.OfType<TriggerDefinition>().Count()}");

                bool success = true;
                if (triggers.SubVersion == null && verified.SubVersion != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ ERROR: SubVersion changed from null!");
                    Console.ResetColor();
                    success = false;
                }

                if (verified.Variables.Count != triggers.Variables.Count)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ ERROR: Variable count mismatch!");
                    Console.ResetColor();
                    success = false;
                }

                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✓✓✓ SAVE SUCCESSFUL! ✓✓✓");
                    Console.WriteLine("✓ Open in World Editor 1.27 to verify triggers appear in correct categories");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Could not verify: {ex.Message}");
                Console.ResetColor();
            }
        }

        // ===== HELPER FUNCTIONS =====

        static void ShowFormatInfo(MapTriggers triggers, string label)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  [{label}] Format: {triggers.FormatVersion}, SubVersion: {triggers.SubVersion?.ToString() ?? "null"}, Game: v{triggers.GameVersion}");
            Console.ResetColor();
        }

        static void ListCategoriesDetailed(MapTriggers triggers)
        {
            var categories = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .ToList();

            if (categories.Count == 0)
            {
                Console.WriteLine("  (No categories)");
                return;
            }

            Console.WriteLine($"\n  Total: {categories.Count} categories\n");
            Console.WriteLine("  Pos | ID  | ParentId | Name");
            Console.WriteLine("  ----|-----|----------|-----------------------------");

            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                var triggerCount = GetTriggersInCategory(triggers, cat.Name).Count;

                // Highlight if ID != Position
                if (cat.Id != i)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine($"  {i,3} | {cat.Id,3} | {cat.ParentId,8} | {cat.Name} ({triggerCount} triggers)");

                if (cat.Id != i)
                {
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        static void ListTriggersInCategory(MapTriggers triggers, string categoryName)
        {
            var triggerList = GetTriggersInCategory(triggers, categoryName);

            if (triggerList.Count == 0)
            {
                Console.WriteLine($"\n  No triggers in '{categoryName}'");
                return;
            }

            Console.WriteLine($"\n  Triggers in '{categoryName}': {triggerList.Count}\n");
            for (int i = 0; i < triggerList.Count; i++)
            {
                var t = triggerList[i];
                Console.WriteLine($"  [{i + 1}] {t.Name} (ParentId={t.ParentId}, Enabled={t.IsEnabled})");
            }
        }

        static List<TriggerDefinition> GetTriggersInCategory(MapTriggers triggers, string categoryName)
        {
            var category = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return new List<TriggerDefinition>();

            return triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId == category.Id)
                .ToList();
        }

        static void RemoveCategory(MapTriggers triggers, string categoryName)
        {
            var category = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return;

            var itemsToRemove = new List<TriggerItem> { category };
            var categoryIndex = triggers.TriggerItems.IndexOf(category);

            for (int i = categoryIndex + 1; i < triggers.TriggerItems.Count; i++)
            {
                var item = triggers.TriggerItems[i];
                if (item is TriggerCategoryDefinition)
                    break;
                itemsToRemove.Add(item);
            }

            foreach (var item in itemsToRemove)
            {
                triggers.TriggerItems.Remove(item);
            }
        }

        static TriggerDefinition CopyTrigger(TriggerDefinition source, int newParentId)
        {
            var copy = new TriggerDefinition(source.Type)
            {
                Id = 0, // Trigger IDs not saved in old format
                ParentId = newParentId,
                Name = source.Name,
                Description = source.Description,
                IsComment = source.IsComment,
                IsEnabled = source.IsEnabled,
                IsCustomTextTrigger = source.IsCustomTextTrigger,
                IsInitiallyOn = source.IsInitiallyOn,
                RunOnMapInit = source.RunOnMapInit
            };

            foreach (var function in source.Functions)
            {
                copy.Functions.Add(CopyTriggerFunction(function));
            }

            return copy;
        }

        static TriggerFunction CopyTriggerFunction(TriggerFunction source)
        {
            var copy = new TriggerFunction
            {
                Type = source.Type,
                Branch = source.Branch,
                Name = source.Name,
                IsEnabled = source.IsEnabled
            };

            foreach (var param in source.Parameters)
            {
                copy.Parameters.Add(CopyTriggerFunctionParameter(param));
            }

            foreach (var childFunc in source.ChildFunctions)
            {
                copy.ChildFunctions.Add(CopyTriggerFunction(childFunc));
            }

            return copy;
        }

        static TriggerFunctionParameter CopyTriggerFunctionParameter(TriggerFunctionParameter source)
        {
            var copy = new TriggerFunctionParameter
            {
                Type = source.Type,
                Value = source.Value
            };

            if (source.Function != null)
            {
                copy.Function = CopyTriggerFunction(source.Function);
            }

            if (source.ArrayIndexer != null)
            {
                copy.ArrayIndexer = CopyTriggerFunctionParameter(source.ArrayIndexer);
            }

            return copy;
        }

        static void CopyMissingVariables(MapTriggers source, MapTriggers target, List<TriggerDefinition> triggers)
        {
            var usedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var trigger in triggers)
            {
                var vars = GetVariablesUsedByTrigger(trigger, source);
                foreach (var v in vars)
                    usedVariables.Add(v);
            }

            if (usedVariables.Count == 0)
                return;

            Console.WriteLine($"\n  Checking {usedVariables.Count} variable(s)...");

            var targetVarNames = new HashSet<string>(target.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
            var sourceVarDict = source.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

            int copied = 0;
            foreach (var varName in usedVariables)
            {
                if (!targetVarNames.Contains(varName) && sourceVarDict.TryGetValue(varName, out var sourceVar))
                {
                    var newVar = new VariableDefinition
                    {
                        Name = sourceVar.Name,
                        Type = sourceVar.Type,
                        Unk = sourceVar.Unk,
                        IsArray = sourceVar.IsArray,
                        ArraySize = sourceVar.ArraySize,
                        IsInitialized = sourceVar.IsInitialized,
                        InitialValue = sourceVar.InitialValue,
                        Id = 0, // Variable IDs not saved in old format
                        ParentId = sourceVar.ParentId
                    };
                    target.Variables.Add(newVar);
                    targetVarNames.Add(newVar.Name);
                    copied++;
                    Console.WriteLine($"    + Variable: {newVar.Name}");
                }
            }

            if (copied > 0)
            {
                Console.WriteLine($"  ✓ Copied {copied} variable(s)");
            }
        }

        static HashSet<string> GetVariablesUsedByTrigger(TriggerDefinition trigger, MapTriggers mapTriggers)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var function in trigger.Functions)
            {
                CollectVariablesFromFunction(function, used, mapTriggers);
            }
            return used;
        }

        static void CollectVariablesFromFunction(TriggerFunction function, HashSet<string> used, MapTriggers mapTriggers)
        {
            foreach (var param in function.Parameters)
            {
                if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrEmpty(param.Value))
                {
                    used.Add(param.Value);
                }

                if (param.Function != null)
                {
                    CollectVariablesFromFunction(param.Function, used, mapTriggers);
                }

                if (param.ArrayIndexer != null)
                {
                    CollectVariablesFromParameter(param.ArrayIndexer, used, mapTriggers);
                }
            }

            foreach (var child in function.ChildFunctions)
            {
                CollectVariablesFromFunction(child, used, mapTriggers);
            }
        }

        static void CollectVariablesFromParameter(TriggerFunctionParameter param, HashSet<string> used, MapTriggers mapTriggers)
        {
            if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrEmpty(param.Value))
            {
                used.Add(param.Value);
            }

            if (param.Function != null)
            {
                CollectVariablesFromFunction(param.Function, used, mapTriggers);
            }

            if (param.ArrayIndexer != null)
            {
                CollectVariablesFromParameter(param.ArrayIndexer, used, mapTriggers);
            }
        }

        static void ShowComprehensiveDebugInfo(MapTriggers source, MapTriggers target)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              COMPREHENSIVE DEBUG INFO                    ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            Console.WriteLine("\n=== SOURCE MAP ===");
            Console.WriteLine($"Format: {source.FormatVersion}");
            Console.WriteLine($"SubVersion: {source.SubVersion?.ToString() ?? "null (OLD FORMAT)"}");
            Console.WriteLine($"Game Version: {source.GameVersion}");
            Console.WriteLine($"Variables: {source.Variables.Count}");
            Console.WriteLine($"Categories: {source.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
            Console.WriteLine($"Triggers: {source.TriggerItems.OfType<TriggerDefinition>().Count()}");

            Console.WriteLine("\n=== TARGET MAP ===");
            Console.WriteLine($"Format: {target.FormatVersion}");
            Console.WriteLine($"SubVersion: {target.SubVersion?.ToString() ?? "null (OLD FORMAT)"}");
            Console.WriteLine($"Game Version: {target.GameVersion}");
            Console.WriteLine($"Variables: {target.Variables.Count}");
            Console.WriteLine($"Categories: {target.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
            Console.WriteLine($"Triggers: {target.TriggerItems.OfType<TriggerDefinition>().Count()}");

            Console.WriteLine("\n=== TARGET CATEGORY STRUCTURE ===");
            var categories = target.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            Console.WriteLine($"Pos | ID  | ParentId | Match? | Name");
            Console.WriteLine("----|-----|----------|--------|-----------------------------");
            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                bool match = (cat.Id == i);
                string matchStr = match ? "✓" : "✗";

                if (!match)
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"{i,3} | {cat.Id,3} | {cat.ParentId,8} | {matchStr,6} | {cat.Name}");

                if (!match)
                    Console.ResetColor();
            }

            Console.WriteLine("\nPress Enter to continue...");
            Console.ReadLine();
        }

        // ===== FILE I/O =====

        static MapTriggers ReadWTGFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"WTG file not found: {filePath}");
            }

            using var fileStream = File.OpenRead(filePath);
            using var reader = new BinaryReader(fileStream);

            var constructorInfo = typeof(MapTriggers).GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new[] { typeof(BinaryReader), typeof(TriggerData) },
                null);

            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Could not find internal MapTriggers constructor");
            }

            try
            {
                var triggers = (MapTriggers)constructorInfo.Invoke(new object[] { reader, TriggerData.Default });
                return triggers;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse war3map.wtg file. " +
                    $"Inner error: {ex.InnerException?.Message ?? ex.Message}",
                    ex.InnerException ?? ex);
            }
        }

        static void WriteWTGFile(string filePath, MapTriggers triggers)
        {
            using var fileStream = File.Create(filePath);
            using var writer = new BinaryWriter(fileStream);

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
            Console.WriteLine($"✓ Wrote {fileStream.Length} bytes");
        }

        static bool IsMapArchive(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".w3x" || ext == ".w3m";
        }

        static string AutoDetectMapFile(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            var w3xFiles = Directory.GetFiles(folderPath, "*.w3x");
            if (w3xFiles.Length > 0)
            {
                Console.WriteLine($"  Detected: {Path.GetFileName(w3xFiles[0])}");
                return w3xFiles[0];
            }

            var w3mFiles = Directory.GetFiles(folderPath, "*.w3m");
            if (w3mFiles.Length > 0)
            {
                Console.WriteLine($"  Detected: {Path.GetFileName(w3mFiles[0])}");
                return w3mFiles[0];
            }

            var wtgPath = Path.Combine(folderPath, "war3map.wtg");
            if (File.Exists(wtgPath))
            {
                Console.WriteLine($"  Detected: war3map.wtg");
                return wtgPath;
            }

            throw new FileNotFoundException($"No map files found in {folderPath}");
        }

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

        static MapTriggers ReadMapArchiveFile(string archivePath)
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Map archive not found: {archivePath}");
            }

            Console.WriteLine($"  Opening MPQ archive...");
            using var archive = MpqArchive.Open(archivePath, true);
            archive.DiscoverFileNames();

            var triggerFileName = MapTriggers.FileName;

            if (!archive.FileExists(triggerFileName))
            {
                throw new FileNotFoundException($"'{triggerFileName}' not found in map archive");
            }

            Console.WriteLine($"  Extracting {triggerFileName}...");
            using var triggerStream = archive.OpenFile(triggerFileName);
            using var reader = new BinaryReader(triggerStream);

            var constructorInfo = typeof(MapTriggers).GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new[] { typeof(BinaryReader), typeof(TriggerData) },
                null);

            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Could not find internal MapTriggers constructor");
            }

            try
            {
                var triggers = (MapTriggers)constructorInfo.Invoke(new object[] { reader, TriggerData.Default });
                return triggers;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse war3map.wtg from map archive. " +
                    $"Inner error: {ex.InnerException?.Message ?? ex.Message}",
                    ex.InnerException ?? ex);
            }
        }

        static void WriteMapArchive(string originalArchivePath, string outputArchivePath, MapTriggers triggers, bool removeJassFile)
        {
            Console.WriteLine($"  Opening original archive...");
            using var originalArchive = MpqArchive.Open(originalArchivePath, true);
            originalArchive.DiscoverFileNames();

            Console.WriteLine($"  Creating archive builder...");
            var builder = new MpqArchiveBuilder(originalArchive);

            using var triggerStream = new MemoryStream();
            using var writer = new BinaryWriter(triggerStream);

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

            var triggerFileName = MapTriggers.FileName;
            Console.WriteLine($"  Replacing {triggerFileName}...");
            builder.RemoveFile(triggerFileName);
            builder.AddFile(MpqFile.New(triggerStream, triggerFileName));

            if (removeJassFile)
            {
                var jassFileName = "war3map.j";
                if (originalArchive.FileExists(jassFileName))
                {
                    Console.WriteLine($"  Removing {jassFileName}...");
                    builder.RemoveFile(jassFileName);
                }

                var jassFileNameAlt = "scripts/war3map.j";
                if (originalArchive.FileExists(jassFileNameAlt))
                {
                    Console.WriteLine($"  Removing {jassFileNameAlt}...");
                    builder.RemoveFile(jassFileNameAlt);
                }
            }

            Console.WriteLine($"  Saving to {outputArchivePath}...");
            builder.SaveTo(outputArchivePath);
            Console.WriteLine($"  ✓ Archive saved!");
        }
    }
}
