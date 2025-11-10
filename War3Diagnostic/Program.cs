using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using War3Net.Build.Script;
using War3Net.Build.Extensions;
using War3Net.Common.Extensions;
using War3Net.IO.Mpq;

namespace WTGDiagnostic
{
    class Program
    {
        // Log file for output
        static StreamWriter logFile;
        static string logFileName;

        // Helper method to write to both console and file (without colors in file)
        static void WriteLine(string message = "")
        {
            Console.WriteLine(message);
            logFile?.WriteLine(message);
        }

        static void WriteColorLine(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
            logFile?.WriteLine(message);  // Write without color codes to file
        }

        static void Main(string[] args)
        {
            // Set up log file
            logFileName = $"WTG_Diagnostic_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            logFile = new StreamWriter(logFileName, false, Encoding.UTF8);
            logFile.AutoFlush = true;

            WriteLine("===============================================================");
            WriteLine("           WTG Binary Diagnostic Tool");
            WriteLine("===============================================================");
            WriteLine();
            WriteLine($"Output will be saved to: {logFileName}");
            WriteLine();

            // Default paths - look in Source/ and Target/ folders
            string sourcePath = "Source/war3map.wtg";
            string targetPath = "Target/war3map.wtg";
            string mergedPath = "Target/war3map_merged.wtg";

            // Allow overriding with command line arguments
            if (args.Length >= 3)
            {
                sourcePath = args[0];
                targetPath = args[1];
                mergedPath = args[2];
            }
            else if (args.Length > 0)
            {
                WriteLine("Usage: WTGDiagnostic [source.wtg/.w3x] [target.wtg/.w3x] [merged.wtg/.w3x]");
                WriteLine();
                WriteLine("If no arguments provided, uses default paths:");
                WriteLine("  Source: Source/war3map.wtg");
                WriteLine("  Target: Target/war3map.wtg");
                WriteLine("  Merged: Target/war3map_merged.wtg");
                WriteLine();
                logFile?.Close();
                return;
            }

            WriteLine("File paths:");
            WriteLine($"  Source: {sourcePath}");
            WriteLine($"  Target: {targetPath}");
            WriteLine($"  Merged: {mergedPath}");
            WriteLine();

            // Verify files exist
            if (!File.Exists(sourcePath))
            {
                WriteColorLine(ConsoleColor.Red, $"[ERROR] Source file not found: {sourcePath}");
                logFile?.Close();
                return;
            }
            if (!File.Exists(targetPath))
            {
                WriteColorLine(ConsoleColor.Red, $"[ERROR] Target file not found: {targetPath}");
                logFile?.Close();
                return;
            }
            if (!File.Exists(mergedPath))
            {
                WriteColorLine(ConsoleColor.Red, $"[ERROR] Merged file not found: {mergedPath}");
                logFile?.Close();
                return;
            }

            try
            {
                WriteLine("===============================================================");
                WriteLine("STEP 1: Reading Files");
                WriteLine("===============================================================");
                WriteLine();

                // Read source
                WriteLine($"Reading SOURCE: {sourcePath}");
                var (sourceData, sourceTriggers) = ReadWTGFile(sourcePath);
                WriteLine($"  Binary size: {sourceData.Length:N0} bytes");
                ShowStats("SOURCE", sourceTriggers);
                WriteLine();

                // Read target
                WriteLine($"Reading TARGET: {targetPath}");
                var (targetData, targetTriggers) = ReadWTGFile(targetPath);
                WriteLine($"  Binary size: {targetData.Length:N0} bytes");
                ShowStats("TARGET", targetTriggers);
                WriteLine();

                // Read merged
                WriteLine($"Reading MERGED: {mergedPath}");
                var (mergedData, mergedTriggers) = ReadWTGFile(mergedPath);
                WriteLine($"  Binary size: {mergedData.Length:N0} bytes");
                ShowStats("MERGED", mergedTriggers);
                WriteLine();

                WriteLine("===============================================================");
                WriteLine("STEP 2: Binary Hex Dumps");
                WriteLine("===============================================================");
                WriteLine();

                // Dump hex for each file (limited to 512 bytes)
                WriteLine("--- SOURCE (first 512 bytes) ---");
                DumpHex(sourceData, 512);
                WriteLine();

                WriteLine("--- TARGET (first 512 bytes) ---");
                DumpHex(targetData, 512);
                WriteLine();

                WriteLine("--- MERGED (first 512 bytes) ---");
                DumpHex(mergedData, 512);
                WriteLine();

                WriteLine("===============================================================");
                WriteLine("STEP 3: Binary Comparison");
                WriteLine("===============================================================");
                WriteLine();

                // Compare merged with target (should be similar format)
                WriteLine("Comparing MERGED vs TARGET (both should be 1.27 format):");
                CompareBinary(mergedData, targetData, "MERGED", "TARGET");
                WriteLine();

                WriteLine("===============================================================");
                WriteLine("STEP 4: Detailed Analysis");
                WriteLine("===============================================================");
                WriteLine();

                AnalyzeWTGStructure(sourceData, "SOURCE");
                WriteLine();
                AnalyzeWTGStructure(targetData, "TARGET");
                WriteLine();
                AnalyzeWTGStructure(mergedData, "MERGED");
                WriteLine();

                // NEW: Comprehensive hierarchy analysis
                ShowHierarchy("SOURCE", sourceTriggers);
                ShowHierarchy("TARGET", targetTriggers);
                ShowHierarchy("MERGED", mergedTriggers);

                // NEW: Compare hierarchies
                CompareHierarchies(sourceTriggers, targetTriggers, mergedTriggers);

                WriteLine("===============================================================");
                WriteLine("DIAGNOSIS COMPLETE");
                WriteLine("===============================================================");
                WriteLine();
                WriteLine("Check the output above to see:");
                WriteLine("  1. If MERGED file has correct statistics (variables/triggers)");
                WriteLine("  2. If MERGED binary data looks valid");
                WriteLine("  3. What bytes are different between MERGED and TARGET");
                WriteLine("  4. Whether the WTG header/structure is correct");
                WriteLine("  5. COMPLETE CATEGORY/TRIGGER HIERARCHIES for each file");
                WriteLine("  6. ParentId distribution and nesting issues");
                WriteLine();
                WriteLine("Pay special attention to:");
                WriteLine("  - Are all triggers nested under one category?");
                WriteLine("  - Do category ParentIds match between files?");
                WriteLine("  - Are there orphaned triggers with invalid ParentIds?");
            }
            catch (Exception ex)
            {
                WriteColorLine(ConsoleColor.Red, "");
                WriteColorLine(ConsoleColor.Red, $"[ERROR] {ex.Message}");
                WriteColorLine(ConsoleColor.Red, "");
                WriteColorLine(ConsoleColor.Red, "Stack trace:");
                WriteColorLine(ConsoleColor.Red, ex.StackTrace);
            }
            finally
            {
                // Close log file
                if (logFile != null)
                {
                    logFile.Close();
                    Console.WriteLine($"\n✓ Diagnostic results saved to: {logFileName}");
                }
            }
        }

        static (byte[] data, MapTriggers triggers) ReadWTGFile(string path)
        {
            byte[] wtgData;
            MapTriggers triggers;

            if (path.EndsWith(".w3x", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".w3m", StringComparison.OrdinalIgnoreCase))
            {
                // Read from MPQ archive
                using var archive = MpqArchive.Open(path, true);
                archive.DiscoverFileNames();

                if (!archive.FileExists("war3map.wtg"))
                {
                    throw new FileNotFoundException("Archive does not contain war3map.wtg");
                }

                using var wtgStream = archive.OpenFile("war3map.wtg");
                using var ms = new MemoryStream();
                wtgStream.CopyTo(ms);
                wtgData = ms.ToArray();

                // Parse with War3Net
                ms.Position = 0;
                using var reader = new BinaryReader(ms);
                triggers = reader.ReadMapTriggers();
            }
            else
            {
                // Read WTG file directly
                wtgData = File.ReadAllBytes(path);

                using var ms = new MemoryStream(wtgData);
                using var reader = new BinaryReader(ms);
                triggers = reader.ReadMapTriggers();
            }

            return (wtgData, triggers);
        }

        static void DumpHex(byte[] data, int maxBytes)
        {
            int bytesToShow = Math.Min(data.Length, maxBytes);

            for (int i = 0; i < bytesToShow; i += 16)
            {
                // Offset
                Write($"{i:X8}  ");

                // Hex bytes
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < bytesToShow)
                    {
                        Write($"{data[i + j]:X2} ");
                    }
                    else
                    {
                        Write("   ");
                    }

                    if (j == 7)
                    {
                        Write(" ");
                    }
                }

                Write(" |");

                // ASCII representation
                for (int j = 0; j < 16 && i + j < bytesToShow; j++)
                {
                    byte b = data[i + j];
                    char c = (b >= 32 && b < 127) ? (char)b : '.';
                    Write(c.ToString());
                }

                WriteLine("|");
            }

            if (bytesToShow < data.Length)
            {
                WriteLine($"... {data.Length - bytesToShow:N0} more bytes not shown");
            }
        }

        static void Write(string message)
        {
            Console.Write(message);
            logFile?.Write(message);
        }

        static void ShowStats(string label, MapTriggers triggers)
        {
            WriteLine($"  [{label}] Statistics:");
            WriteLine($"    Format Version: {triggers.FormatVersion}");
            WriteLine($"    SubVersion: {triggers.SubVersion?.ToString() ?? "null"}");
            WriteLine($"    Game Version: {triggers.GameVersion}");
            WriteLine($"    Variables: {triggers.Variables.Count}");
            WriteLine($"    Trigger Items: {triggers.TriggerItems.Count}");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count();
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().Count();

            WriteLine($"      - Categories: {categories}");
            WriteLine($"      - Triggers: {triggerDefs}");

            // Sample variables
            if (triggers.Variables.Count > 0)
            {
                WriteLine($"    Sample Variables:");
                foreach (var v in triggers.Variables.Take(5))
                {
                    WriteLine($"      - {v.Name} ({v.Type}) [ParentId={v.ParentId}]");
                }
                if (triggers.Variables.Count > 5)
                {
                    WriteLine($"      ... and {triggers.Variables.Count - 5} more");
                }
            }
        }

        static void CompareBinary(byte[] data1, byte[] data2, string label1, string label2)
        {
            WriteLine($"File sizes: {label1}={data1.Length:N0} bytes, {label2}={data2.Length:N0} bytes");

            if (data1.Length != data2.Length)
            {
                WriteColorLine(ConsoleColor.Yellow, $"[WARNING] File sizes differ by {Math.Abs(data1.Length - data2.Length):N0} bytes");
            }

            int minLength = Math.Min(data1.Length, data2.Length);
            int differenceCount = 0;
            int maxDiffsToShow = 50;  // Limit to prevent huge output

            WriteLine();
            WriteLine("Byte-by-byte differences (first 50):");
            for (int i = 0; i < minLength && differenceCount < maxDiffsToShow; i++)
            {
                if (data1[i] != data2[i])
                {
                    WriteLine($"  Offset {i:X8}: {label1}={data1[i]:X2} vs {label2}={data2[i]:X2}");
                    differenceCount++;
                }
            }

            // Count total differences
            int totalDifferences = 0;
            for (int i = 0; i < minLength; i++)
            {
                if (data1[i] != data2[i])
                {
                    totalDifferences++;
                }
            }

            if (differenceCount < totalDifferences)
            {
                WriteLine($"  ... and {totalDifferences - differenceCount:N0} more differences not shown");
            }

            WriteLine();
            WriteLine($"Total differences: {totalDifferences:N0} bytes ({(totalDifferences * 100.0 / minLength):F2}%)");
        }

        static void AnalyzeWTGStructure(byte[] data, string label)
        {
            WriteLine($"[{label}] WTG Structure Analysis:");

            try
            {
                if (data.Length < 12)
                {
                    WriteColorLine(ConsoleColor.Red, "  [ERROR] File too small to be valid WTG");
                    return;
                }

                // Read file ID
                string fileId = Encoding.ASCII.GetString(data, 0, 4);
                WriteLine($"  File ID: '{fileId}'");

                if (fileId != "WTG!")
                {
                    WriteColorLine(ConsoleColor.Red, $"  [ERROR] Invalid file ID (expected 'WTG!')");
                    return;
                }

                // Read format version
                int formatVersion = BitConverter.ToInt32(data, 4);
                WriteLine($"  Format Version: {formatVersion}");

                // Check if it's a known version
                if (formatVersion == 7)
                {
                    // For WC3 1.27 files: Offset 8 = Category Count
                    // For WC3 1.31+ files: Offset 8 = SubVersion (then Category Count)
                    int valueAtOffset8 = BitConverter.ToInt32(data, 8);

                    // Heuristic: If value at offset 8 is small (< 1000), it's likely a category count (1.27 format)
                    // If it's very large or has specific patterns, it might be SubVersion
                    if (valueAtOffset8 < 1000)
                    {
                        WriteLine($"  Category Count: {valueAtOffset8}");
                        WriteLine($"  SubVersion: null (1.27 or earlier format)");
                        WriteLine($"  Note: Variables/triggers can only be counted by full War3Net parse");
                    }
                    else
                    {
                        WriteLine($"  Possible SubVersion: {valueAtOffset8}");
                        WriteLine($"  Note: This appears to be 1.31+ format");
                    }

                    WriteLine();
                    WriteColorLine(ConsoleColor.Cyan, "  ℹ For accurate counts, use War3Net parser (shown in statistics above)");
                }
                else
                {
                    WriteColorLine(ConsoleColor.Yellow, $"  [WARNING] Unknown format version: {formatVersion}");
                }
            }
            catch (Exception ex)
            {
                WriteColorLine(ConsoleColor.Red, $"  [ERROR] Failed to parse structure: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows complete category and trigger hierarchy
        /// </summary>
        static void ShowHierarchy(string label, MapTriggers triggers)
        {
            WriteLine($"\n===============================================================");
            WriteLine($"{label} - COMPLETE HIERARCHY");
            WriteLine($"===============================================================");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            WriteLine($"\nTotal Categories: {categories.Count}");
            WriteLine($"Total Triggers: {triggerDefs.Count}");

            // Show category tree
            WriteLine($"\n--- CATEGORY HIERARCHY ---");
            var rootCategories = categories.Where(c => c.ParentId == -1 || c.ParentId == 0).ToList();

            if (rootCategories.Count == 0)
            {
                WriteColorLine(ConsoleColor.Yellow, "  [WARNING] No root-level categories found!");

                // Show all categories with their ParentIds
                WriteLine("\n  All categories:");
                foreach (var cat in categories.OrderBy(c => c.Id))
                {
                    WriteLine($"    - {cat.Name} (ID={cat.Id}, ParentId={cat.ParentId})");
                }
            }
            else
            {
                foreach (var rootCat in rootCategories.OrderBy(c => c.Id))
                {
                    PrintCategoryTree(rootCat, categories, triggerDefs, 0);
                }
            }

            // Check for orphaned triggers (triggers not in any category)
            WriteLine($"\n--- TRIGGER DISTRIBUTION ---");
            foreach (var category in categories.OrderBy(c => c.Name))
            {
                var triggersInCat = triggerDefs.Where(t => t.ParentId == category.Id).ToList();
                if (triggersInCat.Count > 0)
                {
                    WriteLine($"  {category.Name} (ID={category.Id}): {triggersInCat.Count} trigger(s)");
                }
            }

            // Check for orphaned triggers
            var orphanedTriggers = triggerDefs.Where(t =>
                !categories.Any(c => c.Id == t.ParentId) && t.ParentId != -1 && t.ParentId != 0).ToList();

            if (orphanedTriggers.Count > 0)
            {
                WriteColorLine(ConsoleColor.Yellow, $"\n  [WARNING] {orphanedTriggers.Count} orphaned trigger(s) found:");
                foreach (var t in orphanedTriggers.Take(10))
                {
                    WriteLine($"    - {t.Name} (ID={t.Id}, ParentId={t.ParentId})");
                }
                if (orphanedTriggers.Count > 10)
                {
                    WriteLine($"    ... and {orphanedTriggers.Count - 10} more");
                }
            }

            // Check for triggers with ParentId=0 or -1 (root-level triggers)
            var rootTriggers = triggerDefs.Where(t => t.ParentId == 0 || t.ParentId == -1).ToList();
            if (rootTriggers.Count > 0)
            {
                WriteLine($"\n  Root-level triggers (ParentId=0 or -1): {rootTriggers.Count}");
                foreach (var t in rootTriggers.Take(5))
                {
                    WriteLine($"    - {t.Name} (ID={t.Id}, ParentId={t.ParentId})");
                }
                if (rootTriggers.Count > 5)
                {
                    WriteLine($"    ... and {rootTriggers.Count - 5} more");
                }
            }
        }

        /// <summary>
        /// Recursively prints category tree
        /// </summary>
        static void PrintCategoryTree(TriggerCategoryDefinition category,
            List<TriggerCategoryDefinition> allCategories,
            List<TriggerDefinition> allTriggers,
            int depth,
            HashSet<int> visitedCategories = null)
        {
            // Prevent infinite recursion
            if (depth > 50)
            {
                WriteColorLine(ConsoleColor.Red, $"[ERROR] Max depth reached at category '{category.Name}' - possible circular reference!");
                return;
            }

            // Track visited categories to detect circular references
            if (visitedCategories == null)
            {
                visitedCategories = new HashSet<int>();
            }

            if (visitedCategories.Contains(category.Id))
            {
                WriteColorLine(ConsoleColor.Red, $"[ERROR] Circular reference detected for category '{category.Name}' (ID={category.Id})!");
                return;
            }

            visitedCategories.Add(category.Id);

            // Limit indent to prevent huge spacing
            int maxIndent = Math.Min(depth * 2, 100);
            string indent = new string(' ', maxIndent);

            // Count triggers directly in this category
            var triggersInCategory = allTriggers.Where(t => t.ParentId == category.Id).Count();

            WriteLine($"{indent}📁 {category.Name} (ID={category.Id}, ParentId={category.ParentId}) - {triggersInCategory} trigger(s)");

            // Find subcategories
            var subcategories = allCategories.Where(c => c.ParentId == category.Id).OrderBy(c => c.Name).ToList();

            foreach (var subcat in subcategories)
            {
                PrintCategoryTree(subcat, allCategories, allTriggers, depth + 1, new HashSet<int>(visitedCategories));
            }
        }

        /// <summary>
        /// Compares hierarchies between two files
        /// </summary>
        static void CompareHierarchies(MapTriggers source, MapTriggers target, MapTriggers merged)
        {
            WriteLine($"\n===============================================================");
            WriteLine($"HIERARCHY COMPARISON");
            WriteLine($"===============================================================");

            WriteLine("\n--- Category Count ---");
            WriteLine($"  SOURCE: {source.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
            WriteLine($"  TARGET: {target.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
            WriteLine($"  MERGED: {merged.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");

            WriteLine("\n--- Trigger Count ---");
            WriteLine($"  SOURCE: {source.TriggerItems.OfType<TriggerDefinition>().Count()}");
            WriteLine($"  TARGET: {target.TriggerItems.OfType<TriggerDefinition>().Count()}");
            WriteLine($"  MERGED: {merged.TriggerItems.OfType<TriggerDefinition>().Count()}");

            // Check ParentId distribution in MERGED
            WriteLine("\n--- MERGED: ParentId Analysis ---");
            var mergedTriggers = merged.TriggerItems.OfType<TriggerDefinition>().ToList();
            var parentIdGroups = mergedTriggers.GroupBy(t => t.ParentId).OrderByDescending(g => g.Count());

            WriteLine("  Triggers grouped by ParentId:");
            foreach (var group in parentIdGroups.Take(10))
            {
                var category = merged.TriggerItems.OfType<TriggerCategoryDefinition>()
                    .FirstOrDefault(c => c.Id == group.Key);

                string categoryName = category != null ? category.Name : $"<Unknown/Missing ID={group.Key}>";
                WriteLine($"    ParentId {group.Key} ({categoryName}): {group.Count()} trigger(s)");
            }

            // Detect potential nesting issues
            WriteLine("\n--- NESTING ISSUE DETECTION ---");
            var firstCategory = merged.TriggerItems.OfType<TriggerCategoryDefinition>().FirstOrDefault();
            if (firstCategory != null)
            {
                var triggersInFirst = mergedTriggers.Where(t => t.ParentId == firstCategory.Id).Count();
                var totalTriggers = mergedTriggers.Count;

                if (triggersInFirst > totalTriggers * 0.8)  // More than 80% in one category
                {
                    WriteColorLine(ConsoleColor.Red, $"  ❌ CRITICAL: {triggersInFirst}/{totalTriggers} triggers are in '{firstCategory.Name}'!");
                    WriteColorLine(ConsoleColor.Red, $"     This indicates a ParentId assignment bug.");
                }
                else
                {
                    WriteColorLine(ConsoleColor.Green, $"  ✓ Triggers appear to be distributed across categories");
                }
            }
        }
    }
}
