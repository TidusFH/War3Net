// ------------------------------------------------------------------------------
// <copyright file="TriggerService.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
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

                    Console.WriteLine($"DEBUG: Serializing triggers...");
                    Console.WriteLine($"  - TriggerItems count: {triggers.TriggerItems?.Count ?? 0}");
                    Console.WriteLine($"  - Variables count: {triggers.Variables?.Count ?? 0}");
                    Console.WriteLine($"  - Format version: {triggers.FormatVersion}");

                    // Serialize the triggers to a byte array first
                    byte[] triggerData;
                    using (var triggerStream = new MemoryStream())
                    {
                        using var writer = new BinaryWriter(triggerStream);
                        writer.Write(triggers);
                        writer.Flush();
                        triggerData = triggerStream.ToArray();
                    }

                    Console.WriteLine($"  - Serialized trigger data size: {triggerData.Length} bytes");

                    if (triggerData.Length == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR: Trigger data is empty! Cannot write to map.");
                        Console.ResetColor();
                        throw new InvalidOperationException("Trigger data is empty after serialization.");
                    }

                    // Open the original archive
                    Console.WriteLine($"DEBUG: Opening original archive: {originalMapPath}");
                    using var originalArchive = MpqArchive.Open(originalMapPath, loadListFile: true);
                    Console.WriteLine($"  - Archive contains {originalArchive.Count()} files");

                    // Create a CUSTOM builder that handles duplicate filenames correctly
                    // When we add a file with the same name as an existing file,
                    // our custom builder keeps the NEW file (from _modifiedFiles)
                    // instead of keeping both and letting MpqArchive.Create choose wrong one
                    var builder = new CustomMpqArchiveBuilder(originalArchive);

                    var triggerFileName = MapTriggers.FileName;
                    Console.WriteLine($"DEBUG: Checking for existing trigger file: {triggerFileName}");
                    var triggerExists = originalArchive.FileExists(triggerFileName);
                    if (triggerExists)
                    {
                        Console.WriteLine($"  - Found existing {triggerFileName} (will be overwritten)");
                    }
                    else
                    {
                        Console.WriteLine($"  - No existing {triggerFileName} found (will be added)");
                    }

                    // CRITICAL FIX: Do NOT call RemoveFile() before AddFile()!
                    // If we call RemoveFile, the hashed filename is added to _removedFiles.
                    // Then when GetMpqFiles() runs, it filters out files in _removedFiles,
                    // which includes our newly added file with the same name!
                    // Solution: Just call AddFile() - it automatically overrides the original.

                    // Create a new stream from the byte array for the MpqFile
                    // IMPORTANT: Keep the stream alive until after SaveWithPreArchiveData completes
                    Console.WriteLine($"DEBUG: Adding new trigger file to builder");
                    using var newTriggerStream = new MemoryStream(triggerData);
                    newTriggerStream.Position = 0; // Reset position to start
                    var mpqFile = MpqFile.New(newTriggerStream, triggerFileName, leaveOpen: true);
                    builder.AddFile(mpqFile);
                    Console.WriteLine($"  - Added {triggerFileName} ({triggerData.Length} bytes)");

                    // CRITICAL DEBUG: Check if builder has duplicate files
                    Console.WriteLine($"DEBUG: Checking builder contents...");
                    var allFiles = builder.ToList();
                    Console.WriteLine($"  - Total files in builder: {allFiles.Count}");

                    var wtgFiles = allFiles.Where(f => {
                        var fileName = MpqHash.GetHashedFileName(triggerFileName);
                        return f.Name == fileName;
                    }).ToList();
                    Console.WriteLine($"  - Files with name hash matching '{triggerFileName}': {wtgFiles.Count}");

                    if (wtgFiles.Count > 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ WARNING: Found {wtgFiles.Count} files with same name! MpqArchive.Create might use the wrong one!");
                        Console.ResetColor();
                    }

                    // CRITICAL: Use SaveWithPreArchiveData to preserve the map header
                    // Without this, the map won't be recognized as a valid Warcraft 3 map!
                    // This extension method reads the MapInfo and writes the header before the MPQ data
                    Console.WriteLine($"DEBUG: Saving to: {outputMapPath}");
                    builder.SaveWithPreArchiveData(outputMapPath);
                    Console.WriteLine($"DEBUG: Save completed successfully");

                    // Verify the output file was created
                    if (File.Exists(outputMapPath))
                    {
                        var fileInfo = new FileInfo(outputMapPath);
                        Console.WriteLine($"  - Output file size: {fileInfo.Length} bytes");

                        // VERIFICATION: Open the output file and check if .wtg actually exists
                        Console.WriteLine($"DEBUG: Verifying .wtg was actually written to output...");
                        try
                        {
                            using var verifyArchive = MpqArchive.Open(outputMapPath, loadListFile: true);
                            if (verifyArchive.FileExists(triggerFileName))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"  ✓ CONFIRMED: {triggerFileName} exists in output map");
                                Console.ResetColor();

                                // Also verify size
                                using var verifyStream = verifyArchive.OpenFile(triggerFileName);
                                Console.WriteLine($"  - {triggerFileName} size in output: {verifyStream.Length} bytes");

                                // CRITICAL: Try to READ BACK the .wtg to verify it's valid!
                                Console.WriteLine($"DEBUG: Reading back the .wtg to verify it's parseable...");
                                try
                                {
                                    verifyStream.Position = 0;
                                    using var verifyReader = new BinaryReader(verifyStream);
                                    var readBackTriggers = verifyReader.ReadMapTriggers();

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"  ✓ SUCCESS: Read back triggers from output map!");
                                    Console.ResetColor();
                                    Console.WriteLine($"  - Format version: {readBackTriggers.FormatVersion}");
                                    Console.WriteLine($"  - TriggerItems count: {readBackTriggers.TriggerItems?.Count ?? 0}");
                                    Console.WriteLine($"  - Variables count: {readBackTriggers.Variables?.Count ?? 0}");

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
