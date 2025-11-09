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
        // Dual output writer - writes to both console and file
        static StreamWriter logFile;
        static TextWriter originalConsoleOut;
        static string logFileName;

        static void Main(string[] args)
        {
            // Set up log file
            logFileName = $"WTG_Diagnostic_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            logFile = new StreamWriter(logFileName, false, Encoding.UTF8);
            logFile.AutoFlush = true;

            // Save original console output
            originalConsoleOut = Console.Out;

            // Create a MultiTextWriter that writes to both console and file
            var multiWriter = new MultiTextWriter(originalConsoleOut, logFile);
            Console.SetOut(multiWriter);

            Console.WriteLine("===============================================================");
            Console.WriteLine("           WTG Binary Diagnostic Tool");
            Console.WriteLine("===============================================================");
            Console.WriteLine();
            Console.WriteLine($"Output will be saved to: {logFileName}");
            Console.WriteLine();

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
                Console.WriteLine("Usage: WTGDiagnostic [source.wtg/.w3x] [target.wtg/.w3x] [merged.wtg/.w3x]");
                Console.WriteLine();
                Console.WriteLine("If no arguments provided, uses default paths:");
                Console.WriteLine("  Source: Source/war3map.wtg");
                Console.WriteLine("  Target: Target/war3map.wtg");
                Console.WriteLine("  Merged: Target/war3map_merged.wtg");
                Console.WriteLine();
                return;
            }

            Console.WriteLine("File paths:");
            Console.WriteLine($"  Source: {sourcePath}");
            Console.WriteLine($"  Target: {targetPath}");
            Console.WriteLine($"  Merged: {mergedPath}");
            Console.WriteLine();

            // Verify files exist
            if (!File.Exists(sourcePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Source file not found: {sourcePath}");
                Console.ResetColor();
                return;
            }
            if (!File.Exists(targetPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Target file not found: {targetPath}");
                Console.ResetColor();
                return;
            }
            if (!File.Exists(mergedPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Merged file not found: {mergedPath}");
                Console.ResetColor();
                return;
            }

            try
            {
                Console.WriteLine("===============================================================");
                Console.WriteLine("STEP 1: Reading Files");
                Console.WriteLine("===============================================================");
                Console.WriteLine();

                // Read source
                Console.WriteLine($"Reading SOURCE: {sourcePath}");
                var (sourceData, sourceTriggers) = ReadWTGFile(sourcePath);
                Console.WriteLine($"  Binary size: {sourceData.Length:N0} bytes");
                ShowStats("SOURCE", sourceTriggers);
                Console.WriteLine();

                // Read target
                Console.WriteLine($"Reading TARGET: {targetPath}");
                var (targetData, targetTriggers) = ReadWTGFile(targetPath);
                Console.WriteLine($"  Binary size: {targetData.Length:N0} bytes");
                ShowStats("TARGET", targetTriggers);
                Console.WriteLine();

                // Read merged
                Console.WriteLine($"Reading MERGED: {mergedPath}");
                var (mergedData, mergedTriggers) = ReadWTGFile(mergedPath);
                Console.WriteLine($"  Binary size: {mergedData.Length:N0} bytes");
                ShowStats("MERGED", mergedTriggers);
                Console.WriteLine();

                Console.WriteLine("===============================================================");
                Console.WriteLine("STEP 2: Binary Hex Dumps");
                Console.WriteLine("===============================================================");
                Console.WriteLine();

                // Dump hex for each file
                Console.WriteLine("--- SOURCE (first 512 bytes) ---");
                DumpHex(sourceData, 512);
                Console.WriteLine();

                Console.WriteLine("--- TARGET (first 512 bytes) ---");
                DumpHex(targetData, 512);
                Console.WriteLine();

                Console.WriteLine("--- MERGED (first 512 bytes) ---");
                DumpHex(mergedData, 512);
                Console.WriteLine();

                Console.WriteLine("===============================================================");
                Console.WriteLine("STEP 3: Binary Comparison");
                Console.WriteLine("===============================================================");
                Console.WriteLine();

                // Compare merged with target (should be similar format)
                Console.WriteLine("Comparing MERGED vs TARGET (both should be 1.27 format):");
                CompareBinary(mergedData, targetData, "MERGED", "TARGET");
                Console.WriteLine();

                Console.WriteLine("===============================================================");
                Console.WriteLine("STEP 4: Detailed Analysis");
                Console.WriteLine("===============================================================");
                Console.WriteLine();

                AnalyzeWTGStructure(sourceData, "SOURCE");
                Console.WriteLine();
                AnalyzeWTGStructure(targetData, "TARGET");
                Console.WriteLine();
                AnalyzeWTGStructure(mergedData, "MERGED");
                Console.WriteLine();

                // NEW: Comprehensive hierarchy analysis
                ShowHierarchy("SOURCE", sourceTriggers);
                ShowHierarchy("TARGET", targetTriggers);
                ShowHierarchy("MERGED", mergedTriggers);

                // NEW: Compare hierarchies
                CompareHierarchies(sourceTriggers, targetTriggers, mergedTriggers);

                Console.WriteLine("===============================================================");
                Console.WriteLine("DIAGNOSIS COMPLETE");
                Console.WriteLine("===============================================================");
                Console.WriteLine();
                Console.WriteLine("Check the output above to see:");
                Console.WriteLine("  1. If MERGED file has correct statistics (variables/triggers)");
                Console.WriteLine("  2. If MERGED binary data looks valid");
                Console.WriteLine("  3. What bytes are different between MERGED and TARGET");
                Console.WriteLine("  4. Whether the WTG header/structure is correct");
                Console.WriteLine("  5. COMPLETE CATEGORY/TRIGGER HIERARCHIES for each file");
                Console.WriteLine("  6. ParentId distribution and nesting issues");
                Console.WriteLine();
                Console.WriteLine("Pay special attention to:");
                Console.WriteLine("  - Are all triggers nested under one category?");
                Console.WriteLine("  - Do category ParentIds match between files?");
                Console.WriteLine("  - Are there orphaned triggers with invalid ParentIds?");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
            finally
            {
                // Restore console output and close log file
                Console.SetOut(originalConsoleOut);

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

        static void ShowStats(string label, MapTriggers triggers)
        {
            Console.WriteLine($"  [{label}] Statistics:");
            Console.WriteLine($"    Format Version: {triggers.FormatVersion}");
            Console.WriteLine($"    SubVersion: {triggers.SubVersion?.ToString() ?? "null"}");
            Console.WriteLine($"    Game Version: {triggers.GameVersion}");
            Console.WriteLine($"    Variables: {triggers.Variables.Count}");
            Console.WriteLine($"    Trigger Items: {triggers.TriggerItems.Count}");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().Count();
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().Count();

            Console.WriteLine($"      - Categories: {categories}");
            Console.WriteLine($"      - Triggers: {triggerDefs}");

            // Sample variables
            if (triggers.Variables.Count > 0)
            {
                Console.WriteLine($"    Sample Variables:");
                foreach (var v in triggers.Variables.Take(5))
                {
                    Console.WriteLine($"      - {v.Name} ({v.Type}) [ParentId={v.ParentId}]");
                }
                if (triggers.Variables.Count > 5)
                {
                    Console.WriteLine($"      ... and {triggers.Variables.Count - 5} more");
                }
            }
        }

        static void DumpHex(byte[] data, int maxBytes)
        {
            int bytesToShow = Math.Min(data.Length, maxBytes);

            for (int i = 0; i < bytesToShow; i += 16)
            {
                // Offset
                Console.Write($"{i:X8}  ");

                // Hex bytes
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < bytesToShow)
                    {
                        Console.Write($"{data[i + j]:X2} ");
                    }
                    else
                    {
                        Console.Write("   ");
                    }

                    if (j == 7) Console.Write(" ");
                }

                Console.Write(" |");

                // ASCII
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < bytesToShow)
                    {
                        byte b = data[i + j];
                        char c = (b >= 32 && b <= 126) ? (char)b : '.';
                        Console.Write(c);
                    }
                }

                Console.WriteLine("|");
            }

            if (data.Length > maxBytes)
            {
                Console.WriteLine($"... {data.Length - maxBytes} more bytes not shown");
            }
        }

        static void CompareBinary(byte[] data1, byte[] data2, string label1, string label2)
        {
            int maxLen = Math.Max(data1.Length, data2.Length);
            int differences = 0;
            int maxDifferencesToShow = 50;

            Console.WriteLine($"File sizes: {label1}={data1.Length:N0} bytes, {label2}={data2.Length:N0} bytes");

            if (data1.Length != data2.Length)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARNING] File sizes differ by {Math.Abs(data1.Length - data2.Length):N0} bytes");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine("Byte-by-byte differences:");

            for (int i = 0; i < maxLen; i++)
            {
                byte b1 = i < data1.Length ? data1[i] : (byte)0;
                byte b2 = i < data2.Length ? data2[i] : (byte)0;

                if (b1 != b2)
                {
                    differences++;
                    if (differences <= maxDifferencesToShow)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  Offset {i:X8}: {label1}={b1:X2} vs {label2}={b2:X2}");
                        Console.ResetColor();
                    }
                }
            }

            if (differences > maxDifferencesToShow)
            {
                Console.WriteLine($"  ... and {differences - maxDifferencesToShow} more differences not shown");
            }

            Console.WriteLine();
            Console.WriteLine($"Total differences: {differences:N0} bytes ({100.0 * differences / maxLen:F2}%)");
        }

        static void AnalyzeWTGStructure(byte[] data, string label)
        {
            Console.WriteLine($"[{label}] WTG Structure Analysis:");

            if (data.Length < 12)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  [ERROR] File too small to be a valid WTG");
                Console.ResetColor();
                return;
            }

            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms);

                // Read header
                string fileId = new string(reader.ReadChars(4));
                int formatVersion = reader.ReadInt32();

                Console.WriteLine($"  File ID: '{fileId}'");
                Console.WriteLine($"  Format Version: {formatVersion}");

                if (fileId != "WTG!")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [ERROR] Invalid file ID (expected 'WTG!', got '{fileId}')");
                    Console.ResetColor();
                }

                // Continue reading based on format
                if (formatVersion == 7 || formatVersion == 4)
                {
                    int subVersion = reader.ReadInt32();
                    Console.WriteLine($"  SubVersion: {subVersion}");

                    int variableCount = reader.ReadInt32();
                    Console.WriteLine($"  Variable Count: {variableCount}");

                    if (variableCount == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("  [WARNING] Zero variables - this might indicate empty/corrupted file");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  [WARNING] Unknown format version: {formatVersion}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [ERROR] Failed to parse structure: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Shows complete category and trigger hierarchy
        /// </summary>
        static void ShowHierarchy(string label, MapTriggers triggers)
        {
            Console.WriteLine($"\n===============================================================");
            Console.WriteLine($"{label} - COMPLETE HIERARCHY");
            Console.WriteLine($"===============================================================");

            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggerDefs = triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            Console.WriteLine($"\nTotal Categories: {categories.Count}");
            Console.WriteLine($"Total Triggers: {triggerDefs.Count}");

            // Show category tree
            Console.WriteLine($"\n--- CATEGORY HIERARCHY ---");
            var rootCategories = categories.Where(c => c.ParentId == -1 || c.ParentId == 0).ToList();

            if (rootCategories.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  [WARNING] No root-level categories found!");
                Console.ResetColor();

                // Show all categories with their ParentIds
                Console.WriteLine("\n  All categories:");
                foreach (var cat in categories.OrderBy(c => c.Id))
                {
                    Console.WriteLine($"    - {cat.Name} (ID={cat.Id}, ParentId={cat.ParentId})");
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
            Console.WriteLine($"\n--- TRIGGER DISTRIBUTION ---");
            foreach (var category in categories.OrderBy(c => c.Name))
            {
                var triggersInCat = triggerDefs.Where(t => t.ParentId == category.Id).ToList();
                if (triggersInCat.Count > 0)
                {
                    Console.WriteLine($"  {category.Name} (ID={category.Id}): {triggersInCat.Count} trigger(s)");
                }
            }

            // Check for orphaned triggers
            var orphanedTriggers = triggerDefs.Where(t =>
                !categories.Any(c => c.Id == t.ParentId) && t.ParentId != -1 && t.ParentId != 0).ToList();

            if (orphanedTriggers.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n  [WARNING] {orphanedTriggers.Count} orphaned trigger(s) found:");
                foreach (var t in orphanedTriggers.Take(10))
                {
                    Console.WriteLine($"    - {t.Name} (ID={t.Id}, ParentId={t.ParentId})");
                }
                if (orphanedTriggers.Count > 10)
                {
                    Console.WriteLine($"    ... and {orphanedTriggers.Count - 10} more");
                }
                Console.ResetColor();
            }

            // Check for triggers with ParentId=0 or -1 (root-level triggers)
            var rootTriggers = triggerDefs.Where(t => t.ParentId == 0 || t.ParentId == -1).ToList();
            if (rootTriggers.Count > 0)
            {
                Console.WriteLine($"\n  Root-level triggers (ParentId=0 or -1): {rootTriggers.Count}");
                foreach (var t in rootTriggers.Take(5))
                {
                    Console.WriteLine($"    - {t.Name} (ID={t.Id}, ParentId={t.ParentId})");
                }
                if (rootTriggers.Count > 5)
                {
                    Console.WriteLine($"    ... and {rootTriggers.Count - 5} more");
                }
            }
        }

        /// <summary>
        /// Recursively prints category tree
        /// </summary>
        static void PrintCategoryTree(TriggerCategoryDefinition category,
            List<TriggerCategoryDefinition> allCategories,
            List<TriggerDefinition> allTriggers,
            int depth)
        {
            string indent = new string(' ', depth * 2);

            // Count triggers directly in this category
            var triggersInCategory = allTriggers.Where(t => t.ParentId == category.Id).Count();

            Console.WriteLine($"{indent}📁 {category.Name} (ID={category.Id}, ParentId={category.ParentId}) - {triggersInCategory} trigger(s)");

            // Find subcategories
            var subcategories = allCategories.Where(c => c.ParentId == category.Id).OrderBy(c => c.Name).ToList();

            foreach (var subcat in subcategories)
            {
                PrintCategoryTree(subcat, allCategories, allTriggers, depth + 1);
            }
        }

        /// <summary>
        /// Compares hierarchies between two files
        /// </summary>
        static void CompareHierarchies(MapTriggers source, MapTriggers target, MapTriggers merged)
        {
            Console.WriteLine($"\n===============================================================");
            Console.WriteLine($"HIERARCHY COMPARISON");
            Console.WriteLine($"===============================================================");

            Console.WriteLine("\n--- Category Count ---");
            Console.WriteLine($"  SOURCE: {source.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
            Console.WriteLine($"  TARGET: {target.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");
            Console.WriteLine($"  MERGED: {merged.TriggerItems.OfType<TriggerCategoryDefinition>().Count()}");

            Console.WriteLine("\n--- Trigger Count ---");
            Console.WriteLine($"  SOURCE: {source.TriggerItems.OfType<TriggerDefinition>().Count()}");
            Console.WriteLine($"  TARGET: {target.TriggerItems.OfType<TriggerDefinition>().Count()}");
            Console.WriteLine($"  MERGED: {merged.TriggerItems.OfType<TriggerDefinition>().Count()}");

            // Check ParentId distribution in MERGED
            Console.WriteLine("\n--- MERGED: ParentId Analysis ---");
            var mergedTriggers = merged.TriggerItems.OfType<TriggerDefinition>().ToList();
            var parentIdGroups = mergedTriggers.GroupBy(t => t.ParentId).OrderByDescending(g => g.Count());

            Console.WriteLine("  Triggers grouped by ParentId:");
            foreach (var group in parentIdGroups.Take(10))
            {
                var category = merged.TriggerItems.OfType<TriggerCategoryDefinition>()
                    .FirstOrDefault(c => c.Id == group.Key);

                string categoryName = category != null ? category.Name : $"<Unknown/Missing ID={group.Key}>";
                Console.WriteLine($"    ParentId {group.Key} ({categoryName}): {group.Count()} trigger(s)");
            }

            // Detect potential nesting issues
            Console.WriteLine("\n--- NESTING ISSUE DETECTION ---");
            var firstCategory = merged.TriggerItems.OfType<TriggerCategoryDefinition>().FirstOrDefault();
            if (firstCategory != null)
            {
                var triggersInFirst = mergedTriggers.Where(t => t.ParentId == firstCategory.Id).Count();
                var totalTriggers = mergedTriggers.Count;

                if (triggersInFirst > totalTriggers * 0.8)  // More than 80% in one category
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ❌ CRITICAL: {triggersInFirst}/{totalTriggers} triggers are in '{firstCategory.Name}'!");
                    Console.WriteLine($"     This indicates a ParentId assignment bug.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Triggers appear to be distributed across categories");
                    Console.ResetColor();
                }
            }
        }
    }

    /// <summary>
    /// Text writer that writes to multiple TextWriter instances simultaneously
    /// </summary>
    class MultiTextWriter : TextWriter
    {
        private readonly TextWriter[] writers;

        public MultiTextWriter(params TextWriter[] writers)
        {
            this.writers = writers;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            foreach (var writer in writers)
            {
                writer.Write(value);
            }
        }

        public override void Write(string value)
        {
            foreach (var writer in writers)
            {
                writer.Write(value);
            }
        }

        public override void WriteLine(string value)
        {
            foreach (var writer in writers)
            {
                writer.WriteLine(value);
            }
        }

        public override void WriteLine()
        {
            foreach (var writer in writers)
            {
                writer.WriteLine();
            }
        }

        public override void Flush()
        {
            foreach (var writer in writers)
            {
                writer.Flush();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var writer in writers)
                {
                    writer.Flush();
                }
            }
            base.Dispose(disposing);
        }
    }
}
