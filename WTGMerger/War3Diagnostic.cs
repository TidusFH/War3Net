using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using War3Net.Build.Extensions;
using War3Net.Build.Script;
using War3Net.IO.Mpq;

namespace WTGMerger
{
    /// <summary>
    /// Comprehensive diagnostic tool for analyzing and comparing WTG files
    /// </summary>
    public static class War3Diagnostic
    {
        public class DiagnosticResult
        {
            public string FilePath { get; set; } = "";
            public long FileSize { get; set; }
            public MapTriggers? Triggers { get; set; }
            public byte[]? RawData { get; set; }
            public List<string> Warnings { get; set; } = new();
            public List<string> Errors { get; set; } = new();
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        /// <summary>
        /// Runs comprehensive diagnostic on a WTG file or map archive
        /// </summary>
        public static DiagnosticResult RunDiagnostic(string filePath, bool includeHexDump = false)
        {
            var result = new DiagnosticResult { FilePath = filePath };

            try
            {
                // Determine if it's an archive or WTG file
                bool isArchive = filePath.EndsWith(".w3x", StringComparison.OrdinalIgnoreCase) ||
                                filePath.EndsWith(".w3m", StringComparison.OrdinalIgnoreCase);

                if (isArchive)
                {
                    result.Triggers = ReadFromArchive(filePath);
                }
                else
                {
                    result.Triggers = ReadFromWTG(filePath);
                    result.FileSize = new FileInfo(filePath).Length;

                    if (includeHexDump)
                    {
                        result.RawData = File.ReadAllBytes(filePath);
                    }
                }

                // Analyze the triggers
                if (result.Triggers != null)
                {
                    AnalyzeTriggers(result);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to read file: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Compares three WTG files and generates a detailed comparison report
        /// </summary>
        public static string CompareFiles(string sourcePath, string targetPath, string mergedPath)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("           WTG Binary Diagnostic Tool");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            var sb = new StringBuilder();
            sb.AppendLine("Output will be saved to: WTG_Diagnostic_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt\n");

            sb.AppendLine("File paths:");
            sb.AppendLine($"  Source: {sourcePath}");
            sb.AppendLine($"  Target: {targetPath}");
            sb.AppendLine($"  Merged: {mergedPath}");

            // Read all three files
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("STEP 1: Reading Files");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");

            var sourceResult = RunDiagnostic(sourcePath, true);
            sb.AppendLine(GenerateReport("SOURCE", sourceResult));

            var targetResult = RunDiagnostic(targetPath, true);
            sb.AppendLine(GenerateReport("TARGET", targetResult));

            var mergedResult = RunDiagnostic(mergedPath, true);
            sb.AppendLine(GenerateReport("MERGED", mergedResult));

            // Binary hex dumps
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("STEP 2: Binary Hex Dumps");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");

            sb.AppendLine("--- SOURCE (first 512 bytes) ---");
            sb.AppendLine(GetHexDump(sourceResult.RawData, 512));

            sb.AppendLine("\n--- TARGET (first 512 bytes) ---");
            sb.AppendLine(GetHexDump(targetResult.RawData, 512));

            sb.AppendLine("\n--- MERGED (first 512 bytes) ---");
            sb.AppendLine(GetHexDump(mergedResult.RawData, 512));

            // Binary comparison
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("STEP 3: Binary Comparison");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");

            sb.AppendLine(CompareBinaryData(mergedResult, targetResult));

            // Detailed analysis
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("STEP 4: Detailed Analysis");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");

            sb.AppendLine(AnalyzeStructure("SOURCE", sourceResult));
            sb.AppendLine(AnalyzeStructure("TARGET", targetResult));
            sb.AppendLine(AnalyzeStructure("MERGED", mergedResult));

            // Hierarchy comparison
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("SOURCE - COMPLETE HIERARCHY");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
            sb.AppendLine(GenerateHierarchyReport(sourceResult));

            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("TARGET - COMPLETE HIERARCHY");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
            sb.AppendLine(GenerateHierarchyReport(targetResult));

            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("MERGED - COMPLETE HIERARCHY");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
            sb.AppendLine(GenerateHierarchyReport(mergedResult));

            // Hierarchy comparison
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("HIERARCHY COMPARISON");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
            sb.AppendLine(CompareHierarchies(sourceResult, targetResult, mergedResult));

            // ENHANCEMENT: File order analysis (critical for 1.27 format)
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("FILE ORDER ANALYSIS (Important for WC3 1.27 Visual Nesting)");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
            sb.AppendLine(AnalyzeFileOrder("MERGED", mergedResult));

            // ENHANCEMENT: ParentId distribution
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("PARENTID DISTRIBUTION ANALYSIS");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
            sb.AppendLine(AnalyzeParentIdDistribution("MERGED", mergedResult));

            // ENHANCEMENT: Binary section analysis
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("BINARY SECTION ANALYSIS");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
            sb.AppendLine(AnalyzeBinarySections(mergedResult));

            // ENHANCEMENT: Corruption pattern detection
            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("CORRUPTION PATTERN DETECTION");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
            sb.AppendLine(DetectCorruptionPatterns(mergedResult));

            sb.AppendLine("\n═══════════════════════════════════════════════════════════");
            sb.AppendLine("DIAGNOSIS COMPLETE");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");

            sb.AppendLine("Check the output above to see:");
            sb.AppendLine("  1. If MERGED file has correct statistics (variables/triggers)");
            sb.AppendLine("  2. If MERGED binary data looks valid");
            sb.AppendLine("  3. What bytes are different between MERGED and TARGET");
            sb.AppendLine("  4. Whether the WTG header/structure is correct");
            sb.AppendLine("  5. COMPLETE CATEGORY/TRIGGER HIERARCHIES for each file");
            sb.AppendLine("  6. ParentId distribution and nesting issues\n");

            sb.AppendLine("Pay special attention to:");
            sb.AppendLine("  - Are all triggers nested under one category?");
            sb.AppendLine("  - Do category ParentIds match between files?");
            sb.AppendLine("  - Are there orphaned triggers with invalid ParentIds?");

            string report = sb.ToString();
            Console.WriteLine(report);

            // Save to file
            string outputPath = "WTG_Diagnostic_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
            File.WriteAllText(outputPath, report);

            return report;
        }

        private static MapTriggers ReadFromArchive(string archivePath)
        {
            using var archive = MpqArchive.Open(archivePath, true);
            archive.DiscoverFileNames();

            var triggerFileName = MapTriggers.FileName;
            if (!archive.FileExists(triggerFileName))
            {
                throw new FileNotFoundException($"Trigger file '{triggerFileName}' not found in archive");
            }

            using var triggerStream = archive.OpenFile(triggerFileName);
            using var reader = new BinaryReader(triggerStream);

            var constructorInfo = typeof(MapTriggers).GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new[] { typeof(BinaryReader), typeof(TriggerData) },
                null);

            return (MapTriggers)constructorInfo!.Invoke(new object[] { reader, TriggerData.Default });
        }

        private static MapTriggers ReadFromWTG(string wtgPath)
        {
            using var fileStream = File.OpenRead(wtgPath);
            using var reader = new BinaryReader(fileStream);

            var constructorInfo = typeof(MapTriggers).GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new[] { typeof(BinaryReader), typeof(TriggerData) },
                null);

            return (MapTriggers)constructorInfo!.Invoke(new object[] { reader, TriggerData.Default });
        }

        private static void AnalyzeTriggers(DiagnosticResult result)
        {
            var triggers = result.Triggers!;

            // Store metadata
            result.Metadata["FormatVersion"] = triggers.FormatVersion;
            result.Metadata["SubVersion"] = triggers.SubVersion?.ToString() ?? "null";
            result.Metadata["GameVersion"] = triggers.GameVersion;
            result.Metadata["VariableCount"] = triggers.Variables.Count;
            result.Metadata["TriggerItemCount"] = triggers.TriggerItems.Count;

            // Count item types
            var itemCounts = new Dictionary<TriggerItemType, int>();
            foreach (var item in triggers.TriggerItems)
            {
                if (!itemCounts.ContainsKey(item.Type))
                    itemCounts[item.Type] = 0;
                itemCounts[item.Type]++;
            }
            result.Metadata["ItemCounts"] = itemCounts;

            // Check for orphaned triggers
            var categories = triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var validCategoryIds = new HashSet<int>(categories.Select(c => c.Id));
            validCategoryIds.Add(-1);

            var orphanedTriggers = triggers.TriggerItems
                .OfType<TriggerDefinition>()
                .Where(t => t.ParentId >= 0 && !validCategoryIds.Contains(t.ParentId))
                .ToList();

            if (orphanedTriggers.Any())
            {
                result.Warnings.Add($"Found {orphanedTriggers.Count} orphaned trigger(s) with invalid ParentIds");
            }

            // Check for orphaned categories
            var orphanedCategories = categories
                .Where(c => c.ParentId >= 0 && !validCategoryIds.Contains(c.ParentId))
                .ToList();

            if (orphanedCategories.Any())
            {
                result.Warnings.Add($"Found {orphanedCategories.Count} orphaned category(ies) with invalid ParentIds");
            }

            // Check for duplicate variable IDs
            var variableIdGroups = triggers.Variables.GroupBy(v => v.Id).Where(g => g.Count() > 1).ToList();
            if (variableIdGroups.Any())
            {
                result.Errors.Add($"Found {variableIdGroups.Count} duplicate variable ID(s)");
            }
        }

        private static string GenerateReport(string label, DiagnosticResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Reading {label}: {result.FilePath}");

            if (result.Errors.Any())
            {
                sb.AppendLine("  ERRORS:");
                foreach (var error in result.Errors)
                {
                    sb.AppendLine($"    - {error}");
                }
                return sb.ToString();
            }

            if (result.FileSize > 0)
            {
                sb.AppendLine($"  Binary size: {result.FileSize:N0} bytes");
            }

            sb.AppendLine($"  [{label}] Statistics:");
            sb.AppendLine($"    Format Version: {result.Metadata.GetValueOrDefault("FormatVersion", "unknown")}");
            sb.AppendLine($"    SubVersion: {result.Metadata.GetValueOrDefault("SubVersion", "unknown")}");
            sb.AppendLine($"    Game Version: {result.Metadata.GetValueOrDefault("GameVersion", "unknown")}");
            sb.AppendLine($"    Variables: {result.Metadata.GetValueOrDefault("VariableCount", 0)}");
            sb.AppendLine($"    Trigger Items: {result.Metadata.GetValueOrDefault("TriggerItemCount", 0)}");

            if (result.Metadata.TryGetValue("ItemCounts", out var itemCountsObj) &&
                itemCountsObj is Dictionary<TriggerItemType, int> itemCounts)
            {
                foreach (var kvp in itemCounts)
                {
                    sb.AppendLine($"      - {kvp.Key}: {kvp.Value}");
                }
            }

            // Show sample variables
            if (result.Triggers != null && result.Triggers.Variables.Count > 0)
            {
                sb.AppendLine("    Sample Variables:");
                foreach (var v in result.Triggers.Variables.Take(5))
                {
                    sb.AppendLine($"      - {v.Name} ({v.Type}) [ParentId={v.ParentId}]");
                }
                if (result.Triggers.Variables.Count > 5)
                {
                    sb.AppendLine($"      ... and {result.Triggers.Variables.Count - 5} more");
                }
            }

            if (result.Warnings.Any())
            {
                sb.AppendLine("  WARNINGS:");
                foreach (var warning in result.Warnings)
                {
                    sb.AppendLine($"    - {warning}");
                }
            }

            return sb.ToString();
        }

        private static string GetHexDump(byte[]? data, int maxBytes)
        {
            if (data == null || data.Length == 0)
                return "(no data)";

            var sb = new StringBuilder();
            int bytesToShow = Math.Min(data.Length, maxBytes);

            for (int i = 0; i < bytesToShow; i += 16)
            {
                sb.Append($"{i:X8}  ");

                // Hex values
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < bytesToShow)
                        sb.Append($"{data[i + j]:X2} ");
                    else
                        sb.Append("   ");

                    if (j == 7)
                        sb.Append(" ");
                }

                sb.Append(" |");

                // ASCII values
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < bytesToShow)
                    {
                        byte b = data[i + j];
                        sb.Append(b >= 32 && b < 127 ? (char)b : '.');
                    }
                }

                sb.AppendLine("|");
            }

            if (data.Length > bytesToShow)
            {
                sb.AppendLine($"... {data.Length - bytesToShow:N0} more bytes not shown");
            }

            return sb.ToString();
        }

        private static string CompareBinaryData(DiagnosticResult merged, DiagnosticResult target)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Comparing MERGED vs TARGET (both should be 1.27 format):");

            if (merged.RawData == null || target.RawData == null)
            {
                sb.AppendLine("  Cannot compare: missing binary data");
                return sb.ToString();
            }

            sb.AppendLine($"File sizes: MERGED={merged.RawData.Length:N0} bytes, TARGET={target.RawData.Length:N0} bytes");

            long sizeDiff = merged.RawData.Length - target.RawData.Length;
            if (sizeDiff != 0)
            {
                sb.AppendLine($"[WARNING] File sizes differ by {Math.Abs(sizeDiff):N0} bytes");
            }

            // Compare byte by byte
            int maxLen = Math.Min(merged.RawData.Length, target.RawData.Length);
            var differences = new List<(int offset, byte mergedByte, byte targetByte)>();

            for (int i = 0; i < maxLen; i++)
            {
                if (merged.RawData[i] != target.RawData[i])
                {
                    differences.Add((i, merged.RawData[i], target.RawData[i]));
                }
            }

            sb.AppendLine($"\nByte-by-byte differences (first 50):");
            foreach (var diff in differences.Take(50))
            {
                sb.AppendLine($"  Offset 0x{diff.offset:X8}: MERGED={diff.mergedByte:X2} vs TARGET={diff.targetByte:X2}");
            }

            if (differences.Count > 50)
            {
                sb.AppendLine($"  ... and {differences.Count - 50:N0} more differences not shown");
            }

            sb.AppendLine($"\nTotal differences: {differences.Count:N0} bytes ({100.0 * differences.Count / maxLen:F2}%)");

            return sb.ToString();
        }

        private static string AnalyzeStructure(string label, DiagnosticResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{label}] WTG Structure Analysis:");

            if (result.RawData != null && result.RawData.Length >= 8)
            {
                uint signature = BitConverter.ToUInt32(result.RawData, 0);
                int version = BitConverter.ToInt32(result.RawData, 4);

                sb.AppendLine($"  File ID: '{Encoding.ASCII.GetString(result.RawData, 0, 4)}'");
                sb.AppendLine($"  Format Version: {version}");

                if (result.RawData.Length >= 12)
                {
                    int categoryCount = BitConverter.ToInt32(result.RawData, 8);
                    sb.AppendLine($"  Category Count: {categoryCount}");
                }

                sb.AppendLine($"  SubVersion: {result.Metadata.GetValueOrDefault("SubVersion", "unknown")} ({(result.Metadata.GetValueOrDefault("SubVersion")?.ToString() == "null" ? "1.27 or earlier format" : "1.31+ format")})");
                sb.AppendLine("  Note: Variables/triggers can only be counted by full War3Net parse");
            }

            sb.AppendLine($"\n  ℹ For accurate counts, use War3Net parser (shown in statistics above)");

            return sb.ToString();
        }

        private static string GenerateHierarchyReport(DiagnosticResult result)
        {
            if (result.Triggers == null)
                return "  (no trigger data)";

            var sb = new StringBuilder();
            var categories = result.Triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggers = result.Triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            sb.AppendLine($"Total Categories: {categories.Count}");
            sb.AppendLine($"Total Triggers: {triggers.Count}");

            sb.AppendLine("\n--- CATEGORY HIERARCHY ---");
            var rootCategories = categories.Where(c => c.ParentId == -1 || c.ParentId == 0).ToList();

            foreach (var cat in rootCategories)
            {
                PrintCategoryTree(sb, cat, categories, triggers, 0);
            }

            sb.AppendLine("\n--- TRIGGER DISTRIBUTION ---");
            var triggersByParent = triggers.GroupBy(t => t.ParentId).OrderByDescending(g => g.Count()).ToList();

            foreach (var group in triggersByParent)
            {
                var parentCat = categories.FirstOrDefault(c => c.Id == group.Key);
                string parentName = parentCat != null ? parentCat.Name : (group.Key == -1 ? "<Root>" : $"<Unknown ID={group.Key}>");
                sb.AppendLine($"  {parentName} (ID={group.Key}): {group.Count()} trigger(s)");
            }

            // Find root-level triggers
            var rootTriggers = triggers.Where(t => t.ParentId == 0 || t.ParentId == -1).ToList();
            if (rootTriggers.Any())
            {
                sb.AppendLine($"\n  Root-level triggers (ParentId=0 or -1): {rootTriggers.Count}");
                foreach (var t in rootTriggers.Take(5))
                {
                    sb.AppendLine($"    - {t.Name} (ID={t.Id}, ParentId={t.ParentId})");
                }
                if (rootTriggers.Count > 5)
                {
                    sb.AppendLine($"    ... and {rootTriggers.Count - 5} more");
                }
            }

            // Check for orphaned triggers
            var validCategoryIds = new HashSet<int>(categories.Select(c => c.Id));
            validCategoryIds.Add(-1);
            var orphanedTriggers = triggers.Where(t => t.ParentId >= 0 && !validCategoryIds.Contains(t.ParentId)).ToList();

            if (orphanedTriggers.Any())
            {
                sb.AppendLine($"\n  [WARNING] {orphanedTriggers.Count} orphaned trigger(s) found:");
                foreach (var t in orphanedTriggers.Take(10))
                {
                    sb.AppendLine($"    - {t.Name} (ID={t.Id}, ParentId={t.ParentId})");
                }
                if (orphanedTriggers.Count > 10)
                {
                    sb.AppendLine($"    ... and {orphanedTriggers.Count - 10} more");
                }
            }

            return sb.ToString();
        }

        private static void PrintCategoryTree(StringBuilder sb, TriggerCategoryDefinition category,
            List<TriggerCategoryDefinition> allCategories, List<TriggerDefinition> allTriggers, int indent)
        {
            string indentStr = new string(' ', indent * 2);
            int triggerCount = allTriggers.Count(t => t.ParentId == category.Id);

            sb.AppendLine($"{indentStr}📁 {category.Name} (ID={category.Id}, ParentId={category.ParentId}) - {triggerCount} trigger(s)");

            // Check for circular reference
            var visited = new HashSet<int> { category.Id };
            var current = category;
            while (current.ParentId >= 0)
            {
                if (visited.Contains(current.ParentId))
                {
                    sb.AppendLine($"{indentStr}[ERROR] Circular reference detected for category '{category.Name}' (ID={category.Id})!");
                    break;
                }
                visited.Add(current.ParentId);
                current = allCategories.FirstOrDefault(c => c.Id == current.ParentId);
                if (current == null) break;
            }

            // Print child categories
            var children = allCategories.Where(c => c.ParentId == category.Id && c.Id != category.Id).ToList();
            foreach (var child in children)
            {
                PrintCategoryTree(sb, child, allCategories, allTriggers, indent + 1);
            }
        }

        private static string CompareHierarchies(DiagnosticResult source, DiagnosticResult target, DiagnosticResult merged)
        {
            var sb = new StringBuilder();

            sb.AppendLine("--- Category Count ---");
            sb.AppendLine($"  SOURCE: {source.Triggers?.TriggerItems.OfType<TriggerCategoryDefinition>().Count() ?? 0}");
            sb.AppendLine($"  TARGET: {target.Triggers?.TriggerItems.OfType<TriggerCategoryDefinition>().Count() ?? 0}");
            sb.AppendLine($"  MERGED: {merged.Triggers?.TriggerItems.OfType<TriggerCategoryDefinition>().Count() ?? 0}");

            sb.AppendLine("\n--- Trigger Count ---");
            sb.AppendLine($"  SOURCE: {source.Triggers?.TriggerItems.OfType<TriggerDefinition>().Count() ?? 0}");
            sb.AppendLine($"  TARGET: {target.Triggers?.TriggerItems.OfType<TriggerDefinition>().Count() ?? 0}");
            sb.AppendLine($"  MERGED: {merged.Triggers?.TriggerItems.OfType<TriggerDefinition>().Count() ?? 0}");

            // Analyze merged ParentIds
            if (merged.Triggers != null)
            {
                sb.AppendLine("\n--- MERGED: ParentId Analysis ---");
                var triggers = merged.Triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
                var triggersByParent = triggers.GroupBy(t => t.ParentId).OrderByDescending(g => g.Count()).Take(10).ToList();

                sb.AppendLine("  Triggers grouped by ParentId:");
                foreach (var group in triggersByParent)
                {
                    var categories = merged.Triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
                    var parentCat = categories.FirstOrDefault(c => c.Id == group.Key);
                    string parentName = parentCat != null ? parentCat.Name : $"<Unknown/Missing ID={group.Key}>";
                    sb.AppendLine($"    ParentId {group.Key} ({parentName}): {group.Count()} trigger(s)");
                }
            }

            sb.AppendLine("\n--- NESTING ISSUE DETECTION ---");
            if (merged.Triggers != null)
            {
                var triggers = merged.Triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
                var uniqueParents = triggers.Select(t => t.ParentId).Distinct().Count();

                if (uniqueParents <= 2)
                {
                    sb.AppendLine("  ⚠ WARNING: Most/all triggers have the same ParentId - possible nesting issue");
                }
                else
                {
                    sb.AppendLine("  ✓ Triggers appear to be distributed across categories");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// ENHANCEMENT: Analyzes file order of trigger items (critical for WC3 1.27 format)
        /// </summary>
        private static string AnalyzeFileOrder(string label, DiagnosticResult result)
        {
            if (result.Triggers == null)
                return "  (no trigger data)";

            var sb = new StringBuilder();
            sb.AppendLine($"[{label}] TriggerItems File Order:");
            sb.AppendLine($"Total items: {result.Triggers.TriggerItems.Count}\n");

            // Analyze order patterns
            int categoryCount = 0, triggerCount = 0, otherCount = 0;
            int firstTriggerIndex = -1, lastCategoryIndex = -1;

            for (int i = 0; i < result.Triggers.TriggerItems.Count; i++)
            {
                var item = result.Triggers.TriggerItems[i];
                if (item is TriggerCategoryDefinition)
                {
                    categoryCount++;
                    lastCategoryIndex = i;
                }
                else if (item is TriggerDefinition)
                {
                    triggerCount++;
                    if (firstTriggerIndex == -1)
                        firstTriggerIndex = i;
                }
                else
                {
                    otherCount++;
                }
            }

            sb.AppendLine($"Item type distribution:");
            sb.AppendLine($"  Categories: {categoryCount}");
            sb.AppendLine($"  Triggers: {triggerCount}");
            sb.AppendLine($"  Other: {otherCount}");

            // Check for visual nesting issue
            if (firstTriggerIndex != -1 && lastCategoryIndex > firstTriggerIndex)
            {
                sb.AppendLine($"\n⚠ WARNING: Categories appear AFTER triggers in file order!");
                sb.AppendLine($"  First trigger at index: {firstTriggerIndex}");
                sb.AppendLine($"  Last category at index: {lastCategoryIndex}");
                sb.AppendLine($"  This causes visual nesting in World Editor (WC3 1.27 format)");
            }
            else if (firstTriggerIndex != -1)
            {
                sb.AppendLine($"\n✓ File order is correct: all categories before triggers");
            }

            // Show first 20 items with types
            sb.AppendLine($"\nFirst 20 items in file:");
            for (int i = 0; i < Math.Min(20, result.Triggers.TriggerItems.Count); i++)
            {
                var item = result.Triggers.TriggerItems[i];
                string itemType = item.GetType().Name;
                string itemName = item is TriggerCategoryDefinition cat ? cat.Name :
                                  item is TriggerDefinition trig ? trig.Name :
                                  item.Type.ToString();
                int itemId = item is TriggerCategoryDefinition c ? c.Id :
                             item is TriggerDefinition t ? t.Id : -999;
                int parentId = item is TriggerCategoryDefinition ca ? ca.ParentId :
                               item is TriggerDefinition tr ? tr.ParentId : -999;

                sb.AppendLine($"  [{i:D3}] {itemType,-30} ID={itemId,-10} ParentId={parentId,-10} '{itemName}'");
            }

            if (result.Triggers.TriggerItems.Count > 20)
            {
                sb.AppendLine($"  ... and {result.Triggers.TriggerItems.Count - 20} more items");
            }

            return sb.ToString();
        }

        /// <summary>
        /// ENHANCEMENT: Analyzes ParentId distribution to detect nesting issues
        /// </summary>
        private static string AnalyzeParentIdDistribution(string label, DiagnosticResult result)
        {
            if (result.Triggers == null)
                return "  (no trigger data)";

            var sb = new StringBuilder();
            sb.AppendLine($"[{label}] ParentId Distribution:\n");

            // Category ParentIds
            var categories = result.Triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var catParentIds = categories.GroupBy(c => c.ParentId).OrderByDescending(g => g.Count()).ToList();

            sb.AppendLine($"Category ParentId distribution ({categories.Count} total):");
            foreach (var group in catParentIds)
            {
                int count = group.Count();
                double percent = 100.0 * count / categories.Count;
                string meaning = group.Key == -1 ? "(Root level)" :
                                 group.Key == 0 ? "(Root/Default in 1.27)" :
                                 $"(Parent category ID={group.Key})";
                sb.AppendLine($"  ParentId={group.Key,-6} {meaning,-30} {count,4} categories ({percent:F1}%)");
            }

            // Trigger ParentIds
            var triggers = result.Triggers.TriggerItems.OfType<TriggerDefinition>().ToList();
            var trigParentIds = triggers.GroupBy(t => t.ParentId).OrderByDescending(g => g.Count()).ToList();

            sb.AppendLine($"\nTrigger ParentId distribution ({triggers.Count} total):");
            foreach (var group in trigParentIds.Take(15))
            {
                int count = group.Count();
                double percent = 100.0 * count / triggers.Count;
                var parentCat = categories.FirstOrDefault(c => c.Id == group.Key);
                string meaning = group.Key == -1 ? "(Root level)" :
                                 group.Key == 0 ? "(Root/Default)" :
                                 parentCat != null ? $"(Category: {parentCat.Name})" :
                                 "(⚠ Orphaned - no category exists)";
                sb.AppendLine($"  ParentId={group.Key,-10} {meaning,-40} {count,4} triggers ({percent:F1}%)");
            }

            if (trigParentIds.Count > 15)
            {
                sb.AppendLine($"  ... and {trigParentIds.Count - 15} more unique ParentId values");
            }

            // Check for nesting issue
            if (trigParentIds.Count == 1)
            {
                sb.AppendLine($"\n⚠ CRITICAL: ALL {triggers.Count} triggers have the SAME ParentId={trigParentIds[0].Key}");
                sb.AppendLine($"  This is a common bug where all triggers get nested under one category.");
            }
            else if (trigParentIds.Count <= 3 && triggers.Count > 10)
            {
                sb.AppendLine($"\n⚠ WARNING: Only {trigParentIds.Count} unique ParentIds for {triggers.Count} triggers");
                sb.AppendLine($"  Most triggers may be incorrectly nested under few categories.");
            }
            else
            {
                sb.AppendLine($"\n✓ Good distribution: {trigParentIds.Count} unique ParentIds for {triggers.Count} triggers");
            }

            return sb.ToString();
        }

        /// <summary>
        /// ENHANCEMENT: Analyzes binary file sections
        /// </summary>
        private static string AnalyzeBinarySections(DiagnosticResult result)
        {
            if (result.RawData == null || result.RawData.Length < 12)
                return "  (insufficient binary data)";

            var sb = new StringBuilder();
            int offset = 0;

            try
            {
                // Header
                uint signature = BitConverter.ToUInt32(result.RawData, offset);
                string sigStr = Encoding.ASCII.GetString(result.RawData, offset, 4);
                offset += 4;

                int formatVersion = BitConverter.ToInt32(result.RawData, offset);
                offset += 4;

                sb.AppendLine($"File Header (bytes 0-7):");
                sb.AppendLine($"  Signature: '{sigStr}' (0x{signature:X8})");
                sb.AppendLine($"  Format Version: {formatVersion}");

                // Check if it's 1.27 or 1.31+
                bool is127Format = result.Metadata.GetValueOrDefault("SubVersion")?.ToString() == "null";

                if (is127Format)
                {
                    sb.AppendLine($"\nWC3 1.27 Format (SubVersion=null):");

                    // Categories
                    int categoryCount = BitConverter.ToInt32(result.RawData, offset);
                    int categoriesStartOffset = offset;
                    offset += 4;
                    sb.AppendLine($"  Categories section starts at byte {categoriesStartOffset}");
                    sb.AppendLine($"  Category count: {categoryCount}");

                    // We can't easily parse to find exact section boundaries without parsing the full structure
                    sb.AppendLine($"\n  Note: Exact section boundaries require full parse");
                }
                else
                {
                    sb.AppendLine($"\nWC3 1.31+ Format (SubVersion={result.Metadata.GetValueOrDefault("SubVersion")}):");

                    int subVersion = BitConverter.ToInt32(result.RawData, offset);
                    offset += 4;
                    sb.AppendLine($"  SubVersion: {subVersion}");

                    // TriggerItemType counts
                    sb.AppendLine($"  TriggerItemType counts:");
                    foreach (TriggerItemType type in Enum.GetValues(typeof(TriggerItemType)))
                    {
                        if (offset + 8 <= result.RawData.Length)
                        {
                            int count = BitConverter.ToInt32(result.RawData, offset);
                            offset += 4;
                            int deleted = BitConverter.ToInt32(result.RawData, offset);
                            offset += 4;
                            sb.AppendLine($"    {type}: {count} (deleted: {deleted})");
                        }
                    }
                }

                sb.AppendLine($"\nTotal file size: {result.RawData.Length:N0} bytes");

            }
            catch (Exception ex)
            {
                sb.AppendLine($"\nError parsing binary sections: {ex.Message}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// ENHANCEMENT: Detects common corruption patterns
        /// </summary>
        private static string DetectCorruptionPatterns(DiagnosticResult result)
        {
            if (result.Triggers == null)
                return "  (no trigger data)";

            var sb = new StringBuilder();
            var patterns = new List<string>();

            var categories = result.Triggers.TriggerItems.OfType<TriggerCategoryDefinition>().ToList();
            var triggers = result.Triggers.TriggerItems.OfType<TriggerDefinition>().ToList();

            // Pattern 1: All triggers same ParentId
            var uniqueParentIds = triggers.Select(t => t.ParentId).Distinct().Count();
            if (uniqueParentIds == 1 && triggers.Count > 1)
            {
                int parentId = triggers.First().ParentId;
                patterns.Add($"⚠ PATTERN 1: All {triggers.Count} triggers have ParentId={parentId} (common nesting bug)");
            }

            // Pattern 2: Categories after triggers in file order
            int firstTriggerIndex = -1, lastCategoryIndex = -1;
            for (int i = 0; i < result.Triggers.TriggerItems.Count; i++)
            {
                if (result.Triggers.TriggerItems[i] is TriggerDefinition && firstTriggerIndex == -1)
                    firstTriggerIndex = i;
                if (result.Triggers.TriggerItems[i] is TriggerCategoryDefinition)
                    lastCategoryIndex = i;
            }
            if (firstTriggerIndex != -1 && lastCategoryIndex > firstTriggerIndex)
            {
                patterns.Add($"⚠ PATTERN 2: Categories appear after triggers (causes WE visual nesting)");
            }

            // Pattern 3: Orphaned triggers with specific ParentId (like 234)
            var validCategoryIds = new HashSet<int>(categories.Select(c => c.Id));
            validCategoryIds.Add(-1);
            var orphanGroups = triggers
                .Where(t => t.ParentId >= 0 && !validCategoryIds.Contains(t.ParentId))
                .GroupBy(t => t.ParentId)
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var group in orphanGroups.Take(5))
            {
                patterns.Add($"⚠ PATTERN 3: {group.Count()} orphaned trigger(s) with ParentId={group.Key}");
            }

            // Pattern 4: All categories have ParentId=0
            int categoriesWithZeroParent = categories.Count(c => c.ParentId == 0);
            if (categoriesWithZeroParent == categories.Count && categories.Count > 1)
            {
                patterns.Add($"ℹ PATTERN 4: All {categories.Count} categories have ParentId=0 (normal for WC3 1.27 format)");
            }

            // Pattern 5: Empty category names
            int emptyCategoryNames = categories.Count(c => string.IsNullOrWhiteSpace(c.Name));
            if (emptyCategoryNames > 0)
            {
                patterns.Add($"⚠ PATTERN 5: {emptyCategoryNames} category(ies) with empty names");
            }

            // Pattern 6: Empty trigger names
            int emptyTriggerNames = triggers.Count(t => string.IsNullOrWhiteSpace(t.Name));
            if (emptyTriggerNames > 0)
            {
                patterns.Add($"⚠ PATTERN 6: {emptyTriggerNames} trigger(s) with empty names");
            }

            // Pattern 7: Duplicate category IDs
            var duplicateCatIds = categories.GroupBy(c => c.Id).Where(g => g.Count() > 1).ToList();
            if (duplicateCatIds.Any())
            {
                patterns.Add($"⚠ PATTERN 7: {duplicateCatIds.Count} duplicate category ID(s)");
            }

            // Pattern 8: Duplicate trigger IDs
            var duplicateTrigIds = triggers.GroupBy(t => t.Id).Where(g => g.Count() > 1).ToList();
            if (duplicateTrigIds.Any())
            {
                patterns.Add($"⚠ PATTERN 8: {duplicateTrigIds.Count} duplicate trigger ID(s)");
            }

            // Pattern 9: Category with many triggers
            var categoryWithManyTriggers = triggers.GroupBy(t => t.ParentId)
                .Where(g => g.Count() > 50)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            if (categoryWithManyTriggers != null)
            {
                var cat = categories.FirstOrDefault(c => c.Id == categoryWithManyTriggers.Key);
                string catName = cat?.Name ?? $"ID={categoryWithManyTriggers.Key}";
                patterns.Add($"ℹ PATTERN 9: Category '{catName}' has {categoryWithManyTriggers.Count()} triggers (very large)");
            }

            // Pattern 10: Triggers with no functions
            int triggersNoFunctions = triggers.Count(t => t.Functions == null || t.Functions.Count == 0);
            if (triggersNoFunctions > 0)
            {
                patterns.Add($"ℹ PATTERN 10: {triggersNoFunctions} trigger(s) with no functions (empty triggers)");
            }

            // Output results
            if (patterns.Count == 0)
            {
                sb.AppendLine("✓ No common corruption patterns detected");
            }
            else
            {
                sb.AppendLine($"Found {patterns.Count} pattern(s):\n");
                foreach (var pattern in patterns)
                {
                    sb.AppendLine($"  {pattern}");
                }
            }

            return sb.ToString();
        }
    }
}
