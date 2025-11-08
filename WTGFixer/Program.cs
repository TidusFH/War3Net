using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Script;
using War3Net.IO.Mpq;
using War3Net.Build.Extensions;

namespace WTGFixer
{
    class Program
    {
        static bool DEBUG_MODE = false;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("============================================================");
                Console.WriteLine("           War3Net WTG Fixer Utility");
                Console.WriteLine("     Repairs corrupted/merged WTG files with validation");
                Console.WriteLine("============================================================\n");

                // Ask for debug mode
                Console.Write("Enable debug mode? (y/n): ");
                string? debugResponse = Console.ReadLine();
                if (debugResponse?.ToLower() == "y")
                {
                    DEBUG_MODE = true;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[DEBUG] Debug mode ENABLED - detailed logging will be shown");
                    Console.ResetColor();
                }
                Console.WriteLine();

                string mergedPath, originalPath, sourcePath = null, outputPath;

                if (args.Length >= 2)
                {
                    mergedPath = args[0];
                    originalPath = args[1];
                    sourcePath = args.Length > 2 ? args[2] : null;
                    outputPath = args.Length > 3 ? args[3] : Path.ChangeExtension(mergedPath, ".fixed.wtg");
                }
                else
                {
                    Console.WriteLine("Usage: WTGFixer <merged.wtg> <original.wtg> [source.wtg] [output.wtg]");
                    Console.WriteLine("\nArguments:");
                    Console.WriteLine("  merged.wtg   - Merged/corrupted file to fix (e.g., Target/war3map_merged.wtg)");
                    Console.WriteLine("  original.wtg - Original target file for variable reference (e.g., Target/war3map.wtg)");
                    Console.WriteLine("  source.wtg   - (Optional) Source file to check added triggers");
                    Console.WriteLine("  output.wtg   - (Optional) Output file path");
                    Console.WriteLine("\nOr place files in folders:");
                    Console.WriteLine("  ../Target/   - Your merged file (war3map_merged.wtg) and original target");
                    Console.WriteLine("  ../Source/   - (Optional) Your source triggers for added trigger validation");
                    Console.WriteLine("\nPress Enter to auto-detect or Ctrl+C to exit...");
                    Console.ReadLine();

                    // Auto-detect: Look for merged file in Target
                    mergedPath = AutoDetectMergedFile("../Target");
                    originalPath = AutoDetectOriginalFile("../Target");

                    // Try to find source file (optional)
                    try
                    {
                        sourcePath = AutoDetectMapFile("../Source");
                        Console.WriteLine($"  [INFO] Source file detected: {Path.GetFileName(sourcePath)}");
                    }
                    catch
                    {
                        Console.WriteLine("  [INFO] No source file found - skipping added trigger validation");
                    }

                    outputPath = GenerateOutputPath(mergedPath);
                }

                Console.WriteLine($"\nMerged file:   {Path.GetFullPath(mergedPath)}");
                Console.WriteLine($"Original file: {Path.GetFullPath(originalPath)}");
                if (sourcePath != null)
                    Console.WriteLine($"Source file:   {Path.GetFullPath(sourcePath)}");
                Console.WriteLine($"Output file:   {Path.GetFullPath(outputPath)}");
                Console.WriteLine();

                // Read files
                Console.WriteLine("Reading merged file...");
                MapTriggers mergedTriggers = ReadMapTriggersAuto(mergedPath);
                Console.WriteLine($"  [OK] Merged: {mergedTriggers.TriggerItems.Count} items, {mergedTriggers.Variables.Count} variables");
                DebugLogVariables("After reading merged file", mergedTriggers);

                Console.WriteLine("\nReading original file...");
                MapTriggers originalTriggers = ReadMapTriggersAuto(originalPath);
                Console.WriteLine($"  [OK] Original: {originalTriggers.TriggerItems.Count} items, {originalTriggers.Variables.Count} variables");
                DebugLogVariables("After reading original file", originalTriggers);

                MapTriggers? sourceTriggers = null;
                if (sourcePath != null)
                {
                    Console.WriteLine("\nReading source file...");
                    sourceTriggers = ReadMapTriggersAuto(sourcePath);
                    Console.WriteLine($"  [OK] Source: {sourceTriggers.TriggerItems.Count} items, {sourceTriggers.Variables.Count} variables");
                    DebugLogVariables("After reading source file", sourceTriggers);
                }

                // Run validation and fixes
                Console.WriteLine("\n============================================================");
                Console.WriteLine("                 VALIDATION PHASE");
                Console.WriteLine("============================================================\n");

                var issues = new ValidationIssues();
                bool needsFix = false;

                // Check 1: SubVersion
                if (CheckSubVersion(mergedTriggers, issues))
                    needsFix = true;

                // Check 2: Missing variables (from original target)
                if (CheckMissingVariables(mergedTriggers, originalTriggers, issues, "original target"))
                    needsFix = true;

                // Check 3: Missing variables from added triggers (if source provided)
                if (sourceTriggers != null)
                {
                    if (CheckMissingVariablesFromSource(mergedTriggers, sourceTriggers, issues))
                        needsFix = true;
                }

                // Check 4: Undefined variables in triggers
                if (CheckUndefinedVariables(mergedTriggers, issues))
                    needsFix = true;

                // Check 5: Wrong ParentId (nested in initialization)
                if (CheckWrongParentIds(mergedTriggers, issues))
                    needsFix = true;

                // Check 6: Orphaned triggers/categories
                if (CheckOrphaned(mergedTriggers, issues))
                    needsFix = true;

                // Check 7: Duplicate IDs
                if (CheckDuplicateIds(mergedTriggers, issues))
                    needsFix = true;

                // Show summary
                Console.WriteLine("\n============================================================");
                Console.WriteLine("                VALIDATION SUMMARY");
                Console.WriteLine("============================================================\n");

                if (!needsFix)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [OK] No issues found! File is valid.");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Found {issues.TotalIssues()} issue(s) that need fixing:");
                Console.WriteLine($"  - SubVersion issues: {issues.SubVersionIssues}");
                Console.WriteLine($"  - Missing variables (from original): {issues.MissingVariables.Count}");
                Console.WriteLine($"  - Missing variables (from source): {issues.MissingSourceVariables.Count}");
                Console.WriteLine($"  - Undefined variables: {issues.UndefinedVariables.Count}");
                Console.WriteLine($"  - Wrong ParentIds: {issues.WrongParentIds.Count}");
                Console.WriteLine($"  - Orphaned items: {issues.OrphanedItems.Count}");
                Console.WriteLine($"  - Duplicate IDs: {issues.DuplicateIds.Count}");
                Console.ResetColor();

                Console.Write("\nAttempt automatic fix? (y/n): ");
                string? response = Console.ReadLine();
                if (response?.ToLower() != "y")
                {
                    Console.WriteLine("Fix cancelled.");
                    return;
                }

                // Apply fixes
                Console.WriteLine("\n============================================================");
                Console.WriteLine("                   FIXING PHASE");
                Console.WriteLine("============================================================\n");

                ApplyFixes(mergedTriggers, originalTriggers, sourceTriggers, issues);

                // Save fixed file
                Console.WriteLine($"\nSaving fixed file to: {outputPath}");
                DebugLog($"About to save - Variable count: {mergedTriggers.Variables.Count}");
                DebugLogVariables("BEFORE saving to disk", mergedTriggers);

                if (IsMapArchive(outputPath))
                {
                    DebugLog($"Writing to map archive: {outputPath}");
                    WriteMapArchive(mergedPath, outputPath, mergedTriggers);
                }
                else
                {
                    DebugLog($"Writing to WTG file: {outputPath}");
                    WriteWTGFile(outputPath, mergedTriggers);
                }

                DebugLog($"File saved successfully. Verifying file exists: {File.Exists(outputPath)}");

                // Verify fix
                Console.WriteLine("\n============================================================");
                Console.WriteLine("                    VERIFICATION");
                Console.WriteLine("============================================================\n");

                DebugLog($"Reading back the fixed file from: {outputPath}");
                MapTriggers verifyTriggers = ReadMapTriggersAuto(outputPath);
                DebugLog($"Read back - Variable count: {verifyTriggers.Variables.Count}");
                DebugLogVariables("AFTER reading back from disk", verifyTriggers);

                var verifyIssues = new ValidationIssues();
                bool stillHasIssues = false;

                stillHasIssues |= CheckSubVersion(verifyTriggers, verifyIssues);
                stillHasIssues |= CheckWrongParentIds(verifyTriggers, verifyIssues);
                stillHasIssues |= CheckOrphaned(verifyTriggers, verifyIssues);
                stillHasIssues |= CheckDuplicateIds(verifyTriggers, verifyIssues);

                // CRITICAL: Verify the specific variables we added are actually present
                Console.WriteLine("\nVerifying added variables are present in fixed file...");
                DebugLog("=== COMPREHENSIVE VARIABLE VERIFICATION ===");

                var addedVariables = new List<string>();
                addedVariables.AddRange(issues.MissingVariables);
                addedVariables.AddRange(issues.MissingSourceVariables);

                if (addedVariables.Count > 0)
                {
                    Console.WriteLine($"Checking {addedVariables.Count} variable(s) that should have been added...");
                    DebugLog($"Variables to verify: {string.Join(", ", addedVariables)}");

                    var verifiedVarNames = new HashSet<string>(verifyTriggers.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
                    var stillMissing = new List<string>();
                    var foundVars = new List<string>();

                    foreach (var varName in addedVariables)
                    {
                        if (verifiedVarNames.Contains(varName))
                        {
                            foundVars.Add(varName);
                            DebugLog($"  [FOUND] Variable '{varName}' is present in fixed file");
                        }
                        else
                        {
                            stillMissing.Add(varName);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"  [MISSING] Variable '{varName}' is STILL MISSING from fixed file!");
                            Console.ResetColor();
                            DebugLog($"  [ERROR] Variable '{varName}' was added but not found in fixed file!");
                        }
                    }

                    if (stillMissing.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  [OK] All {addedVariables.Count} added variable(s) are present in fixed file!");
                        Console.ResetColor();
                        DebugLog($"Successfully verified: {string.Join(", ", foundVars)}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  [ERROR] {stillMissing.Count} variable(s) are STILL MISSING!");
                        Console.WriteLine($"  Missing: {string.Join(", ", stillMissing)}");
                        Console.ResetColor();
                        stillHasIssues = true;
                    }

                    // Dump all variables to log for debugging
                    if (DEBUG_MODE)
                    {
                        DebugLog("=== COMPLETE VARIABLE LIST IN FIXED FILE ===");
                        foreach (var v in verifyTriggers.Variables.OrderBy(v => v.Id))
                        {
                            DebugLog($"  Variable[{v.Id}]: {v.Name} ({v.Type}, ParentId={v.ParentId})");
                        }
                        DebugLog("=== END OF VARIABLE LIST ===");
                    }
                }

                if (!stillHasIssues)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [OK] All issues fixed successfully!");
                    Console.WriteLine($"  [OK] Variables: {verifyTriggers.Variables.Count}");
                    Console.WriteLine($"  [OK] Trigger items: {verifyTriggers.TriggerItems.Count}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [WARNING] Some issues remain ({verifyIssues.TotalIssues()})");
                    Console.ResetColor();
                }

                Console.WriteLine($"\n  [OK] Fixed file saved: {Path.GetFullPath(outputPath)}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Fatal Error: {ex.Message}");

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                }
                else
                {
                    Console.WriteLine("\n(Set DEBUG_MODE=true for full stack trace)");
                }

                Console.ResetColor();

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();

                // Set exit code to indicate failure
                // More graceful than Environment.Exit(1) as it allows cleanup
                Environment.ExitCode = 1;
                return;
            }
        }

        #region Validation Checks

        static bool CheckSubVersion(MapTriggers triggers, ValidationIssues issues)
        {
            Console.WriteLine("Checking SubVersion...");
            if (triggers.SubVersion == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  [WARNING] SubVersion is null - ParentId won't be saved!");
                Console.ResetColor();
                issues.SubVersionIssues = 1;
                return true;
            }

            Console.WriteLine($"  [OK] SubVersion: {triggers.SubVersion}");
            return false;
        }

        static bool CheckMissingVariables(MapTriggers merged, MapTriggers original, ValidationIssues issues, string label)
        {
            Console.WriteLine($"\nChecking for missing variables (from {label})...");

            var mergedVarNames = new HashSet<string>(merged.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
            var originalVarNames = new HashSet<string>(original.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

            var missing = originalVarNames.Except(mergedVarNames).ToList();

            if (missing.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  [WARNING] {missing.Count} variable(s) from {label} are missing:");

                // CRITICAL FIX: Add ALL missing variables to the issues list, not just the first 10!
                foreach (var varName in missing)
                {
                    issues.MissingVariables.Add(varName);
                    DebugLog($"  Added to missing list: {varName}");
                }

                // Display only first 10 for readability
                foreach (var varName in missing.Take(10))
                {
                    var origVar = original.Variables.First(v => v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                    Console.WriteLine($"    - {varName} ({origVar.Type})");
                }
                if (missing.Count > 10)
                    Console.WriteLine($"    ... and {missing.Count - 10} more");
                Console.ResetColor();
                return true;
            }

            Console.WriteLine($"  [OK] All {label} variables present");
            return false;
        }

        static bool CheckMissingVariablesFromSource(MapTriggers merged, MapTriggers source, ValidationIssues issues)
        {
            Console.WriteLine("\nChecking for missing variables from added triggers (source)...");

            var mergedVarNames = new HashSet<string>(merged.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
            var sourceVarNames = new HashSet<string>(source.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

            // Find variables in source that aren't in merged
            var missing = sourceVarNames.Except(mergedVarNames).ToList();

            if (missing.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  [WARNING] {missing.Count} variable(s) from source triggers are missing:");

                // CRITICAL FIX: Add ALL missing variables to the issues list, not just the first 10!
                foreach (var varName in missing)
                {
                    issues.MissingSourceVariables.Add(varName);
                    DebugLog($"  Added to missing source list: {varName}");
                }

                // Display only first 10 for readability
                foreach (var varName in missing.Take(10))
                {
                    var sourceVar = source.Variables.First(v => v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                    Console.WriteLine($"    - {varName} ({sourceVar.Type})");
                }
                if (missing.Count > 10)
                    Console.WriteLine($"    ... and {missing.Count - 10} more");
                Console.ResetColor();
                return true;
            }

            Console.WriteLine("  [OK] All source variables present");
            return false;
        }

        static bool CheckUndefinedVariables(MapTriggers triggers, ValidationIssues issues)
        {
            Console.WriteLine("\nChecking for undefined variables in triggers...");

            var definedVars = new HashSet<string>(triggers.Variables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
            var usedVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Scan all triggers for variable usage
            foreach (var trigger in triggers.TriggerItems.OfType<TriggerDefinition>())
            {
                var varsInTrigger = GetVariablesUsedByTrigger(trigger);
                foreach (var varName in varsInTrigger)
                {
                    usedVars.Add(varName);
                }
            }

            var undefined = usedVars.Except(definedVars).ToList();

            if (undefined.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  [WARNING] {undefined.Count} undefined variable(s) used in triggers:");

                // Add ALL undefined variables to the issues list
                foreach (var varName in undefined)
                {
                    issues.UndefinedVariables.Add(varName);
                }

                // Display only first 10 for readability
                foreach (var varName in undefined.Take(10))
                {
                    Console.WriteLine($"    - {varName}");
                }
                if (undefined.Count > 10)
                    Console.WriteLine($"    ... and {undefined.Count - 10} more");
                Console.ResetColor();
                return true;
            }

            Console.WriteLine("  [OK] All used variables are defined");
            return false;
        }

        static bool CheckWrongParentIds(MapTriggers triggers, ValidationIssues issues)
        {
            Console.WriteLine("\nChecking ParentId values...");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var categoryIds = new HashSet<int>(categories.Select(c => c.Id));

            // Check categories - should be -1 for root level
            var wrongCategories = categories.Where(c => c.ParentId >= 0).ToList();

            if (wrongCategories.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  [WARNING] {wrongCategories.Count} categor(ies) with ParentId >= 0 (should be -1 for root):");
                foreach (var cat in wrongCategories.Take(5))
                {
                    Console.WriteLine($"    - '{cat.Name}' (ParentId={cat.ParentId})");
                    issues.WrongParentIds.Add(cat.Name);
                }
                if (wrongCategories.Count > 5)
                    Console.WriteLine($"    ... and {wrongCategories.Count - 5} more");
                Console.ResetColor();
                return true;
            }

            Console.WriteLine("  [OK] All categories have correct ParentId");
            return false;
        }

        static bool CheckOrphaned(MapTriggers triggers, ValidationIssues issues)
        {
            Console.WriteLine("\nChecking for orphaned items...");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var categoryIds = new HashSet<int>(categories.Select(c => c.Id));

            // Check orphaned triggers (ParentId doesn't match any category)
            var orphanedTriggers = triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId >= 0 && !categoryIds.Contains(t.ParentId))
                .ToList();

            if (orphanedTriggers.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  [WARNING] {orphanedTriggers.Count} orphaned trigger(s) (ParentId points to non-existent category):");
                foreach (var trigger in orphanedTriggers.Take(5))
                {
                    Console.WriteLine($"    - '{trigger.Name}' (ParentId={trigger.ParentId})");
                    issues.OrphanedItems.Add(trigger.Name);
                }
                if (orphanedTriggers.Count > 5)
                    Console.WriteLine($"    ... and {orphanedTriggers.Count - 5} more");
                Console.ResetColor();
                return true;
            }

            Console.WriteLine("  [OK] No orphaned items");
            return false;
        }

        static bool CheckDuplicateIds(MapTriggers triggers, ValidationIssues issues)
        {
            Console.WriteLine("\nChecking for duplicate IDs...");

            var duplicates = triggers.TriggerItems
                .GroupBy(item => item.Id)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  [WARNING] {duplicates.Count} duplicate ID(s) found:");
                foreach (var group in duplicates.Take(5))
                {
                    Console.WriteLine($"    - ID {group.Key}: {string.Join(", ", group.Select(i => i.Name))}");
                    issues.DuplicateIds.Add(group.Key);
                }
                if (duplicates.Count > 5)
                    Console.WriteLine($"    ... and {duplicates.Count - 5} more");
                Console.ResetColor();
                return true;
            }

            Console.WriteLine("  [OK] No duplicate IDs");
            return false;
        }

        #endregion

        #region Fix Application

        static void ApplyFixes(MapTriggers merged, MapTriggers original, MapTriggers? source, ValidationIssues issues)
        {
            int fixCount = 0;

            // Fix 1: Set SubVersion
            if (issues.SubVersionIssues > 0)
            {
                Console.WriteLine("Setting SubVersion to v4...");
                merged.SubVersion = MapTriggersSubVersion.v4;
                Console.WriteLine("  [OK] SubVersion set");
                fixCount++;
            }

            // Fix 2: Copy missing variables from original
            if (issues.MissingVariables.Count > 0)
            {
                Console.WriteLine($"\nCopying {issues.MissingVariables.Count} missing variable(s) from original...");
                DebugLog($"Variable count BEFORE copying: {merged.Variables.Count}");
                DebugLogVariables("BEFORE copying from original", merged);

                var originalVarDict = original.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

                foreach (var varName in issues.MissingVariables)
                {
                    DebugLog($"Attempting to copy variable: {varName}");
                    if (originalVarDict.TryGetValue(varName, out var origVar))
                    {
                        var newVar = new VariableDefinition
                        {
                            Name = origVar.Name,
                            Type = origVar.Type,
                            Unk = origVar.Unk,
                            IsArray = origVar.IsArray,
                            ArraySize = origVar.ArraySize,
                            IsInitialized = origVar.IsInitialized,
                            InitialValue = origVar.InitialValue,
                            Id = merged.Variables.Count,
                            // CRITICAL FIX: Always use -1 for ParentId (root level)
                            // Never inherit ParentId from source - those indices don't exist in merged map
                            // This prevents WE 1.27 from silently dropping variables with foreign ParentIds
                            ParentId = -1
                        };
                        merged.Variables.Add(newVar);
                        DebugLog($"  Added variable {varName} with Id={newVar.Id}, Type={newVar.Type}, ParentId={newVar.ParentId}");
                        Console.WriteLine($"  + {varName} ({origVar.Type})");
                        fixCount++;
                    }
                    else
                    {
                        DebugLog($"  ERROR: Variable {varName} not found in original dictionary!");
                    }
                }

                DebugLog($"Variable count AFTER copying: {merged.Variables.Count}");
                DebugLogVariables("AFTER copying from original", merged);
                Console.WriteLine($"  [OK] Copied {issues.MissingVariables.Count} variable(s)");
            }

            // Fix 3: Copy missing variables from source
            if (source != null && issues.MissingSourceVariables.Count > 0)
            {
                Console.WriteLine($"\nCopying {issues.MissingSourceVariables.Count} missing variable(s) from source...");
                DebugLog($"Variable count BEFORE copying from source: {merged.Variables.Count}");

                var sourceVarDict = source.Variables.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

                foreach (var varName in issues.MissingSourceVariables)
                {
                    DebugLog($"Attempting to copy variable from source: {varName}");
                    if (sourceVarDict.TryGetValue(varName, out var sourceVar))
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
                            Id = merged.Variables.Count,
                            // CRITICAL FIX: Always use -1 for ParentId (root level)
                            // Never inherit ParentId from source - those indices don't exist in merged map
                            // This prevents WE 1.27 from silently dropping variables with foreign ParentIds
                            ParentId = -1
                        };
                        merged.Variables.Add(newVar);
                        DebugLog($"  Added variable {varName} with Id={newVar.Id}, Type={newVar.Type}, ParentId={newVar.ParentId}");
                        Console.WriteLine($"  + {varName} ({sourceVar.Type})");
                        fixCount++;
                    }
                    else
                    {
                        DebugLog($"  ERROR: Variable {varName} not found in source dictionary!");
                    }
                }

                DebugLog($"Variable count AFTER copying from source: {merged.Variables.Count}");
                DebugLogVariables("AFTER copying from source", merged);
                Console.WriteLine($"  [OK] Copied {issues.MissingSourceVariables.Count} variable(s)");
            }

            // Fix 4: Fix wrong ParentIds (set categories to -1)
            if (issues.WrongParentIds.Count > 0)
            {
                Console.WriteLine($"\nFixing {issues.WrongParentIds.Count} categor(ies) with wrong ParentId...");
                var categories = merged.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();

                foreach (var cat in categories.Where(c => c.ParentId >= 0))
                {
                    Console.WriteLine($"  - '{cat.Name}': {cat.ParentId} → -1");
                    cat.ParentId = -1;
                    fixCount++;
                }
                Console.WriteLine("  [OK] All categories set to root level");
            }

            // Fix 4: Fix orphaned triggers (assign to first available category or create one)
            if (issues.OrphanedItems.Count > 0)
            {
                Console.WriteLine($"\nFixing {issues.OrphanedItems.Count} orphaned trigger(s)...");
                var categories = merged.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();

                TriggerCategoryDefinition? targetCategory = categories.FirstOrDefault();
                if (targetCategory == null)
                {
                    // Create a default category
                    targetCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
                    {
                        Id = GetNextId(merged),
                        ParentId = -1,
                        Name = "Recovered Triggers",
                        IsComment = false,
                        IsExpanded = true
                    };
                    merged.TriggerItems.Insert(0, targetCategory);
                    Console.WriteLine($"  + Created category '{targetCategory.Name}' (ID={targetCategory.Id})");
                }

                var orphanedTriggers = merged.TriggerItems
                    .OfType<TriggerDefinition>()
                    .Where(t => t.ParentId >= 0 && !categories.Any(c => c.Id == t.ParentId))
                    .ToList();

                foreach (var trigger in orphanedTriggers)
                {
                    Console.WriteLine($"  - '{trigger.Name}': ParentId {trigger.ParentId} → {targetCategory.Id}");
                    trigger.ParentId = targetCategory.Id;
                    fixCount++;
                }
                Console.WriteLine("  [OK] Orphaned triggers assigned to category");
            }

            // Fix 5: Fix duplicate IDs
            if (issues.DuplicateIds.Count > 0)
            {
                Console.WriteLine($"\nFixing {issues.DuplicateIds.Count} duplicate ID(s)...");
                Console.WriteLine("  Reassigning sequential IDs to all items...");

                for (int i = 0; i < merged.TriggerItems.Count; i++)
                {
                    merged.TriggerItems[i].Id = i;
                }

                // Update ParentIds to match new IDs
                var categories = merged.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                foreach (var trigger in merged.TriggerItems.OfType<TriggerDefinition>())
                {
                    if (trigger.ParentId >= 0)
                    {
                        var parentCat = categories.FirstOrDefault(c => c.Name ==
                            categories.FirstOrDefault(old => old.Id == trigger.ParentId)?.Name);
                        if (parentCat != null)
                        {
                            trigger.ParentId = merged.TriggerItems.IndexOf(parentCat);
                        }
                    }
                }

                Console.WriteLine("  [OK] IDs reassigned");
                fixCount++;
            }

            // Update trigger item counts
            UpdateTriggerItemCounts(merged);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[OK] Applied {fixCount} fix(es)");
            Console.ResetColor();
        }

        #endregion

        #region Helper Methods

        static void DebugLog(string message)
        {
            if (DEBUG_MODE)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[DEBUG] {message}");
                Console.ResetColor();
            }
        }

        static void DebugLogVariables(string label, MapTriggers triggers)
        {
            if (!DEBUG_MODE) return;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[DEBUG] {label} - Variable count: {triggers.Variables.Count}");
            if (triggers.Variables.Count > 0)
            {
                Console.WriteLine($"[DEBUG]   First 10 variables:");
                foreach (var v in triggers.Variables.Take(10))
                {
                    Console.WriteLine($"[DEBUG]     - {v.Name} ({v.Type}, Id={v.Id})");
                }
            }
            Console.ResetColor();
        }

        static HashSet<string> GetVariablesUsedByTrigger(TriggerDefinition trigger)
        {
            var usedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var function in trigger.Functions)
            {
                CollectVariablesFromFunction(function, usedVariables);
            }

            return usedVariables;
        }

        static void CollectVariablesFromFunction(TriggerFunction function, HashSet<string> usedVariables)
        {
            foreach (var param in function.Parameters)
            {
                if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrWhiteSpace(param.Value))
                {
                    usedVariables.Add(param.Value);
                }

                if (param.Function != null)
                {
                    CollectVariablesFromFunction(param.Function, usedVariables);
                }

                if (param.ArrayIndexer != null)
                {
                    CollectVariablesFromParameter(param.ArrayIndexer, usedVariables);
                }
            }

            foreach (var childFunc in function.ChildFunctions)
            {
                CollectVariablesFromFunction(childFunc, usedVariables);
            }
        }

        static void CollectVariablesFromParameter(TriggerFunctionParameter param, HashSet<string> usedVariables)
        {
            if (param.Type == TriggerFunctionParameterType.Variable && !string.IsNullOrWhiteSpace(param.Value))
            {
                usedVariables.Add(param.Value);
            }

            if (param.Function != null)
            {
                CollectVariablesFromFunction(param.Function, usedVariables);
            }

            if (param.ArrayIndexer != null)
            {
                CollectVariablesFromParameter(param.ArrayIndexer, usedVariables);
            }
        }

        static int GetNextId(MapTriggers triggers)
        {
            if (triggers.TriggerItems == null || triggers.TriggerItems.Count == 0)
                return 0;

            return triggers.TriggerItems.Max(item => item.Id) + 1;
        }

        static void UpdateTriggerItemCounts(MapTriggers triggers)
        {
            triggers.TriggerItemCounts.Clear();

            foreach (var item in triggers.TriggerItems)
            {
                if (triggers.TriggerItemCounts.ContainsKey(item.Type))
                    triggers.TriggerItemCounts[item.Type]++;
                else
                    triggers.TriggerItemCounts[item.Type] = 1;
            }
        }

        static bool IsMapArchive(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".w3x" || ext == ".w3m";
        }

        static string AutoDetectMapFile(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            var w3xFiles = Directory.GetFiles(folderPath, "*.w3x");
            if (w3xFiles.Length > 0)
                return w3xFiles[0];

            var w3mFiles = Directory.GetFiles(folderPath, "*.w3m");
            if (w3mFiles.Length > 0)
                return w3mFiles[0];

            var wtgPath = Path.Combine(folderPath, "war3map.wtg");
            if (File.Exists(wtgPath))
                return wtgPath;

            throw new FileNotFoundException($"No map files found in {folderPath}");
        }

        static string AutoDetectMergedFile(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            // Look for merged files first (war3map_merged.*)
            var mergedW3x = Path.Combine(folderPath, "war3map_merged.w3x");
            if (File.Exists(mergedW3x))
                return mergedW3x;

            var mergedW3m = Path.Combine(folderPath, "war3map_merged.w3m");
            if (File.Exists(mergedW3m))
                return mergedW3m;

            var mergedWtg = Path.Combine(folderPath, "war3map_merged.wtg");
            if (File.Exists(mergedWtg))
                return mergedWtg;

            // Fall back to any pattern matching *_merged.*
            var mergedFiles = Directory.GetFiles(folderPath, "*_merged.w3x")
                .Concat(Directory.GetFiles(folderPath, "*_merged.w3m"))
                .Concat(Directory.GetFiles(folderPath, "*_merged.wtg"))
                .ToList();

            if (mergedFiles.Count > 0)
                return mergedFiles[0];

            throw new FileNotFoundException($"No merged file found in {folderPath}. Looking for war3map_merged.* or *_merged.*");
        }

        static string AutoDetectOriginalFile(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            // Look for original war3map.* files (not merged)
            var w3xPath = Path.Combine(folderPath, "war3map.w3x");
            if (File.Exists(w3xPath))
                return w3xPath;

            var w3mPath = Path.Combine(folderPath, "war3map.w3m");
            if (File.Exists(w3mPath))
                return w3mPath;

            var wtgPath = Path.Combine(folderPath, "war3map.wtg");
            if (File.Exists(wtgPath))
                return wtgPath;

            throw new FileNotFoundException($"No original file found in {folderPath}. Looking for war3map.* (not *_merged.*)");
        }

        static string GenerateOutputPath(string inputPath)
        {
            var dir = Path.GetDirectoryName(inputPath) ?? ".";
            var name = Path.GetFileNameWithoutExtension(inputPath);
            var ext = Path.GetExtension(inputPath);
            return Path.Combine(dir, $"{name}_fixed{ext}");
        }

        static MapTriggers ReadMapTriggersAuto(string filePath)
        {
            if (IsMapArchive(filePath))
                return ReadMapArchive(filePath);
            else
                return ReadWTGFile(filePath);
        }

        static MapTriggers ReadWTGFile(string filePath)
        {
            DebugLog($"ReadWTGFile: Opening file {filePath}");
            DebugLog($"ReadWTGFile: File exists = {File.Exists(filePath)}, Size = {new FileInfo(filePath).Length} bytes");

            using var fileStream = File.OpenRead(filePath);
            using var reader = new BinaryReader(fileStream);

            // Use public War3Net extension method instead of reflection
            var result = reader.ReadMapTriggers();
            DebugLog($"ReadWTGFile: Loaded {result.Variables.Count} variables, {result.TriggerItems.Count} trigger items");
            return result;
        }

        static MapTriggers ReadMapArchive(string archivePath)
        {
            using var archive = MpqArchive.Open(archivePath, true);
            archive.DiscoverFileNames();

            var triggerFileName = MapTriggers.FileName;
            if (!archive.FileExists(triggerFileName))
                throw new FileNotFoundException($"'{triggerFileName}' not found in map archive");

            using var triggerStream = archive.OpenFile(triggerFileName);
            using var reader = new BinaryReader(triggerStream);

            // Use public War3Net extension method instead of reflection
            return reader.ReadMapTriggers();
        }

        static void WriteWTGFile(string filePath, MapTriggers triggers)
        {
            DebugLog($"WriteWTGFile: About to write to {filePath}");
            DebugLog($"WriteWTGFile: Writing {triggers.Variables.Count} variables, {triggers.TriggerItems.Count} trigger items");
            DebugLog($"WriteWTGFile: SubVersion = {triggers.SubVersion}");

            using var fileStream = File.Create(filePath);
            using var writer = new BinaryWriter(fileStream);

            // Use public War3Net extension method instead of reflection
            DebugLog($"WriteWTGFile: Writing MapTriggers...");
            writer.Write(triggers);

            writer.Flush();
            fileStream.Flush();

            DebugLog($"WriteWTGFile: Write complete. File size = {new FileInfo(filePath).Length} bytes");
        }

        static void WriteMapArchive(string originalArchivePath, string outputArchivePath, MapTriggers triggers)
        {
            DebugLog($"WriteMapArchive: Source archive = {originalArchivePath}");
            DebugLog($"WriteMapArchive: Output archive = {outputArchivePath}");
            DebugLog($"WriteMapArchive: Writing {triggers.Variables.Count} variables, {triggers.TriggerItems.Count} trigger items");
            DebugLog($"WriteMapArchive: SubVersion = {triggers.SubVersion}");

            using var originalArchive = MpqArchive.Open(originalArchivePath, true);
            originalArchive.DiscoverFileNames();

            var builder = new MpqArchiveBuilder(originalArchive);

            // Serialize triggers to byte array to ensure data persists beyond stream disposal
            byte[] triggerData;
            using (var triggerStream = new MemoryStream())
            using (var writer = new BinaryWriter(triggerStream))
            {
                // Use public War3Net extension method instead of reflection
                DebugLog($"WriteMapArchive: Writing MapTriggers...");
                writer.Write(triggers);
                writer.Flush();

                triggerData = triggerStream.ToArray();
                DebugLog($"WriteMapArchive: Trigger data size = {triggerData.Length} bytes");
            }

            // Create MpqFile from byte array (data is now safely copied)
            var triggerFileName = MapTriggers.FileName;
            builder.RemoveFile(triggerFileName);

            using (var dataStream = new MemoryStream(triggerData))
            {
                builder.AddFile(MpqFile.New(dataStream, triggerFileName));
            }

            DebugLog($"WriteMapArchive: Building archive...");
            builder.SaveTo(outputArchivePath);
            DebugLog($"WriteMapArchive: Archive saved successfully");
        }

        #endregion
    }

    class ValidationIssues
    {
        public int SubVersionIssues { get; set; }
        public List<string> MissingVariables { get; set; } = new();
        public List<string> MissingSourceVariables { get; set; } = new();
        public List<string> UndefinedVariables { get; set; } = new();
        public List<string> WrongParentIds { get; set; } = new();
        public List<string> OrphanedItems { get; set; } = new();
        public List<int> DuplicateIds { get; set; } = new();

        public int TotalIssues() =>
            SubVersionIssues +
            MissingVariables.Count +
            MissingSourceVariables.Count +
            UndefinedVariables.Count +
            WrongParentIds.Count +
            OrphanedItems.Count +
            DuplicateIds.Count;
    }
}
