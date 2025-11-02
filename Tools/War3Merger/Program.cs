// ------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

using War3Net.Tools.TriggerMerger.Commands;

namespace War3Net.Tools.TriggerMerger
{
    /// <summary>
    /// TriggerMerger - CLI tool for managing and copying trigger categories between Warcraft 3 maps.
    /// Supports v1.27 and all other Warcraft 3 versions.
    /// </summary>
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("TriggerMerger - Manage and copy trigger categories between Warcraft 3 maps")
            {
                CreateListCommand(),
                CreateCopyCategoryCommand(),
                CreateDeepValidateWtgCommand(),
            };

            return await rootCommand.InvokeAsync(args);
        }

        private static Command CreateListCommand()
        {
            var command = new Command("list", "List all trigger categories and triggers in a map");

            var mapOption = new Option<FileInfo>(
                aliases: new[] { "--map", "-m" },
                description: "Path to the Warcraft 3 map file (.w3x or .w3m)")
            {
                IsRequired = true,
            };
            mapOption.AddValidator(result =>
            {
                var file = result.GetValueForOption(mapOption);
                if (file != null && !file.Exists)
                {
                    result.ErrorMessage = $"Map file not found: {file.FullName}";
                }
            });

            var detailOption = new Option<bool>(
                aliases: new[] { "--detailed", "-d" },
                description: "Show detailed information including trigger contents",
                getDefaultValue: () => false);

            command.AddOption(mapOption);
            command.AddOption(detailOption);

            command.SetHandler(async (FileInfo map, bool detailed) =>
            {
                await ListCommand.ExecuteAsync(map, detailed);
            }, mapOption, detailOption);

            return command;
        }

        private static Command CreateCopyCategoryCommand()
        {
            var command = new Command("copy-category", "Copy a trigger category (folder) with all its triggers from one map to another");

            var sourceOption = new Option<FileInfo>(
                aliases: new[] { "--source", "-s" },
                description: "Path to the source Warcraft 3 map file (.w3x or .w3m)")
            {
                IsRequired = true,
            };
            sourceOption.AddValidator(result =>
            {
                var file = result.GetValueForOption(sourceOption);
                if (file != null && !file.Exists)
                {
                    result.ErrorMessage = $"Source map file not found: {file.FullName}";
                }
            });

            var targetOption = new Option<FileInfo>(
                aliases: new[] { "--target", "-t" },
                description: "Path to the target Warcraft 3 map file (.w3x or .w3m)")
            {
                IsRequired = true,
            };
            targetOption.AddValidator(result =>
            {
                var file = result.GetValueForOption(targetOption);
                if (file != null && !file.Exists)
                {
                    result.ErrorMessage = $"Target map file not found: {file.FullName}";
                }
            });

            var outputOption = new Option<FileInfo>(
                aliases: new[] { "--output", "-o" },
                description: "Path for the output map file (defaults to target path with _merged suffix)");

            var categoryOption = new Option<string>(
                aliases: new[] { "--category", "-c" },
                description: "Name of the trigger category to copy")
            {
                IsRequired = true,
            };

            var categoriesOption = new Option<string[]>(
                aliases: new[] { "--categories" },
                description: "Names of multiple trigger categories to copy (comma-separated)")
            {
                AllowMultipleArgumentsPerToken = true,
            };

            var dryRunOption = new Option<bool>(
                aliases: new[] { "--dry-run" },
                description: "Preview the changes without modifying any files",
                getDefaultValue: () => false);

            var backupOption = new Option<bool>(
                aliases: new[] { "--backup" },
                description: "Create a backup of the target file before modifying",
                getDefaultValue: () => true);

            var overwriteOption = new Option<bool>(
                aliases: new[] { "--overwrite" },
                description: "If a category with the same name exists in target, overwrite it",
                getDefaultValue: () => false);

            command.AddOption(sourceOption);
            command.AddOption(targetOption);
            command.AddOption(outputOption);
            command.AddOption(categoryOption);
            command.AddOption(categoriesOption);
            command.AddOption(dryRunOption);
            command.AddOption(backupOption);
            command.AddOption(overwriteOption);

            command.SetHandler(async (FileInfo source, FileInfo target, FileInfo? output, string category, string[]? categories, bool dryRun, bool backup, bool overwrite) =>
            {
                await CopyCategoryCommand.ExecuteAsync(source, target, output, category, categories, dryRun, backup, overwrite);
            }, sourceOption, targetOption, outputOption, categoryOption, categoriesOption, dryRunOption, backupOption, overwriteOption);

            return command;
        }

        private static Command CreateDeepValidateWtgCommand()
        {
            var command = new Command("deep-validate-wtg", "Perform deep validation of trigger file structure");

            var fileOption = new Option<FileInfo>(
                aliases: new[] { "--file", "-f" },
                description: "Path to the WTG file or map file to validate (.wtg, .w3x, or .w3m)")
            {
                IsRequired = true,
            };
            fileOption.AddValidator(result =>
            {
                var file = result.GetValueForOption(fileOption);
                if (file != null && !file.Exists)
                {
                    result.ErrorMessage = $"File not found: {file.FullName}";
                }
            });

            command.AddOption(fileOption);

            command.SetHandler(async (FileInfo file) =>
            {
                await DeepValidateWtgCommand.ExecuteAsync(file);
            }, fileOption);

            return command;
        }
    }
}
