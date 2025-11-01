// ------------------------------------------------------------------------------
// <copyright file="DiagnoseMapCommand.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.Build.Script;

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
                Console.WriteLine("Map File Diagnostic");
                Console.WriteLine("===================");
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

                var fileName = mapFile.Name.ToLowerInvariant();

                // Determine file type and diagnose accordingly
                if (fileName.EndsWith(".w3i"))
                {
                    DiagnoseMapInfo(mapFile);
                }
                else if (fileName.EndsWith(".wtg"))
                {
                    DiagnoseTriggers(mapFile);
                }
                else if (fileName.EndsWith(".j"))
                {
                    DiagnoseJassScript(mapFile);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: Unknown file type '{mapFile.Extension}'");
                    Console.WriteLine($"Supported: .w3i (MapInfo), .wtg (Triggers), .j (JASS Script)");
                    Console.ResetColor();
                }
            });
        }

        private static void DiagnoseMapInfo(FileInfo mapFile)
        {
            try
            {
                using var stream = mapFile.OpenRead();
                using var reader = new BinaryReader(stream);
                var mapInfo = reader.ReadMapInfo();

                Console.WriteLine("MAP INFORMATION (war3map.w3i):");
                Console.WriteLine("==============================");
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
                    Console.WriteLine($"  Total Player Slots: {mapInfo.Players.Count}");

                    var activePlayers = mapInfo.Players.Where(p =>
                        p.Controller == PlayerController.User ||
                        p.Controller == PlayerController.Computer).ToList();

                    Console.WriteLine($"  Active Players: {activePlayers.Count}");
                    Console.WriteLine();

                    for (int i = 0; i < mapInfo.Players.Count; i++)
                    {
                        var player = mapInfo.Players[i];
                        var isActive = player.Controller == PlayerController.User ||
                                      player.Controller == PlayerController.Computer;
                        var marker = isActive ? "●" : "○";

                        Console.WriteLine($"  {marker} [{i}] {player.Name}");
                        Console.WriteLine($"      Controller: {player.Controller}");
                        Console.WriteLine($"      Race: {player.Race}");
                        Console.WriteLine($"      Flags: {player.Flags}");
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
        }

        private static void DiagnoseTriggers(FileInfo wtgFile)
        {
            try
            {
                using var stream = wtgFile.OpenRead();
                using var reader = new BinaryReader(stream);
                var triggers = reader.ReadMapTriggers();

                Console.WriteLine("TRIGGER DATA (war3map.wtg):");
                Console.WriteLine("============================");
                Console.WriteLine($"  Format Version: {triggers.FormatVersion}");
                Console.WriteLine($"  Total Items: {triggers.TriggerItems?.Count ?? 0}");
                Console.WriteLine();

                var categories = triggers.TriggerItems?.OfType<TriggerCategoryDefinition>().ToList() ?? new();
                var triggerDefs = triggers.TriggerItems?.OfType<TriggerDefinition>().ToList() ?? new();

                Console.WriteLine($"  Categories: {categories.Count}");
                Console.WriteLine($"  Triggers: {triggerDefs.Count}");
                Console.WriteLine();

                Console.WriteLine("VARIABLES:");
                Console.WriteLine($"  Total Variables: {triggers.Variables?.Count ?? 0}");
                if (triggers.Variables != null && triggers.Variables.Count > 0)
                {
                    Console.WriteLine($"  First 10 variables:");
                    foreach (var variable in triggers.Variables.Take(10))
                    {
                        var arrayInfo = variable.IsArray ? $"[{variable.ArraySize}]" : "";
                        Console.WriteLine($"    - {variable.Name} ({variable.Type}){arrayInfo}");
                    }
                    if (triggers.Variables.Count > 10)
                    {
                        Console.WriteLine($"    ... and {triggers.Variables.Count - 10} more");
                    }
                }
                Console.WriteLine();

                Console.WriteLine("CATEGORIES & TRIGGERS:");
                foreach (var category in categories)
                {
                    var categoryTriggers = triggerDefs.Where(t => t.ParentId == category.Id).ToList();
                    var marker = category.IsComment ? "💬" : "📁";
                    Console.WriteLine($"  {marker} {category.Name} (ID: {category.Id})");
                    Console.WriteLine($"      Triggers: {categoryTriggers.Count}");

                    if (categoryTriggers.Count > 0 && categoryTriggers.Count <= 5)
                    {
                        foreach (var trigger in categoryTriggers)
                        {
                            var enabled = trigger.IsEnabled ? "✓" : "✗";
                            Console.WriteLine($"        {enabled} {trigger.Name}");
                        }
                    }
                    else if (categoryTriggers.Count > 5)
                    {
                        foreach (var trigger in categoryTriggers.Take(3))
                        {
                            var enabled = trigger.IsEnabled ? "✓" : "✗";
                            Console.WriteLine($"        {enabled} {trigger.Name}");
                        }
                        Console.WriteLine($"        ... and {categoryTriggers.Count - 3} more");
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine("✓ Triggers read successfully");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error reading triggers: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }

        private static void DiagnoseJassScript(FileInfo jassFile)
        {
            try
            {
                var content = File.ReadAllText(jassFile.FullName);
                var lines = content.Split('\n');

                Console.WriteLine("JASS SCRIPT (war3map.j):");
                Console.WriteLine("=========================");
                Console.WriteLine($"  File Size: {jassFile.Length} bytes");
                Console.WriteLine($"  Total Lines: {lines.Length}");
                Console.WriteLine();

                // Count various elements
                var functionCount = lines.Count(l => l.TrimStart().StartsWith("function "));
                var globalCount = lines.Count(l => l.Contains("udg_"));
                var configCount = lines.Count(l => l.Contains("config function"));

                Console.WriteLine("CODE STATISTICS:");
                Console.WriteLine($"  Functions: {functionCount}");
                Console.WriteLine($"  Global Variables (udg_): ~{globalCount} references");
                Console.WriteLine($"  Config Functions: {configCount}");
                Console.WriteLine();

                // Check for main functions
                Console.WriteLine("KEY FUNCTIONS:");
                var hasMain = content.Contains("function main takes nothing returns nothing");
                var hasConfig = content.Contains("function config takes nothing returns nothing");
                var hasInitGlobals = content.Contains("function InitGlobals takes nothing returns nothing");
                var hasInitCustomTriggers = content.Contains("function InitCustomTriggers takes nothing returns nothing");

                Console.WriteLine($"  {(hasMain ? "✓" : "✗")} main");
                Console.WriteLine($"  {(hasConfig ? "✓" : "✗")} config");
                Console.WriteLine($"  {(hasInitGlobals ? "✓" : "✗")} InitGlobals");
                Console.WriteLine($"  {(hasInitCustomTriggers ? "✓" : "✗")} InitCustomTriggers");
                Console.WriteLine();

                // Show first few lines
                Console.WriteLine("FIRST 20 LINES:");
                foreach (var line in lines.Take(20))
                {
                    Console.WriteLine($"  {line.TrimEnd()}");
                }
                if (lines.Length > 20)
                {
                    Console.WriteLine($"  ... and {lines.Length - 20} more lines");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine("✓ JASS script read successfully");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error reading JASS script: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }
    }
}
