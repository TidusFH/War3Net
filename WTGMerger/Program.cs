using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using War3Net.Build.Script;
using War3Net.IO.Mpq;
using War3Net.Build.Extensions;

namespace WTGMerger
{
    class Program
    {
        // Global debug flag - set to true to enable detailed logging
        static bool DEBUG_MODE = false;

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

                // Check variable IDs
                if (DEBUG_MODE && sourceTriggers.Variables.Count > 0)
                {
                    Console.WriteLine("[DEBUG] Source variable IDs:");
                    var idCounts = sourceTriggers.Variables.GroupBy(v => v.Id).OrderBy(g => g.Key).ToList();
                    foreach (var group in idCounts.Take(10))
                    {
                        Console.WriteLine($"[DEBUG]   ID {group.Key}: {group.Count()} variable(s) - {string.Join(", ", group.Select(v => v.Name).Take(3))}");
                    }
                    if (idCounts.Count > 10)
                    {
                        Console.WriteLine($"[DEBUG]   ... and {idCounts.Count - 10} more IDs");
                    }
                }

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

                // Fix variable IDs if they're all 0 or have duplicates
                FixVariableIds(sourceTriggers, "source");
                FixVariableIds(targetTriggers, "target");

                // Check and report on category structure
                CheckCategoryStructure(targetTriggers);

                // Show informational summary about variables
                ShowVariableSummary(sourceTriggers, targetTriggers);

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
                    Console.WriteLine("6. Repair orphaned triggers (fix invalid ParentIds)");
                    Console.WriteLine("7. Diagnose orphans (show orphaned triggers/categories)");
                    Console.WriteLine("8. DEBUG: Show comprehensive debug information");
                    Console.WriteLine("9. Run War3Diagnostic (comprehensive WTG file analysis)");
                    Console.WriteLine("10. Perform FULL MERGE using intermediate approach");
                    Console.WriteLine("11. Perform SELECTIVE MERGE using intermediate approach (choose categories)");
                    Console.WriteLine("12. VALIDATE: Deep validation of specific trigger");
                    Console.WriteLine("13. VALIDATE: Validate all triggers in target");
                    Console.WriteLine("14. TEST: Copy trigger to empty map (isolation test)");
                    Console.WriteLine("15. EXPORT: Export trigger to detailed text file");
                    Console.WriteLine("16. DIAGNOSE: Check source trigger for corruption");
                    Console.WriteLine("17. COMPARE: Compare two triggers side-by-side");
                    Console.WriteLine("18. EXTRACT: Extract trigger + variables to standalone .wtg");
                    Console.WriteLine("19. HEALTH CHECK: Comprehensive WTG file validation");
                    Console.WriteLine($"d. DEBUG: Toggle debug mode (currently: {(DEBUG_MODE ? "ON" : "OFF")})");
                    Console.WriteLine($"l. DIAGNOSTIC: Toggle deep diagnostic logging (currently: {(DiagnosticLogger.IsEnabled ? "ON - logging to file" : "OFF")})");
                    Console.WriteLine("s. Save and exit");
                    Console.WriteLine("0. Exit without saving");
                    Console.WriteLine();
                    Console.Write("Select option: ");

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
                            Console.WriteLine("║          REPAIR ORPHANED TRIGGERS                        ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nThis will fix triggers with invalid ParentIds.");
                            Console.WriteLine("Modes:");
                            Console.WriteLine("  1. Smart - Try to match triggers to categories by name");
                            Console.WriteLine("  2. Root - Move all orphaned triggers to root level");
                            Console.Write("\nSelect mode (1-2): ");
                            string? repairMode = Console.ReadLine();

                            if (repairMode == "1" || repairMode == "2")
                            {
                                string mode = repairMode == "1" ? "smart" : "root";
                                int repaired = OrphanRepair.RepairOrphanedTriggers(targetTriggers, mode);
                                if (repaired > 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"\n✓ Repaired {repaired} orphaned trigger(s)");
                                    Console.ResetColor();
                                    modified = true;
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("\n✓ No orphaned triggers found");
                                    Console.ResetColor();
                                }
                            }
                            break;

                        case "7":
                            OrphanRepair.DiagnoseOrphans(targetTriggers);
                            break;

                        case "8":
                            ShowComprehensiveDebugInfo(sourceTriggers, targetTriggers);
                            break;

                        case "9":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║        WAR3DIAGNOSTIC - COMPREHENSIVE ANALYSIS           ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nThis will generate a detailed diagnostic report comparing:");
                            Console.WriteLine("  - SOURCE file");
                            Console.WriteLine("  - TARGET file");
                            Console.WriteLine("  - Current in-memory state (as if saved)");
                            Console.WriteLine("\nThe report includes:");
                            Console.WriteLine("  • Binary hex dumps and comparisons");
                            Console.WriteLine("  • Complete hierarchy trees");
                            Console.WriteLine("  • File order analysis (WC3 1.27 visual nesting)");
                            Console.WriteLine("  • ParentId distribution statistics");
                            Console.WriteLine("  • Corruption pattern detection");
                            Console.WriteLine("\nReport will be saved to: WTG_Diagnostic_[timestamp].txt");
                            Console.Write("\nProceed? (y/n): ");
                            string? confirmDiag = Console.ReadLine();
                            if (confirmDiag?.ToLower() == "y")
                            {
                                // Save current state to temp file for comparison
                                string tempMergedPath = Path.Combine(Path.GetTempPath(), "WTGMerger_temp_merged.wtg");
                                try
                                {
                                    WriteWTGFile(tempMergedPath, targetTriggers);
                                    Console.WriteLine("\nRunning diagnostic...");
                                    War3Diagnostic.CompareFiles(sourcePath, targetPath, tempMergedPath);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("\n✓ Diagnostic complete! Check the output file.");
                                    Console.ResetColor();
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"\n❌ Diagnostic failed: {ex.Message}");
                                    Console.ResetColor();
                                }
                                finally
                                {
                                    if (File.Exists(tempMergedPath))
                                    {
                                        try { File.Delete(tempMergedPath); } catch { }
                                    }
                                }
                            }
                            break;

                        case "10":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║      FULL MERGE USING INTERMEDIATE APPROACH              ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nThis will:");
                            Console.WriteLine("  1. Disassemble both SOURCE and TARGET into intermediate format");
                            Console.WriteLine("  2. Merge source into target with conflict detection");
                            Console.WriteLine("  3. Rebuild with predictable IDs (like BetterTriggers)");
                            Console.WriteLine("\nThis replaces the current TARGET with the merged result.");
                            Console.Write("\nProceed? (y/n): ");
                            string? confirmMerge = Console.ReadLine();
                            if (confirmMerge?.ToLower() == "y")
                            {
                                try
                                {
                                    DiagnosticLogger.LogOperationStart("FULL MERGE (Option 10)");

                                    // Enable debug mode for converters if global debug is on
                                    IntermediateConverter.SetDebugMode(DEBUG_MODE);
                                    IntermediateMerger.SetDebugMode(DEBUG_MODE);

                                    Console.WriteLine("\n[1/3] Disassembling SOURCE...");
                                    DiagnosticLogger.Log("Step 1: Disassembling SOURCE");
                                    DiagnosticLogger.LogMapTriggersState(sourceTriggers, "SOURCE Before Disassemble");
                                    var sourceIntermediate = IntermediateConverter.Disassemble(sourceTriggers, sourcePath);
                                    DiagnosticLogger.LogIntermediateState(sourceIntermediate, "SOURCE After Disassemble");

                                    Console.WriteLine("[2/3] Disassembling TARGET...");
                                    DiagnosticLogger.Log("Step 2: Disassembling TARGET");
                                    DiagnosticLogger.LogMapTriggersState(targetTriggers, "TARGET Before Disassemble");
                                    var targetIntermediate = IntermediateConverter.Disassemble(targetTriggers, targetPath);
                                    DiagnosticLogger.LogIntermediateState(targetIntermediate, "TARGET After Disassemble");

                                    Console.WriteLine("[3/3] Merging...");
                                    DiagnosticLogger.Log("Step 3: Merging intermediate representations");
                                    var (mergedIntermediate, conflicts) = IntermediateMerger.Merge(sourceIntermediate, targetIntermediate);
                                    DiagnosticLogger.LogIntermediateState(mergedIntermediate, "MERGED Result");
                                    foreach (var conflict in conflicts)
                                    {
                                        DiagnosticLogger.LogConflict(conflict);
                                    }

                                    Console.WriteLine("\n[4/4] Rebuilding War3Net MapTriggers...");
                                    DiagnosticLogger.Log("Step 4: Rebuilding War3Net MapTriggers");
                                    targetTriggers = IntermediateConverter.Rebuild(mergedIntermediate);
                                    DiagnosticLogger.LogMapTriggersState(targetTriggers, "FINAL Rebuilt MapTriggers");

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("\n✓ Full merge complete!");
                                    Console.ResetColor();

                                    if (conflicts.Count > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"\n⚠ Note: {conflicts.Count} conflicts were detected (duplicates skipped)");
                                        Console.ResetColor();
                                    }

                                    modified = true;
                                    DiagnosticLogger.LogOperationEnd("FULL MERGE (Option 10)", true);
                                }
                                catch (Exception ex)
                                {
                                    DiagnosticLogger.Log($"ERROR: {ex.Message}");
                                    DiagnosticLogger.Log($"Stack trace: {ex.StackTrace}");
                                    DiagnosticLogger.LogOperationEnd("FULL MERGE (Option 10)", false);

                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"\n❌ Merge failed: {ex.Message}");
                                    if (DEBUG_MODE)
                                    {
                                        Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                                    }
                                    Console.ResetColor();
                                }
                            }
                            break;

                        case "11":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    SELECTIVE MERGE USING INTERMEDIATE APPROACH           ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nThis will:");
                            Console.WriteLine("  1. Disassemble both SOURCE and TARGET into intermediate format");
                            Console.WriteLine("  2. Let you choose which categories to merge");
                            Console.WriteLine("  3. Merge only selected categories with conflict detection");
                            Console.WriteLine("  4. Rebuild with predictable IDs (like BetterTriggers)");
                            Console.WriteLine("\nThis replaces the current TARGET with the merged result.");
                            Console.Write("\nProceed? (y/n): ");
                            string? confirmSelectiveMerge = Console.ReadLine();
                            if (confirmSelectiveMerge?.ToLower() == "y")
                            {
                                try
                                {
                                    DiagnosticLogger.LogOperationStart("SELECTIVE MERGE (Option 11)");

                                    // Enable debug mode for converters if global debug is on
                                    IntermediateConverter.SetDebugMode(DEBUG_MODE);
                                    IntermediateMerger.SetDebugMode(DEBUG_MODE);

                                    Console.WriteLine("\n[1/4] Disassembling SOURCE...");
                                    DiagnosticLogger.Log("Step 1: Disassembling SOURCE");
                                    DiagnosticLogger.LogMapTriggersState(sourceTriggers, "SOURCE Before Disassemble");
                                    var sourceIntermediate = IntermediateConverter.Disassemble(sourceTriggers, sourcePath);
                                    DiagnosticLogger.LogIntermediateState(sourceIntermediate, "SOURCE After Disassemble");

                                    Console.WriteLine("[2/4] Disassembling TARGET...");
                                    DiagnosticLogger.Log("Step 2: Disassembling TARGET");
                                    DiagnosticLogger.LogMapTriggersState(targetTriggers, "TARGET Before Disassemble");
                                    var targetIntermediate = IntermediateConverter.Disassemble(targetTriggers, targetPath);
                                    DiagnosticLogger.LogIntermediateState(targetIntermediate, "TARGET After Disassemble");

                                    // Show available categories from source
                                    Console.WriteLine("\n[3/4] Available categories in SOURCE:");
                                    var sourceCategories = sourceIntermediate.GetAllCategories().ToList();
                                    for (int i = 0; i < sourceCategories.Count; i++)
                                    {
                                        var cat = sourceCategories[i];
                                        var triggerCount = cat.GetChildren<TriggerItemNode>().Count();
                                        var subCatCount = cat.GetChildren<CategoryNode>().Count();
                                        Console.WriteLine($"  {i + 1}. {cat.Name} ({triggerCount} triggers, {subCatCount} subcategories)");
                                    }

                                    Console.WriteLine("\nEnter category numbers to merge (comma-separated, or 'all' for all categories):");
                                    Console.Write("Categories to merge: ");
                                    string? categoryInput = Console.ReadLine();

                                    if (string.IsNullOrWhiteSpace(categoryInput))
                                    {
                                        Console.WriteLine("No categories selected. Aborting.");
                                        break;
                                    }

                                    List<CategoryNode> selectedCategories;
                                    if (categoryInput.Trim().ToLower() == "all")
                                    {
                                        selectedCategories = sourceCategories;
                                    }
                                    else
                                    {
                                        selectedCategories = new List<CategoryNode>();
                                        var indices = categoryInput.Split(',').Select(s => s.Trim());
                                        foreach (var indexStr in indices)
                                        {
                                            if (int.TryParse(indexStr, out int index) && index >= 1 && index <= sourceCategories.Count)
                                            {
                                                selectedCategories.Add(sourceCategories[index - 1]);
                                            }
                                            else
                                            {
                                                Console.WriteLine($"⚠ Warning: Invalid index '{indexStr}' ignored");
                                            }
                                        }
                                    }

                                    if (selectedCategories.Count == 0)
                                    {
                                        Console.WriteLine("No valid categories selected. Aborting.");
                                        DiagnosticLogger.Log("No valid categories selected - aborting");
                                        DiagnosticLogger.LogOperationEnd("SELECTIVE MERGE (Option 11)", false);
                                        break;
                                    }

                                    DiagnosticLogger.Log($"Step 3: User selected {selectedCategories.Count} categories:");
                                    DiagnosticLogger.Indent();
                                    foreach (var cat in selectedCategories)
                                    {
                                        DiagnosticLogger.Log($"- '{cat.Name}' (OriginalId={cat.OriginalId})");
                                    }
                                    DiagnosticLogger.Unindent();

                                    Console.WriteLine($"\n[4/4] Merging {selectedCategories.Count} selected categor{(selectedCategories.Count == 1 ? "y" : "ies")}...");
                                    DiagnosticLogger.Log("Step 4: Performing selective merge");

                                    // Merge selected categories
                                    var (mergedIntermediate, conflicts) = IntermediateMerger.MergeSelective(
                                        sourceIntermediate, targetIntermediate, selectedCategories);

                                    DiagnosticLogger.LogIntermediateState(mergedIntermediate, "MERGED Result");
                                    foreach (var conflict in conflicts)
                                    {
                                        DiagnosticLogger.LogConflict(conflict);
                                    }

                                    Console.WriteLine("\nRebuilding War3Net MapTriggers...");
                                    DiagnosticLogger.Log("Step 5: Rebuilding War3Net MapTriggers");
                                    targetTriggers = IntermediateConverter.Rebuild(mergedIntermediate);
                                    DiagnosticLogger.LogMapTriggersState(targetTriggers, "FINAL Rebuilt MapTriggers");

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("\n✓ Selective merge complete!");
                                    Console.ResetColor();

                                    if (conflicts.Count > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"\n⚠ Note: {conflicts.Count} conflicts were detected (duplicates skipped)");
                                        Console.ResetColor();
                                    }

                                    modified = true;
                                    DiagnosticLogger.LogOperationEnd("SELECTIVE MERGE (Option 11)", true);
                                }
                                catch (Exception ex)
                                {
                                    DiagnosticLogger.Log($"ERROR: {ex.Message}");
                                    DiagnosticLogger.Log($"Stack trace: {ex.StackTrace}");
                                    DiagnosticLogger.LogOperationEnd("SELECTIVE MERGE (Option 11)", false);

                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"\n❌ Merge failed: {ex.Message}");
                                    if (DEBUG_MODE)
                                    {
                                        Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                                    }
                                    Console.ResetColor();
                                }
                            }
                            break;

                        case "12":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    DEEP VALIDATION OF SPECIFIC TRIGGER                  ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.Write("\nValidate from (s)ource or (t)arget? ");
                            string? mapChoice = Console.ReadLine();
                            var mapToValidate = mapChoice?.ToLower() == "s" ? sourceTriggers : targetTriggers;
                            string mapName = mapChoice?.ToLower() == "s" ? "SOURCE" : "TARGET";

                            Console.Write("\nEnter category name: ");
                            string? valCatName = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(valCatName))
                            {
                                ListTriggersInCategory(mapToValidate, valCatName);
                                Console.Write("\nEnter trigger name to validate: ");
                                string? valTrigName = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(valTrigName))
                                {
                                    var trigger = mapToValidate.TriggerItems
                                        .OfType<TriggerDefinition>()
                                        .FirstOrDefault(t => t.Name.Equals(valTrigName, StringComparison.OrdinalIgnoreCase));

                                    if (trigger != null)
                                    {
                                        TriggerValidator.ValidateTrigger(trigger, mapToValidate, verbose: true);
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"\n✗ Trigger '{valTrigName}' not found in {mapName}");
                                        Console.ResetColor();
                                    }
                                }
                            }
                            break;

                        case "13":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    VALIDATE ALL TRIGGERS IN TARGET                      ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.Write("\nThis will validate ALL triggers. Continue? (y/n): ");
                            string? confirmValidateAll = Console.ReadLine();
                            if (confirmValidateAll?.ToLower() == "y")
                            {
                                var allTriggers = targetTriggers.TriggerItems.OfType<TriggerDefinition>().ToList();
                                Console.WriteLine($"\nValidating {allTriggers.Count} triggers...\n");

                                int validCount = 0;
                                int invalidCount = 0;
                                var invalidTriggers = new List<string>();

                                foreach (var trigger in allTriggers)
                                {
                                    var issues = new List<string>();
                                    var referencedVars = new HashSet<string>();

                                    // Quick validation: check variable references
                                    CollectVariableReferences(trigger, referencedVars, targetTriggers);
                                    var mapVarNames = new HashSet<string>(targetTriggers.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

                                    foreach (var varName in referencedVars)
                                    {
                                        if (!mapVarNames.Contains(varName))
                                        {
                                            issues.Add($"Missing variable: '{varName}'");
                                        }
                                    }

                                    // Check ParentId
                                    if (trigger.ParentId < 0 && trigger.ParentId != -1)
                                    {
                                        issues.Add($"Invalid ParentId: {trigger.ParentId}");
                                    }

                                    if (issues.Count > 0)
                                    {
                                        invalidCount++;
                                        invalidTriggers.Add(trigger.Name);
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"✗ {trigger.Name}: {issues.Count} issue(s)");
                                        foreach (var issue in issues)
                                        {
                                            Console.WriteLine($"    - {issue}");
                                        }
                                        Console.ResetColor();
                                    }
                                    else
                                    {
                                        validCount++;
                                    }
                                }

                                Console.WriteLine($"\n═══ VALIDATION SUMMARY ═══");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"✓ Valid: {validCount}");
                                Console.ResetColor();

                                if (invalidCount > 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"✗ Invalid: {invalidCount}");
                                    Console.ResetColor();

                                    Console.WriteLine($"\nInvalid triggers:");
                                    foreach (var name in invalidTriggers)
                                    {
                                        Console.WriteLine($"  • {name}");
                                    }

                                    Console.Write("\nValidate specific trigger in detail? (y/n): ");
                                    string? detailChoice = Console.ReadLine();
                                    if (detailChoice?.ToLower() == "y")
                                    {
                                        Console.Write("Enter trigger name: ");
                                        string? detailTrigName = Console.ReadLine();
                                        if (!string.IsNullOrWhiteSpace(detailTrigName))
                                        {
                                            var trigger = allTriggers.FirstOrDefault(t =>
                                                t.Name.Equals(detailTrigName, StringComparison.OrdinalIgnoreCase));
                                            if (trigger != null)
                                            {
                                                TriggerValidator.ValidateTrigger(trigger, targetTriggers, verbose: true);
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case "14":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    ISOLATION TEST - Copy Trigger to Empty Map           ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nThis test will:");
                            Console.WriteLine("  1. Create a minimal empty map trigger structure");
                            Console.WriteLine("  2. Copy ONE trigger from source");
                            Console.WriteLine("  3. Save to test file");
                            Console.WriteLine("  4. Validate the result");
                            Console.WriteLine("\nThis helps isolate if the problem is with:");
                            Console.WriteLine("  - The trigger itself (would fail even in empty map)");
                            Console.WriteLine("  - Conflicts with existing content in target map");

                            Console.Write("\nEnter category name where trigger is: ");
                            string? testCatName = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(testCatName))
                            {
                                ListTriggersInCategory(sourceTriggers, testCatName);
                                Console.Write("\nEnter trigger name to test: ");
                                string? testTrigName = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(testTrigName))
                                {
                                    try
                                    {
                                        // Create minimal map
                                        var emptyMap = CreateMinimalMapTriggers(sourceTriggers);

                                        // Copy the trigger
                                        Console.WriteLine("\nCopying trigger to empty map...");
                                        CopySpecificTriggers(sourceTriggers, emptyMap, testCatName, new[] { testTrigName }, "Test Category");

                                        // Save to test file
                                        string testOutputPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? ".", "ISOLATION_TEST.wtg");
                                        Console.WriteLine($"\nSaving to: {testOutputPath}");
                                        WriteWTGFile(testOutputPath, emptyMap);

                                        // Validate
                                        Console.WriteLine("\nReading back and validating...");
                                        var readBack = ReadWTGFile(testOutputPath);
                                        var copiedTrigger = readBack.TriggerItems.OfType<TriggerDefinition>()
                                            .FirstOrDefault(t => t.Name.Equals(testTrigName, StringComparison.OrdinalIgnoreCase));

                                        if (copiedTrigger != null)
                                        {
                                            TriggerValidator.ValidateTrigger(copiedTrigger, readBack, verbose: true);

                                            Console.ForegroundColor = ConsoleColor.Cyan;
                                            Console.WriteLine($"\n✓ Isolation test complete!");
                                            Console.WriteLine($"\nTest file saved to: {testOutputPath}");
                                            Console.WriteLine("Try opening this file in World Editor to see if trigger loads.");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("\n✗ Trigger not found after copy!");
                                            Console.ResetColor();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"\n✗ Isolation test failed: {ex.Message}");
                                        if (DEBUG_MODE)
                                        {
                                            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                                        }
                                        Console.ResetColor();
                                    }
                                }
                            }
                            break;

                        case "15":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    EXPORT TRIGGER TO DETAILED TEXT FILE                 ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.Write("\nExport from (s)ource or (t)arget? ");
                            string? exportMapChoice = Console.ReadLine();
                            var exportMap = exportMapChoice?.ToLower() == "s" ? sourceTriggers : targetTriggers;
                            string exportMapName = exportMapChoice?.ToLower() == "s" ? "SOURCE" : "TARGET";

                            Console.Write("\nEnter category name: ");
                            string? exportCatName = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(exportCatName))
                            {
                                ListTriggersInCategory(exportMap, exportCatName);
                                Console.Write("\nEnter trigger name to export: ");
                                string? exportTrigName = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(exportTrigName))
                                {
                                    var exportTrigger = exportMap.TriggerItems
                                        .OfType<TriggerDefinition>()
                                        .FirstOrDefault(t => t.Name.Equals(exportTrigName, StringComparison.OrdinalIgnoreCase));

                                    if (exportTrigger != null)
                                    {
                                        Console.Write("\nInclude hex dumps of strings? (y/n): ");
                                        bool showHex = Console.ReadLine()?.ToLower() == "y";

                                        string exportText = TriggerExporter.ExportToDetailedText(exportTrigger, exportMap, showHex);

                                        string exportFileName = $"TRIGGER_EXPORT_{exportTrigger.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                                        string exportPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? ".", exportFileName);

                                        File.WriteAllText(exportPath, exportText);

                                        Console.WriteLine($"\n{exportText}");

                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"\n✓ Export saved to: {exportPath}");
                                        Console.ResetColor();

                                        // Also show pseudo-code
                                        Console.WriteLine("\n=== PSEUDO-CODE REPRESENTATION ===");
                                        Console.WriteLine(TriggerExporter.ExportToPseudoCode(exportTrigger));
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"\n✗ Trigger '{exportTrigName}' not found in {exportMapName}");
                                        Console.ResetColor();
                                    }
                                }
                            }
                            break;

                        case "16":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    CHECK SOURCE TRIGGER FOR CORRUPTION                  ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nThis checks if the SOURCE trigger is already corrupted");
                            Console.WriteLine("(e.g., by BetterTriggers or other tools)");

                            Console.Write("\nEnter category name: ");
                            string? corruptCatName = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(corruptCatName))
                            {
                                ListTriggersInCategory(sourceTriggers, corruptCatName);
                                Console.Write("\nEnter trigger name to check: ");
                                string? corruptTrigName = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(corruptTrigName))
                                {
                                    var corruptTrigger = sourceTriggers.TriggerItems
                                        .OfType<TriggerDefinition>()
                                        .FirstOrDefault(t => t.Name.Equals(corruptTrigName, StringComparison.OrdinalIgnoreCase));

                                    if (corruptTrigger != null)
                                    {
                                        Console.WriteLine("\nRunning corruption detection...\n");

                                        var corruptionIssues = TriggerExporter.DetectCorruption(corruptTrigger, sourceTriggers);

                                        if (corruptionIssues.Count == 0)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("✓ No corruption detected in source trigger!");
                                            Console.WriteLine("✓ Trigger structure appears valid");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine($"✗ FOUND {corruptionIssues.Count} ISSUE(S):\n");
                                            Console.ResetColor();

                                            foreach (var issue in corruptionIssues)
                                            {
                                                if (issue.StartsWith("CORRUPT"))
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.WriteLine($"  ✗ {issue}");
                                                    Console.ResetColor();
                                                }
                                                else if (issue.StartsWith("SUSPICIOUS"))
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                                    Console.WriteLine($"  ⚠ {issue}");
                                                    Console.ResetColor();
                                                }
                                                else if (issue.StartsWith("MISSING"))
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                                    Console.WriteLine($"  ⚠ {issue}");
                                                    Console.ResetColor();
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"  • {issue}");
                                                }
                                            }

                                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                                            Console.WriteLine("║  RECOMMENDATION                                          ║");
                                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

                                            var hasCorrupt = corruptionIssues.Any(i => i.StartsWith("CORRUPT"));
                                            var hasMissing = corruptionIssues.Any(i => i.StartsWith("MISSING"));

                                            if (hasCorrupt)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine("\n✗ SOURCE TRIGGER IS CORRUPTED!");
                                                Console.WriteLine("\nThis trigger cannot be safely copied in its current state.");
                                                Console.WriteLine("\nOptions:");
                                                Console.WriteLine("  1. Fix the source map in World Editor");
                                                Console.WriteLine("  2. Recreate the trigger manually");
                                                Console.WriteLine("  3. Try restoring from backup");
                                                Console.ResetColor();
                                            }
                                            else if (hasMissing)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Yellow;
                                                Console.WriteLine("\n⚠ MISSING VARIABLES IN SOURCE");
                                                Console.WriteLine("\nThe trigger references variables that don't exist in source map.");
                                                Console.WriteLine("\nOptions:");
                                                Console.WriteLine("  1. Create the missing variables in source map");
                                                Console.WriteLine("  2. Fix the trigger to not reference them");
                                                Console.WriteLine("  3. This might be intentional (e.g., variables in .wct file)");
                                                Console.ResetColor();
                                            }
                                        }

                                        Console.Write("\n\nExport detailed report? (y/n): ");
                                        if (Console.ReadLine()?.ToLower() == "y")
                                        {
                                            string reportText = TriggerExporter.ExportToDetailedText(corruptTrigger, sourceTriggers, showHex: true);
                                            string reportFileName = $"CORRUPTION_REPORT_{corruptTrigger.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                                            string reportPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? ".", reportFileName);

                                            var reportBuilder = new StringBuilder();
                                            reportBuilder.AppendLine("╔══════════════════════════════════════════════════════════╗");
                                            reportBuilder.AppendLine("║  CORRUPTION DETECTION REPORT                            ║");
                                            reportBuilder.AppendLine("╚══════════════════════════════════════════════════════════╝");
                                            reportBuilder.AppendLine($"Generated: {DateTime.Now}");
                                            reportBuilder.AppendLine($"Trigger: {corruptTrigger.Name}");
                                            reportBuilder.AppendLine($"Source: {sourcePath}");
                                            reportBuilder.AppendLine();
                                            reportBuilder.AppendLine("=== ISSUES DETECTED ===");
                                            if (corruptionIssues.Count == 0)
                                            {
                                                reportBuilder.AppendLine("No issues detected.");
                                            }
                                            else
                                            {
                                                foreach (var issue in corruptionIssues)
                                                {
                                                    reportBuilder.AppendLine($"• {issue}");
                                                }
                                            }
                                            reportBuilder.AppendLine();
                                            reportBuilder.AppendLine("=== FULL TRIGGER EXPORT ===");
                                            reportBuilder.AppendLine(reportText);

                                            File.WriteAllText(reportPath, reportBuilder.ToString());

                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"✓ Report saved to: {reportPath}");
                                            Console.ResetColor();
                                        }
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"\n✗ Trigger '{corruptTrigName}' not found in SOURCE");
                                        Console.ResetColor();
                                    }
                                }
                            }
                            break;

                        case "17":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    COMPARE TWO TRIGGERS SIDE-BY-SIDE                    ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nCompare source vs target version of same trigger");

                            Console.Write("\nEnter category name in SOURCE: ");
                            string? cmp1CatName = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(cmp1CatName))
                            {
                                ListTriggersInCategory(sourceTriggers, cmp1CatName);
                                Console.Write("\nEnter trigger name in SOURCE: ");
                                string? cmp1TrigName = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(cmp1TrigName))
                                {
                                    Console.Write("\nEnter category name in TARGET (or press Enter for same): ");
                                    string? cmp2CatName = Console.ReadLine();
                                    if (string.IsNullOrWhiteSpace(cmp2CatName))
                                    {
                                        cmp2CatName = cmp1CatName;
                                    }

                                    ListTriggersInCategory(targetTriggers, cmp2CatName);
                                    Console.Write("\nEnter trigger name in TARGET (or press Enter for same): ");
                                    string? cmp2TrigName = Console.ReadLine();
                                    if (string.IsNullOrWhiteSpace(cmp2TrigName))
                                    {
                                        cmp2TrigName = cmp1TrigName;
                                    }

                                    var trigger1 = sourceTriggers.TriggerItems
                                        .OfType<TriggerDefinition>()
                                        .FirstOrDefault(t => t.Name.Equals(cmp1TrigName, StringComparison.OrdinalIgnoreCase));

                                    var trigger2 = targetTriggers.TriggerItems
                                        .OfType<TriggerDefinition>()
                                        .FirstOrDefault(t => t.Name.Equals(cmp2TrigName, StringComparison.OrdinalIgnoreCase));

                                    if (trigger1 != null && trigger2 != null)
                                    {
                                        TriggerValidator.CompareTriggers(trigger1, trigger2, "SOURCE", "TARGET");

                                        Console.WriteLine("\n=== CORRUPTION CHECK ===");
                                        Console.WriteLine("\nSOURCE:");
                                        var issues1 = TriggerExporter.DetectCorruption(trigger1, sourceTriggers);
                                        if (issues1.Count == 0)
                                        {
                                            Console.WriteLine("  ✓ No issues");
                                        }
                                        else
                                        {
                                            foreach (var issue in issues1) Console.WriteLine($"  • {issue}");
                                        }

                                        Console.WriteLine("\nTARGET:");
                                        var issues2 = TriggerExporter.DetectCorruption(trigger2, targetTriggers);
                                        if (issues2.Count == 0)
                                        {
                                            Console.WriteLine("  ✓ No issues");
                                        }
                                        else
                                        {
                                            foreach (var issue in issues2) Console.WriteLine($"  • {issue}");
                                        }
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        if (trigger1 == null) Console.WriteLine($"\n✗ '{cmp1TrigName}' not found in SOURCE");
                                        if (trigger2 == null) Console.WriteLine($"✗ '{cmp2TrigName}' not found in TARGET");
                                        Console.ResetColor();
                                    }
                                }
                            }
                            break;

                        case "18":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    EXTRACT TRIGGER + VARIABLES TO STANDALONE .WTG       ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nThis creates a minimal .wtg with just the trigger and its variables");
                            Console.WriteLine("Useful for sharing triggers or testing in isolation");

                            Console.Write("\nEnter category name: ");
                            string? extractCatName = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(extractCatName))
                            {
                                ListTriggersInCategory(sourceTriggers, extractCatName);
                                Console.Write("\nEnter trigger name to extract: ");
                                string? extractTrigName = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(extractTrigName))
                                {
                                    var extractTrigger = sourceTriggers.TriggerItems
                                        .OfType<TriggerDefinition>()
                                        .FirstOrDefault(t => t.Name.Equals(extractTrigName, StringComparison.OrdinalIgnoreCase));

                                    if (extractTrigger != null)
                                    {
                                        try
                                        {
                                            // Create minimal map
                                            var extractedMap = CreateMinimalMapTriggers(sourceTriggers);

                                            // Copy trigger and variables
                                            Console.WriteLine("\nExtracting trigger and dependencies...");
                                            CopySpecificTriggers(sourceTriggers, extractedMap, extractCatName, new[] { extractTrigName }, extractCatName);

                                            // Save
                                            string extractFileName = $"EXTRACTED_{extractTrigger.Name.Replace(" ", "_")}.wtg";
                                            string extractPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? ".", extractFileName);

                                            WriteWTGFile(extractPath, extractedMap);

                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"\n✓ Extracted trigger saved to: {extractPath}");
                                            Console.WriteLine($"\nContents:");
                                            Console.WriteLine($"  • 1 category: '{extractCatName}'");
                                            Console.WriteLine($"  • 1 trigger: '{extractTrigger.Name}'");
                                            Console.WriteLine($"  • {extractedMap.Variables.Count} variable(s)");
                                            Console.WriteLine($"\nYou can:");
                                            Console.WriteLine($"  1. Share this file with others");
                                            Console.WriteLine($"  2. Open in World Editor to test");
                                            Console.WriteLine($"  3. Use as backup of this trigger");
                                            Console.ResetColor();
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine($"\n✗ Extraction failed: {ex.Message}");
                                            if (DEBUG_MODE)
                                            {
                                                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                                            }
                                            Console.ResetColor();
                                        }
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"\n✗ Trigger '{extractTrigName}' not found");
                                        Console.ResetColor();
                                    }
                                }
                            }
                            break;

                        case "19":
                            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║    COMPREHENSIVE WTG HEALTH CHECK                       ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                            Console.WriteLine("\nCheck (s)ource, (t)arget, or (m)erged file? ");
                            Console.Write("Choice: ");
                            string? healthCheckChoice = Console.ReadLine();

                            MapTriggers? mapToCheck = null;
                            string checkPath = "";

                            if (healthCheckChoice?.ToLower() == "s")
                            {
                                mapToCheck = sourceTriggers;
                                checkPath = sourcePath;
                            }
                            else if (healthCheckChoice?.ToLower() == "t")
                            {
                                mapToCheck = targetTriggers;
                                checkPath = targetPath;
                            }
                            else if (healthCheckChoice?.ToLower() == "m")
                            {
                                Console.Write("\nEnter path to merged .wtg file: ");
                                string? mergedPath = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(mergedPath))
                                {
                                    try
                                    {
                                        mapToCheck = ReadMapTriggersAuto(mergedPath);
                                        checkPath = mergedPath;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"\n✗ Failed to load file: {ex.Message}");
                                        Console.ResetColor();
                                    }
                                }
                            }

                            if (mapToCheck != null)
                            {
                                var healthResult = WTGHealthCheck.PerformHealthCheck(mapToCheck, checkPath);

                                Console.Write("\n\nSave detailed report to file? (y/n): ");
                                if (Console.ReadLine()?.ToLower() == "y")
                                {
                                    string reportFileName = $"HEALTH_CHECK_{Path.GetFileNameWithoutExtension(checkPath)}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                                    string reportPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? ".", reportFileName);

                                    var reportBuilder = new StringBuilder();
                                    reportBuilder.AppendLine("╔══════════════════════════════════════════════════════════╗");
                                    reportBuilder.AppendLine("║       COMPREHENSIVE WTG HEALTH CHECK REPORT             ║");
                                    reportBuilder.AppendLine("╚══════════════════════════════════════════════════════════╝");
                                    reportBuilder.AppendLine($"Generated: {DateTime.Now}");
                                    reportBuilder.AppendLine($"File: {checkPath}");
                                    reportBuilder.AppendLine($"Format: {mapToCheck.FormatVersion}, SubVersion: {mapToCheck.SubVersion?.ToString() ?? "null (1.27)"}");
                                    reportBuilder.AppendLine();

                                    reportBuilder.AppendLine("=== STATISTICS ===");
                                    foreach (var stat in healthResult.Statistics)
                                    {
                                        reportBuilder.AppendLine($"{stat.Key}: {stat.Value}");
                                    }
                                    reportBuilder.AppendLine();

                                    if (healthResult.Errors.Count > 0)
                                    {
                                        reportBuilder.AppendLine($"=== ERRORS ({healthResult.Errors.Count}) ===");
                                        foreach (var error in healthResult.Errors)
                                        {
                                            reportBuilder.AppendLine($"• {error}");
                                        }
                                        reportBuilder.AppendLine();
                                    }

                                    if (healthResult.Warnings.Count > 0)
                                    {
                                        reportBuilder.AppendLine($"=== WARNINGS ({healthResult.Warnings.Count}) ===");
                                        foreach (var warning in healthResult.Warnings)
                                        {
                                            reportBuilder.AppendLine($"• {warning}");
                                        }
                                        reportBuilder.AppendLine();
                                    }

                                    if (healthResult.Info.Count > 0)
                                    {
                                        reportBuilder.AppendLine($"=== INFO ({healthResult.Info.Count}) ===");
                                        foreach (var info in healthResult.Info)
                                        {
                                            reportBuilder.AppendLine($"• {info}");
                                        }
                                        reportBuilder.AppendLine();
                                    }

                                    reportBuilder.AppendLine("=== HEALTH STATUS ===");
                                    if (healthResult.IsHealthy)
                                    {
                                        reportBuilder.AppendLine("✓ FILE IS HEALTHY");
                                    }
                                    else
                                    {
                                        reportBuilder.AppendLine("✗ FILE HAS ISSUES");
                                        reportBuilder.AppendLine($"  Errors: {healthResult.Errors.Count}");
                                        reportBuilder.AppendLine($"  Warnings: {healthResult.Warnings.Count}");
                                    }

                                    File.WriteAllText(reportPath, reportBuilder.ToString());

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"✓ Report saved to: {reportPath}");
                                    Console.ResetColor();
                                }
                            }
                            break;

                        case "d":
                        case "D":
                            DEBUG_MODE = !DEBUG_MODE;
                            Console.ForegroundColor = DEBUG_MODE ? ConsoleColor.Yellow : ConsoleColor.Green;
                            Console.WriteLine($"\n✓ Debug mode is now {(DEBUG_MODE ? "ON" : "OFF")}");
                            Console.ResetColor();
                            break;

                        case "l":
                        case "L":
                            if (DiagnosticLogger.IsEnabled)
                            {
                                DiagnosticLogger.StopLogging();
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("\n✓ Deep diagnostic logging DISABLED");
                                Console.ResetColor();
                            }
                            else
                            {
                                DiagnosticLogger.StartLogging();
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("\n✓ Deep diagnostic logging ENABLED");
                                Console.WriteLine("  All operations will be logged to file with complete details");
                                Console.WriteLine("  This includes: file operations, data transformations, IDs, ParentIds, hierarchy");
                                Console.ResetColor();
                            }
                            break;

                        case "s":
                        case "S":
                            if (modified)
                            {
                                Console.WriteLine($"\nPreparing to save merged WTG to: {outputPath}");

                                // CRITICAL SAFETY CHECK: Verify variables exist before saving
                                Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                                Console.WriteLine("║              PRE-SAVE VERIFICATION                       ║");
                                Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                                Console.WriteLine($"Variables in memory: {targetTriggers.Variables.Count}");
                                Console.WriteLine($"Trigger items: {targetTriggers.TriggerItems.Count}");
                                Console.WriteLine($"Categories: {targetTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
                                Console.WriteLine($"Triggers: {targetTriggers.TriggerItems.OfType<TriggerDefinition>().Count()}");

                                if (targetTriggers.Variables.Count == 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("\n❌ CRITICAL ERROR: All variables have been deleted!");
                                    Console.WriteLine("❌ Saving now would corrupt the map!");
                                    Console.WriteLine("❌ Aborting save to prevent data loss.");
                                    Console.ResetColor();
                                    Console.WriteLine("\nPress Enter to return to menu...");
                                    Console.ReadLine();
                                    break;
                                }

                                if (DEBUG_MODE)
                                {
                                    Console.WriteLine("\n[DEBUG] Sample variables before save:");
                                    foreach (var v in targetTriggers.Variables.Take(10))
                                    {
                                        Console.WriteLine($"[DEBUG]   ID={v.Id}, Name={v.Name}, Type={v.Type}");
                                    }
                                }

                                // NOTE: SubVersion handling
                                // If SubVersion=null (WC3 1.27 format), ParentIds won't be saved
                                // This is expected behavior for 1.27 compatibility
                                // Variable IDs also won't be saved (they stay at 0)
                                if (targetTriggers.SubVersion == null)
                                {
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine("\nℹ Map is in WC3 1.27 format (SubVersion=null)");
                                    Console.WriteLine("  • Variable IDs will not be saved (stays at 0)");
                                    Console.WriteLine("  • ParentIds will not be saved");
                                    Console.WriteLine("  • This maintains 1.27 compatibility");
                                    Console.ResetColor();
                                }

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

                                // AUTOMATIC NESTING FIX for WC3 1.27 format
                                if (targetTriggers.SubVersion == null)
                                {
                                    bool hasNestingIssue = CheckForNestingIssue(targetTriggers);
                                    if (hasNestingIssue)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine("\n⚠ NESTING ISSUE DETECTED!");
                                        Console.WriteLine("Categories appear after triggers in file order.");
                                        Console.WriteLine("This causes incorrect visual nesting in World Editor.");
                                        Console.WriteLine("\n✓ Automatically fixing file order...");
                                        Console.ResetColor();

                                        FixFileOrder(targetTriggers);

                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("✓ File order fixed: All categories now appear before triggers");
                                        Console.ResetColor();
                                    }
                                }

                                Console.WriteLine($"\nWriting file...");

                                if (DEBUG_MODE)
                                {
                                    Console.WriteLine($"[DEBUG] About to write {targetTriggers.Variables.Count} variables");
                                    Console.WriteLine($"[DEBUG] About to write {targetTriggers.TriggerItems.Count} trigger items");
                                }

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

                                // VERIFICATION: Read back the saved file to confirm everything was written correctly
                                Console.WriteLine("\n=== VERIFICATION: Reading saved file ===");
                                try
                                {
                                    MapTriggers verifyTriggers = ReadMapTriggersAuto(outputPath);

                                    // CRITICAL: Check if variables were preserved
                                    int originalVarCount = targetTriggers.Variables.Count;
                                    int savedVarCount = verifyTriggers.Variables.Count;

                                    Console.WriteLine($"Variables written: {originalVarCount}");
                                    Console.WriteLine($"Variables in saved file: {savedVarCount}");

                                    if (savedVarCount == 0 && originalVarCount > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("\n❌❌❌ CRITICAL ERROR: ALL VARIABLES WERE LOST! ❌❌❌");
                                        Console.WriteLine($"❌ We tried to write {originalVarCount} variables but the file has 0!");
                                        Console.WriteLine("❌ This is a BUG in War3Net library's WriteTo method!");
                                        Console.WriteLine("❌ DO NOT use this file - it will corrupt your map!");
                                        Console.ResetColor();
                                    }
                                    else if (savedVarCount < originalVarCount)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"\n❌ ERROR: Variable loss detected!");
                                        Console.WriteLine($"❌ {originalVarCount - savedVarCount} variables were lost during save!");
                                        Console.ResetColor();
                                    }
                                    else if (savedVarCount > originalVarCount)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"\n⚠ WARNING: Extra variables appeared!");
                                        Console.WriteLine($"⚠ {savedVarCount - originalVarCount} more variables than expected!");
                                        Console.ResetColor();
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("✓ All variables were saved correctly!");
                                        Console.ResetColor();
                                    }

                                    // Check file order (critical for 1.27 format)
                                    Console.WriteLine($"\nFile Order Verification:");

                                    if (verifyTriggers.SubVersion == null)
                                    {
                                        // WC3 1.27 format - check file order
                                        bool hasOrderIssue = CheckForNestingIssue(verifyTriggers);

                                        if (hasOrderIssue)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("❌ ERROR: File order is incorrect!");
                                            Console.WriteLine("  Categories appear after triggers in saved file.");
                                            Console.WriteLine("  This will cause visual nesting issues in World Editor.");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("✓ File order is correct: categories before triggers");
                                            Console.ResetColor();
                                        }

                                        // Note about ParentIds
                                        var verifyCats = verifyTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                                        var allZero = verifyCats.All(c => c.ParentId == 0 || c.ParentId == -1);
                                        if (allZero)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Cyan;
                                            Console.WriteLine("\nℹ Note: All category ParentIds are 0 or -1");
                                            Console.WriteLine("  This is NORMAL for WC3 1.27 format (ParentIds not saved)");
                                            Console.WriteLine("  World Editor uses file order for visual nesting, not ParentIds");
                                            Console.ResetColor();
                                        }
                                    }
                                    else
                                    {
                                        // WC3 1.31+ format - check ParentIds
                                        var verifyCats = verifyTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                                        Console.WriteLine($"  Categories: {verifyCats.Count} total");
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("✓ WC3 1.31+ format - ParentIds preserved");
                                        Console.ResetColor();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"⚠ Could not verify saved file: {ex.Message}");
                                    Console.ResetColor();
                                }

                                Console.WriteLine("\n✓ Merge complete!");

                                // Offer to debug the merged file
                                Console.WriteLine("\n=== POST-MERGE DEBUG ===");
                                Console.Write("Show comprehensive debug info for MERGED file? (y/n): ");
                                string? debugMerged = Console.ReadLine();
                                if (debugMerged?.ToLower() == "y")
                                {
                                    try
                                    {
                                        Console.WriteLine("\nReading merged file for analysis...");
                                        MapTriggers mergedTriggers = ReadMapTriggersAuto(outputPath);
                                        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                                        Console.WriteLine("║           MERGED FILE DEBUG INFORMATION                  ║");
                                        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

                                        // Show merged file variables
                                        Console.WriteLine("\n=== MERGED FILE VARIABLES ===");
                                        Console.WriteLine($"Total: {mergedTriggers.Variables.Count}");
                                        if (mergedTriggers.Variables.Count > 0)
                                        {
                                            Console.WriteLine("\nID | Name                      | Type           | Array | Init");
                                            Console.WriteLine("---|---------------------------|----------------|-------|-----");
                                            foreach (var v in mergedTriggers.Variables.OrderBy(v => v.Id))
                                            {
                                                Console.WriteLine($"{v.Id,2} | {v.Name,-25} | {v.Type,-14} | {(v.IsArray ? "Yes" : "No"),-5} | {(v.IsInitialized ? "Yes" : "No")}");
                                            }
                                        }

                                        // Check for duplicate variable IDs
                                        var varIdGroups = mergedTriggers.Variables.GroupBy(v => v.Id).Where(g => g.Count() > 1).ToList();
                                        if (varIdGroups.Count > 0)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine($"\n❌ ERROR: {varIdGroups.Count} duplicate variable ID(s) found:");
                                            foreach (var group in varIdGroups.Take(5))
                                            {
                                                Console.WriteLine($"  ID {group.Key}: {string.Join(", ", group.Select(v => v.Name))}");
                                            }
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("\n✓ No duplicate variable IDs found");
                                            Console.ResetColor();
                                        }

                                        Console.WriteLine("\nPress Enter to continue...");
                                        Console.ReadLine();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"❌ Could not read merged file: {ex.Message}");
                                        Console.ResetColor();
                                    }
                                }

                                Console.WriteLine("\n=== Final Target Categories ===");
                                ListCategoriesDetailed(targetTriggers);
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

            try
            {
                var triggers = (MapTriggers)constructorInfo.Invoke(new object[] { reader, TriggerData.Default });
                return triggers;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // Show the actual inner exception that caused the problem
                throw new InvalidOperationException(
                    $"Failed to parse war3map.wtg file. " +
                    $"Inner error: {ex.InnerException?.Message ?? ex.Message}\n" +
                    $"This might be due to:\n" +
                    $"  - Corrupted .wtg file\n" +
                    $"  - Unsupported WTG format version\n" +
                    $"  - File from very old or very new WC3 version",
                    ex.InnerException ?? ex);
            }
        }

        /// <summary>
        /// Writes MapTriggers object to a WTG file
        /// Uses War3Writer custom implementation (BUGFIX: replaced War3Net reflection)
        /// </summary>
        static void WriteWTGFile(string filePath, MapTriggers triggers)
        {
            DiagnosticLogger.LogFileOperation("Writing WTG", filePath);
            DiagnosticLogger.LogMapTriggersState(triggers, "Before Writing");

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] WriteWTGFile: Writing to {filePath}");
                Console.WriteLine($"[DEBUG]   Variables to write: {triggers.Variables.Count}");
                Console.WriteLine($"[DEBUG]   Trigger items to write: {triggers.TriggerItems.Count}");
            }

            // CRITICAL: Renumber all categories sequentially before writing
            // This ensures category IDs match what War3Net will assign when reading back
            DiagnosticLogger.Log("Renumbering categories sequentially before write");
            RenumberCategoriesSequentially(triggers);
            DiagnosticLogger.LogMapTriggersState(triggers, "After Renumbering");

            // INTEGRATION: Use War3Writer instead of War3Net's internal WriteTo
            War3Writer.SetDebugMode(DEBUG_MODE);
            DiagnosticLogger.Log("Writing to file using War3Writer");
            War3Writer.WriteMapTriggers(filePath, triggers);

            if (DEBUG_MODE)
            {
                var fileInfo = new FileInfo(filePath);
                Console.WriteLine($"[DEBUG] WriteWTGFile: Completed. File size: {fileInfo.Length} bytes");
            }

            DiagnosticLogger.Log($"Write completed. File size: {new FileInfo(filePath).Length} bytes");
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
            DiagnosticLogger.LogOperationStart($"COPY SPECIFIC TRIGGERS (Option 5)");
            DiagnosticLogger.Log($"Source Category: '{sourceCategoryName}'");
            DiagnosticLogger.Log($"Destination Category: '{destCategoryName}'");
            DiagnosticLogger.Log($"Triggers to copy: {string.Join(", ", triggerNames.Select(t => $"'{t}'"))}");

            // Get source triggers
            var sourceTriggers = GetTriggersInCategory(source, sourceCategoryName);
            DiagnosticLogger.Log($"Found {sourceTriggers.Count} triggers in source category '{sourceCategoryName}'");

            var triggersToCopy = new List<TriggerDefinition>();

            foreach (var triggerName in triggerNames)
            {
                var trigger = sourceTriggers.FirstOrDefault(t => t.Name.Equals(triggerName, StringComparison.OrdinalIgnoreCase));
                if (trigger != null)
                {
                    triggersToCopy.Add(trigger);
                    DiagnosticLogger.Log($"Found trigger to copy: '{triggerName}' (ID={trigger.Id})");
                }
                else
                {
                    Console.WriteLine($"  ⚠ Warning: Trigger '{triggerName}' not found in category '{sourceCategoryName}'");
                    DiagnosticLogger.Log($"WARNING: Trigger '{triggerName}' not found in category '{sourceCategoryName}'");
                }
            }

            if (triggersToCopy.Count == 0)
            {
                Console.WriteLine("\n  No triggers to copy.");
                DiagnosticLogger.Log("No triggers to copy - aborting");
                DiagnosticLogger.LogOperationEnd($"COPY SPECIFIC TRIGGERS (Option 5)", false);
                return;
            }

            // Find or create destination category
            var destCategory = target.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(destCategoryName, StringComparison.OrdinalIgnoreCase));

            if (destCategory == null)
            {
                DiagnosticLogger.Log($"Destination category '{destCategoryName}' not found - creating new category");

                // Create new category at root level
                // For 1.27 format: use ParentId=0 (since -1 gets read back as 0 anyway)
                // For newer formats: use ParentId=-1
                bool is127Format = target.SubVersion == null;
                int rootParentId = is127Format ? 0 : -1;

                destCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
                {
                    Id = GetNextId(target),
                    ParentId = rootParentId,  // CRITICAL: Root-level category
                    Name = destCategoryName,
                    IsComment = false,
                    IsExpanded = true
                };

                DiagnosticLogger.Log($"Created new category (1.27={is127Format}): ID={destCategory.Id}, ParentId={destCategory.ParentId}, Name='{destCategory.Name}'");

                // CRITICAL: Insert category BEFORE first trigger to maintain correct file order for 1.27 format
                // Find the first trigger in TriggerItems
                int firstTriggerIndex = -1;
                for (int i = 0; i < target.TriggerItems.Count; i++)
                {
                    if (target.TriggerItems[i] is TriggerDefinition)
                    {
                        firstTriggerIndex = i;
                        break;
                    }
                }

                // Insert category before first trigger, or at end if no triggers exist
                if (firstTriggerIndex >= 0)
                {
                    target.TriggerItems.Insert(firstTriggerIndex, destCategory);
                    Console.WriteLine($"\n  ✓ Created new category '{destCategoryName}' at position {firstTriggerIndex} (ID={destCategory.Id}, ParentId={destCategory.ParentId})");
                }
                else
                {
                    target.TriggerItems.Add(destCategory);
                    Console.WriteLine($"\n  ✓ Created new category '{destCategoryName}' (ID={destCategory.Id}, ParentId={destCategory.ParentId})");
                }
            }

            // Find insertion point (immediately after the category)
            var categoryIndex = target.TriggerItems.IndexOf(destCategory);
            int insertIndex = categoryIndex + 1;

            // CRITICAL FOR 1.27 FORMAT: Insert triggers IMMEDIATELY after category
            // (not at the end of the file - file order determines visual nesting)
            DiagnosticLogger.Log($"Category '{destCategoryName}' is at index {categoryIndex}, inserting triggers at index {insertIndex}");

            // Copy missing variables from source to target before copying triggers
            DiagnosticLogger.Log("Copying missing variables from source to target");
            CopyMissingVariables(source, target, triggersToCopy);

            // Copy triggers
            Console.WriteLine($"\n  Copying {triggersToCopy.Count} trigger(s) to category '{destCategoryName}':");
            DiagnosticLogger.Log($"Copying {triggersToCopy.Count} trigger(s) to category '{destCategoryName}' at index {insertIndex}");
            DiagnosticLogger.Indent();
            foreach (var sourceTrigger in triggersToCopy)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, GetNextId(target), destCategory.Id);
                target.TriggerItems.Insert(insertIndex, copiedTrigger);
                insertIndex++;
                Console.WriteLine($"    ✓ {copiedTrigger.Name}");
                DiagnosticLogger.Log($"Copied trigger: '{copiedTrigger.Name}' (ID={copiedTrigger.Id}, ParentId={copiedTrigger.ParentId})");
            }
            DiagnosticLogger.Unindent();

            // Update trigger item counts
            DiagnosticLogger.Log("Updating trigger item counts");
            UpdateTriggerItemCounts(target);

            DiagnosticLogger.LogOperationEnd($"COPY SPECIFIC TRIGGERS (Option 5)", true);
        }

        /// <summary>
        /// Checks if a ParentId value represents root level (-1 or 0 in 1.27 format)
        /// </summary>
        static bool IsRootLevel(int parentId, MapTriggers triggers)
        {
            // ParentId=-1 always means root
            if (parentId == -1) return true;

            // In WC3 1.27 format (SubVersion==null), ParentId=0 means root level
            // In newer formats, ParentId=0 can be a valid category reference
            bool is127Format = triggers.SubVersion == null;
            if (is127Format && parentId == 0)
            {
                // BUT: If there's actually a category with ID=0, then ParentId=0 refers to it
                bool hasCategoryZero = triggers.TriggerItems
                    .OfType<TriggerCategoryDefinition>()
                    .Any(c => c.Id == 0);

                return !hasCategoryZero;  // Root only if no category with ID=0 exists
            }

            return false;
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
            // CRITICAL: Don't match triggers with ParentId=0 if that means "root level" in 1.27 format
            var triggersInCategory = triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId == category.Id && !IsRootLevel(t.ParentId, triggers))
                .ToList();

            return triggersInCategory;
        }

        /// <summary>
        /// Merges a category from source to target
        /// </summary>
        static void MergeCategory(MapTriggers source, MapTriggers target, string categoryName)
        {
            DiagnosticLogger.LogOperationStart($"MERGE CATEGORY '{categoryName}' (Option 4)");
            DiagnosticLogger.Log($"Category to merge: '{categoryName}'");

            // Find source category
            var sourceCategory = source.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (sourceCategory == null)
            {
                DiagnosticLogger.Log($"ERROR: Category '{categoryName}' not found in source");
                DiagnosticLogger.LogOperationEnd($"MERGE CATEGORY '{categoryName}' (Option 4)", false);
                throw new InvalidOperationException($"Category '{categoryName}' not found in source");
            }

            DiagnosticLogger.Log($"Found source category: ID={sourceCategory.Id}, ParentId={sourceCategory.ParentId}");

            // Get triggers from source category
            var sourceCategoryTriggers = GetTriggersInCategory(source, categoryName);
            Console.WriteLine($"  Found {sourceCategoryTriggers.Count} triggers in source category");
            DiagnosticLogger.Log($"Found {sourceCategoryTriggers.Count} triggers in source category");

            // Check if category already exists in target
            var targetCategory = target.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (targetCategory != null)
            {
                Console.WriteLine($"  Category '{categoryName}' already exists in target - removing it");
                DiagnosticLogger.Log($"Category '{categoryName}' already exists in target (ID={targetCategory.Id}) - removing it");
                RemoveCategory(target, categoryName);
            }

            // Create new category in target (Type must be set via constructor)
            // ALWAYS set ParentId for root-level when copying between files
            // (source ParentId might point to non-existent category in target)
            // For 1.27 format: use ParentId=0 (since -1 gets read back as 0 anyway)
            // For newer formats: use ParentId=-1
            bool is127Format = target.SubVersion == null;
            int rootParentId = is127Format ? 0 : -1;

            var newCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
            {
                Id = GetNextId(target),
                ParentId = rootParentId,  // CRITICAL: Root-level for copied categories
                Name = sourceCategory.Name,
                IsComment = sourceCategory.IsComment,
                IsExpanded = sourceCategory.IsExpanded
            };

            DiagnosticLogger.Log($"Created new category (1.27={is127Format}): ID={newCategory.Id}, ParentId={newCategory.ParentId}, Name='{newCategory.Name}'");

            // CRITICAL: Insert category BEFORE first trigger to maintain correct file order for 1.27 format
            // Find the first trigger in TriggerItems
            int firstTriggerIndex = -1;
            for (int i = 0; i < target.TriggerItems.Count; i++)
            {
                if (target.TriggerItems[i] is TriggerDefinition)
                {
                    firstTriggerIndex = i;
                    break;
                }
            }

            // Insert category before first trigger, or at end if no triggers exist
            if (firstTriggerIndex >= 0)
            {
                target.TriggerItems.Insert(firstTriggerIndex, newCategory);
                Console.WriteLine($"  Inserted category '{categoryName}' at position {firstTriggerIndex} (ID={newCategory.Id}, ParentId={newCategory.ParentId})");
            }
            else
            {
                target.TriggerItems.Add(newCategory);
                Console.WriteLine($"  Added category '{categoryName}' to target (ID={newCategory.Id}, ParentId={newCategory.ParentId})");
            }

            // Copy missing variables from source to target before copying triggers
            DiagnosticLogger.Log("Copying missing variables from source to target");
            CopyMissingVariables(source, target, sourceCategoryTriggers);

            // CRITICAL FOR 1.27 FORMAT: Insert triggers IMMEDIATELY after category
            // (not at the end of the file - file order determines visual nesting)
            var categoryIndex = target.TriggerItems.IndexOf(newCategory);
            int insertIndex = categoryIndex + 1;
            DiagnosticLogger.Log($"Category '{categoryName}' is at index {categoryIndex}, inserting {sourceCategoryTriggers.Count} triggers at index {insertIndex}");

            DiagnosticLogger.Indent();
            foreach (var sourceTrigger in sourceCategoryTriggers)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, GetNextId(target), newCategory.Id);
                target.TriggerItems.Insert(insertIndex, copiedTrigger);
                insertIndex++; // Move insertion point forward for next trigger
                Console.WriteLine($"    + Copied trigger: {copiedTrigger.Name}");
                DiagnosticLogger.Log($"Copied trigger: '{copiedTrigger.Name}' at index {insertIndex-1} (ID={copiedTrigger.Id}, ParentId={copiedTrigger.ParentId})");
            }
            DiagnosticLogger.Unindent();

            // Update trigger item counts
            DiagnosticLogger.Log("Updating trigger item counts");
            UpdateTriggerItemCounts(target);

            DiagnosticLogger.LogOperationEnd($"MERGE CATEGORY '{categoryName}' (Option 4)", true);
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
        /// Fixes all categories to root-level by setting ParentId appropriately
        /// For 1.27 format: ParentId=0 (all categories default to 0)
        /// For newer formats: ParentId=-1 (explicit root level)
        /// </summary>
        static int FixAllCategoriesToRoot(MapTriggers triggers)
        {
            bool is127Format = triggers.SubVersion == null;
            int rootParentId = is127Format ? 0 : -1;

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            int fixedCount = 0;

            foreach (var category in categories)
            {
                if (category.ParentId != rootParentId)
                {
                    Console.WriteLine($"  Fixing '{category.Name}' (was ParentId={category.ParentId}, setting to {rootParentId} for {(is127Format ? "1.27" : "newer")} format)");
                    category.ParentId = rootParentId;
                    fixedCount++;
                }
            }

            return fixedCount;
        }

        /// <summary>
        /// Checks if there's a nesting issue (categories appearing after triggers in file order)
        /// This is critical for WC3 1.27 format where visual nesting is based on file order
        /// </summary>
        static bool CheckForNestingIssue(MapTriggers triggers)
        {
            int firstTriggerIndex = -1;
            int lastCategoryIndex = -1;

            for (int i = 0; i < triggers.TriggerItems.Count; i++)
            {
                var item = triggers.TriggerItems[i];
                if (item is TriggerDefinition && firstTriggerIndex == -1)
                {
                    firstTriggerIndex = i;
                }
                if (item is TriggerCategoryDefinition && item.Type != TriggerItemType.RootCategory)
                {
                    lastCategoryIndex = i;
                }
            }

            // Issue exists if any category appears after the first trigger
            return firstTriggerIndex != -1 && lastCategoryIndex > firstTriggerIndex;
        }

        /// <summary>
        /// Fixes file order by moving all categories before triggers
        /// This ensures correct visual nesting in World Editor for WC3 1.27 format
        /// </summary>
        static void FixFileOrder(MapTriggers triggers)
        {
            // Separate items by type
            var categories = new List<TriggerItem>();
            var triggerDefs = new List<TriggerItem>();
            var otherItems = new List<TriggerItem>();

            foreach (var item in triggers.TriggerItems)
            {
                if (item is TriggerCategoryDefinition && item.Type != TriggerItemType.RootCategory)
                {
                    categories.Add(item);
                }
                else if (item is TriggerDefinition)
                {
                    triggerDefs.Add(item);
                }
                else
                {
                    otherItems.Add(item);
                }
            }

            // CRITICAL FIX: Build ID remapping before reordering
            // When War3Net reads 1.27 format, it assigns category IDs sequentially based on file order
            // After reordering, we must reassign IDs and update all trigger ParentIds
            var categoryList = categories.Cast<TriggerCategoryDefinition>().ToList();
            var oldIdToNewId = new Dictionary<int, int>();

            for (int i = 0; i < categoryList.Count; i++)
            {
                int oldId = categoryList[i].Id;
                int newId = i;  // Sequential IDs: 0, 1, 2, 3...
                oldIdToNewId[oldId] = newId;

                if (DEBUG_MODE && oldId != newId)
                {
                    Console.WriteLine($"[DEBUG] Remapping category '{categoryList[i].Name}': ID {oldId} -> {newId}");
                }

                categoryList[i].Id = newId;
            }

            // Update trigger ParentIds to match new category IDs
            var triggerList = triggerDefs.Cast<TriggerDefinition>().ToList();
            foreach (var trigger in triggerList)
            {
                if (trigger.ParentId >= 0 && oldIdToNewId.ContainsKey(trigger.ParentId))
                {
                    int oldParentId = trigger.ParentId;
                    int newParentId = oldIdToNewId[oldParentId];

                    if (DEBUG_MODE && oldParentId != newParentId)
                    {
                        Console.WriteLine($"[DEBUG] Remapping trigger '{trigger.Name}': ParentId {oldParentId} -> {newParentId}");
                    }

                    trigger.ParentId = newParentId;
                }
            }

            // Rebuild TriggerItems with correct order: categories first, then triggers, then other items
            triggers.TriggerItems.Clear();
            foreach (var item in categories)
            {
                triggers.TriggerItems.Add(item);
            }
            foreach (var item in triggerDefs)
            {
                triggers.TriggerItems.Add(item);
            }
            foreach (var item in otherItems)
            {
                triggers.TriggerItems.Add(item);
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] Reordered: {categories.Count} categories, {triggerDefs.Count} triggers, {otherItems.Count} other items");
                Console.WriteLine($"[DEBUG] Remapped {oldIdToNewId.Count(kvp => kvp.Key != kvp.Value)} category IDs");
            }
        }

        /// <summary>
        /// Renumbers all categories sequentially (0, 1, 2, ...) and updates trigger ParentIds
        /// CRITICAL: Must be called before writing to ensure IDs match what War3Net assigns when reading
        /// </summary>
        static void RenumberCategoriesSequentially(MapTriggers triggers)
        {
            DiagnosticLogger.Log("RenumberCategoriesSequentially: Starting");
            bool is127Format = triggers.SubVersion == null;
            DiagnosticLogger.Log($"Format: {(is127Format ? "1.27" : "newer")}");

            // Get all non-root categories
            var categories = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .Where(c => c.Type != TriggerItemType.RootCategory)
                .ToList();

            DiagnosticLogger.Log($"Found {categories.Count} categories to renumber");

            // Build mapping of old ID to new ID
            var oldIdToNewId = new Dictionary<int, int>();

            for (int i = 0; i < categories.Count; i++)
            {
                int oldId = categories[i].Id;
                int newId = i; // Sequential: 0, 1, 2, 3...

                if (oldId != newId)
                {
                    oldIdToNewId[oldId] = newId;

                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[DEBUG] Renumbering category '{categories[i].Name}': ID {oldId} -> {newId}");
                    }
                    DiagnosticLogger.Log($"Renumbering category '{categories[i].Name}': ID {oldId} -> {newId}");

                    categories[i].Id = newId;
                }

                // CRITICAL FIX FOR 1.27 FORMAT:
                // In WC3 1.27, category ParentIds are NOT saved to file
                // All categories default to ParentId=0 when read back
                // So we must normalize ALL category ParentIds to 0 before writing
                if (is127Format)
                {
                    int oldParentId = categories[i].ParentId;

                    // Normalize: -1 becomes 0, everything else stays the same
                    // (In 1.27, ParentId doesn't matter - hierarchy is by file order)
                    if (oldParentId == -1)
                    {
                        if (DEBUG_MODE)
                        {
                            Console.WriteLine($"[DEBUG] Normalizing category '{categories[i].Name}': ParentId -1 -> 0 (1.27 format)");
                        }
                        DiagnosticLogger.Log($"Normalizing category '{categories[i].Name}': ParentId -1 -> 0 (1.27 format)");
                        categories[i].ParentId = 0;
                    }
                }
            }

            // Update all trigger ParentIds to match new category IDs
            DiagnosticLogger.Log($"Updating trigger ParentIds (found {oldIdToNewId.Count} category ID changes)");

            if (oldIdToNewId.Count > 0)
            {
                var triggers_list = triggers.TriggerItems
                    .OfType<TriggerDefinition>()
                    .ToList();

                DiagnosticLogger.Log($"Processing {triggers_list.Count} triggers");
                int updatedCount = 0;

                foreach (var trigger in triggers_list)
                {
                    // Skip root-level triggers using helper function
                    if (IsRootLevel(trigger.ParentId, triggers))
                    {
                        // Normalize to -1
                        trigger.ParentId = -1;
                        continue;
                    }

                    if (oldIdToNewId.ContainsKey(trigger.ParentId))
                    {
                        int oldParentId = trigger.ParentId;
                        int newParentId = oldIdToNewId[oldParentId];

                        if (DEBUG_MODE)
                        {
                            Console.WriteLine($"[DEBUG] Updating trigger '{trigger.Name}': ParentId {oldParentId} -> {newParentId}");
                        }
                        DiagnosticLogger.Log($"Updating trigger '{trigger.Name}': ParentId {oldParentId} -> {newParentId}");

                        trigger.ParentId = newParentId;
                        updatedCount++;
                    }
                }

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] Renumbered {oldIdToNewId.Count} categories and updated {updatedCount} trigger ParentIds");
                }
                DiagnosticLogger.Log($"Renumbered {oldIdToNewId.Count} categories and updated {updatedCount} trigger ParentIds");
            }

            DiagnosticLogger.Log("RenumberCategoriesSequentially: Complete");
        }

        /// <summary>
        /// Copies variables used by triggers, with automatic renaming on conflicts
        /// </summary>
        static void CopyMissingVariables(MapTriggers source, MapTriggers target, List<TriggerDefinition> triggers)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine("\n[DEBUG] ═══ CopyMissingVariables START ═══");
                Console.WriteLine($"[DEBUG] Analyzing {triggers.Count} trigger(s)");
            }

            // Collect all variables used by the triggers being copied
            var usedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var trigger in triggers)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] Scanning trigger: {trigger.Name}");
                }

                var varsInTrigger = GetVariablesUsedByTrigger(trigger, source);

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG]   Found {varsInTrigger.Count} variable(s) in this trigger");
                    foreach (var v in varsInTrigger)
                    {
                        Console.WriteLine($"[DEBUG]     - {v}");
                    }
                }

                foreach (var varName in varsInTrigger)
                {
                    usedVariables.Add(varName);
                }
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] Total unique variables used: {usedVariables.Count}");
            }

            if (usedVariables.Count == 0)
            {
                Console.WriteLine("  ℹ No variables used by these triggers");
                if (DEBUG_MODE)
                {
                    Console.WriteLine("[DEBUG] ═══ CopyMissingVariables END (no variables) ═══\n");
                }
                return;
            }

            Console.WriteLine($"\n  Analyzing {usedVariables.Count} variable(s) used by triggers:");

            var targetVarNames = new HashSet<string>(target.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
            var sourceVarDict = source.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);
            var targetVarDict = target.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

            int copiedCount = 0;
            int renamedCount = 0;
            var renamedMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var varName in usedVariables)
            {
                if (!sourceVarDict.TryGetValue(varName, out var sourceVar))
                {
                    Console.WriteLine($"    ⚠ Warning: Variable '{varName}' not found in source map");
                    continue;
                }

                // Check if variable exists in target
                if (targetVarDict.TryGetValue(varName, out var targetVar))
                {
                    // Variable exists in both - check type
                    if (sourceVar.Type == targetVar.Type)
                    {
                        Console.WriteLine($"    ✓ '{varName}' already exists with same type - no action needed");
                    }
                    else
                    {
                        // TYPE CONFLICT - need to rename
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"    ⚠ CONFLICT: '{varName}' has different types:");
                        Console.WriteLine($"      Source: {sourceVar.Type}");
                        Console.WriteLine($"      Target: {targetVar.Type}");
                        Console.WriteLine($"      → Will rename source variable");
                        Console.ResetColor();

                        // Generate unique name
                        string newName = GenerateUniqueVariableName(varName, targetVarNames);
                        renamedMappings[varName] = newName;

                        // Create renamed copy
                        var newVar = new VariableDefinition
                        {
                            Name = newName,
                            Type = sourceVar.Type,
                            Unk = sourceVar.Unk,
                            IsArray = sourceVar.IsArray,
                            ArraySize = sourceVar.ArraySize,
                            IsInitialized = sourceVar.IsInitialized,
                            InitialValue = sourceVar.InitialValue,
                            Id = target.Variables.Count,
                            ParentId = sourceVar.ParentId
                        };

                        target.Variables.Add(newVar);
                        targetVarNames.Add(newName);
                        renamedCount++;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"    ✓ Renamed and copied: '{varName}' → '{newName}'");
                        Console.ResetColor();
                    }
                }
                else
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
                        Id = target.Variables.Count,
                        ParentId = sourceVar.ParentId
                    };

                    target.Variables.Add(newVar);
                    targetVarNames.Add(newVar.Name);
                    copiedCount++;
                    Console.WriteLine($"    + Copied: '{newVar.Name}' ({newVar.Type})");
                }
            }

            if (copiedCount > 0 || renamedCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n  ✓ Copied {copiedCount} variable(s), renamed {renamedCount} variable(s)");
                Console.ResetColor();
            }

            // Apply renamings to triggers if any
            if (renamedMappings.Count > 0)
            {
                Console.WriteLine($"\n  Updating variable references in triggers...");

                if (DEBUG_MODE)
                {
                    Console.WriteLine("[DEBUG] Rename mappings:");
                    foreach (var kvp in renamedMappings)
                    {
                        Console.WriteLine($"[DEBUG]   '{kvp.Key}' → '{kvp.Value}'");
                    }
                }

                foreach (var trigger in triggers)
                {
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[DEBUG] Renaming variables in trigger: {trigger.Name}");
                    }
                    RenameVariablesInTrigger(trigger, renamedMappings);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Updated all variable references");
                Console.ResetColor();
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine("[DEBUG] ═══ CopyMissingVariables END ═══\n");
            }
        }

        /// <summary>
        /// Gets all variable names referenced in a trigger
        /// </summary>
        static HashSet<string> GetVariablesUsedByTrigger(TriggerDefinition trigger, MapTriggers mapTriggers)
        {
            var usedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG]   GetVariablesUsedByTrigger: {trigger.Name} ({trigger.Functions.Count} functions)");
            }

            // Scan all functions in the trigger
            foreach (var function in trigger.Functions)
            {
                CollectVariablesFromFunction(function, usedVariables, mapTriggers);
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG]   Total variables collected: {usedVariables.Count}");
            }

            return usedVariables;
        }

        /// <summary>
        /// Recursively collects variable names from a trigger function and its parameters
        /// </summary>
        static void CollectVariablesFromFunction(TriggerFunction function, HashSet<string> usedVariables, MapTriggers mapTriggers)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG]     Function: {function.Type} - {function.Name} ({function.Parameters.Count} params)");
            }

            // Check parameters
            foreach (var param in function.Parameters)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG]       Param: Type={param.Type}, Value='{param.Value}'");
                }

                if (param.Type == TriggerFunctionParameterType.Variable)
                {
                    // Parameter is a variable reference - extract the variable name
                    var varName = GetVariableNameFromParameter(param, mapTriggers);
                    if (!string.IsNullOrEmpty(varName))
                    {
                        if (DEBUG_MODE)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"[DEBUG]       >>> VARIABLE DETECTED: '{varName}'");
                            Console.ResetColor();
                        }
                        usedVariables.Add(varName);
                    }
                }

                // Check nested function
                if (param.Function != null)
                {
                    CollectVariablesFromFunction(param.Function, usedVariables, mapTriggers);
                }

                // Check array indexer
                if (param.ArrayIndexer != null)
                {
                    CollectVariablesFromParameterRecursive(param.ArrayIndexer, usedVariables, mapTriggers);
                }
            }

            // Check child functions (if-then-else blocks)
            foreach (var childFunc in function.ChildFunctions)
            {
                CollectVariablesFromFunction(childFunc, usedVariables, mapTriggers);
            }
        }

        /// <summary>
        /// Recursively collects variables from a parameter
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
        /// Gets variable name from a parameter value
        /// </summary>
        static string GetVariableNameFromParameter(TriggerFunctionParameter param, MapTriggers mapTriggers)
        {
            var value = param.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // The parameter value is typically the variable name directly
            // First, check if it's a valid variable name in the map
            var varByName = mapTriggers.Variables.FirstOrDefault(v =>
                v.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (varByName != null)
            {
                return varByName.Name;
            }

            // Some triggers might use variable IDs - try parsing as int
            if (int.TryParse(value, out int varId))
            {
                var varById = mapTriggers.Variables.FirstOrDefault(v => v.Id == varId);
                if (varById != null)
                {
                    return varById.Name;
                }
            }

            // Return the value as-is if we can't resolve it
            // (it might be a variable that will be resolved later)
            return value;
        }

        /// <summary>
        /// Generates a unique variable name by appending a suffix
        /// </summary>
        static string GenerateUniqueVariableName(string baseName, HashSet<string> existingNames)
        {
            // Try different suffixes until we find one that doesn't exist
            string[] suffixes = { "_Source", "_Merged", "_Copy", "_Alt", "_2" };

            foreach (var suffix in suffixes)
            {
                string candidate = baseName + suffix;
                if (!existingNames.Contains(candidate))
                {
                    return candidate;
                }
            }

            // If all standard suffixes are taken, use numeric suffix
            int counter = 2;
            while (counter < 1000) // Safety limit
            {
                string candidate = $"{baseName}_{counter}";
                if (!existingNames.Contains(candidate))
                {
                    return candidate;
                }
                counter++;
            }

            // Last resort
            return $"{baseName}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        /// <summary>
        /// Renames variable references in a trigger
        /// </summary>
        static void RenameVariablesInTrigger(TriggerDefinition trigger, Dictionary<string, string> renameMappings)
        {
            foreach (var function in trigger.Functions)
            {
                RenameVariablesInFunction(function, renameMappings);
            }
        }

        /// <summary>
        /// Recursively renames variable references in a function
        /// </summary>
        static void RenameVariablesInFunction(TriggerFunction function, Dictionary<string, string> renameMappings)
        {
            foreach (var param in function.Parameters)
            {
                if (param.Type == TriggerFunctionParameterType.Variable)
                {
                    // Check if this variable needs to be renamed
                    if (renameMappings.TryGetValue(param.Value, out var newName))
                    {
                        param.Value = newName;
                    }
                }

                if (param.Function != null)
                {
                    RenameVariablesInFunction(param.Function, renameMappings);
                }

                if (param.ArrayIndexer != null)
                {
                    RenameVariablesInParameterRecursive(param.ArrayIndexer, renameMappings);
                }
            }

            foreach (var childFunc in function.ChildFunctions)
            {
                RenameVariablesInFunction(childFunc, renameMappings);
            }
        }

        /// <summary>
        /// Recursively renames variables in a parameter
        /// </summary>
        static void RenameVariablesInParameterRecursive(TriggerFunctionParameter param, Dictionary<string, string> renameMappings)
        {
            if (param.Type == TriggerFunctionParameterType.Variable)
            {
                if (renameMappings.TryGetValue(param.Value, out var newName))
                {
                    param.Value = newName;
                }
            }

            if (param.Function != null)
            {
                RenameVariablesInFunction(param.Function, renameMappings);
            }

            if (param.ArrayIndexer != null)
            {
                RenameVariablesInParameterRecursive(param.ArrayIndexer, renameMappings);
            }
        }

        /// <summary>
        /// Shows comprehensive debug information about both maps
        /// </summary>
        static void ShowComprehensiveDebugInfo(MapTriggers source, MapTriggers target)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           COMPREHENSIVE DEBUG INFORMATION                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            // SOURCE MAP VARIABLES
            Console.WriteLine("\n=== SOURCE MAP VARIABLES ===");
            Console.WriteLine($"Total: {source.Variables.Count}");
            if (source.Variables.Count > 0)
            {
                Console.WriteLine("\nID | Name                      | Type           | Array | Init");
                Console.WriteLine("---|---------------------------|----------------|-------|-----");
                foreach (var v in source.Variables.OrderBy(v => v.Id))
                {
                    Console.WriteLine($"{v.Id,2} | {v.Name,-25} | {v.Type,-14} | {(v.IsArray ? "Yes" : "No"),-5} | {(v.IsInitialized ? "Yes" : "No")}");
                }
            }

            // TARGET MAP VARIABLES
            Console.WriteLine("\n=== TARGET MAP VARIABLES ===");
            Console.WriteLine($"Total: {target.Variables.Count}");
            if (target.Variables.Count > 0)
            {
                Console.WriteLine("\nID | Name                      | Type           | Array | Init");
                Console.WriteLine("---|---------------------------|----------------|-------|-----");
                foreach (var v in target.Variables.OrderBy(v => v.Id))
                {
                    Console.WriteLine($"{v.Id,2} | {v.Name,-25} | {v.Type,-14} | {(v.IsArray ? "Yes" : "No"),-5} | {(v.IsInitialized ? "Yes" : "No")}");
                }
            }

            // VARIABLE CONFLICTS
            Console.WriteLine("\n=== VARIABLE NAME CONFLICTS (Same name, different type) ===");
            var sourceVarDict = source.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);
            var targetVarDict = target.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

            var conflicts = new List<(string Name, string SourceType, string TargetType)>();
            foreach (var sv in source.Variables)
            {
                if (targetVarDict.TryGetValue(sv.Name, out var tv))
                {
                    if (sv.Type != tv.Type)
                    {
                        conflicts.Add((sv.Name, sv.Type, tv.Type));
                    }
                }
            }

            if (conflicts.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Found {conflicts.Count} variable(s) with same name but different types:");
                Console.WriteLine("\nVariable Name             | Source Type    | Target Type");
                Console.WriteLine("--------------------------|----------------|----------------");
                foreach (var c in conflicts)
                {
                    Console.WriteLine($"{c.Name,-25} | {c.SourceType,-14} | {c.TargetType}");
                }
                Console.ResetColor();
                Console.WriteLine("\nℹ These will be auto-renamed if used by triggers you copy");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ No variable name/type conflicts found");
                Console.ResetColor();
            }

            // SOURCE CATEGORIES AND TRIGGERS
            Console.WriteLine("\n=== SOURCE MAP STRUCTURE ===");
            var sourceCategories = source.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            Console.WriteLine($"Categories: {sourceCategories.Count}");
            Console.WriteLine($"Triggers: {source.TriggerItems.OfType<TriggerDefinition>().Count()}");

            // TARGET CATEGORIES AND TRIGGERS
            Console.WriteLine("\n=== TARGET MAP STRUCTURE ===");
            var targetCategories = target.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            Console.WriteLine($"Categories: {targetCategories.Count}");
            Console.WriteLine($"Triggers: {target.TriggerItems.OfType<TriggerDefinition>().Count()}");

            Console.WriteLine("\n=== SAMPLE TRIGGER VARIABLE ANALYSIS ===");
            Console.Write("Enter category name to analyze (or press Enter to skip): ");
            string? catName = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(catName))
            {
                var triggers = GetTriggersInCategory(source, catName);
                if (triggers.Count > 0)
                {
                    Console.WriteLine($"\nFound {triggers.Count} trigger(s) in '{catName}'");
                    Console.Write("Enter trigger name to analyze: ");
                    string? trigName = Console.ReadLine();

                    var trigger = triggers.FirstOrDefault(t => t.Name.Equals(trigName, StringComparison.OrdinalIgnoreCase));
                    if (trigger != null)
                    {
                        DebugAnalyzeTrigger(trigger, source);
                    }
                    else
                    {
                        Console.WriteLine($"⚠ Trigger '{trigName}' not found");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠ No triggers found in category '{catName}'");
                }
            }

            Console.WriteLine("\nPress Enter to continue...");
            Console.ReadLine();
        }

        /// <summary>
        /// Deep analysis of a single trigger for debugging
        /// </summary>
        static void DebugAnalyzeTrigger(TriggerDefinition trigger, MapTriggers mapTriggers)
        {
            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  TRIGGER: {trigger.Name,-47}║");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════╝");

            Console.WriteLine($"ID: {trigger.Id}");
            Console.WriteLine($"ParentId: {trigger.ParentId}");
            Console.WriteLine($"Enabled: {trigger.IsEnabled}");
            Console.WriteLine($"Functions: {trigger.Functions.Count}");

            // Analyze variables
            var usedVars = GetVariablesUsedByTrigger(trigger, mapTriggers);
            Console.WriteLine($"\n=== VARIABLES DETECTED ({usedVars.Count}) ===");
            if (usedVars.Count > 0)
            {
                foreach (var varName in usedVars.OrderBy(v => v))
                {
                    var varDef = mapTriggers.Variables.FirstOrDefault(v =>
                        v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                    if (varDef != null)
                    {
                        Console.WriteLine($"  ✓ '{varName}' (Type: {varDef.Type}, Array: {varDef.IsArray})");
                    }
                    else
                    {
                        Console.WriteLine($"  ⚠ '{varName}' (NOT FOUND IN MAP!)");
                    }
                }
            }
            else
            {
                Console.WriteLine("  (No variables detected)");
            }

            // Detailed function analysis
            Console.WriteLine($"\n=== DETAILED FUNCTION ANALYSIS ===");
            for (int i = 0; i < trigger.Functions.Count; i++)
            {
                var func = trigger.Functions[i];
                Console.WriteLine($"\n[{i + 1}] {func.Type}: {func.Name}");
                Console.WriteLine($"    Enabled: {func.IsEnabled}");
                Console.WriteLine($"    Parameters: {func.Parameters.Count}");

                for (int p = 0; p < func.Parameters.Count; p++)
                {
                    var param = func.Parameters[p];
                    Console.WriteLine($"      [{p}] Type={param.Type}, Value='{param.Value}'");

                    if (param.Type == TriggerFunctionParameterType.Variable)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"          ^^^ VARIABLE REFERENCE ^^^");
                        Console.ResetColor();
                    }

                    if (param.Function != null)
                    {
                        Console.WriteLine($"          Nested Function: {param.Function.Name}");
                    }

                    if (param.ArrayIndexer != null)
                    {
                        Console.WriteLine($"          Array Indexer: Type={param.ArrayIndexer.Type}, Value='{param.ArrayIndexer.Value}'");
                    }
                }

                if (func.ChildFunctions.Count > 0)
                {
                    Console.WriteLine($"    Child Functions: {func.ChildFunctions.Count}");
                }
            }
        }

        /// <summary>
        /// Shows a simple summary of variables in both maps
        /// </summary>
        static void ShowVariableSummary(MapTriggers source, MapTriggers target)
        {
            Console.WriteLine("\n=== Variable Summary ===");
            Console.WriteLine($"Source map: {source.Variables.Count} variables");
            Console.WriteLine($"Target map: {target.Variables.Count} variables");
            Console.WriteLine("\nℹ Variable conflicts will be detected and automatically resolved");
            Console.WriteLine("  when you copy triggers (variables will be renamed if needed)");
            Console.WriteLine();
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
        /// Fixes variable IDs by ensuring they are sequential (0, 1, 2, ...)
        /// </summary>
        static void FixVariableIds(MapTriggers triggers, string mapName)
        {
            if (triggers.Variables.Count == 0)
            {
                return;
            }

            // Check if all variables have the same ID (corrupted) or if there are duplicates
            var idGroups = triggers.Variables.GroupBy(v => v.Id).ToList();
            var duplicateIds = idGroups.Where(g => g.Count() > 1).ToList();
            bool allSameId = idGroups.Count == 1;

            if (allSameId)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ WARNING: All variables in {mapName} have ID={triggers.Variables[0].Id}!");
                Console.WriteLine($"  This indicates corrupted variable IDs. Reassigning sequential IDs...");
                Console.ResetColor();

                // Reassign sequential IDs
                for (int i = 0; i < triggers.Variables.Count; i++)
                {
                    triggers.Variables[i].Id = i;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Reassigned variable IDs: 0 to {triggers.Variables.Count - 1}");
                Console.ResetColor();
            }
            else if (duplicateIds.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ WARNING: Found {duplicateIds.Count} duplicate variable ID(s) in {mapName}!");
                Console.WriteLine($"  Reassigning sequential IDs...");
                Console.ResetColor();

                // Reassign sequential IDs
                for (int i = 0; i < triggers.Variables.Count; i++)
                {
                    triggers.Variables[i].Id = i;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Reassigned variable IDs: 0 to {triggers.Variables.Count - 1}");
                Console.ResetColor();
            }
            else if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] Variable IDs in {mapName} are valid (no duplicates)");
            }
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

            // Determine root ParentId value for this format
            bool is127Format = triggers.SubVersion == null;
            int rootParentId = is127Format ? 0 : -1;

            // Update ParentIds in ALL items (both categories and triggers)
            foreach (var item in triggers.TriggerItems)
            {
                // Skip root-level items using helper function
                if (IsRootLevel(item.ParentId, triggers))
                {
                    // Normalize root level to appropriate value for format
                    item.ParentId = rootParentId;
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
                        trigger.ParentId = rootParentId;
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
                        category.ParentId = rootParentId;
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
        /// AUTOMATICALLY fixes SubVersion and variable IDs for old format maps
        /// </summary>
        static MapTriggers ReadMapTriggersAuto(string filePath)
        {
            DiagnosticLogger.LogFileOperation("Reading", filePath);

            MapTriggers triggers;

            if (IsMapArchive(filePath))
            {
                DiagnosticLogger.Log("Detected map archive format");
                triggers = ReadMapArchiveFile(filePath);
            }
            else
            {
                DiagnosticLogger.Log("Detected WTG file format");
                triggers = ReadWTGFile(filePath);
            }

            DiagnosticLogger.LogMapTriggersState(triggers, "After Reading File");

            // WC3 1.27 FORMAT HANDLING
            // If map has SubVersion=null, it's WC3 1.27 format
            // In this format, variable IDs are NOT saved/loaded from file
            // All variables default to Id=0 when read
            // We assign sequential IDs in memory for internal use only
            // These IDs won't be saved back (maintains 1.27 compatibility)
            if (triggers.SubVersion == null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ℹ Map is WC3 1.27 format (SubVersion=null)");
                Console.ResetColor();

                DiagnosticLogger.Log("Detected WC3 1.27 format (SubVersion=null)");

                // Assign sequential IDs in memory for internal tracking
                // (These won't be saved - 1.27 format doesn't support variable IDs)
                for (int i = 0; i < triggers.Variables.Count; i++)
                {
                    triggers.Variables[i].Id = i;
                }

                Console.WriteLine($"  ✓ Assigned in-memory IDs to {triggers.Variables.Count} variable(s) for tracking");
                Console.WriteLine($"  ✓ Maintaining 1.27 compatibility (SubVersion=null)");

                if (DEBUG_MODE)
                {
                    Console.WriteLine("[DEBUG] In-memory variable IDs (not saved to file):");
                    foreach (var v in triggers.Variables.Take(5))
                    {
                        Console.WriteLine($"[DEBUG]   ID={v.Id}, Name={v.Name}");
                    }
                }
            }

            return triggers;
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

            try
            {
                var triggers = (MapTriggers)constructorInfo.Invoke(new object[] { reader, TriggerData.Default });
                return triggers;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // Show the actual inner exception that caused the problem
                throw new InvalidOperationException(
                    $"Failed to parse war3map.wtg from map archive. " +
                    $"Inner error: {ex.InnerException?.Message ?? ex.Message}\n" +
                    $"This might be due to:\n" +
                    $"  - Corrupted .wtg file in the map\n" +
                    $"  - Unsupported WTG format version\n" +
                    $"  - Map from very old or very new WC3 version\n" +
                    $"  - Try extracting war3map.wtg manually and check if it's valid",
                    ex.InnerException ?? ex);
            }
        }

        /// <summary>
        /// Writes MapTriggers to a map archive, optionally removing war3map.j
        /// </summary>
        static void WriteMapArchive(string originalArchivePath, string outputArchivePath, MapTriggers triggers, bool removeJassFile)
        {
            DiagnosticLogger.LogFileOperation("Writing Map Archive", outputArchivePath);
            DiagnosticLogger.Log($"Original archive: {originalArchivePath}");
            DiagnosticLogger.Log($"Remove JASS file: {removeJassFile}");
            DiagnosticLogger.LogMapTriggersState(triggers, "Before Writing Archive");

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] WriteMapArchive: Writing to {outputArchivePath}");
                Console.WriteLine($"[DEBUG]   Variables to write: {triggers.Variables.Count}");
                Console.WriteLine($"[DEBUG]   Trigger items to write: {triggers.TriggerItems.Count}");
            }

            Console.WriteLine($"  Opening original archive...");
            DiagnosticLogger.Log("Opening original archive");
            using var originalArchive = MpqArchive.Open(originalArchivePath, true);
            originalArchive.DiscoverFileNames();

            Console.WriteLine($"  Creating archive builder...");
            DiagnosticLogger.Log("Creating archive builder");
            var builder = new MpqArchiveBuilder(originalArchive);

            // CRITICAL: Renumber all categories sequentially before writing
            // This ensures category IDs match what War3Net will assign when reading back
            DiagnosticLogger.Log("Renumbering categories sequentially before write");
            RenumberCategoriesSequentially(triggers);
            DiagnosticLogger.LogMapTriggersState(triggers, "After Renumbering");

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

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] Serialized war3map.wtg to memory: {triggerStream.Length} bytes");
            }

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
                    DiagnosticLogger.Log($"Removing {jassFileName} from archive");
                    builder.RemoveFile(jassFileName);
                }

                // Also check for scripts/war3map.j
                var jassFileNameAlt = "scripts/war3map.j";
                if (originalArchive.FileExists(jassFileNameAlt))
                {
                    Console.WriteLine($"  Removing {jassFileNameAlt} for sync...");
                    DiagnosticLogger.Log($"Removing {jassFileNameAlt} from archive");
                    builder.RemoveFile(jassFileNameAlt);
                }
            }

            // Save the modified archive
            Console.WriteLine($"  Saving to {outputArchivePath}...");
            DiagnosticLogger.Log("Saving modified archive");
            builder.SaveTo(outputArchivePath);
            Console.WriteLine($"  Archive updated successfully!");

            DiagnosticLogger.Log($"Archive write completed. File size: {new FileInfo(outputArchivePath).Length} bytes");
        }

        /// <summary>
        /// Helper wrapper for GetVariablesUsedByTrigger - collects variable references from a trigger
        /// </summary>
        static void CollectVariableReferences(TriggerDefinition trigger, HashSet<string> variables, MapTriggers map)
        {
            var vars = GetVariablesUsedByTrigger(trigger, map);
            foreach (var v in vars)
            {
                variables.Add(v);
            }
        }

        /// <summary>
        /// Creates a minimal empty MapTriggers structure for isolation testing
        /// </summary>
        static MapTriggers CreateMinimalMapTriggers(MapTriggers sourceTemplate)
        {
            // Create new MapTriggers with same format version as source
            var minimal = new MapTriggers(sourceTemplate.FormatVersion, sourceTemplate.SubVersion)
            {
                GameVersion = sourceTemplate.GameVersion,
            };

            // Initialize empty collections
            minimal.Variables.Clear();
            minimal.TriggerItems.Clear();
            minimal.TriggerItemCounts.Clear();

            // Add root category item if it exists in source
            var rootCategory = sourceTemplate.TriggerItems.FirstOrDefault(item => item.Type == TriggerItemType.RootCategory);
            if (rootCategory != null)
            {
                minimal.TriggerItems.Add(rootCategory);
            }

            return minimal;
        }
    }
}
