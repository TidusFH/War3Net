// ------------------------------------------------------------------------------
// <copyright file="CompareMapCommand.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using War3Net.Build.Extensions;
using War3Net.Build.Script;
using War3Net.IO.Mpq;

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to compare two map files and show differences.
    /// </summary>
    internal static class CompareMapCommand
    {
        public static async Task ExecuteAsync(FileInfo map1File, FileInfo map2File)
        {
            await Task.Run(() =>
            {
                Console.WriteLine("Map Comparison Tool");
                Console.WriteLine("===================");
                Console.WriteLine();
                Console.WriteLine($"Map 1: {map1File.FullName}");
                Console.WriteLine($"Map 2: {map2File.FullName}");
                Console.WriteLine();

                if (!map1File.Exists)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Map 1 not found!");
                    Console.ResetColor();
                    return;
                }

                if (!map2File.Exists)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Map 2 not found!");
                    Console.ResetColor();
                    return;
                }

                try
                {
                    // Open both archives
                    using var archive1 = MpqArchive.Open(map1File.FullName, loadListFile: true);
                    using var archive2 = MpqArchive.Open(map2File.FullName, loadListFile: true);

                    Console.WriteLine("FILE COMPARISON:");
                    Console.WriteLine("================");
                    Console.WriteLine();

                    // Get file lists - MpqArchive enumerates MpqEntry objects
                    var files1 = archive1
                        .Where(entry => entry.FileName != null)
                        .Select(entry => entry.FileName!)
                        .OrderBy(f => f)
                        .ToList();
                    var files2 = archive2
                        .Where(entry => entry.FileName != null)
                        .Select(entry => entry.FileName!)
                        .OrderBy(f => f)
                        .ToList();

                    var files1Set = new HashSet<string>(files1);
                    var files2Set = new HashSet<string>(files2);

                    Console.WriteLine($"Map 1 files: {files1.Count}");
                    Console.WriteLine($"Map 2 files: {files2.Count}");
                    Console.WriteLine();

                    // Files only in Map 1
                    var onlyInMap1 = files1Set.Except(files2Set).ToList();
                    if (onlyInMap1.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Files ONLY in Map 1: {onlyInMap1.Count}");
                        foreach (var file in onlyInMap1.Take(10))
                        {
                            Console.WriteLine($"  - {file}");
                        }
                        if (onlyInMap1.Count > 10)
                        {
                            Console.WriteLine($"  ... and {onlyInMap1.Count - 10} more");
                        }
                        Console.ResetColor();
                        Console.WriteLine();
                    }

                    // Files only in Map 2
                    var onlyInMap2 = files2Set.Except(files1Set).ToList();
                    if (onlyInMap2.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Files ONLY in Map 2: {onlyInMap2.Count}");
                        foreach (var file in onlyInMap2.Take(10))
                        {
                            Console.WriteLine($"  + {file}");
                        }
                        if (onlyInMap2.Count > 10)
                        {
                            Console.WriteLine($"  ... and {onlyInMap2.Count - 10} more");
                        }
                        Console.ResetColor();
                        Console.WriteLine();
                    }

                    // Common files
                    var commonFiles = files1Set.Intersect(files2Set).ToList();
                    Console.WriteLine($"Common files: {commonFiles.Count}");
                    Console.WriteLine();

                    // Check critical files
                    Console.WriteLine("CRITICAL FILES CHECK:");
                    Console.WriteLine("=====================");
                    Console.WriteLine();

                    var criticalFiles = new[]
                    {
                        "war3map.w3i",
                        "war3map.wtg",
                        "war3map.j",
                        "war3map.doo",  // Doodads - crash point!
                        "war3map.w3e",  // Terrain
                        "war3map.w3u",  // Units
                        "war3map.w3t",  // Trees
                        "war3map.w3a",  // Abilities
                        "war3map.wts",  // Trigger strings
                    };

                    foreach (var file in criticalFiles)
                    {
                        CompareFile(archive1, archive2, file);
                    }

                    // Specific checks for .wtg and .j mismatch
                    Console.WriteLine();
                    Console.WriteLine("TRIGGER DATA VALIDATION:");
                    Console.WriteLine("========================");
                    Console.WriteLine();

                    CheckTriggerDataSync(archive1, archive2, "Map 1", "Map 2");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine();
                    Console.WriteLine("✓ Comparison complete");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error comparing maps: {ex.Message}");
                    Console.WriteLine();
                    Console.WriteLine("Stack trace:");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            });
        }

        private static void CompareFile(MpqArchive archive1, MpqArchive archive2, string fileName)
        {
            var exists1 = archive1.FileExists(fileName);
            var exists2 = archive2.FileExists(fileName);

            Console.Write($"{fileName,-20} ");

            if (!exists1 && !exists2)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("(not in either map)");
                Console.ResetColor();
                return;
            }

            if (!exists1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ MISSING in Map 1!");
                Console.ResetColor();
                return;
            }

            if (!exists2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ MISSING in Map 2!");
                Console.ResetColor();
                return;
            }

            // Both exist - compare sizes
            using var stream1 = archive1.OpenFile(fileName);
            using var stream2 = archive2.OpenFile(fileName);

            var size1 = stream1.Length;
            var size2 = stream2.Length;

            if (size1 == size2)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Same size ({size1} bytes)");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"△ Different sizes: {size1} vs {size2} bytes (diff: {size2 - size1:+#;-#;0})");
                Console.ResetColor();
            }
        }

        private static void CheckTriggerDataSync(MpqArchive archive1, MpqArchive archive2, string label1, string label2)
        {
            // Check Map 1
            Console.WriteLine($"{label1}:");
            CheckSingleMapTriggerSync(archive1);
            Console.WriteLine();

            // Check Map 2
            Console.WriteLine($"{label2}:");
            CheckSingleMapTriggerSync(archive2);
        }

        private static void CheckSingleMapTriggerSync(MpqArchive archive)
        {
            var hasWtg = archive.FileExists("war3map.wtg");
            var hasJ = archive.FileExists("war3map.j");

            Console.WriteLine($"  war3map.wtg: {(hasWtg ? "✓" : "✗")}");
            Console.WriteLine($"  war3map.j:   {(hasJ ? "✓" : "✗")}");

            if (!hasWtg && !hasJ)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠ No trigger data found");
                Console.ResetColor();
                return;
            }

            if (!hasWtg)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ✗ ERROR: Has .j but no .wtg - World Editor will reject!");
                Console.ResetColor();
                return;
            }

            if (!hasJ)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠ WARNING: Has .wtg but no .j - World Editor will regenerate .j");
                Console.ResetColor();
                return;
            }

            // Both exist - check if they're in sync
            try
            {
                using var wtgStream = archive.OpenFile("war3map.wtg");
                using var wtgReader = new BinaryReader(wtgStream);
                var triggers = wtgReader.ReadMapTriggers();

                using var jStream = archive.OpenFile("war3map.j");
                using var jReader = new StreamReader(jStream);
                var jContent = jReader.ReadToEnd();

                // Count variables in .wtg
                var wtgVarCount = triggers.Variables?.Count ?? 0;

                // Count udg_ references in .j
                var jVarReferences = System.Text.RegularExpressions.Regex.Matches(jContent, @"\budg_(\w+)\b")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Groups[1].Value)
                    .Distinct()
                    .Count();

                // Count trigger functions in .j (functions starting with Trig_)
                var jTriggerFunctions = System.Text.RegularExpressions.Regex.Matches(jContent, @"function\s+Trig_(\w+)")
                    .Count;

                var wtgTriggerCount = triggers.TriggerItems?.OfType<TriggerDefinition>().Count() ?? 0;

                Console.WriteLine($"  .wtg variables: {wtgVarCount}");
                Console.WriteLine($"  .j variable references: {jVarReferences}");
                Console.WriteLine($"  .wtg triggers: {wtgTriggerCount}");
                Console.WriteLine($"  .j trigger functions: {jTriggerFunctions}");

                // Check for mismatches
                var hasIssues = false;

                if (jVarReferences > wtgVarCount)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ ERROR: .j references {jVarReferences - wtgVarCount} MORE variables than .wtg defines!");
                    Console.WriteLine($"    This will cause 'trigger data invalid' error!");
                    Console.ResetColor();
                    hasIssues = true;
                }

                if (jTriggerFunctions < wtgTriggerCount)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ WARNING: .j has {wtgTriggerCount - jTriggerFunctions} FEWER trigger functions than .wtg");
                    Console.WriteLine($"    .j might be outdated!");
                    Console.ResetColor();
                    hasIssues = true;
                }

                if (!hasIssues)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ .wtg and .j appear synchronized");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ ERROR checking sync: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
