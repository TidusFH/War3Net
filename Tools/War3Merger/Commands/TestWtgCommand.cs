// ------------------------------------------------------------------------------
// <copyright file="TestWtgCommand.cs" company="Drake53">
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

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to test if War3Net can read and write .wtg files without corruption.
    /// </summary>
    internal static class TestWtgCommand
    {
        public static async Task ExecuteAsync(FileInfo mapFile)
        {
            await Task.Run(() =>
            {
                Console.WriteLine("War3Net .wtg Format Test");
                Console.WriteLine("========================");
                Console.WriteLine();
                Console.WriteLine($"Testing: {mapFile.FullName}");
                Console.WriteLine();

                if (!mapFile.Exists)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: File not found!");
                    Console.ResetColor();
                    return;
                }

                try
                {
                    byte[] originalWtgData;
                    MapTriggers triggers;

                    // Step 1: Read original .wtg
                    Console.WriteLine("STEP 1: Reading original .wtg from map...");
                    using (var archive = MpqArchive.Open(mapFile.FullName, loadListFile: true))
                    {
                        if (!archive.FileExists("war3map.wtg"))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error: Map has no war3map.wtg file!");
                            Console.ResetColor();
                            return;
                        }

                        using var wtgStream = archive.OpenFile("war3map.wtg");
                        originalWtgData = new byte[wtgStream.Length];
                        wtgStream.Read(originalWtgData, 0, originalWtgData.Length);

                        wtgStream.Position = 0;
                        using var reader = new BinaryReader(wtgStream);
                        triggers = reader.ReadMapTriggers();

                        Console.WriteLine($"  Format Version: {triggers.FormatVersion}");
                        Console.WriteLine($"  SubVersion: {triggers.SubVersion?.ToString() ?? "null"}");
                        Console.WriteLine($"  TriggerItems: {triggers.TriggerItems?.Count ?? 0}");
                        Console.WriteLine($"  Variables: {triggers.Variables?.Count ?? 0}");
                        Console.WriteLine($"  Original size: {originalWtgData.Length} bytes");
                    }
                    Console.WriteLine();

                    // Step 2: Write triggers to memory
                    Console.WriteLine("STEP 2: Serializing triggers back to binary...");
                    byte[] reserializedData;
                    using (var memoryStream = new MemoryStream())
                    {
                        using var writer = new BinaryWriter(memoryStream);
                        writer.Write(triggers);
                        writer.Flush();
                        reserializedData = memoryStream.ToArray();
                    }

                    Console.WriteLine($"  Reserialized size: {reserializedData.Length} bytes");
                    Console.WriteLine($"  Size difference: {reserializedData.Length - originalWtgData.Length:+#;-#;0} bytes");
                    Console.WriteLine();

                    // Step 3: Compare binary data
                    Console.WriteLine("STEP 3: Comparing binary data...");

                    if (originalWtgData.Length != reserializedData.Length)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ SIZE MISMATCH: Original={originalWtgData.Length}, Reserialized={reserializedData.Length}");
                        Console.ResetColor();
                        Console.WriteLine();

                        // Show first difference
                        var minLength = Math.Min(originalWtgData.Length, reserializedData.Length);
                        for (int i = 0; i < minLength; i++)
                        {
                            if (originalWtgData[i] != reserializedData[i])
                            {
                                Console.WriteLine($"  First byte difference at position {i}:");
                                Console.WriteLine($"    Original: 0x{originalWtgData[i]:X2}");
                                Console.WriteLine($"    Reserialized: 0x{reserializedData[i]:X2}");

                                // Show context (10 bytes before and after)
                                var start = Math.Max(0, i - 10);
                                var end = Math.Min(minLength, i + 10);

                                Console.WriteLine($"  Context (bytes {start}-{end}):");
                                Console.Write($"    Original:      ");
                                for (int j = start; j <= end; j++)
                                {
                                    if (j == i)
                                        Console.Write($"[{originalWtgData[j]:X2}] ");
                                    else
                                        Console.Write($"{originalWtgData[j]:X2} ");
                                }
                                Console.WriteLine();

                                Console.Write($"    Reserialized:  ");
                                for (int j = start; j <= end; j++)
                                {
                                    if (j == i)
                                        Console.Write($"[{reserializedData[j]:X2}] ");
                                    else
                                        Console.Write($"{reserializedData[j]:X2} ");
                                }
                                Console.WriteLine();
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Same size - compare byte by byte
                        bool identical = true;
                        int firstDiff = -1;

                        for (int i = 0; i < originalWtgData.Length; i++)
                        {
                            if (originalWtgData[i] != reserializedData[i])
                            {
                                identical = false;
                                firstDiff = i;
                                break;
                            }
                        }

                        if (identical)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  ✓ BINARY IDENTICAL: War3Net perfectly preserves the .wtg file!");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"  △ BINARY DIFFERENT: Same size but different content");
                            Console.WriteLine($"  First difference at byte {firstDiff}:");
                            Console.WriteLine($"    Original: 0x{originalWtgData[firstDiff]:X2}");
                            Console.WriteLine($"    Reserialized: 0x{reserializedData[firstDiff]:X2}");
                            Console.ResetColor();
                        }
                    }
                    Console.WriteLine();

                    // Step 4: Try to parse the reserialized data
                    Console.WriteLine("STEP 4: Verifying reserialized data can be parsed...");
                    try
                    {
                        using var verifyStream = new MemoryStream(reserializedData);
                        using var verifyReader = new BinaryReader(verifyStream);
                        var reparsed = verifyReader.ReadMapTriggers();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Reserialized data is valid!");
                        Console.ResetColor();
                        Console.WriteLine($"  Format Version: {reparsed.FormatVersion}");
                        Console.WriteLine($"  TriggerItems: {reparsed.TriggerItems?.Count ?? 0}");
                        Console.WriteLine($"  Variables: {reparsed.Variables?.Count ?? 0}");

                        // Check if format version changed
                        if (reparsed.FormatVersion != triggers.FormatVersion)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine();
                            Console.WriteLine($"  ✗ FORMAT VERSION CHANGED!");
                            Console.WriteLine($"    Original: {triggers.FormatVersion}");
                            Console.WriteLine($"    Reserialized: {reparsed.FormatVersion}");
                            Console.WriteLine($"    This could cause Warcraft 1.27 to reject the file!");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ Cannot parse reserialized data!");
                        Console.WriteLine($"  Error: {ex.Message}");
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("CONCLUSION:");
                    Console.WriteLine("===========");

                    if (originalWtgData.Length == reserializedData.Length &&
                        originalWtgData.SequenceEqual(reserializedData))
                    {
                        Console.WriteLine("War3Net can perfectly read and write .wtg files without changes.");
                        Console.WriteLine("The corruption must be happening during the merge logic itself.");
                    }
                    else
                    {
                        Console.WriteLine("War3Net MODIFIES the .wtg file when writing it back!");
                        Console.WriteLine("This could be why Warcraft 1.27 can't read the merged maps.");
                        Console.WriteLine();
                        Console.WriteLine("Possible causes:");
                        Console.WriteLine("  - Format version conversion");
                        Console.WriteLine("  - Different serialization order");
                        Console.WriteLine("  - Adding/removing optional fields");
                        Console.WriteLine("  - Encoding differences");
                    }
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine();
                    Console.WriteLine("Stack trace:");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            });
        }
    }
}
