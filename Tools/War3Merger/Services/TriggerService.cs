// ------------------------------------------------------------------------------
// <copyright file="TriggerService.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;

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

                    // Try to open as MPQ archive (standard .w3x/.w3m)
                    using var archive = MpqArchive.Open(mapPath, true);

                    // Try to find the triggers file
                    var triggerFileName = MapTriggers.FileName; // "war3map.wtg"

                    if (!archive.FileExists(triggerFileName))
                    {
                        Console.WriteLine($"Warning: Trigger file '{triggerFileName}' not found in map archive.");
                        return null;
                    }

                    using var triggerStream = archive.OpenFile(triggerFileName);
                    using var reader = new BinaryReader(triggerStream);

                    // Read triggers using War3Net's built-in deserialization
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

                    // Create a temporary directory for working with files
                    var tempDir = Path.Combine(Path.GetTempPath(), $"War3NetTriggerMerger_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempDir);

                    try
                    {
                        // Copy original map to output location if they're different
                        if (!string.Equals(originalMapPath, outputMapPath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(originalMapPath, outputMapPath, true);
                        }

                        // Open the map archive in read-write mode
                        using (var archive = MpqArchive.Open(outputMapPath, false))
                        {
                            // Serialize the triggers to a memory stream
                            using var triggerStream = new MemoryStream();
                            using var writer = new BinaryWriter(triggerStream);
                            writer.Write(triggers);
                            writer.Flush();

                            // Get the byte array
                            var triggerData = triggerStream.ToArray();

                            // Remove old trigger file if it exists
                            var triggerFileName = MapTriggers.FileName;
                            if (archive.FileExists(triggerFileName))
                            {
                                archive.RemoveFile(triggerFileName);
                            }

                            // Add the new trigger file
                            using var dataStream = new MemoryStream(triggerData);
                            archive.AddFile(MpqFile.New(dataStream, triggerFileName));
                        }
                    }
                    finally
                    {
                        // Clean up temp directory
                        if (Directory.Exists(tempDir))
                        {
                            try
                            {
                                Directory.Delete(tempDir, true);
                            }
                            catch
                            {
                                // Ignore cleanup errors
                            }
                        }
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
