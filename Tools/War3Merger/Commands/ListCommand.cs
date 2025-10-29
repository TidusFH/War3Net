// ------------------------------------------------------------------------------
// <copyright file="ListCommand.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using War3Net.Tools.TriggerMerger.Services;

namespace War3Net.Tools.TriggerMerger.Commands
{
    /// <summary>
    /// Command to list trigger categories and triggers in a map.
    /// </summary>
    internal static class ListCommand
    {
        public static async Task ExecuteAsync(FileInfo mapFile, bool detailed)
        {
            try
            {
                Console.WriteLine($"Reading map: {mapFile.FullName}");
                Console.WriteLine();

                var triggerService = new TriggerService();
                var triggers = await triggerService.ReadTriggersAsync(mapFile.FullName);

                if (triggers == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Could not read triggers from map file.");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine($"Map Triggers Information:");
                Console.WriteLine($"  Format Version: {triggers.FormatVersion}");
                Console.WriteLine($"  Sub Version: {triggers.SubVersion}");
                Console.WriteLine($"  Game Version: {triggers.GameVersion}");
                Console.WriteLine();

                if (triggers.Variables != null && triggers.Variables.Any())
                {
                    Console.WriteLine($"Global Variables ({triggers.Variables.Count}):");
                    foreach (var variable in triggers.Variables)
                    {
                        var arrayInfo = variable.IsArray ? $"[{variable.ArraySize}]" : string.Empty;
                        Console.WriteLine($"  - {variable.Name}: {variable.Type}{arrayInfo}");
                    }
                    Console.WriteLine();
                }

                if (triggers.TriggerItems == null || !triggers.TriggerItems.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No triggers found in this map.");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine($"Trigger Categories and Triggers ({triggers.TriggerItems.Count} items):");
                Console.WriteLine();

                // In .wtg files, categories are NOT nested - they're all at the same level
                // Triggers simply belong to the most recently encountered category
                string? currentCategoryName = null;

                foreach (var item in triggers.TriggerItems)
                {
                    if (item is War3Net.Build.Script.TriggerCategoryDefinition category)
                    {
                        // Show the category at root level
                        var commentMarker = category.IsComment ? " [COMMENT]" : string.Empty;
                        var expandedMarker = category.IsExpanded ? "[-]" : "[+]";

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"{expandedMarker} {category.Name}{commentMarker}");
                        Console.ResetColor();

                        if (detailed)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"    Type: Category, ID: {category.Id}");
                            Console.ResetColor();
                        }

                        // Track this as the current category for subsequent triggers
                        currentCategoryName = category.Name;
                    }
                    else if (item is War3Net.Build.Script.TriggerDefinition trigger)
                    {
                        // Show triggers indented under their category
                        var indent = "  "; // Simple 2-space indent under category
                        var enabledMarker = trigger.IsEnabled ? "" : " [DISABLED]";
                        var commentMarker = trigger.IsComment ? " [COMMENT]" : string.Empty;
                        var initMarker = trigger.RunOnMapInit ? " [INIT]" : string.Empty;

                        Console.ForegroundColor = trigger.IsEnabled ? ConsoleColor.Green : ConsoleColor.DarkGray;
                        Console.WriteLine($"{indent}• {trigger.Name}{enabledMarker}{commentMarker}{initMarker}");
                        Console.ResetColor();

                        if (detailed)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            if (!string.IsNullOrEmpty(trigger.Description))
                            {
                                Console.WriteLine($"{indent}    Description: {trigger.Description}");
                            }

                            if (trigger.Functions != null && trigger.Functions.Any())
                            {
                                var events = trigger.Functions.Count(f => f.Type == War3Net.Build.Script.TriggerFunctionType.Event);
                                var conditions = trigger.Functions.Count(f => f.Type == War3Net.Build.Script.TriggerFunctionType.Condition);
                                var actions = trigger.Functions.Count(f => f.Type == War3Net.Build.Script.TriggerFunctionType.Action);

                                Console.WriteLine($"{indent}    Functions: {events} events, {conditions} conditions, {actions} actions");
                            }

                            Console.ResetColor();
                        }
                    }
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Successfully read trigger information.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                if (detailed)
                {
                    Console.WriteLine();
                    Console.WriteLine("Stack trace:");
                    Console.WriteLine(ex.StackTrace);
                }

                Console.ResetColor();
            }
        }
    }
}
