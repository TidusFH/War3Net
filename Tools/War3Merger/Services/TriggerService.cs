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
                    Console.WriteLine($"  - Archive contains {originalArchive.Count} files");

                    // Create a builder to modify the archive
                    var builder = new MpqArchiveBuilder(originalArchive);

                    // Remove old trigger file if it exists
                    var triggerFileName = MapTriggers.FileName;
                    Console.WriteLine($"DEBUG: Checking for existing trigger file: {triggerFileName}");
                    if (originalArchive.FileExists(triggerFileName))
                    {
                        Console.WriteLine($"  - Removing old {triggerFileName}");
                        builder.RemoveFile(triggerFileName);
                    }
                    else
                    {
                        Console.WriteLine($"  - No existing {triggerFileName} found");
                    }

                    // Create a new stream from the byte array for the MpqFile
                    // IMPORTANT: Keep the stream alive until after SaveWithPreArchiveData completes
                    Console.WriteLine($"DEBUG: Adding new trigger file to builder");
                    using var newTriggerStream = new MemoryStream(triggerData);
                    newTriggerStream.Position = 0; // Reset position to start
                    var mpqFile = MpqFile.New(newTriggerStream, triggerFileName, leaveOpen: true);
                    builder.AddFile(mpqFile);
                    Console.WriteLine($"  - Added {triggerFileName} ({triggerData.Length} bytes)");

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
