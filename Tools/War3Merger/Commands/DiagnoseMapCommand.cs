// ------------------------------------------------------------------------------
// <copyright file="DiagnoseMapCommand.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;

using War3Net.Build.Extensions;
using War3Net.Build.Info;

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to diagnose map files and show their properties.
    /// </summary>
    internal static class DiagnoseMapCommand
    {
        public static async Task ExecuteAsync(FileInfo mapFile)
        {
            await Task.Run(() =>
            {
                Console.WriteLine("MapInfo Diagnostic");
                Console.WriteLine("==================");
                Console.WriteLine();
                Console.WriteLine($"Reading: {mapFile.FullName}");
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
                    using var stream = mapFile.OpenRead();
                    using var reader = new BinaryReader(stream);
                    var mapInfo = reader.ReadMapInfo();

                    Console.WriteLine("MAP INFORMATION:");
                    Console.WriteLine($"  Map Name: {mapInfo.MapName}");
                    Console.WriteLine($"  Map Author: {mapInfo.MapAuthor}");
                    Console.WriteLine($"  Map Description: {mapInfo.MapDescription}");
                    Console.WriteLine($"  Recommended Players: {mapInfo.RecommendedPlayers}");
                    Console.WriteLine($"  Format Version: {mapInfo.FormatVersion}");
                    Console.WriteLine($"  Editor Version: {mapInfo.EditorVersion}");
                    Console.WriteLine();

                    Console.WriteLine("PLAYERS:");
                    if (mapInfo.Players != null && mapInfo.Players.Count > 0)
                    {
                        Console.WriteLine($"  Total Players: {mapInfo.Players.Count}");
                        for (int i = 0; i < mapInfo.Players.Count; i++)
                        {
                            var player = mapInfo.Players[i];
                            Console.WriteLine($"  [{i}] {player.Name} - Type: {player.Type}, Controller: {player.Controller}, Race: {player.Race}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  No players defined");
                    }

                    Console.WriteLine();
                    Console.WriteLine("FORCES:");
                    if (mapInfo.Forces != null && mapInfo.Forces.Count > 0)
                    {
                        Console.WriteLine($"  Total Forces: {mapInfo.Forces.Count}");
                        for (int i = 0; i < mapInfo.Forces.Count; i++)
                        {
                            var force = mapInfo.Forces[i];
                            Console.WriteLine($"  [{i}] {force.Name} - Flags: {force.Flags}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  No forces defined");
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine();
                    Console.WriteLine("✓ MapInfo read successfully");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error reading MapInfo: {ex.Message}");
                    Console.WriteLine();
                    Console.WriteLine("Stack trace:");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            });
        }
    }
}
