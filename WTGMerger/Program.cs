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

                // CRITICAL: Store original format version to preserve it
                var originalFormatVersion = targetTriggers.FormatVersion;
                var originalSubVersion = targetTriggers.SubVersion;
                var originalGameVersion = targetTriggers.GameVersion;

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] Original target format:");
                    Console.WriteLine($"[DEBUG]   FormatVersion: {originalFormatVersion}");
                    Console.WriteLine($"[DEBUG]   SubVersion: {originalSubVersion?.ToString() ?? "null"}");
                    Console.WriteLine($"[DEBUG]   GameVersion: {originalGameVersion}");
                }

                // Auto-adjust output path based on target type
                if (IsMapArchive(targetPath) && !IsMapArchive(outputPath))
                {
                    // If target is .w3x but output is .wtg, change output to .w3x
                    outputPath = Path.ChangeExtension(outputPath, Path.GetExtension(targetPath));
                    Console.WriteLine($"\n⚠ Output adjusted to match target type: {outputPath}");
                }

                // CRITICAL: Fix category IDs IMMEDIATELY after load for old format
                // This must run BEFORE any other operations
                FixCategoryIdsForOldFormat(sourceTriggers, "source");
                FixCategoryIdsForOldFormat(targetTriggers, "target");

                // TRACE: Show state after load fixes
                if (DEBUG_MODE)
                {
                    Console.WriteLine("\n" + new string('═', 70));
                    Console.WriteLine("TRACE: STATE AFTER LOAD FIXES (Before any user operations)");
                    Console.WriteLine(new string('═', 70));
                    TraceCategoryStructure(targetTriggers, "TARGET");
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
                    Console.WriteLine("5. Copy SPECIFIC trigger(s) [AUTO-FIXES categories]");
                    Console.WriteLine("6. Manual: Fix all TARGET categories (version-aware)");
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
                            Console.WriteLine("║          FIX CATEGORY NESTING (VERSION-AWARE)           ║");
                            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

                            if (targetTriggers.SubVersion == null)
                            {
                                Console.WriteLine("\nDetected OLD format (SubVersion=null)");
                                Console.WriteLine("This will set all categories to ParentId=0 (old format standard).");
                            }
                            else
                            {
                                Console.WriteLine("\nDetected ENHANCED format (SubVersion=v4)");
                                Console.WriteLine("This will set all categories to ParentId=-1 (root level).");
                            }

                            Console.WriteLine("Use this if your categories are incorrectly nested.");
                            Console.Write("\nProceed? (y/n): ");
                            string? confirmFix = Console.ReadLine();
                            if (confirmFix?.ToLower() == "y")
                            {
                                AutoFixCategoriesForFormat(targetTriggers);

                                // Verify the fix worked
                                Console.WriteLine("\n=== Verification ===");
                                var categories = targetTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                                var rootCount = categories.Count(c => c.ParentId == -1);
                                var zeroCount = categories.Count(c => c.ParentId == 0);
                                var nestedCount = categories.Count(c => c.ParentId > 0);
                                Console.WriteLine($"Categories with ParentId=-1: {rootCount}");
                                Console.WriteLine($"Categories with ParentId=0: {zeroCount}");
                                Console.WriteLine($"Categories with ParentId>0: {nestedCount}");

                                if (targetTriggers.SubVersion == null && rootCount > 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("⚠ Note: Old format doesn't support ParentId=-1");
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

                                // CRITICAL: Restore original format EXACTLY as-is
                                // DO NOT change SubVersion! This breaks World Editor/BetterTriggers compatibility
                                targetTriggers.FormatVersion = originalFormatVersion;
                                targetTriggers.SubVersion = originalSubVersion;
                                targetTriggers.GameVersion = originalGameVersion;

                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"\n✓ Preserving original file format:");
                                Console.WriteLine($"  FormatVersion: {targetTriggers.FormatVersion}");
                                Console.WriteLine($"  SubVersion: {targetTriggers.SubVersion?.ToString() ?? "null"}");
                                Console.WriteLine($"  GameVersion: {targetTriggers.GameVersion}");
                                Console.ResetColor();

                                if (originalSubVersion == null)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"\n⚠ NOTE: Original file uses OLD format (SubVersion=null)");
                                    Console.WriteLine($"   Old format has some limitations:");
                                    Console.WriteLine($"   - All variables may show ID=0 (this is normal for old format)");
                                    Console.WriteLine($"   - ParentId=-1 might not be fully supported");
                                    Console.WriteLine($"   Keeping original format for maximum compatibility.");
                                    Console.ResetColor();
                                }

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

                                // NOTE: We used to change SubVersion here, but that breaks compatibility!
                                // Original format must be preserved exactly as-is
                                if (DEBUG_MODE && targetTriggers.SubVersion == null)
                                {
                                    Console.WriteLine("[DEBUG] Note: SubVersion is null (this is OK, preserving original format)");
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

                                // CRITICAL: Ensure file is flushed to disk and not cached
                                System.Threading.Thread.Sleep(500); // Wait for OS to flush
                                GC.Collect(); // Force garbage collection to clear any caches
                                GC.WaitForPendingFinalizers();

                                // Show file system proof
                                if (File.Exists(outputPath))
                                {
                                    var fileInfo = new FileInfo(outputPath);
                                    Console.WriteLine($"\n📁 FILE SYSTEM VERIFICATION:");
                                    Console.WriteLine($"   Path: {Path.GetFullPath(outputPath)}");
                                    Console.WriteLine($"   Size: {fileInfo.Length:N0} bytes");
                                    Console.WriteLine($"   Last Modified: {fileInfo.LastWriteTime}");
                                    Console.WriteLine($"   Exists: {fileInfo.Exists}");

                                    // Compare with original target file size
                                    if (File.Exists(targetPath))
                                    {
                                        var originalSize = new FileInfo(targetPath).Length;
                                        var sizeDiff = fileInfo.Length - originalSize;
                                        Console.WriteLine($"   Original target size: {originalSize:N0} bytes");
                                        Console.WriteLine($"   Size difference: {sizeDiff:+#;-#;0} bytes");
                                    }

                                    // HEX COMPARISON: First 200 bytes
                                    Console.WriteLine($"\n🔬 HEX COMPARISON (first 200 bytes):");
                                    try
                                    {
                                        byte[] originalBytes = File.ReadAllBytes(targetPath).Take(200).ToArray();
                                        byte[] mergedBytes = File.ReadAllBytes(outputPath).Take(200).ToArray();

                                        Console.WriteLine($"\n   ORIGINAL TARGET:");
                                        PrintHexDump(originalBytes, 0, Math.Min(200, originalBytes.Length));

                                        Console.WriteLine($"\n   MERGED FILE:");
                                        PrintHexDump(mergedBytes, 0, Math.Min(200, mergedBytes.Length));

                                        // Find first difference
                                        int firstDiff = -1;
                                        for (int i = 0; i < Math.Min(originalBytes.Length, mergedBytes.Length); i++)
                                        {
                                            if (originalBytes[i] != mergedBytes[i])
                                            {
                                                firstDiff = i;
                                                break;
                                            }
                                        }

                                        if (firstDiff >= 0)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"\n   ⚠ First difference at byte {firstDiff} (0x{firstDiff:X})");
                                            Console.WriteLine($"      Original: 0x{originalBytes[firstDiff]:X2}");
                                            Console.WriteLine($"      Merged:   0x{mergedBytes[firstDiff]:X2}");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"\n   ✓ First 200 bytes are IDENTICAL");
                                            Console.ResetColor();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"   ⚠ Could not compare hex: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("\n❌ CRITICAL: Output file does NOT exist on disk!");
                                    Console.ResetColor();
                                }

                                try
                                {
                                    // Open with completely new file handle to avoid any caching
                                    Console.WriteLine("\n🔍 PARSING VERIFICATION (fresh file read):");
                                    MapTriggers verifyTriggers = ReadMapTriggersAuto(outputPath);

                                    // CRITICAL: Check if variables were preserved
                                    int originalVarCount = targetTriggers.Variables.Count;
                                    int savedVarCount = verifyTriggers.Variables.Count;

                                    Console.WriteLine($"   Variables we tried to write: {originalVarCount}");
                                    Console.WriteLine($"   Variables parser found in file: {savedVarCount}");

                                    if (savedVarCount == 0 && originalVarCount > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("\n❌❌❌ CRITICAL ERROR: ALL VARIABLES WERE LOST! ❌❌❌");
                                        Console.WriteLine($"❌ We tried to write {originalVarCount} variables but the file has 0!");
                                        Console.WriteLine("❌ This is a BUG in War3Net library's WriteTo method!");
                                        Console.WriteLine("❌ DO NOT use this file - it will corrupt your map!");
                                        Console.ResetColor();

                                        // RAW BYTE ANALYSIS to prove what's actually in the file
                                        Console.WriteLine("\n🔬 RAW BYTE ANALYSIS:");
                                        try
                                        {
                                            using var fs = File.OpenRead(outputPath);
                                            using var br = new BinaryReader(fs);

                                            // Read WTG header
                                            var fileId = new string(br.ReadChars(4)); // Should be "WTG!"
                                            var version = br.ReadInt32();

                                            Console.WriteLine($"   File signature: '{fileId}' (should be 'WTG!')");
                                            Console.WriteLine($"   Format version: {version}");

                                            // Try to find variable count in file
                                            // WTG format: after header, there's usually a variable count as int32
                                            var possibleVarCount = br.ReadInt32();
                                            Console.WriteLine($"   Value at offset 8 (possible var count): {possibleVarCount}");

                                            // Read first 100 bytes as hex for manual inspection
                                            fs.Position = 0;
                                            byte[] header = new byte[Math.Min(100, fs.Length)];
                                            fs.Read(header, 0, header.Length);

                                            Console.WriteLine($"\n   First 100 bytes (hex):");
                                            for (int i = 0; i < header.Length; i += 16)
                                            {
                                                var line = string.Join(" ", header.Skip(i).Take(16).Select(b => b.ToString("X2")));
                                                Console.WriteLine($"   {i:000}: {line}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"   ⚠ Could not read raw bytes: {ex.Message}");
                                        }
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
                                        Console.WriteLine("   ✓ All variables were saved correctly!");
                                        Console.ResetColor();

                                        // Show sample variables to prove they're real
                                        if (DEBUG_MODE && savedVarCount > 0)
                                        {
                                            Console.WriteLine("\n   Sample variables from saved file:");
                                            foreach (var v in verifyTriggers.Variables.Take(5))
                                            {
                                                Console.WriteLine($"      ID={v.Id}, Name={v.Name}, Type={v.Type}");
                                            }
                                            if (savedVarCount > 5)
                                            {
                                                Console.WriteLine($"      ... and {savedVarCount - 5} more");
                                            }
                                        }

                                        // WARNING about tool compatibility
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine("\n   ⚠ IMPORTANT: If BetterTriggers shows 0 variables:");
                                        Console.WriteLine("   1. Our parser found {0} variables in the file", savedVarCount);
                                        Console.WriteLine("   2. The file exists and has the correct size");
                                        Console.WriteLine("   3. This suggests BetterTriggers may not support our format");
                                        Console.WriteLine("   4. Try opening in World Editor instead");
                                        Console.WriteLine("   5. Or there may be a SubVersion/FormatVersion incompatibility");
                                        Console.ResetColor();
                                    }

                                    // Check categories
                                    var verifyCats = verifyTriggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                                    var verifyRoot = verifyCats.Count(c => c.ParentId == -1);
                                    var verifyNested = verifyCats.Count(c => c.ParentId >= 0);

                                    Console.WriteLine($"\n   Categories:");
                                    Console.WriteLine($"      Root-level (ParentId=-1): {verifyRoot}");
                                    Console.WriteLine($"      Nested (ParentId>=0): {verifyNested}");

                                    // Show format information
                                    Console.WriteLine($"\n   📋 Format Information:");
                                    Console.WriteLine($"      Format Version: {verifyTriggers.FormatVersion}");
                                    Console.WriteLine($"      SubVersion: {verifyTriggers.SubVersion?.ToString() ?? "null"}");
                                    Console.WriteLine($"      Game Version: {verifyTriggers.GameVersion}");

                                    // Compare with original
                                    if (DEBUG_MODE)
                                    {
                                        Console.WriteLine($"\n   [DEBUG] Original target had:");
                                        Console.WriteLine($"   [DEBUG]   SubVersion: {(targetTriggers.SubVersion?.ToString() ?? "null")}");
                                        Console.WriteLine($"   [DEBUG] We may have changed SubVersion during save");
                                        Console.WriteLine($"   [DEBUG] This could cause BetterTriggers compatibility issues");
                                    }

                                    // Check ParentIds based on format version
                                    if (verifyTriggers.SubVersion == null)
                                    {
                                        // OLD FORMAT: ParentId=0 is expected and correct
                                        var zeroParentCount = verifyCats.Count(c => c.ParentId == 0);
                                        if (zeroParentCount == verifyCats.Count)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("✓ ParentIds saved correctly for OLD format (all ParentId=0)");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"⚠ Note: Old format - {zeroParentCount}/{verifyCats.Count} categories have ParentId=0");
                                            Console.ResetColor();
                                        }
                                    }
                                    else
                                    {
                                        // ENHANCED FORMAT: ParentId=-1 is expected for root categories
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

                                        // Check for duplicate variable IDs (version-aware)
                                        var varIdGroups = mergedTriggers.Variables.GroupBy(v => v.Id).Where(g => g.Count() > 1).ToList();
                                        if (varIdGroups.Count > 0)
                                        {
                                            if (mergedTriggers.SubVersion == null)
                                            {
                                                // OLD FORMAT: All variables having same ID is NORMAL
                                                Console.ForegroundColor = ConsoleColor.Green;
                                                Console.WriteLine($"\n✓ Variable IDs correct for OLD format (all ID={varIdGroups[0].Key} is normal)");
                                                Console.ResetColor();
                                            }
                                            else
                                            {
                                                // ENHANCED FORMAT: Duplicate IDs are a problem
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine($"\n❌ ERROR: {varIdGroups.Count} duplicate variable ID(s) found:");
                                                foreach (var group in varIdGroups.Take(5))
                                                {
                                                    Console.WriteLine($"  ID {group.Key}: {string.Join(", ", group.Select(v => v.Name))}");
                                                }
                                                Console.ResetColor();
                                            }
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
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[READ-WTG] Opening file: {filePath}");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"WTG file not found: {filePath}");
            }

            using var fileStream = File.OpenRead(filePath);
            using var reader = new BinaryReader(fileStream);

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[READ-WTG] File size: {fileStream.Length} bytes");
            }

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

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[READ-WTG] ✓ Parsed successfully");
                    Console.WriteLine($"[READ-WTG]   Format: SubVersion={triggers.SubVersion?.ToString() ?? "null"} (FormatVersion={triggers.FormatVersion})");
                    Console.WriteLine($"[READ-WTG]   Variables: {triggers.Variables.Count}");
                    Console.WriteLine($"[READ-WTG]   TriggerItems: {triggers.TriggerItems.Count}");
                    Console.WriteLine($"[READ-WTG]   Categories: {triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
                    Console.WriteLine($"[READ-WTG]   Triggers: {triggers.TriggerItems.OfType<TriggerDefinition>().Count()}");
                }

                return triggers;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[READ-WTG] ✗ Parse failed: {ex.InnerException?.Message ?? ex.Message}");
                }

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
        /// Uses reflection to access internal WriteTo method
        /// </summary>
        static void WriteWTGFile(string filePath, MapTriggers triggers)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[WRITE-WTG] Writing to: {filePath}");
                Console.WriteLine($"[WRITE-WTG]   Format: SubVersion={triggers.SubVersion?.ToString() ?? "null"} (FormatVersion={triggers.FormatVersion})");
                Console.WriteLine($"[WRITE-WTG]   Variables: {triggers.Variables.Count}");
                Console.WriteLine($"[WRITE-WTG]   TriggerItems: {triggers.TriggerItems.Count}");
                Console.WriteLine($"[WRITE-WTG]   Categories: {triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
                Console.WriteLine($"[WRITE-WTG]   Triggers: {triggers.TriggerItems.OfType<TriggerDefinition>().Count()}");
            }

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

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[WRITE-WTG] ✓ Completed. File size: {fileStream.Length} bytes");
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
                // Calculate correct ID based on format
                int newCategoryId;
                int newParentId;

                if (target.SubVersion == null)
                {
                    // OLD FORMAT: Category IDs must not conflict with trigger IDs
                    // Find the max trigger ID and use IDs after that for categories
                    var existingTriggerIds = target.TriggerItems
                        .OfType<TriggerDefinition>()
                        .Select(t => t.Id)
                        .ToList();

                    int startCategoryId = 0;
                    if (existingTriggerIds.Count > 0)
                    {
                        startCategoryId = existingTriggerIds.Max() + 1;
                    }

                    var existingCategoryCount = target.TriggerItems.OfType<TriggerCategoryDefinition>().Count();
                    newCategoryId = startCategoryId + existingCategoryCount;
                    newParentId = 0;  // Old format uses ParentId=0

                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[CREATE-CAT] OLD FORMAT: Max trigger ID = {existingTriggerIds.DefaultIfEmpty(-1).Max()}, assigning category ID = {newCategoryId}");
                    }
                }
                else
                {
                    // ENHANCED FORMAT: Can use any unique ID
                    newCategoryId = GetNextId(target);
                    newParentId = -1;  // Root-level in enhanced format
                }

                // Create new category
                destCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
                {
                    Id = newCategoryId,
                    ParentId = newParentId,
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

            // Check for existing triggers in destination category to avoid duplicates
            var existingTriggersInDest = target.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId == destCategory.Id)
                .Select(t => t.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Copy triggers
            Console.WriteLine($"\n  Copying {triggersToCopy.Count} trigger(s) to category '{destCategoryName}':");
            int copiedCount = 0;
            int skippedCount = 0;

            foreach (var sourceTrigger in triggersToCopy)
            {
                // Check if trigger already exists in destination
                if (existingTriggersInDest.Contains(sourceTrigger.Name))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    ⊘ Skipped '{sourceTrigger.Name}' (already exists in destination)");
                    Console.ResetColor();
                    skippedCount++;
                    continue;
                }

                var copiedTrigger = CopyTrigger(sourceTrigger, GetNextId(target), destCategory.Id);
                target.TriggerItems.Insert(insertIndex, copiedTrigger);
                insertIndex++;
                existingTriggersInDest.Add(copiedTrigger.Name); // Track copied trigger
                Console.WriteLine($"    ✓ {copiedTrigger.Name}");
                copiedCount++;
            }

            if (skippedCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n  Summary: {copiedCount} copied, {skippedCount} skipped (duplicates)");
                Console.ResetColor();
            }

            // Update trigger item counts
            UpdateTriggerItemCounts(target);

            // TRACE: Show state BEFORE auto-fix
            if (DEBUG_MODE)
            {
                Console.WriteLine("\n" + new string('═', 70));
                Console.WriteLine("TRACE: STATE AFTER COPY, BEFORE AUTO-FIX");
                Console.WriteLine(new string('═', 70));
                TraceCategoryStructure(target, "TARGET");
            }

            // AUTOMATIC FIX: Apply version-aware category fixing
            AutoFixCategoriesForFormat(target);

            // TRACE: Show state AFTER auto-fix
            if (DEBUG_MODE)
            {
                Console.WriteLine("\n" + new string('═', 70));
                Console.WriteLine("TRACE: STATE AFTER AUTO-FIX");
                Console.WriteLine(new string('═', 70));
                TraceCategoryStructure(target, "TARGET");
            }
        }

        /// <summary>
        /// Gets all triggers that belong to a specific category (using ParentId)
        /// </summary>
        static List<TriggerDefinition> GetTriggersInCategory(MapTriggers triggers, string categoryName)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[GET-TRIGGERS] Looking for category: '{categoryName}'");
            }

            var category = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[GET-TRIGGERS] ✗ Category not found: '{categoryName}'");
                }
                return new List<TriggerDefinition>();
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[GET-TRIGGERS] ✓ Found category: ID={category.Id}, ParentId={category.ParentId}");
            }

            // Get all triggers that have this category as their parent (using ParentId)
            var triggersInCategory = triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId == category.Id)
                .ToList();

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[GET-TRIGGERS] Found {triggersInCategory.Count} triggers in category '{categoryName}'");
                foreach (var trigger in triggersInCategory.Take(5))
                {
                    Console.WriteLine($"[GET-TRIGGERS]   - '{trigger.Name}' (ID={trigger.Id})");
                }
                if (triggersInCategory.Count > 5)
                {
                    Console.WriteLine($"[GET-TRIGGERS]   ... and {triggersInCategory.Count - 5} more");
                }
            }

            return triggersInCategory;
        }

        /// <summary>
        /// Merges a category from source to target
        /// </summary>
        static void MergeCategory(MapTriggers source, MapTriggers target, string categoryName)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[MERGE-CAT] Starting merge of category: '{categoryName}'");
                Console.WriteLine($"[MERGE-CAT]   Source format: SubVersion={source.SubVersion?.ToString() ?? "null"}");
                Console.WriteLine($"[MERGE-CAT]   Target format: SubVersion={target.SubVersion?.ToString() ?? "null"}");
            }

            // Find source category
            var sourceCategory = source.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (sourceCategory == null)
            {
                throw new InvalidOperationException($"Category '{categoryName}' not found in source");
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[MERGE-CAT] Source category found: ID={sourceCategory.Id}, ParentId={sourceCategory.ParentId}");
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

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[MERGE-CAT] Existing target category: ID={targetCategory.Id}, ParentId={targetCategory.ParentId}");
                }

                RemoveCategory(target, categoryName);
            }

            // Create new category in target (Type must be set via constructor)
            // Set ParentId based on format (source ParentId might point to non-existent category in target)
            int newParentId = target.SubVersion == null ? 0 : -1;  // Old format uses 0, enhanced uses -1
            int newId = GetNextId(target);

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[MERGE-CAT] Creating new category with ID={newId}, ParentId={newParentId}");
            }

            var newCategory = new TriggerCategoryDefinition(TriggerItemType.Category)
            {
                Id = newId,
                ParentId = newParentId,  // Root-level for copied categories
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
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[MERGE-CAT] Copying {sourceCategoryTriggers.Count} triggers...");
            }

            foreach (var sourceTrigger in sourceCategoryTriggers)
            {
                var copiedTrigger = CopyTrigger(sourceTrigger, GetNextId(target), newCategory.Id);
                target.TriggerItems.Add(copiedTrigger);
                Console.WriteLine($"    + Copied trigger: {copiedTrigger.Name}");
            }

            // Update trigger item counts
            UpdateTriggerItemCounts(target);

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[MERGE-CAT] Applying auto-fix for format...");
            }

            // AUTOMATIC FIX: Apply version-aware category fixing
            AutoFixCategoriesForFormat(target);

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[MERGE-CAT] ✓ Merge completed");
            }
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
        /// Automatically fixes category structure based on file format version
        /// This is a safety net - main fixing happens at load time
        /// </summary>
        static void AutoFixCategoriesForFormat(MapTriggers triggers)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[AUTO-FIX] Checking format: SubVersion={triggers.SubVersion?.ToString() ?? "null"}");
            }

            if (triggers.SubVersion == null)
            {
                // OLD FORMAT: Only ensure ParentId=0 for categories
                // Category IDs are already correctly assigned by FixCategoryIdsForOldFormat() at load time
                // and by category creation logic during operations
                var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[AUTO-FIX] OLD FORMAT: Verifying {categories.Count} categories...");
                }

                // Only fix ParentId for categories (should all be 0 in old format)
                int fixedParentIds = 0;
                foreach (var category in categories)
                {
                    if (category.ParentId != 0)
                    {
                        if (DEBUG_MODE)
                        {
                            Console.WriteLine($"[AUTO-FIX]   Category '{category.Name}': ParentId {category.ParentId} → 0");
                        }
                        category.ParentId = 0;
                        fixedParentIds++;
                    }
                }

                if (DEBUG_MODE)
                {
                    if (fixedParentIds > 0)
                    {
                        Console.WriteLine($"[AUTO-FIX] ✓ Fixed {fixedParentIds} category ParentIds");
                    }
                    else
                    {
                        Console.WriteLine($"[AUTO-FIX] ✓ All category ParentIds correct (0)");
                    }

                    // Show category ID range for verification
                    if (categories.Count > 0)
                    {
                        int minCatId = categories.Min(c => c.Id);
                        int maxCatId = categories.Max(c => c.Id);
                        Console.WriteLine($"[AUTO-FIX] Category ID range: {minCatId}-{maxCatId}");
                    }

                    // Verify trigger ParentIds reference valid categories
                    var categoryIds = categories.Select(c => c.Id).ToHashSet();
                    var triggersWithInvalidParent = triggers.TriggerItems.OfType<TriggerDefinition>()
                        .Where(t => !categoryIds.Contains(t.ParentId))
                        .ToList();

                    if (triggersWithInvalidParent.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[AUTO-FIX] ⚠ WARNING: {triggersWithInvalidParent.Count} triggers have invalid ParentIds:");
                        foreach (var trigger in triggersWithInvalidParent.Take(5))
                        {
                            Console.WriteLine($"[AUTO-FIX]   Trigger '{trigger.Name}': ParentId={trigger.ParentId} (not found in categories)");
                        }
                        if (triggersWithInvalidParent.Count > 5)
                        {
                            Console.WriteLine($"[AUTO-FIX]   ... and {triggersWithInvalidParent.Count - 5} more");
                        }
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"[AUTO-FIX] ✓ All trigger ParentIds reference valid categories");
                    }
                }
            }
            else if (DEBUG_MODE)
            {
                Console.WriteLine($"[AUTO-FIX] ENHANCED FORMAT: No fixes needed");
            }
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
            if (DEBUG_MODE && renameMappings.Count > 0)
            {
                Console.WriteLine($"[RENAME-VARS] Renaming variables in trigger '{trigger.Name}':");
                foreach (var mapping in renameMappings)
                {
                    Console.WriteLine($"[RENAME-VARS]   '{mapping.Key}' → '{mapping.Value}'");
                }
            }

            foreach (var function in trigger.Functions)
            {
                RenameVariablesInFunction(function, renameMappings);
            }

            if (DEBUG_MODE && renameMappings.Count > 0)
            {
                Console.WriteLine($"[RENAME-VARS] ✓ Completed renaming in '{trigger.Name}'");
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
        /// Prints hex dump of byte array
        /// </summary>
        static void PrintHexDump(byte[] bytes, int offset, int length)
        {
            for (int i = offset; i < offset + length && i < bytes.Length; i += 16)
            {
                // Offset
                Console.Write($"      {i:000}: ");

                // Hex bytes
                for (int j = 0; j < 16 && (i + j) < bytes.Length && (i + j) < offset + length; j++)
                {
                    Console.Write($"{bytes[i + j]:X2} ");
                }

                Console.WriteLine();
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
        /// CRITICAL: Fixes category IDs for old format (Warcraft 1.27)
        /// In old format, category IDs must not conflict with trigger IDs!
        /// We assign sequential IDs starting after the highest trigger ID.
        /// </summary>
        static void FixCategoryIdsForOldFormat(MapTriggers triggers, string mapName)
        {
            // Only fix for old format
            if (triggers.SubVersion != null)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[LOAD-FIX] {mapName}: Enhanced format, skipping category ID fix");
                }
                return;
            }

            // Get categories and triggers
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggersList = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
            if (categories.Count == 0) return;

            // Get all existing trigger IDs
            var usedTriggerIds = new HashSet<int>(triggersList.Select(t => t.Id));

            // Find the starting ID for categories (after all trigger IDs)
            int startCategoryId = 0;
            if (usedTriggerIds.Count > 0)
            {
                int maxTriggerId = usedTriggerIds.Max();
                startCategoryId = maxTriggerId + 1;

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[LOAD-FIX] {mapName}: Max trigger ID = {maxTriggerId}, categories will start at ID {startCategoryId}");
                }
            }

            // Build ID mapping: oldID → newID (sequential, no conflicts with triggers)
            var idMapping = new Dictionary<int, int>();
            bool needsIdFix = false;
            bool needsParentIdFix = false;

            for (int position = 0; position < categories.Count; position++)
            {
                int oldId = categories[position].Id;
                int newId = startCategoryId + position;

                idMapping[oldId] = newId;

                if (oldId != newId)
                {
                    needsIdFix = true;
                }

                // Check if ParentId needs fixing (should always be 0 in old format)
                if (categories[position].ParentId != 0)
                {
                    needsParentIdFix = true;
                }
            }

            // IMPORTANT: Always fix ParentIds even if IDs are correct!
            if (!needsIdFix && !needsParentIdFix)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[LOAD-FIX] {mapName}: Category IDs and ParentIds already correct");
                }
                return;
            }

            // Apply fixes
            if (needsIdFix || needsParentIdFix)
            {
                if (needsIdFix)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n⚠ [{mapName}] OLD FORMAT: Fixing category IDs to avoid trigger ID conflicts...");
                    Console.WriteLine($"  Assigning category IDs: {startCategoryId}-{startCategoryId + categories.Count - 1}");
                    Console.ResetColor();
                }

                if (needsParentIdFix && DEBUG_MODE)
                {
                    Console.WriteLine($"[LOAD-FIX] {mapName}: Fixing category ParentIds to 0");
                }
            }

            // Fix category IDs and ParentIds
            for (int position = 0; position < categories.Count; position++)
            {
                int oldId = categories[position].Id;
                int newId = startCategoryId + position;

                if (oldId != newId)
                {
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[LOAD-FIX]   '{categories[position].Name}': ID {oldId} → {newId}");
                    }
                    categories[position].Id = newId;
                }

                // CRITICAL: Always set ParentId=0 for old format
                if (categories[position].ParentId != 0)
                {
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[LOAD-FIX]   '{categories[position].Name}': ParentId {categories[position].ParentId} → 0");
                    }
                    categories[position].ParentId = 0;
                }
            }

            // Fix trigger ParentIds using mapping
            var triggers_list = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
            int triggersFixed = 0;

            foreach (var trigger in triggers_list)
            {
                if (idMapping.TryGetValue(trigger.ParentId, out int newParentId))
                {
                    if (trigger.ParentId != newParentId)
                    {
                        if (DEBUG_MODE)
                        {
                            Console.WriteLine($"[LOAD-FIX]   Trigger '{trigger.Name}': ParentId {trigger.ParentId} → {newParentId}");
                        }
                        trigger.ParentId = newParentId;
                        triggersFixed++;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ [{mapName}] Fixed {categories.Count} category IDs and {triggersFixed} trigger ParentIds");
            Console.ResetColor();
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

            // CRITICAL: In OLD format (SubVersion=null), all variables having ID=0 is NORMAL
            // Do NOT "fix" this as it breaks compatibility!
            if (triggers.SubVersion == null)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[DEBUG] {mapName} uses old format (SubVersion=null)");
                    Console.WriteLine($"[DEBUG] Skipping variable ID fixes to preserve original format");
                }
                return;
            }

            // Only fix variable IDs for ENHANCED format (SubVersion != null)
            var idGroups = triggers.Variables.GroupBy(v => v.Id).ToList();
            var duplicateIds = idGroups.Where(g => g.Count() > 1).ToList();
            bool allSameId = idGroups.Count == 1;

            if (allSameId)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ WARNING: All variables in {mapName} have ID={triggers.Variables[0].Id}!");
                Console.WriteLine($"  This is unexpected for enhanced format. Reassigning sequential IDs...");
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
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[FIX-DUP-IDS] Checking for duplicate IDs in {triggers.TriggerItems.Count} items...");
            }

            // Check if there are any duplicate IDs
            var allIds = triggers.TriggerItems.Select(item => item.Id).ToList();
            var duplicateIds = allIds.GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Count == 0)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[FIX-DUP-IDS] ✓ No duplicate IDs found");
                }
                return; // No duplicates, nothing to fix
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠ WARNING: Found {duplicateIds.Count} duplicate ID(s) in target file!");

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[FIX-DUP-IDS] Duplicate IDs:");
                foreach (var dupId in duplicateIds.Take(5))
                {
                    var itemsWithId = triggers.TriggerItems.Where(item => item.Id == dupId).ToList();
                    Console.WriteLine($"[FIX-DUP-IDS]   ID {dupId}: {string.Join(", ", itemsWithId.Select(i => $"'{i.Name}'"))}");
                }
                if (duplicateIds.Count > 5)
                {
                    Console.WriteLine($"[FIX-DUP-IDS]   ... and {duplicateIds.Count - 5} more");
                }
            }

            Console.WriteLine("  Reassigning unique IDs to all trigger items...");
            Console.ResetColor();

            // Build a mapping of OLD ID -> NEW ID BEFORE changing anything
            var oldIdToNewId = new Dictionary<int, int>();
            for (int i = 0; i < triggers.TriggerItems.Count; i++)
            {
                var oldId = triggers.TriggerItems[i].Id;
                var newId = i;

                // Only store if not already in dictionary (handles duplicates)
                if (!oldIdToNewId.ContainsKey(oldId))
                {
                    oldIdToNewId[oldId] = newId;
                }
            }

            // Now reassign IDs
            for (int i = 0; i < triggers.TriggerItems.Count; i++)
            {
                triggers.TriggerItems[i].Id = i;
            }

            // Update ParentIds in ALL items using the mapping
            foreach (var item in triggers.TriggerItems)
            {
                // Skip root-level items
                if (item.ParentId < 0)
                {
                    continue;
                }

                // Use the mapping to find the new ParentId
                if (oldIdToNewId.TryGetValue(item.ParentId, out var newParentId))
                {
                    item.ParentId = newParentId;
                }
                else
                {
                    // Parent not found in mapping, make it root-level
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[DEBUG] Item '{item.Name}' has ParentId={item.ParentId} which doesn't exist, setting to -1");
                    }
                    item.ParentId = -1;
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Reassigned IDs: 0 to {triggers.TriggerItems.Count - 1}");
            Console.WriteLine($"  ✓ Updated ParentIds using ID mapping");
            Console.ResetColor();
        }

        /// <summary>
        /// Removes a category and all its triggers from MapTriggers
        /// </summary>
        static void RemoveCategory(MapTriggers triggers, string categoryName)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[REMOVE-CAT] Removing category: '{categoryName}'");
            }

            var category = triggers.TriggerItems
                .OfType<TriggerCategoryDefinition>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[REMOVE-CAT] ✗ Category not found: '{categoryName}'");
                }
                return;
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[REMOVE-CAT] Found category: ID={category.Id}, ParentId={category.ParentId}");
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

                if (item is TriggerDefinition trigger)
                {
                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[REMOVE-CAT]   Will remove trigger: '{trigger.Name}' (ID={trigger.Id})");
                    }
                }

                itemsToRemove.Add(item);
            }

            // Remove all items
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[REMOVE-CAT] Removing {itemsToRemove.Count} items total (1 category + {itemsToRemove.Count - 1} triggers)");
            }

            foreach (var item in itemsToRemove)
            {
                triggers.TriggerItems.Remove(item);
            }

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[REMOVE-CAT] ✓ Removed category '{categoryName}' and its triggers");
            }
        }

        /// <summary>
        /// Gets the next available ID for a trigger item
        /// </summary>
        static int GetNextId(MapTriggers triggers)
        {
            if (triggers.TriggerItems.Count == 0)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[GET-NEXT-ID] No items, returning ID=0");
                }
                return 0;
            }

            int maxId = triggers.TriggerItems.Max(item => item.Id);
            int nextId = maxId + 1;

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[GET-NEXT-ID] Max ID={maxId}, returning next ID={nextId}");
            }

            return nextId;
        }

        /// <summary>
        /// Creates a deep copy of a trigger with a new ID and parent ID
        /// </summary>
        static TriggerDefinition CopyTrigger(TriggerDefinition source, int newId, int newParentId)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[COPY-TRIGGER] Copying '{source.Name}':");
                Console.WriteLine($"[COPY-TRIGGER]   Source: ID={source.Id}, ParentId={source.ParentId}");
                Console.WriteLine($"[COPY-TRIGGER]   Target: ID={newId}, ParentId={newParentId}");
            }

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

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[COPY-TRIGGER] ✓ Created: ID={copy.Id}, ParentId={copy.ParentId}");
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
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[READ-AUTO] Determining file type: {Path.GetFileName(filePath)}");
            }

            if (IsMapArchive(filePath))
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[READ-AUTO] Detected map archive format");
                }
                return ReadMapArchiveFile(filePath);
            }
            else
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[READ-AUTO] Detected raw WTG file");
                }
                return ReadWTGFile(filePath);
            }
        }

        /// <summary>
        /// Reads MapTriggers from a map archive (.w3x/.w3m)
        /// </summary>
        static MapTriggers ReadMapArchiveFile(string archivePath)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[READ-ARCHIVE] Opening: {archivePath}");
            }

            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Map archive not found: {archivePath}");
            }

            Console.WriteLine($"  Opening MPQ archive...");
            using var archive = MpqArchive.Open(archivePath, true);
            archive.DiscoverFileNames();

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[READ-ARCHIVE] Archive opened, files discovered");
            }

            var triggerFileName = MapTriggers.FileName; // "war3map.wtg"

            if (!archive.FileExists(triggerFileName))
            {
                throw new FileNotFoundException($"Trigger file '{triggerFileName}' not found in map archive.");
            }

            Console.WriteLine($"  Extracting {triggerFileName}...");
            using var triggerStream = archive.OpenFile(triggerFileName);
            using var reader = new BinaryReader(triggerStream);

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[READ-ARCHIVE] Stream opened for {triggerFileName}");
            }

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

                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[READ-ARCHIVE] ✓ Parsed successfully");
                    Console.WriteLine($"[READ-ARCHIVE]   Format: SubVersion={triggers.SubVersion?.ToString() ?? "null"} (FormatVersion={triggers.FormatVersion})");
                    Console.WriteLine($"[READ-ARCHIVE]   Variables: {triggers.Variables.Count}");
                    Console.WriteLine($"[READ-ARCHIVE]   TriggerItems: {triggers.TriggerItems.Count}");
                    Console.WriteLine($"[READ-ARCHIVE]   Categories: {triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
                    Console.WriteLine($"[READ-ARCHIVE]   Triggers: {triggers.TriggerItems.OfType<TriggerDefinition>().Count()}");
                }

                return triggers;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                if (DEBUG_MODE)
                {
                    Console.WriteLine($"[READ-ARCHIVE] ✗ Parse failed: {ex.InnerException?.Message ?? ex.Message}");
                }

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
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[WRITE-ARCHIVE] Writing to: {outputArchivePath}");
                Console.WriteLine($"[WRITE-ARCHIVE]   Format: SubVersion={triggers.SubVersion?.ToString() ?? "null"} (FormatVersion={triggers.FormatVersion})");
                Console.WriteLine($"[WRITE-ARCHIVE]   Variables: {triggers.Variables.Count}");
                Console.WriteLine($"[WRITE-ARCHIVE]   TriggerItems: {triggers.TriggerItems.Count}");
                Console.WriteLine($"[WRITE-ARCHIVE]   Categories: {triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
                Console.WriteLine($"[WRITE-ARCHIVE]   Triggers: {triggers.TriggerItems.OfType<TriggerDefinition>().Count()}");
                Console.WriteLine($"[WRITE-ARCHIVE]   Remove JASS: {removeJassFile}");
            }

            Console.WriteLine($"  Opening original archive...");
            using var originalArchive = MpqArchive.Open(originalArchivePath, true);
            originalArchive.DiscoverFileNames();

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[WRITE-ARCHIVE] Original archive opened");
            }

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

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[WRITE-ARCHIVE] Serialized war3map.wtg: {triggerStream.Length} bytes");
            }

            triggerStream.Position = 0;

            // Remove old trigger file and add new one
            var triggerFileName = MapTriggers.FileName; // "war3map.wtg"
            Console.WriteLine($"  Replacing {triggerFileName}...");
            builder.RemoveFile(triggerFileName);
            builder.AddFile(MpqFile.New(triggerStream, triggerFileName));

            if (DEBUG_MODE)
            {
                Console.WriteLine($"[WRITE-ARCHIVE] war3map.wtg replaced in builder");
            }

            // Optionally remove war3map.j to force regeneration
            if (removeJassFile)
            {
                var jassFileName = "war3map.j";
                if (originalArchive.FileExists(jassFileName))
                {
                    Console.WriteLine($"  Removing {jassFileName} for sync...");
                    builder.RemoveFile(jassFileName);

                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[WRITE-ARCHIVE] Removed {jassFileName}");
                    }
                }

                // Also check for scripts/war3map.j
                var jassFileNameAlt = "scripts/war3map.j";
                if (originalArchive.FileExists(jassFileNameAlt))
                {
                    Console.WriteLine($"  Removing {jassFileNameAlt} for sync...");
                    builder.RemoveFile(jassFileNameAlt);

                    if (DEBUG_MODE)
                    {
                        Console.WriteLine($"[WRITE-ARCHIVE] Removed {jassFileNameAlt}");
                    }
                }
            }

            // Save the modified archive
            Console.WriteLine($"  Saving to {outputArchivePath}...");
            builder.SaveTo(outputArchivePath);
            Console.WriteLine($"  Archive updated successfully!");

            if (DEBUG_MODE)
            {
                var fileInfo = new FileInfo(outputArchivePath);
                Console.WriteLine($"[WRITE-ARCHIVE] ✓ Completed. Output file size: {fileInfo.Length} bytes");
            }
        }

        /// <summary>
        /// Traces the category-trigger structure to understand what's happening
        /// </summary>
        static void TraceCategoryStructure(MapTriggers triggers, string label)
        {
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggerslist = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            Console.WriteLine($"\n[{label}] FORMAT: SubVersion={triggers.SubVersion?.ToString() ?? "null"}");
            Console.WriteLine($"[{label}] CATEGORIES ({categories.Count}):");

            for (int i = 0; i < Math.Min(5, categories.Count); i++)
            {
                var cat = categories[i];
                Console.WriteLine($"  [{i}] '{cat.Name}' - ID={cat.Id}, ParentId={cat.ParentId}");
            }
            if (categories.Count > 5)
            {
                Console.WriteLine($"  ... and {categories.Count - 5} more");
                // Show last category (usually the newly created one)
                var lastCat = categories[categories.Count - 1];
                Console.WriteLine($"  [{categories.Count - 1}] '{lastCat.Name}' - ID={lastCat.Id}, ParentId={lastCat.ParentId}");
            }

            Console.WriteLine($"\n[{label}] TRIGGERS ({triggerslist.Count}):");

            // Group triggers by ParentId
            var triggersByParent = triggerslist.GroupBy(t => t.ParentId).ToList();
            foreach (var group in triggersByParent.OrderBy(g => g.Key))
            {
                var parentId = group.Key;
                var count = group.Count();
                var parentCat = categories.FirstOrDefault(c => c.Id == parentId);
                var parentName = parentCat?.Name ?? $"[UNKNOWN ID={parentId}]";

                Console.WriteLine($"  ParentId={parentId} ({parentName}): {count} triggers");

                // Show first few triggers
                foreach (var trig in group.Take(3))
                {
                    Console.WriteLine($"    - '{trig.Name}' (ID={trig.Id})");
                }
                if (group.Count() > 3)
                {
                    Console.WriteLine($"    ... and {group.Count() - 3} more");
                }
            }

            // Check for orphaned triggers
            var validCategoryIds = categories.Select(c => c.Id).ToHashSet();
            var orphaned = triggerslist.Where(t => !validCategoryIds.Contains(t.ParentId)).ToList();
            if (orphaned.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[{label}] ⚠ ORPHANED TRIGGERS ({orphaned.Count}) - ParentId doesn't match any category:");
                Console.ResetColor();
                foreach (var trig in orphaned.Take(5))
                {
                    Console.WriteLine($"  - '{trig.Name}' has ParentId={trig.ParentId} (no category with this ID!)");
                }
                if (orphaned.Count > 5)
                {
                    Console.WriteLine($"  ... and {orphaned.Count - 5} more");
                }
            }
        }
    }
}
