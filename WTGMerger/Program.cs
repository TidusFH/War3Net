using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Script;
using War3Net.IO.Mpq;
using War3Net.Build.Extensions;

namespace WTGMerger
{
    /// <summary>
    /// WC3 1.27 Trigger Merger - Old Format (SubVersion=null) Support
    /// </summary>
    class Program
    {
        static bool DEBUG_MODE = false;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("===============================================================");
                Console.WriteLine("   WTG MERGER FOR WARCRAFT 3 1.27 (OLD FORMAT)");
                Console.WriteLine("===============================================================\n");

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

                Console.WriteLine("File paths:");
                Console.WriteLine($"  Source: {Path.GetFullPath(sourcePath)}");
                Console.WriteLine($"  Target: {Path.GetFullPath(targetPath)}");
                Console.WriteLine($"  Output: {Path.GetFullPath(outputPath)}\n");

                // Load source map
                Console.WriteLine($"+ Reading source: {Path.GetFileName(sourcePath)}");
                MapTriggers sourceTriggers = ReadMapTriggersAuto(sourcePath);
                Console.WriteLine($"  Items: {sourceTriggers.TriggerItems.Count}, Variables: {sourceTriggers.Variables.Count}");

                // Load target map
                Console.WriteLine($"\n+ Reading target: {Path.GetFileName(targetPath)}");
                MapTriggers targetTriggers = ReadMapTriggersAuto(targetPath);
                Console.WriteLine($"  Items: {targetTriggers.TriggerItems.Count}, Variables: {targetTriggers.Variables.Count}");

                // CRITICAL: Fix category structure immediately after loading
                FixCategoryIdsForOldFormat(sourceTriggers, "source");
                FixCategoryIdsForOldFormat(targetTriggers, "target");

                // Show format info
                ShowFormatInfo(sourceTriggers, "SOURCE");
                ShowFormatInfo(targetTriggers, "TARGET");

                // Adjust output path based on target type
                if (IsMapArchive(targetPath) && !IsMapArchive(outputPath))
                {
                    outputPath = Path.ChangeExtension(outputPath, Path.GetExtension(targetPath));
                    Console.WriteLine($"\n! Output adjusted to match target type: {Path.GetFileName(outputPath)}");
                }

                // Interactive menu
                bool modified = false;
                while (true)
                {
                    Console.WriteLine("\n===============================================================");
                    Console.WriteLine("                      MERGE OPTIONS");
                    Console.WriteLine("===============================================================");
                    Console.WriteLine("1. List all categories from SOURCE");
                    Console.WriteLine("2. List all categories from TARGET");
                    Console.WriteLine("3. List triggers in a specific category");
                    Console.WriteLine("4. Copy ENTIRE category");
                    Console.WriteLine("5. Copy SPECIFIC trigger(s)");
                    Console.WriteLine("6. Show format & structure debug info");
                    Console.WriteLine("7. Manually fix target category IDs (if needed)");
                    Console.WriteLine($"8. Toggle debug mode (currently: {(DEBUG_MODE ? "ON" : "OFF")})");
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
                                Console.WriteLine($"\n+ Merging category '{categoryName}' from source to target...");
                                MergeCategory(sourceTriggers, targetTriggers, categoryName);
                                Console.WriteLine("+ Category merged!");
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
                                    Console.WriteLine("+ Trigger(s) copied!");
                                    modified = true;
                                }
                            }
                            break;

                        case "6":
                            ShowDebugInfo(targetTriggers);
                            break;

                        case "7":
                            Console.WriteLine("\n=== Manual Category ID Fix ===");
                            Console.WriteLine("This will force-fix category IDs to match positions.");
                            Console.Write("\nProceed? (y/n): ");
                            string? confirmFix = Console.ReadLine();
                            if (confirmFix?.ToLower() == "y")
                            {
                                FixCategoryIdsForOldFormat(targetTriggers, "target");
                                Console.WriteLine("+ Categories fixed!");
                                modified = true;
                            }
                            break;

                        case "8":
                            DEBUG_MODE = !DEBUG_MODE;
                            Console.WriteLine($"\n+ Debug mode is now {(DEBUG_MODE ? "ON" : "OFF")}");
                            break;

                        case "9":
                            if (modified)
                            {
                                SaveMergedMap(targetPath, outputPath, targetTriggers);
                                Console.WriteLine("\n+ Merge complete!");
                                Console.WriteLine($"+ Output saved to: {Path.GetFileName(outputPath)}");
                            }
                            else
                            {
                                Console.WriteLine("\nNo changes made.");
                            }
                            return;

                        case "0":
                            Console.WriteLine("\nExiting without saving changes.");
                            return;

                        default:
                            Console.WriteLine("\n! Invalid option. Please try again.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nX Error: {ex.Message}");
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                }
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// CRITICAL FUNCTION: Fixes category IDs to match their positions (required for old format)
        /// </summary>
        static void FixCategoryIdsForOldFormat(MapTriggers triggers, string mapName)
        {
            if (triggers.SubVersion != null)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] {mapName} is enhanced format (SubVersion={triggers.SubVersion}), skipping position fix");
                }
                return;
            }

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            if (categories.Count == 0)
            {
                return;
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"\n[DEBUG] === FixCategoryIdsForOldFormat ({mapName}) ===");
                Console.WriteLine($"[DEBUG] Old format detected (SubVersion=null)");
                Console.WriteLine($"[DEBUG] Categories found: {categories.Count}");
            }

            // Build mapping: oldId → newId (position-based)
            var oldIdToNewId = new Dictionary<int, int>();
            var positionToCategory = new Dictionary<int, TriggerCategoryDefinition>();

            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                int currentPosition = triggers.TriggerItems.IndexOf(cat);
                int expectedId = i; // ID should equal position in category list

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] Category '{cat.Name}': ID={cat.Id}, Position={currentPosition}, Expected ID={expectedId}");
                }

                oldIdToNewId[cat.Id] = expectedId;
                positionToCategory[i] = cat;
            }

            // Remove all categories from TriggerItems
            foreach (var cat in categories)
            {
                triggers.TriggerItems.Remove(cat);
            }

            // Re-insert categories at positions 0,1,2,3... with correct IDs
            for (int i = 0; i < categories.Count; i++)
            {
                var cat = positionToCategory[i];
                cat.Id = i; // ID = position
                cat.ParentId = 0; // CRITICAL: Old format uses ParentId=0 for root categories
                triggers.TriggerItems.Insert(i, cat);

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] Re-inserted '{cat.Name}' at position {i} with ID={cat.Id}, ParentId={cat.ParentId}");
                }
            }

            // Update trigger ParentIds using the mapping
            var allTriggers = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
            foreach (var trigger in allTriggers)
            {
                if (oldIdToNewId.ContainsKey(trigger.ParentId))
                {
                    int oldParent = trigger.ParentId;
                    trigger.ParentId = oldIdToNewId[oldParent];

                    if (DEBUG_MODE && oldParent != trigger.ParentId)
                    {
                        Console.WriteLine($"[DEBUG] Updated trigger '{trigger.Name}': ParentId {oldParent} → {trigger.ParentId}");
                    }
                }
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] === Fix Complete ===\n");
            }

            // Validation
            var mismatchCount = 0;
            for (int i = 0; i < categories.Count; i++)
            {
                var cat = triggers.TriggerItems[i] as TriggerCategoryDefinition;
                if (cat != null && cat.Id != i)
                {
                    mismatchCount++;
                }
            }

            if (mismatchCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"X ERROR: {mismatchCount} categories still have ID mismatch after fix!");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Copies specific triggers from source to target
        /// </summary>
        static void CopySpecificTriggers(MapTriggers source, MapTriggers target, string sourceCategoryName,
                                        string[] triggerNames, string destCategoryName)
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
                    Console.WriteLine($"  ! Warning: Trigger '{triggerName}' not found");
                }
            }

            if (triggersToCopy.Count == 0)
            {
                Console.WriteLine("\n  ! No triggers to copy.");
                return;
            }

            // Find or create destination category
            var categories = target.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var destCategory = categories.FirstOrDefault(c => c.Name.Equals(destCategoryName, StringComparison.OrdinalIgnoreCase));

            if (destCategory == null)
            {
                // Create new category
                int newCategoryId = categories.Count; // ID = position
                destCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
                {
                    Id = newCategoryId,
                    ParentId = 0, // CRITICAL: Old format uses 0, not -1
                    Name = destCategoryName,
                    IsComment = false,
                    IsExpanded = true
                };

                // Insert at position = category count (before any triggers)
                target.TriggerItems.Insert(newCategoryId, destCategory);
                Console.WriteLine($"\n  + Created category '{destCategoryName}' (ID={destCategory.Id}, ParentId={destCategory.ParentId})");
            }

            // Copy variables used by triggers
            CopyMissingVariables(source, target, triggersToCopy);

            // Copy triggers with correct ParentId (category position)
            Console.WriteLine($"\n  + Copying {triggersToCopy.Count} trigger(s):");
            foreach (var sourceTrigger in triggersToCopy)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, destCategory.Id);
                target.TriggerItems.Add(copiedTrigger); // Add after all categories
                Console.WriteLine($"    + {copiedTrigger.Name}");
            }

            // Fix category structure to ensure consistency
            FixCategoryIdsForOldFormat(target, "target");
        }

        /// <summary>
        /// Merges entire category with all triggers
        /// </summary>
        static void MergeCategory(MapTriggers source, MapTriggers target, string categoryName)
        {
            // Find source category
            var sourceCategory = source.TriggerItems.OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (sourceCategory == null)
            {
                throw new InvalidOperationException($"Category '{categoryName}' not found in source");
            }

            // Get triggers from source category
            var sourceTriggers = GetTriggersInCategory(source, categoryName);
            Console.WriteLine($"  + Found {sourceTriggers.Count} triggers in source category");

            // Check if category exists in target - if so, remove it
            var targetCategory = target.TriggerItems.OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (targetCategory != null)
            {
                Console.WriteLine($"  + Removing existing category '{categoryName}' from target");
                RemoveCategory(target, categoryName);
                FixCategoryIdsForOldFormat(target, "target");
            }

            // Create new category
            var categories = target.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            int newCategoryId = categories.Count; // ID = position

            var newCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
            {
                Id = newCategoryId,
                ParentId = 0, // CRITICAL: Old format uses 0, not -1
                Name = sourceCategory.Name,
                IsComment = sourceCategory.IsComment,
                IsExpanded = sourceCategory.IsExpanded
            };

            // Insert at correct position
            target.TriggerItems.Insert(newCategoryId, newCategory);
            Console.WriteLine($"  + Added category '{categoryName}' (ID={newCategory.Id}, ParentId={newCategory.ParentId})");

            // Copy variables
            CopyMissingVariables(source, target, sourceTriggers);

            // Copy all triggers
            foreach (var sourceTrigger in sourceTriggers)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, newCategory.Id);
                target.TriggerItems.Add(copiedTrigger);
                Console.WriteLine($"    + {copiedTrigger.Name}");
            }

            // Fix category structure
            FixCategoryIdsForOldFormat(target, "target");
        }

        /// <summary>
        /// Saves merged map with format preservation
        /// </summary>
        static void SaveMergedMap(string targetPath, string outputPath, MapTriggers triggers)
        {
            Console.WriteLine("\n===============================================================");
            Console.WriteLine("                   PRE-SAVE VALIDATION");
            Console.WriteLine("===============================================================");

            // Verify SubVersion hasn't changed
            if (triggers.SubVersion != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("X ERROR: SubVersion was changed from null!");
                Console.WriteLine("  This breaks WC3 1.27 compatibility!");
                Console.ResetColor();
                throw new InvalidOperationException("SubVersion must remain null for old format");
            }

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var allTriggers = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            Console.WriteLine($"SubVersion: {(triggers.SubVersion == null ? "null (OLD FORMAT)" : triggers.SubVersion.ToString())}");
            Console.WriteLine($"Categories: {categories.Count}");
            Console.WriteLine($"Triggers: {allTriggers.Count}");
            Console.WriteLine($"Variables: {triggers.Variables.Count}");

            // Validate category structure
            bool structureValid = true;
            for (int i = 0; i < categories.Count; i++)
            {
                var cat = triggers.TriggerItems[i] as TriggerCategoryDefinition;
                if (cat == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"X ERROR: Position {i} is not a category!");
                    Console.ResetColor();
                    structureValid = false;
                    break;
                }

                if (cat.Id != i)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"X ERROR: Category '{cat.Name}' has ID={cat.Id} but position={i}");
                    Console.ResetColor();
                    structureValid = false;
                }

                if (cat.ParentId != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"X ERROR: Category '{cat.Name}' has ParentId={cat.ParentId} (should be 0)");
                    Console.ResetColor();
                    structureValid = false;
                }
            }

            if (!structureValid)
            {
                Console.WriteLine("\n! Attempting to fix structure before save...");
                FixCategoryIdsForOldFormat(triggers, "target");
            }

            // Verify triggers have valid ParentIds
            foreach (var trigger in allTriggers)
            {
                if (trigger.ParentId < 0 || trigger.ParentId >= categories.Count)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"! Warning: Trigger '{trigger.Name}' has invalid ParentId={trigger.ParentId}");
                    Console.ResetColor();
                }
            }

            // Save
            Console.WriteLine("\n+ Writing file...");
            if (IsMapArchive(outputPath))
            {
                Console.WriteLine("\n  JASS Synchronization:");
                Console.WriteLine("  Do you want to DELETE war3map.j from output?");
                Console.WriteLine("  (World Editor will regenerate it)");
                Console.WriteLine("\n  1. YES - Delete war3map.j (RECOMMENDED)");
                Console.WriteLine("  2. NO  - Keep war3map.j (may cause errors)");
                Console.Write("\n  Choice (1-2): ");

                string? syncChoice = Console.ReadLine();
                bool deleteJassFile = syncChoice == "1";

                WriteMapArchive(targetPath, outputPath, triggers, deleteJassFile);

                if (deleteJassFile)
                {
                    Console.WriteLine("\n  + war3map.j removed - World Editor will regenerate it");
                }
            }
            else
            {
                WriteWTGFile(outputPath, triggers);
                Console.WriteLine("\n  ! NOTE: Remember to delete war3map.j from your map archive");
                Console.WriteLine("    so World Editor can regenerate it!");
            }

            // Verification
            Console.WriteLine("\n===============================================================");
            Console.WriteLine("                  POST-SAVE VERIFICATION");
            Console.WriteLine("===============================================================");

            try
            {
                MapTriggers verifyTriggers = ReadMapTriggersAuto(outputPath);

                Console.WriteLine($"Variables saved: {verifyTriggers.Variables.Count} (expected: {triggers.Variables.Count})");
                Console.WriteLine($"Categories saved: {verifyTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count()} (expected: {categories.Count})");
                Console.WriteLine($"Triggers saved: {verifyTriggers.TriggerItems.OfType<TriggerDefinition>().Count()} (expected: {allTriggers.Count})");
                Console.WriteLine($"SubVersion: {(verifyTriggers.SubVersion == null ? "null (CORRECT)" : $"{verifyTriggers.SubVersion} (ERROR!)")}");

                if (verifyTriggers.SubVersion == null &&
                    verifyTriggers.Variables.Count == triggers.Variables.Count &&
                    verifyTriggers.TriggerItems.Count == triggers.TriggerItems.Count)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n+ All checks passed!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"! Could not verify: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Copies variables used by triggers, with conflict resolution
        /// </summary>
        static void CopyMissingVariables(MapTriggers source, MapTriggers target, List<TriggerDefinition> triggers)
        {
            var usedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var trigger in triggers)
            {
                var varsInTrigger = GetVariablesUsedByTrigger(trigger, source);
                foreach (var varName in varsInTrigger)
                {
                    usedVariables.Add(varName);
                }
            }

            if (usedVariables.Count == 0)
            {
                return;
            }

            Console.WriteLine($"\n  + Analyzing {usedVariables.Count} variable(s):");

            var targetVarNames = new HashSet<string>(target.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
            var sourceVarDict = source.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

            int copiedCount = 0;

            foreach (var varName in usedVariables)
            {
                if (!sourceVarDict.TryGetValue(varName, out var sourceVar))
                {
                    Console.WriteLine($"    ! Warning: '{varName}' not found in source");
                    continue;
                }

                if (!targetVarNames.Contains(varName))
                {
                    // Variable doesn't exist in target - copy it
                    var newVar = new VariableDefinition
                    {
                        Name = sourceVar.Name,
                        Type = sourceVar.Type,
                        Unk = sourceVar.Unk,
                        IsArray = sourceVar.IsArray,
                        ArraySize = sourceVar.ArraySize,
                        IsInitialized = sourceVar.IsInitialized,
                        InitialValue = sourceVar.InitialValue,
                        Id = 0, // Old format doesn't save IDs
                        ParentId = sourceVar.ParentId
                    };

                    target.Variables.Add(newVar);
                    targetVarNames.Add(newVar.Name);
                    copiedCount++;
                    Console.WriteLine($"    + '{newVar.Name}' ({newVar.Type})");
                }
            }

            if (copiedCount > 0)
            {
                Console.WriteLine($"  + Copied {copiedCount} variable(s)");
            }
        }

        /// <summary>
        /// Gets all variable names referenced in a trigger
        /// </summary>
        static HashSet<string> GetVariablesUsedByTrigger(TriggerDefinition trigger, MapTriggers mapTriggers)
        {
            var usedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var function in trigger.Functions)
            {
                CollectVariablesFromFunction(function, usedVariables, mapTriggers);
            }

            return usedVariables;
        }

        /// <summary>
        /// Recursively collects variable names from functions
        /// </summary>
        static void CollectVariablesFromFunction(TriggerFunction function, HashSet<string> usedVariables, MapTriggers mapTriggers)
        {
            foreach (var param in function.Parameters)
            {
                if (param.Type == TriggerFunctionParameterType.Variable)
                {
                    var varName = GetVariableNameFromParameter(param, mapTriggers);
                    if (!string.IsNullOrEmpty(varName))
                    {
                        usedVariables.Add(varName);
                    }
                }

                if (param.Function != null)
                {
                    CollectVariablesFromFunction(param.Function, usedVariables, mapTriggers);
                }

                if (param.ArrayIndexer != null)
                {
                    CollectVariablesFromParameterRecursive(param.ArrayIndexer, usedVariables, mapTriggers);
                }
            }

            foreach (var childFunc in function.ChildFunctions)
            {
                CollectVariablesFromFunction(childFunc, usedVariables, mapTriggers);
            }
        }

        /// <summary>
        /// Recursively collects variables from parameters
        /// </summary>
        static void CollectVariablesFromParameterRecursive(TriggerFunctionParameter param, HashSet<string> usedVariables, MapTriggers mapTriggers)
        {
            if (param.Type == TriggerFunctionParameterType.Variable)
            {
                var varName = GetVariableNameFromParameter(param, mapTriggers);
                if (!string.IsNullOrEmpty(varName))
                {
                    usedVariables.Add(varName);
                }
            }

            if (param.Function != null)
            {
                CollectVariablesFromFunction(param.Function, usedVariables, mapTriggers);
            }

            if (param.ArrayIndexer != null)
            {
                CollectVariablesFromParameterRecursive(param.ArrayIndexer, usedVariables, mapTriggers);
            }
        }

        /// <summary>
        /// Gets variable name from parameter value
        /// </summary>
        static string GetVariableNameFromParameter(TriggerFunctionParameter param, MapTriggers mapTriggers)
        {
            var value = param.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // Check if it's a valid variable name
            var varByName = mapTriggers.Variables.FirstOrDefault(v =>
                v.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (varByName != null)
            {
                return varByName.Name;
            }

            return value;
        }

        /// <summary>
        /// Lists all categories with detailed information
        /// </summary>
        static void ListCategoriesDetailed(MapTriggers triggers)
        {
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();

            if (categories.Count == 0)
            {
                Console.WriteLine("  (No categories found)");
                return;
            }

            Console.WriteLine($"\nTotal: {categories.Count} categories\n");

            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var position = triggers.TriggerItems.IndexOf(category);
                var categoryTriggers = GetTriggersInCategory(triggers, category.Name);

                Console.WriteLine($"  [{i + 1}] {category.Name}");
                Console.WriteLine($"      ID: {category.Id}, Position: {position}, ParentId: {category.ParentId}");
                Console.WriteLine($"      Triggers: {categoryTriggers.Count}");

                // Highlight mismatches
                if (category.Id != position)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"      ! MISMATCH: ID != Position");
                    Console.ResetColor();
                }
                if (triggers.SubVersion == null && category.ParentId != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"      ! WRONG: ParentId should be 0 for old format");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Lists triggers in a category
        /// </summary>
        static void ListTriggersInCategory(MapTriggers mapTriggers, string categoryName)
        {
            var triggers = GetTriggersInCategory(mapTriggers, categoryName);

            if (triggers.Count == 0)
            {
                Console.WriteLine($"\n  No triggers found in category '{categoryName}'");
                return;
            }

            Console.WriteLine($"\nTriggers in '{categoryName}': {triggers.Count}\n");
            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                Console.WriteLine($"  [{i + 1}] {trigger.Name}");
                Console.WriteLine($"      Enabled: {trigger.IsEnabled}");
                Console.WriteLine($"      ParentId: {trigger.ParentId}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Gets triggers in a category (using ParentId)
        /// </summary>
        static List<TriggerDefinition> GetTriggersInCategory(MapTriggers triggers, string categoryName)
        {
            var category = triggers.TriggerItems.OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                return new List<TriggerDefinition>();
            }

            return triggers.TriggerItems.OfType<TriggerDefinition>()
                .Where(t => t.ParentId == category.Id)
                .ToList();
        }

        /// <summary>
        /// Removes category and its triggers
        /// </summary>
        static void RemoveCategory(MapTriggers triggers, string categoryName)
        {
            var category = triggers.TriggerItems.OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                return;
            }

            var itemsToRemove = new List<TriggerItem> { category };

            // Find all triggers in this category
            var categoryTriggers = triggers.TriggerItems.OfType<TriggerDefinition>()
                .Where(t => t.ParentId == category.Id)
                .ToList();

            itemsToRemove.AddRange(categoryTriggers);

            // Remove all items
            foreach (var item in itemsToRemove)
            {
                triggers.TriggerItems.Remove(item);
            }
        }

        /// <summary>
        /// Creates a deep copy of a trigger
        /// </summary>
        static TriggerDefinition CopyTrigger(TriggerDefinition source, int newParentId)
        {
            var copy = new TriggerDefinition(source.Type)
            {
                Id = 0, // Old format doesn't save trigger IDs
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

        /// <summary>
        /// Deep copy of trigger function
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

        /// <summary>
        /// Deep copy of function parameter
        /// </summary>
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

        /// <summary>
        /// Shows format information
        /// </summary>
        static void ShowFormatInfo(MapTriggers triggers, string mapName)
        {
            Console.WriteLine($"\n=== {mapName} Format ===");
            Console.WriteLine($"FormatVersion: {triggers.FormatVersion}");
            Console.WriteLine($"SubVersion: {(triggers.SubVersion == null ? "null (OLD FORMAT)" : triggers.SubVersion.ToString())}");
            Console.WriteLine($"GameVersion: {triggers.GameVersion}");

            if (triggers.SubVersion == null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("+ This is OLD FORMAT (WC3 1.27 compatible)");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("! This is ENHANCED FORMAT (WC3 Reforged)");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Shows debug information
        /// </summary>
        static void ShowDebugInfo(MapTriggers triggers)
        {
            Console.WriteLine("\n===============================================================");
            Console.WriteLine("                  DEBUG INFORMATION");
            Console.WriteLine("===============================================================");

            ShowFormatInfo(triggers, "TARGET");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var allTriggers = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            Console.WriteLine($"\n=== Structure ===");
            Console.WriteLine($"Total TriggerItems: {triggers.TriggerItems.Count}");
            Console.WriteLine($"Categories: {categories.Count}");
            Console.WriteLine($"Triggers: {allTriggers.Count}");
            Console.WriteLine($"Variables: {triggers.Variables.Count}");

            Console.WriteLine($"\n=== Category Position vs ID ===");
            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                var position = triggers.TriggerItems.IndexOf(cat);
                Console.Write($"  '{cat.Name}': Position={position}, ID={cat.Id}, ParentId={cat.ParentId}");

                if (cat.Id != position)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" [MISMATCH!]");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            Console.WriteLine("\nPress Enter to continue...");
            Console.ReadLine();
        }

        /// <summary>
        /// Reads WTG file using reflection
        /// </summary>
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
                    $"Failed to parse war3map.wtg file. Inner error: {ex.InnerException?.Message ?? ex.Message}",
                    ex.InnerException ?? ex);
            }
        }

        /// <summary>
        /// Writes WTG file using reflection
        /// </summary>
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
        }

        /// <summary>
        /// Checks if file is map archive
        /// </summary>
        static bool IsMapArchive(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".w3x" || extension == ".w3m";
        }

        /// <summary>
        /// Auto-detects map files in folder
        /// </summary>
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

        /// <summary>
        /// Reads MapTriggers from .wtg or archive
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
        /// Reads MapTriggers from map archive
        /// </summary>
        static MapTriggers ReadMapArchiveFile(string archivePath)
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Map archive not found: {archivePath}");
            }

            using var archive = MpqArchive.Open(archivePath, true);
            archive.DiscoverFileNames();

            var triggerFileName = MapTriggers.FileName;

            if (!archive.FileExists(triggerFileName))
            {
                throw new FileNotFoundException($"Trigger file '{triggerFileName}' not found in archive.");
            }

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
                    $"Failed to parse war3map.wtg from archive. Inner error: {ex.InnerException?.Message ?? ex.Message}",
                    ex.InnerException ?? ex);
            }
        }

        /// <summary>
        /// Writes MapTriggers to map archive
        /// </summary>
        static void WriteMapArchive(string originalArchivePath, string outputArchivePath, MapTriggers triggers, bool removeJassFile)
        {
            using var originalArchive = MpqArchive.Open(originalArchivePath, true);
            originalArchive.DiscoverFileNames();

            var builder = new MpqArchiveBuilder(originalArchive);

            // Serialize triggers to memory
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

            // Replace trigger file
            var triggerFileName = MapTriggers.FileName;
            builder.RemoveFile(triggerFileName);
            builder.AddFile(MpqFile.New(triggerStream, triggerFileName));

            // Optionally remove JASS file
            if (removeJassFile)
            {
                var jassFiles = new[] { "war3map.j", "scripts/war3map.j" };
                foreach (var jassFile in jassFiles)
                {
                    if (originalArchive.FileExists(jassFile))
                    {
                        builder.RemoveFile(jassFile);
                    }
                }
            }

            // Save
            builder.SaveTo(outputArchivePath);
        }
    }
}
