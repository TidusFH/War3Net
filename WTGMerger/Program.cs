using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using War3Net.Build.Script;
using War3Net.IO.Mpq;
using War3Net.Build.Extensions;
using War3Net.Common.Extensions;

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
                        var targetDir = Path.GetDirectoryName(targetPath);

                        if (string.IsNullOrEmpty(targetDir))
                        {
                            // If no directory component, use current directory
                            targetDir = Directory.GetCurrentDirectory();
                        }

                        outputPath = Path.Combine(targetDir, $"{targetFileName}_merged{targetExt}");
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
                    Console.WriteLine("6. Fix all TARGET categories to root-level (ParentId = -1)");
                    Console.WriteLine("7. DEBUG: Show comprehensive debug information");
                    Console.WriteLine($"8. DEBUG: Toggle debug mode (currently: {(DEBUG_MODE ? "ON" : "OFF")})");
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
                            ShowComprehensiveDebugInfo(sourceTriggers, targetTriggers);
                            break;

                        case "8":
                            DEBUG_MODE = !DEBUG_MODE;
                            Console.ForegroundColor = DEBUG_MODE ? ConsoleColor.Yellow : ConsoleColor.Green;
                            Console.WriteLine($"\n✓ Debug mode is now {(DEBUG_MODE ? "ON" : "OFF")}");
                            Console.ResetColor();
                            break;

                        case "9":
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

                                // CRITICAL: Set SubVersion if null to enable ParentId writing
                                if (targetTriggers.SubVersion == null)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("\n⚠ WARNING: Target map has SubVersion=null, ParentId won't be saved!");
                                    Console.WriteLine("   Setting SubVersion=v4 to enable ParentId support...");
                                    Console.ResetColor();
                                    targetTriggers.SubVersion = MapTriggersSubVersion.v4;
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("   ✓ SubVersion set to v4");
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

                                    // Check categories
                                    var verifyCats = verifyTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                                    var verifyRoot = verifyCats.Count(c => c.ParentId == -1);
                                    var verifyNested = verifyCats.Count(c => c.ParentId >= 0);

                                    Console.WriteLine($"\nCategories:");
                                    Console.WriteLine($"  Root-level (ParentId=-1): {verifyRoot}");
                                    Console.WriteLine($"  Nested (ParentId>=0): {verifyNested}");

                                    if (verifyNested > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("\n❌ ERROR: ParentIds were NOT saved correctly!");
                                        Console.WriteLine("The saved file still has nested categories:");
                                        foreach (var cat in verifyCats.Where(c => c.ParentId >= 0).Take(5))
                                        {
                                            Console.WriteLine($"  '{cat.Name}': ParentId={cat.ParentId}");
                                        }
                                        Console.WriteLine("\n⚠ This means the ParentId field is NOT being written to disk.");
                                        Console.WriteLine("⚠ The issue is in the WriteTo method or file format.");
                                        Console.ResetColor();
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("✓ ParentIds were saved correctly!");
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
                Console.WriteLine($"\n❌ Fatal Error: {ex.Message}");

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                }
                else
                {
                    Console.WriteLine("\n(Run with debug mode enabled for full stack trace)");
                }

                Console.ResetColor();

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();

                // Set exit code to indicate failure
                // This is more graceful than Environment.Exit(1) as it allows cleanup
                Environment.ExitCode = 1;
                return;
            }
        }

        /// <summary>
        /// Reads a raw WTG file and returns MapTriggers object
        /// Uses public War3Net extension method
        /// </summary>
        static MapTriggers ReadWTGFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"WTG file not found: {filePath}");
            }

            using var fileStream = File.OpenRead(filePath);
            using var reader = new BinaryReader(fileStream);

            // Use public War3Net extension method
            return reader.ReadMapTriggers();
        }

        /// <summary>
        /// Runs wc3libs WTGBridge to copy/re-serialize a WTG file.
        /// This ensures the output uses wc3libs' stable serialization.
        /// </summary>
        static void RunWc3libsCopy(string inputPath, string outputPath)
        {
            string bridgeScript = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                "..", "WTGBridge", "run.sh"
            );

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"\"{bridgeScript}\" copy \"{Path.GetFullPath(inputPath)}\" \"{Path.GetFullPath(outputPath)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] RunWc3libsCopy: Executing: {startInfo.FileName} {startInfo.Arguments}");
            }

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start wc3libs WTGBridge process");
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (DEBUG_MODE && !string.IsNullOrEmpty(output))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[DEBUG] wc3libs output:");
                    Console.WriteLine(output);
                    Console.ResetColor();
                }

                if (process.ExitCode != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] wc3libs WTGBridge failed with exit code {process.ExitCode}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("Error output:");
                        Console.WriteLine(error);
                    }
                    Console.ResetColor();
                    throw new Exception($"wc3libs WTGBridge failed: {error}");
                }

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] RunWc3libsCopy: wc3libs completed successfully");
                }
            }
        }

        /// <summary>
        /// Writes WTG file using wc3libs for stable serialization.
        /// Uses War3Net to write temp file, then wc3libs to re-serialize it correctly.
        /// </summary>
        static void WriteWTGFile(string filePath, MapTriggers triggers)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] WriteWTGFile: Writing to {filePath}");
                Console.WriteLine($"[DEBUG]   Variables to write: {triggers.Variables.Count}");
                Console.WriteLine($"[DEBUG]   Trigger items to write: {triggers.TriggerItems.Count}");
            }

            // Step 1: Write using War3Net to a temporary file
            string tempFile = filePath + ".war3net.tmp";
            try
            {
                using (var fileStream = File.Create(tempFile))
                using (var writer = new BinaryWriter(fileStream))
                {
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[DEBUG] WriteWTGFile: Writing temporary file with War3Net...");
                    }
                    writer.Write(triggers);
                }

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] WriteWTGFile: Temp file written: {new FileInfo(tempFile).Length} bytes");
                }

                // Step 2: Re-serialize using wc3libs for stable output
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] WriteWTGFile: Re-serializing with wc3libs for stable output...");
                }
                RunWc3libsCopy(tempFile, filePath);

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] WriteWTGFile: Completed. File size: {new FileInfo(filePath).Length} bytes");
                }
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[DEBUG] WriteWTGFile: Cleaned up temp file");
                    }
                }
            }
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

            // Copy missing variables from source to target before copying triggers
            CopyMissingVariables(source, target, triggersToCopy);

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

            // Copy missing variables from source to target before copying triggers
            CopyMissingVariables(source, target, sourceCategoryTriggers);

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
                            // CRITICAL FIX: Always use -1 for ParentId (root level)
                            // Never inherit ParentId from source - those indices don't exist in target map
                            // This prevents WE 1.27 from silently dropping variables with foreign ParentIds
                            ParentId = -1
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
                        // CRITICAL FIX: Always use -1 for ParentId (root level)
                        // Never inherit ParentId from source - those indices don't exist in target map
                        // This prevents WE 1.27 from silently dropping variables with foreign ParentIds
                        ParentId = -1
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
                triggers.TriggerItems[i].Id = i;
            }

            // Update ParentIds in ALL items (both categories and triggers)
            // Note: After reassignment above, each item's ID equals its index in the list
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
                if (w3xFiles.Length > 1)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ WARNING: Found {w3xFiles.Length} .w3x files in {folderPath}");
                    Console.WriteLine($"  Using first one: {Path.GetFileName(w3xFiles[0])}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  Detected: {Path.GetFileName(w3xFiles[0])} (WC3 map archive)");
                }
                return w3xFiles[0];
            }

            // Priority 2: Look for .w3m files
            var w3mFiles = Directory.GetFiles(folderPath, "*.w3m");
            if (w3mFiles.Length > 0)
            {
                if (w3mFiles.Length > 1)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ WARNING: Found {w3mFiles.Length} .w3m files in {folderPath}");
                    Console.WriteLine($"  Using first one: {Path.GetFileName(w3mFiles[0])}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  Detected: {Path.GetFileName(w3mFiles[0])} (WC3 campaign archive)");
                }
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

            // Use public War3Net extension method
            return reader.ReadMapTriggers();
        }

        /// <summary>
        /// Writes MapTriggers to a map archive, optionally removing war3map.j
        /// </summary>
        static void WriteMapArchive(string originalArchivePath, string outputArchivePath, MapTriggers triggers, bool removeJassFile)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[DEBUG] WriteMapArchive: Writing to {outputArchivePath}");
                Console.WriteLine($"[DEBUG]   Variables to write: {triggers.Variables.Count}");
                Console.WriteLine($"[DEBUG]   Trigger items to write: {triggers.TriggerItems.Count}");
            }

            Console.WriteLine($"  Opening original archive...");
            using var originalArchive = MpqArchive.Open(originalArchivePath, true);
            originalArchive.DiscoverFileNames();

            Console.WriteLine($"  Creating archive builder...");
            var builder = new MpqArchiveBuilder(originalArchive);

            // Serialize triggers to byte array to ensure data persists beyond stream disposal
            byte[] triggerData;
            using (var triggerStream = new MemoryStream())
            using (var writer = new BinaryWriter(triggerStream))
            {
                // Use public War3Net extension method
                writer.Write(triggers);
                writer.Flush();

                triggerData = triggerStream.ToArray();

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] Serialized war3map.wtg to memory: {triggerData.Length} bytes");
                }
            }

            // Remove old trigger file and add new one
            var triggerFileName = MapTriggers.FileName; // "war3map.wtg"
            Console.WriteLine($"  Replacing {triggerFileName}...");
            builder.RemoveFile(triggerFileName);

            // Create MpqFile from byte array (data is now safely copied)
            using (var dataStream = new MemoryStream(triggerData))
            {
                builder.AddFile(MpqFile.New(dataStream, triggerFileName));
            }

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
