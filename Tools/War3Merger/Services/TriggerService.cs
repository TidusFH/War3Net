// ------------------------------------------------------------------------------
// <copyright file="TriggerService.cs" company="Drake53">
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

namespace War3Net.Tools.TriggerMerger.Services
{
    /// <summary>
    /// Service for reading and writing Warcraft 3 map triggers.
    /// </summary>
    internal class TriggerService
    {
        /// <summary>
        /// Reads triggers from a Warcraft 3 map file.
        /// </summary>
        /// <param name="mapPath">Path to the .w3x or .w3m file.</param>
        /// <returns>MapTriggers object or null if not found.</returns>
        public async Task<MapTriggers?> ReadTriggersAsync(string mapPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(mapPath))
                    {
                        throw new FileNotFoundException($"Map file not found: {mapPath}");
                    }

                    // Open as MPQ archive (standard .w3x/.w3m)
                    using var archive = MpqArchive.Open(mapPath, loadListFile: true);

                    // Try to find the triggers file
                    var triggerFileName = MapTriggers.FileName; // "war3map.wtg"

                    if (!archive.FileExists(triggerFileName))
                    {
                        Console.WriteLine($"Warning: Trigger file '{triggerFileName}' not found in map archive.");
                        return null;
                    }

                    using var triggerStream = archive.OpenFile(triggerFileName);
                    using var reader = new BinaryReader(triggerStream);

                    // Read triggers using War3Net's extension method
                    var triggers = reader.ReadMapTriggers();

                    return triggers;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to read triggers from map: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Writes modified triggers back to a map file.
        /// </summary>
        /// <param name="originalMapPath">Path to the original map file.</param>
        /// <param name="outputMapPath">Path where the modified map should be saved.</param>
        /// <param name="triggers">The modified MapTriggers object.</param>
        public async Task WriteTriggersAsync(string originalMapPath, string outputMapPath, MapTriggers triggers)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(originalMapPath))
                    {
                        throw new FileNotFoundException($"Original map file not found: {originalMapPath}");
                    }

                    Console.WriteLine($"");
                    Console.WriteLine($"STEP 1: Serializing triggers to binary...");
                    Console.WriteLine($"  - TriggerItems count: {triggers.TriggerItems?.Count ?? 0}");
                    Console.WriteLine($"  - Variables count: {triggers.Variables?.Count ?? 0}");
                    Console.WriteLine($"  - Format version: {triggers.FormatVersion}");

                    // CRITICAL: Verify variables are actually in the list
                    if (triggers.Variables != null && triggers.Variables.Any())
                    {
                        Console.WriteLine($"  - First 5 variables: {string.Join(", ", triggers.Variables.Take(5).Select(v => $"{v.Name}({v.Type})"))}");
                        var lastVar = triggers.Variables.Last();
                        Console.WriteLine($"  - Last variable: {lastVar.Name} ({lastVar.Type})");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ ERROR: No variables in triggers object!");
                        Console.ResetColor();
                    }

                    // Serialize the triggers to a byte array first
                    byte[] triggerData;
                    using (var triggerStream = new MemoryStream())
                    {
                        using var writer = new BinaryWriter(triggerStream);
                        writer.Write(triggers);
                        writer.Flush();
                        triggerData = triggerStream.ToArray();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Serialized to {triggerData.Length} bytes");
                    Console.ResetColor();

                    if (triggerData.Length == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR: Trigger data is empty! Cannot write to map.");
                        Console.ResetColor();
                        throw new InvalidOperationException("Trigger data is empty after serialization.");
                    }

                    // Open the original archive
                    Console.WriteLine($"");
                    Console.WriteLine($"STEP 2: Opening target map archive...");
                    using var originalArchive = MpqArchive.Open(originalMapPath, loadListFile: true);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Opened archive with {originalArchive.Count()} files");
                    Console.ResetColor();

                    // Create a CUSTOM builder that handles duplicate filenames correctly
                    // When we add a file with the same name as an existing file,
                    // our custom builder keeps the NEW file (from _modifiedFiles)
                    // instead of keeping both and letting MpqArchive.Create choose wrong one
                    Console.WriteLine($"");
                    Console.WriteLine($"STEP 3: Creating MPQ builder...");
                    var builder = new CustomMpqArchiveBuilder(originalArchive);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Custom builder created (deduplicates files)");
                    Console.ResetColor();

                    var triggerFileName = MapTriggers.FileName;
                    var triggerExists = originalArchive.FileExists(triggerFileName);
                    if (triggerExists)
                    {
                        Console.WriteLine($"  - Found existing {triggerFileName} in original (will be replaced)");
                    }
                    else
                    {
                        Console.WriteLine($"  - No existing {triggerFileName} (will be added as new)");
                    }

                    // CRITICAL FIX: Do NOT call RemoveFile() before AddFile()!
                    // If we call RemoveFile, the hashed filename is added to _removedFiles.
                    // Then when GetMpqFiles() runs, it filters out files in _removedFiles,
                    // which includes our newly added file with the same name!
                    // Solution: Just call AddFile() - it automatically overrides the original.

                    // Create a new stream from the byte array for the MpqFile
                    // IMPORTANT: Keep the stream alive until after SaveWithPreArchiveData completes
                    Console.WriteLine($"");
                    Console.WriteLine($"STEP 4: Adding .wtg file to builder...");
                    using var newTriggerStream = new MemoryStream(triggerData);
                    newTriggerStream.Position = 0; // Reset position to start
                    var mpqFile = MpqFile.New(newTriggerStream, triggerFileName, leaveOpen: true);
                    builder.AddFile(mpqFile);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Added {triggerFileName} ({triggerData.Length} bytes)");
                    Console.ResetColor();

                    // CRITICAL DEBUG: Check if builder has duplicate files
                    var allFiles = builder.ToList();
                    Console.WriteLine($"  - Total files in builder: {allFiles.Count}");

                    var wtgFiles = allFiles.Where(f => {
                        var fileName = MpqHash.GetHashedFileName(triggerFileName);
                        return f.Name == fileName;
                    }).ToList();

                    if (wtgFiles.Count == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Deduplication working: Only 1 {triggerFileName} file");
                        Console.ResetColor();
                    }
                    else if (wtgFiles.Count > 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ ERROR: Found {wtgFiles.Count} .wtg files! Deduplication failed!");
                        Console.ResetColor();
                    }

                    // CRITICAL DIAGNOSTIC: Check what files are in the builder BEFORE saving
                    Console.WriteLine($"");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"DIAGNOSTIC: Analyzing builder contents BEFORE save...");
                    Console.ResetColor();

                    var filesInBuilder = builder.ToList();
                    Console.WriteLine($"  - Total files in builder: {filesInBuilder.Count}");

                    // Group by file hash to find duplicates
                    var filesByHash = filesInBuilder
                        .GroupBy(f => f.Name)
                        .ToList();

                    var duplicateFiles = filesByHash.Where(g => g.Count() > 1).ToList();
                    if (duplicateFiles.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ FOUND {duplicateFiles.Count} DUPLICATE FILES:");
                        foreach (var dup in duplicateFiles)
                        {
                            Console.WriteLine($"    - Hash {dup.Key}: {dup.Count()} copies");
                        }
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ No duplicate files found");
                        Console.ResetColor();
                    }

                    // CRITICAL: Check war3map.w3i specifically (this controls player count!)
                    // Get the hash for war3map.w3i
                    var w3iHash = MpqHash.GetHashedFileName("war3map.w3i");
                    var w3iFiles = filesByHash.FirstOrDefault(g => g.Key == w3iHash);

                    if (w3iFiles != null)
                    {
                        Console.WriteLine($"  - war3map.w3i found: {w3iFiles.Count()} instance(s)");

                        // Read and display the MapInfo to see player count
                        try
                        {
                            var w3iFile = w3iFiles.First();
                            using var w3iStream = w3iFile.MpqStream;
                            w3iStream.Position = 0;
                            using var w3iReader = new BinaryReader(w3iStream);
                            var mapInfo = w3iReader.ReadMapInfo();

                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"  - MapInfo from builder:");
                            Console.WriteLine($"    - Map name: {mapInfo.MapName}");
                            Console.WriteLine($"    - Players: {mapInfo.Players?.Count ?? 0}");
                            Console.WriteLine($"    - Recommended players: {mapInfo.RecommendedPlayers}");
                            Console.ResetColor();

                            if (mapInfo.Players?.Count != 1 && mapInfo.RecommendedPlayers != "1")
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"  ✗ WARNING: MapInfo shows {mapInfo.Players?.Count ?? 0} players, not 1!");
                                Console.WriteLine($"  ✗ This explains the 12-player bug!");
                                Console.WriteLine($"  ✗ The war3map.w3i in the builder is WRONG!");
                                Console.ResetColor();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"  Warning: Could not read MapInfo: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ ERROR: war3map.w3i NOT FOUND in builder!");
                        Console.WriteLine($"  ✗ SaveWithPreArchiveData will fail!");
                        Console.ResetColor();
                    }

                    // List file count comparison with original
                    Console.WriteLine($"  - Original archive files: {originalArchive.Count()}");
                    Console.WriteLine($"  - Builder total files (before dedup): {filesInBuilder.Count}");
                    Console.WriteLine($"  - Builder unique files (after dedup): {filesByHash.Count}");

                    // CRITICAL: Use SaveWithPreArchiveData to preserve the map header
                    // Without this, the map won't be recognized as a valid Warcraft 3 map!
                    // This extension method reads the MapInfo and writes the header before the MPQ data
                    Console.WriteLine($"");
                    Console.WriteLine($"STEP 5: Saving map with pre-archive header...");
                    Console.WriteLine($"  - Output path: {outputMapPath}");
                    builder.SaveWithPreArchiveData(outputMapPath);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Save completed");
                    Console.ResetColor();

                    // Verify the output file was created
                    if (File.Exists(outputMapPath))
                    {
                        var fileInfo = new FileInfo(outputMapPath);
                        Console.WriteLine($"  - Output file size: {fileInfo.Length} bytes");

                        // VERIFICATION: Open the output file and check if .wtg actually exists
                        Console.WriteLine($"");
                        Console.WriteLine($"STEP 6: Verifying .wtg exists in output...");
                        try
                        {
                            using var verifyArchive = MpqArchive.Open(outputMapPath, loadListFile: true);
                            if (verifyArchive.FileExists(triggerFileName))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"  ✓ {triggerFileName} exists in output map");
                                Console.ResetColor();

                                // Also verify size
                                using var verifyStream = verifyArchive.OpenFile(triggerFileName);
                                Console.WriteLine($"  - File size: {verifyStream.Length} bytes");

                                // CRITICAL: Try to READ BACK the .wtg to verify it's valid!
                                Console.WriteLine($"");
                                Console.WriteLine($"STEP 7: Reading back .wtg to verify it's parseable...");
                                try
                                {
                                    verifyStream.Position = 0;
                                    using var verifyReader = new BinaryReader(verifyStream);
                                    var readBackTriggers = verifyReader.ReadMapTriggers();

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"  ✓ Successfully parsed .wtg from output!");
                                    Console.ResetColor();
                                    Console.WriteLine($"  - Format version: {readBackTriggers.FormatVersion}");
                                    Console.WriteLine($"  - TriggerItems count: {readBackTriggers.TriggerItems?.Count ?? 0}");
                                    Console.WriteLine($"  - Variables count: {readBackTriggers.Variables?.Count ?? 0}");

                                    // CRITICAL: Verify variables were actually written!
                                    if (readBackTriggers.Variables != null && readBackTriggers.Variables.Any())
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"  ✓ Variables preserved in output:");
                                        Console.ResetColor();
                                        Console.WriteLine($"    - First 5: {string.Join(", ", readBackTriggers.Variables.Take(5).Select(v => v.Name))}");
                                        Console.WriteLine($"    - Last: {readBackTriggers.Variables.Last().Name}");
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"  ✗ ERROR: No variables in output .wtg!");
                                        Console.WriteLine($"  - This will cause World Editor to reject the map!");
                                        Console.ResetColor();
                                    }

                                    // Verify "Spels Heroes" is in there
                                    if (readBackTriggers.TriggerItems != null)
                                    {
                                        var categories = readBackTriggers.TriggerItems.OfType<War3Net.Build.Script.TriggerCategoryDefinition>().ToList();
                                        var spelsHeroes = categories.FirstOrDefault(c => c.Name.Equals("Spels Heroes", StringComparison.OrdinalIgnoreCase));
                                        if (spelsHeroes != null)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"  ✓ 'Spels Heroes' category CONFIRMED in output .wtg!");
                                            Console.ResetColor();

                                            var allTriggers = readBackTriggers.TriggerItems.OfType<War3Net.Build.Script.TriggerDefinition>().ToList();
                                            var spelsHeroesTriggers = allTriggers.Where(t => t.ParentId == spelsHeroes.Id).ToList();
                                            Console.WriteLine($"  - Triggers in 'Spels Heroes': {spelsHeroesTriggers.Count}");
                                            if (spelsHeroesTriggers.Any())
                                            {
                                                Console.WriteLine($"  - First 3: {string.Join(", ", spelsHeroesTriggers.Take(3).Select(t => t.Name))}");
                                            }
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine($"  ✗ ERROR: 'Spels Heroes' NOT FOUND in output .wtg!");
                                            Console.WriteLine($"  - This means the serialization wrote WRONG data!");
                                            Console.ResetColor();
                                        }
                                    }
                                }
                                catch (Exception readEx)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"  ✗ ERROR: Cannot parse the .wtg we just wrote!");
                                    Console.WriteLine($"  - Error: {readEx.Message}");
                                    Console.WriteLine($"  - This means the .wtg is CORRUPTED or INVALID!");
                                    Console.WriteLine($"  - World Editor won't be able to open this map!");
                                    Console.ResetColor();
                                }
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"  ✗ ERROR: {triggerFileName} NOT FOUND in output map!");
                                Console.WriteLine($"  - This means SaveWithPreArchiveData didn't save the file properly");
                                Console.ResetColor();
                            }

                            Console.WriteLine($"  - Output archive contains {verifyArchive.Count()} files total");

                            // CRITICAL: Verify other important map files weren't lost
                            Console.WriteLine($"");
                            Console.WriteLine($"STEP 8: Verifying map integrity...");
                            var criticalFiles = new[]
                            {
                                "war3map.j",
                                "war3map.w3i",
                                "war3map.doo",
                                "war3map.w3e",
                                "war3map.w3u",
                                "war3map.w3t",
                                "war3map.w3a"
                            };

                            var missingFiles = new List<string>();
                            foreach (var file in criticalFiles)
                            {
                                if (!verifyArchive.FileExists(file))
                                {
                                    missingFiles.Add(file);
                                }
                            }

                            if (missingFiles.Any())
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"  ✗ ERROR: {missingFiles.Count} critical files MISSING from output!");
                                foreach (var missing in missingFiles)
                                {
                                    Console.WriteLine($"    - {missing}");
                                }
                                Console.WriteLine($"  - This explains why World Editor crashes!");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"  ✓ All critical map files preserved");
                                Console.ResetColor();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"  ✗ ERROR verifying output: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR: Output file was not created!");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to write triggers to map: {ex.Message}", ex);
                }
            });
        }
    }
}
